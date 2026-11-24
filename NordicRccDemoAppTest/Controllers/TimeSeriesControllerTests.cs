using System.Net;
using NordicRccDemoApp.Dtos;
using NordicRccDemoApp.Enums;
using NordicRccDemoAppTest.Infrastructure;

namespace NordicRccDemoAppTest.Controllers;

public class TimeSeriesControllerTests : ApiTestBase
{
    [Test]
    public async Task Upsert_WithInvalidPeriod_Returns400()
    {
        var create = await Client.PostAsJsonAsync("/api/timeseries",
            new { BiddingZone = BiddingZone.Dk2, Resolution = Resolution.QuarterHour });
        var created = await create.Content.ReadFromJsonAsync<TimeSeriesResponse>(JsonOptions);

        var response = await Client.PutAsJsonAsync($"/api/timeseries/{created!.Id}",
            new
            {
                StartTime = "2026-11-26T12:00:00+00:00",
                EndTime = "2026-11-26T12:16:00+00:00",
                Energy = new { Value = 42.5m, Unit = EnergyUnit.MWh }
            });
        
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }
}