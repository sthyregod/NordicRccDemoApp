using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using NordicRccDemoApp.Data;
using NordicRccDemoApp.Service;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddScoped<TimeSeriesService>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddOpenApi(options =>
{
    options.AddSchemaTransformer((schema, context, _) =>
    {
        var clr = context.JsonTypeInfo.Type;
        clr = Nullable.GetUnderlyingType(clr) ?? clr;

        if (clr.IsEnum && schema.Type == JsonSchemaType.Integer)
        {
            schema.Type = JsonSchemaType.String;
            schema.Enum = clr.GetEnumNames()
                .Select(n => JsonSerializer.SerializeToNode(n)!)
                .ToList();
        }

        return Task.CompletedTask;
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await DbSeeder.SeedIfEmpty(db);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapControllers();

app.Run();
