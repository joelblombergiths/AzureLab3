using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AzureLab3.Models;

public class RecipeModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public List<IngredientModel> Ingredients { get; set; } = new();
    public bool Done { get; set; } = false;
}