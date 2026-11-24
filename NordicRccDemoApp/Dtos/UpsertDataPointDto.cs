using NordicRccDemoApp.Models;

namespace NordicRccDemoApp.Dtos;

public record UpsertDataPointDto(DateTimeOffset StartTime, DateTimeOffset EndTime, Energy Energy);