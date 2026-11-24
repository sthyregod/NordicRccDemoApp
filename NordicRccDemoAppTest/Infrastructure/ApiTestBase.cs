using System.Text.Json;
using DotNet.Testcontainers.Builders;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NordicRccDemoApp.Data;
using Testcontainers.PostgreSql;

namespace NordicRccDemoAppTest.Infrastructure;

public abstract class ApiTestBase : WebApplicationFactory<Program>
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16")
        .WithDatabase("NordicRccDemoAppTest")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .WithWaitStrategy(Wait.ForUnixContainer()
            .UntilMessageIsLogged("database system is ready to accept connections"))
        .Build();

    protected HttpClient Client = null!;
    protected JsonSerializerOptions JsonOptions = null!; 

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor is not null) services.Remove(descriptor);
            
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(_container.GetConnectionString()));
        });
    }

    [OneTimeSetUp]
    public async Task Setup()
    {
        await _container.StartAsync();
        Client = CreateClient();
        
        JsonOptions = Services
            .GetRequiredService<IOptions<Microsoft.AspNetCore.Mvc.JsonOptions>>()
            .Value.JsonSerializerOptions;
        
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }
    
    [OneTimeTearDown]
    public async Task TearDown()
    {
        await _container.DisposeAsync();
        await DisposeAsync();
    }
}