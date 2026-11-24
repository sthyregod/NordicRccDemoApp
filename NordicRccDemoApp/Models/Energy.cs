using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using NordicRccDemoApp.Enums;

namespace NordicRccDemoApp.Models;

[Owned]
public class Energy
{
    [Column("value")]
    public decimal Value { get; private set; }

    [Column("unit")]
    [JsonInclude]
    [JsonRequired]
    [EnumDataType(typeof(EnergyUnit))]
    public EnergyUnit Unit { get; private set; }
    
    public Energy(decimal value, EnergyUnit unit)
    {
        Value = value;
        Unit = unit;
    }
    
    public override string ToString()
    {
        return $"{Value} {Unit}";
    }

    public static Energy operator +(Energy a, Energy b)
    {
        var inWh = a.InBaseUnit() + b.InBaseUnit();
        return new Energy(inWh / a.WhFactor(), a.Unit);
    }

    public static Energy operator -(Energy a, Energy b)
    {
        var inWh = a.InBaseUnit() - b.InBaseUnit();
        return new Energy(inWh / a.WhFactor(), a.Unit);
    }

    public static Energy operator *(Energy a, decimal factor) => new (a.Value * factor, a.Unit);

    public bool Equals(Energy? other) => other is not null && InBaseUnit() == other.InBaseUnit();
    
    public override bool Equals(object? obj) => obj is Energy energy && Equals(energy);
    
    public override int GetHashCode() => InBaseUnit().GetHashCode();

    private decimal InBaseUnit()
    {
        return Value * WhFactor();
    }

    private decimal WhFactor() => Unit switch
    {
        EnergyUnit.Wh => 1m,
        EnergyUnit.KWh => 1_000m,
        EnergyUnit.MWh => 1_000_000m,
        EnergyUnit.GWh => 1_000_000_000m,
        _ => throw new ArgumentOutOfRangeException(nameof(Unit))
    };
}