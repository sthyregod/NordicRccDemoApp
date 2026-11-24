using NordicRccDemoApp.Enums;
using NordicRccDemoApp.Models;
using NpgsqlTypes;

namespace NordicRccDemoAppTest.Domain;

public class TimeSeriesTests
{
    private static TimeSeries QuarterHourSeries() => new()
    {
        BiddingZone = BiddingZone.Dk1,
        Resolution = Resolution.QuarterHour
    };

    private static DateTimeOffset BaseTime() => new(2026, 11, 26, 12, 0, 0, TimeSpan.Zero);

    [Test]
    public void IsValidPeriod_WithCorrectQuarterHourPeruid_ReturnsTrue()
    {
        var series = QuarterHourSeries();
        var period = new NpgsqlRange<DateTimeOffset>(BaseTime(), BaseTime().AddMinutes(15));

        Assert.IsTrue(series.IsValidPeriod(period));
    }

    [Test]
    public void IsValidPeriod_WithWrongDuration_ReturnsFalse()
    {
        var series = QuarterHourSeries();
        var period = new NpgsqlRange<DateTimeOffset>(BaseTime(), BaseTime().AddMinutes(16));

        Assert.IsFalse(series.IsValidPeriod(period));
    }

    [Test]
    public void IsValidPeriod_WithMisalignedStart_ReturnsFalse()
    {
        var series = QuarterHourSeries();
        var start = BaseTime().AddMinutes(5);
        var period = new NpgsqlRange<DateTimeOffset>(start, start.AddMinutes(16));

        Assert.IsFalse(series.IsValidPeriod(period));
    }

    [Test]
    public void IsValidPeriod_WithSubSecondPrecision_ReturnsFalse()
    {
        var series = QuarterHourSeries();
        var start = BaseTime().AddMilliseconds(500);
        var period = new NpgsqlRange<DateTimeOffset>(start, start.AddMinutes(15));

        Assert.IsFalse(series.IsValidPeriod(period));
    }

    [TestCase(0, true)]
    [TestCase(15, true)]
    [TestCase(30, true)]
    [TestCase(45, true)]
    [TestCase(5, false)]
    [TestCase(10, false)]
    [TestCase(20, false)]
    public void IsValidPeriod_WithMisalignedEnd_ReturnsFalse(int minute, bool expected)
    {
        var series = QuarterHourSeries();
        var start = BaseTime().AddMinutes(minute);
        var period = new NpgsqlRange<DateTimeOffset>(start, start.AddMinutes(15));

        Assert.That(expected, Is.EqualTo(series.IsValidPeriod(period)));
    }
}