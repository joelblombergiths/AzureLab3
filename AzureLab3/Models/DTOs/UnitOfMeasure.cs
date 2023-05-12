using System;
using System.Linq;

namespace AzureLab3.Models.DTOs;

public static class UnitOfMeasure
{
    private static readonly string[] _units = {
        "g",
        "kg",
        "ml",
        "dl",
        "teaspoon",
        "tablespoon",
        "container",
        "ea"
    };

    public static ReadOnlySpan<string> Units => _units;

    public static bool Validate(string unit) => _units.Contains(unit, StringComparer.InvariantCultureIgnoreCase);
}