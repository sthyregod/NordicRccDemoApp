using NordicRccDemoApp.Enums;
using NordicRccDemoApp.Models;

namespace NordicRccDemoAppTest.Domain;

public class EnergyTests
{
    [Test]
    public void Add_DifferentUnits_ConvertsCorrectly()
    {
        var a = new Energy(100, EnergyUnit.MWh);
        var b = new Energy(500, EnergyUnit.KWh);
        
        var result = a + b;
        
        Assert.AreEqual(100.5m, result.Value);
        Assert.AreEqual(EnergyUnit.MWh, result.Unit);
    }
    
    [Test]
    public void Subtract_DifferentUnits_ConvertsCorrectly()
    {
        var a = new Energy(100, EnergyUnit.MWh);
        var b = new Energy(500, EnergyUnit.KWh);

        var result = a - b;

        Assert.Multiple(() =>
        {
            Assert.That(result.Value, Is.EqualTo(99.5m));
            Assert.That(result.Unit, Is.EqualTo(EnergyUnit.MWh));
        });
    }
    
    [Test]
    public void Multiply_ConvertsCorrectly()
    {
        var a = new Energy(100, EnergyUnit.MWh);

        var result = a * 5;
        
        Assert.Multiple(() =>
        {
            Assert.That(result.Value, Is.EqualTo(500m));
            Assert.That(result.Unit, Is.EqualTo(EnergyUnit.MWh));
        });
    }
}