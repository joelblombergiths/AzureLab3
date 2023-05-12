using System;

namespace AzureLab3.Models;

public class IngredientModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; } = 0;
    public string Unit { get; set; } = string.Empty;
    public bool Added { get; set; } = false;
}