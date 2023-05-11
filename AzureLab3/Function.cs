using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using AzureLab3.Data;
using AzureLab3.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Newtonsoft.Json;
using AzureLab3.Models.DTOs;

namespace AzureLab3;

public class Function
{
    private readonly MongoDBContext _context;

    public Function(MongoDBContext context)
    {
        _context = context;
    }

    [FunctionName("GetAllRecipes")]
    public async Task<IActionResult> GetAllRecipes([HttpTrigger(AuthorizationLevel.Function, "get", Route = "recipes")]
        HttpRequest req, ILogger log)
    {
        try
        {
            log.LogInformation("Fetching all recipes");

            List<RecipeModel> recipes = await _context.CookBook.Find(Builders<RecipeModel>.Filter.Empty).ToListAsync();

            return new OkObjectResult(recipes);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return new BadRequestErrorMessageResult(ex.Message);
        }
    }

    [FunctionName("GetRecipeById")]
    public async Task<IActionResult> GetRecipeById([HttpTrigger(AuthorizationLevel.Function, "get", Route = "recipes/{id}")]
        HttpRequest req, ILogger log, string id)
    {
        try
        {
            log.LogInformation("Fetching specific recipe");

            RecipeModel recipe = await _context.CookBook.Find(r => r.Id == id).FirstOrDefaultAsync();

            return recipe == null ? new NotFoundResult() : new OkObjectResult(recipe);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return new BadRequestErrorMessageResult(ex.Message);
        }
    }

    [FunctionName("CreateRecipe")]
    public async Task<IActionResult> NewRecipe([HttpTrigger(AuthorizationLevel.Function, "post", Route = "recipes")]
        HttpRequest req, ILogger log)
    {
        try
        {
            log.LogInformation("Creating recipe");

            string reqData = await new StreamReader(req.Body).ReadToEndAsync();
            CreateRecipe newRecipe = JsonConvert.DeserializeObject<CreateRecipe>(reqData);
            RecipeModel recipe = new()
            {
                Name = newRecipe.Name
            };

            await _context.CookBook.InsertOneAsync(recipe);

            return new OkObjectResult(recipe);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return new BadRequestErrorMessageResult(ex.Message);
        }
    }

    [FunctionName("UpdateRecipe")]
    public async Task<IActionResult> UpdateRecipe([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "recipes/{id}")]
        HttpRequest req, ILogger log, string id)
    {
        try
        {
            log.LogInformation("Updating recipe");

            RecipeModel recipe = await _context.CookBook.Find(r => r.Id == id).FirstOrDefaultAsync();
            if (recipe == null) return new NotFoundResult();

            string reqData = await new StreamReader(req.Body).ReadToEndAsync();
            CreateRecipe updateRecipe = JsonConvert.DeserializeObject<CreateRecipe>(reqData);

            recipe.Name = updateRecipe.Name;

            ReplaceOneResult res = await _context.CookBook.ReplaceOneAsync(r => r.Id == id, recipe);

            return (res.IsAcknowledged && res.ModifiedCount > 0) ? new OkObjectResult(recipe) : new NotFoundResult();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return new BadRequestErrorMessageResult(ex.Message);
        }
    }

    [FunctionName("DeleteRecipe")]
    public async Task<IActionResult> DeleteShoppingCartItem([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "recipes/{id}")]
        HttpRequest req, ILogger log, string id)
    {
        try
        {
            log.LogInformation("deleting recipe");

            DeleteResult res = await _context.CookBook.DeleteOneAsync(r => r.Id == id);

            return (res.IsAcknowledged && res.DeletedCount > 0) ? new OkResult() : new NotFoundResult();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return new BadRequestErrorMessageResult(ex.Message);
        }
    }

    [FunctionName("AddIngredients")]
    public async Task<IActionResult> AddIngredients([HttpTrigger(AuthorizationLevel.Function, "post", Route = "recipes/{id}/ingredients")]
        HttpRequest req, ILogger log, string id)
    {
        try
        {
            log.LogInformation("Adding ingredients");

            RecipeModel recipe = await _context.CookBook.Find(r => r.Id == id).FirstOrDefaultAsync();
            if (recipe == null) return new NotFoundResult();

            string reqData = await new StreamReader(req.Body).ReadToEndAsync();
            List<AddIngredient> ingredients = JsonConvert.DeserializeObject<AddIngredient[]>(reqData).ToList();

            ingredients.ForEach(i =>
            {
                IngredientModel ingredient = recipe.Ingredients.FirstOrDefault(x => x.Name.Equals(i.Name, StringComparison.InvariantCultureIgnoreCase));
                if (ingredient == null) recipe.Ingredients.Add(new()
                {
                    Name = i.Name,
                    Quantity = i.Quantity
                });
                else ingredient.Quantity += i.Quantity;
            });

            recipe.Done = false;

            ReplaceOneResult res = await _context.CookBook.ReplaceOneAsync(r => r.Id == id, recipe);

            return (res.IsAcknowledged && res.ModifiedCount > 0) ? new OkObjectResult(recipe) : new NotFoundResult();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return new BadRequestErrorMessageResult(ex.Message);
        }
    }

    [FunctionName("EditIngredient")]
    public async Task<IActionResult> EditIngredients([HttpTrigger(AuthorizationLevel.Function, "patch", Route = "recipes/{recipeId}/ingredients/{ingredientId}")]
        HttpRequest req, ILogger log, string recipeId, string ingredientId)
    {
        try
        {
            log.LogInformation("Adding ingredients");

            RecipeModel recipe = await _context.CookBook.Find(r => r.Id == recipeId).FirstOrDefaultAsync();
            if (recipe == null) return new NotFoundObjectResult("Recipe not found");

            IngredientModel ingredient = recipe.Ingredients.FirstOrDefault(i => i.Id == ingredientId);
            if (ingredient == null) return new NotFoundObjectResult("Ingredient not found");

            string reqData = await new StreamReader(req.Body).ReadToEndAsync();
            UpdateIngredient updateIngredient = JsonConvert.DeserializeObject<UpdateIngredient>(reqData);

            ingredient.Added = updateIngredient.Added;
            recipe.Done = recipe.Ingredients.All(i => i.Added);

            ReplaceOneResult res = await _context.CookBook.ReplaceOneAsync(r => r.Id == recipeId, recipe);

            return (res.IsAcknowledged && res.ModifiedCount > 0) ? new OkObjectResult(recipe) : new NotFoundResult();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return new BadRequestErrorMessageResult(ex.Message);
        }
    }
}
