using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using NordicRccDemoApp.Enums;

namespace NordicRccDemoApp.Dtos;

public record CreateTimeSeriesDto(
    [property: JsonRequired] [EnumDataType(typeof(BiddingZone))] BiddingZone BiddingZone,
    [property: JsonRequired] [EnumDataType(typeof(Resolution))] Resolution Resolution);
