using System.Net;
using System.Text;
using NordicRccDemoAppTest.Infrastructure;

namespace NordicRccDemoAppTest.Controllers;

public class EnumValidationTests : ApiTestBase
{
    [TestCase("{}")]
    [TestCase("{\"biddingZone\":\"Dk1\"}")]
    [TestCase("{\"resolution\":\"QuarterHour\"}")]
    [TestCase("{\"biddingZone\":999,\"resolution\":\"QuarterHour\"}")]
    [TestCase("{\"biddingZone\":\"Dk1\",\"resolution\":999}")]
    [TestCase("{\"biddingZone\":\"unknown\",\"resolution\":\"QuarterHour\"}")]
    public async Task Create_WithMissingOrInvalidEnum_ReturnsBadRequest(string json)
    {
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await Client.PostAsync("/api/timeseries", content);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [TestCase("{}")]
    [TestCase("{\"value\":42,\"unit\":999}")]
    [TestCase("{\"value\":42,\"unit\":\"unknown\"}")]
    public async Task Upsert_WithMissingOrInvalidEnergyUnit_ReturnsBadRequest(string energy)
    {
        var json = $$"""{"startTime":"2026-11-26T12:00:00Z","endTime":"2026-11-26T12:15:00Z","energy":{{energy}}}""";
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await Client.PutAsync($"/api/timeseries/{Guid.NewGuid()}", content);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [TestCase("999")]
    [TestCase("unknown")]
    public async Task List_WithInvalidBiddingZone_ReturnsBadRequest(string zone)
    {
        var response = await Client.GetAsync($"/api/timeseries?biddingZones={zone}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }
}