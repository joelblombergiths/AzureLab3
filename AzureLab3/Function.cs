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
#pragma warning disable IDE0060

namespace AzureLab3;

public class Function
{
    private readonly MongoDBContext _context;

    /*
     * Depenency Injection for connection to MongoDB database
     */
    public Function(MongoDBContext context)
    {
        _context = context;
    }

    /*
     * Endpoint: https://{base_url}/api/recipes
     * Method: GET
     * Parameters: None
     * Body: None
     * Returns: A list of all recipes and their ingredients, or No Content if there are no recipes in the database
     */
    [FunctionName("GetAllRecipes")]
    public async Task<IActionResult> GetAllRecipes([HttpTrigger(AuthorizationLevel.Function,
        "get", Route = "recipes")] HttpRequest req, ILogger log)
    {
        try
        {
            log.LogInformation("Fetching all recipes");

            List<RecipeModel> recipes = await _context.CookBook.Find(Builders<RecipeModel>.Filter.Empty).ToListAsync();

            return recipes.Any() ? new OkObjectResult(recipes) : new NoContentResult();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return new BadRequestErrorMessageResult(ex.Message);
        }
    }

    /*
     * Endpoint: https://{base_url}/api/recipes/{id}
     * Method: GET
     * Parameters:
     *      "id": id of one recipe
     * Body: None
     * Returns: The recipe with id {id} if it exists, otherwise Not Found
     */
    [FunctionName("GetRecipeById")]
    public async Task<IActionResult> GetRecipeById([HttpTrigger(AuthorizationLevel.Function,
        "get", Route = "recipes/{id}")] HttpRequest req, ILogger log, string id)
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

    /*
     * Endpoint: https://{base_url}/api/recipes
     * Method: POST
     * Parameters: None
     * Body:
     * {
     *      Name:"Name of dish"
     * }
     * Returns: The newly created recipe
     */
    [FunctionName("CreateRecipe")]
    public async Task<IActionResult> NewRecipe([HttpTrigger(AuthorizationLevel.Function,
            "post", Route = "recipes")] HttpRequest req, ILogger log)
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

    /*
    * Endpoint: https://{base_url}/api/recipes/{id}
    * Method: PUT
    * Parameters:
    *      "id": The id of a recipe to update
    * Body:
    * {
    *      Name:"New name of the dish"
    * }
    * Returns: The updated recipe, or Not Found if the id did not exist in the database
    */
    [FunctionName("UpdateRecipe")]
    public async Task<IActionResult> UpdateRecipe([HttpTrigger(AuthorizationLevel.Function,
        "put", Route = "recipes/{id}")] HttpRequest req, ILogger log, string id)
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

            return (res.IsAcknowledged && res.ModifiedCount > 0) ? new OkObjectResult(recipe) : throw new("Unexpected Error");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return new BadRequestErrorMessageResult(ex.Message);
        }
    }

    /*
     * Endpoint: https://{base_url}/api/recipes/{id}
     * Method: DELETE
     * Parameters:
     *      "id": The id of one recipe to delete
     * Body: None
     * Returns: OK if the recipe was successfully deleted, or Not Found if it was... not found
     */
    [FunctionName("DeleteRecipe")]
    public async Task<IActionResult> DeleteRecipe([HttpTrigger(AuthorizationLevel.Function,
        "delete", Route = "recipes/{id}")] HttpRequest req, ILogger log, string id)
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

    /*
     * Endpoint: https://{base_url}/api/recipes/{id}/ingredients
     * Method: POST
     * Parameters:
     *      "id": The id of a recipe
     * Body:
     * [
     *    {
     *        Name:"Name of an ingredient",
     *        Quantity: {int} //number of this ingredient
     *        Unit: "{UnitOfMeasure}" //one of the available units eg. "g,kg,ml,dl..."
     *    },
     *    {
     *        Name:"Name of another ingredient",
     *        Quantity: {int} //number of that ingredient
     * *      Unit: "{UnitOfMeasure}" //one of the available units eg. "g,kg,ml,dl..."
     *    },
     *    ...
     * ]
     * Returns: The recipe with added ingredients or Not Found if a recipe with that id was not found
     */    
    [FunctionName("AddIngredients")]
    public async Task<IActionResult> AddIngredients([HttpTrigger(AuthorizationLevel.Function,
        "post", Route = "recipes/{id}/ingredients")] HttpRequest req, ILogger log, string id)
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
                    Quantity = i.Quantity,
                    Unit = UnitOfMeasure.Validate(i.Unit) ? i.Unit : throw new($"Unknown unit, available units ({string.Join(",", UnitOfMeasure.Units.ToArray())})")
                });
                else ingredient.Quantity += i.Quantity;
            });

            recipe.Done = false;

            ReplaceOneResult res = await _context.CookBook.ReplaceOneAsync(r => r.Id == id, recipe);

            return (res.IsAcknowledged && res.ModifiedCount > 0) ? new OkObjectResult(recipe) : throw new("Unexpected Error");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return new BadRequestErrorMessageResult(ex.Message);
        }
    }

    /*
     * Endpoint: https://{base_url}/api/recipes/{recipeId}/ingredients/{ingredientId}
     * Method: PATCH
     * Parameters:
     *      "receipeId": The id of a recipe
     *      "ingredientId": The id of an ingredient in this recipe
     * Body:
     * {
     *      Added:{bool} //true if ingredient is added, false to reset
     * }
     * Returns: The recipe with updated ingredients (and if all ingredients added, sets the Done property of the Recipe to true)
     *  or an Error Message if recipe or ingredient not found. 
     */
    [FunctionName("UpdateIngredient")]
    public async Task<IActionResult> UpdateIngredient([HttpTrigger(AuthorizationLevel.Function,
        "patch", Route = "recipes/{recipeId}/ingredients/{ingredientId}")] HttpRequest req, ILogger log, string recipeId, string ingredientId)
    {
        try
        {
            log.LogInformation("Updating ingredient");

            RecipeModel recipe = await _context.CookBook.Find(r => r.Id == recipeId).FirstOrDefaultAsync();
            if (recipe == null) throw new("Recipe not found");

            IngredientModel ingredient = recipe.Ingredients.FirstOrDefault(i => i.Id == ingredientId);
            if (ingredient == null) throw new("Ingredient not found");

            string reqData = await new StreamReader(req.Body).ReadToEndAsync();
            UpdateIngredient updateIngredient = JsonConvert.DeserializeObject<UpdateIngredient>(reqData);

            ingredient.Added = updateIngredient.Added;
            recipe.Done = recipe.Ingredients.All(i => i.Added);

            ReplaceOneResult res = await _context.CookBook.ReplaceOneAsync(r => r.Id == recipeId, recipe);

            return (res.IsAcknowledged && res.ModifiedCount > 0) ? new OkObjectResult(recipe) : throw new("Unexpected Error");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return new BadRequestErrorMessageResult(ex.Message);
        }
    }
}
