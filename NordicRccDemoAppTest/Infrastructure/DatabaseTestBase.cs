using DotNet.Testcontainers.Builders;
using Microsoft.EntityFrameworkCore;
using NordicRccDemoApp.Data;
using Testcontainers.PostgreSql;

namespace NordicRccDemoAppTest.Infrastructure;

public abstract class DatabaseTestBase
{
    protected PostgreSqlContainer Container { get; private set; } = null!;

    protected AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(Container.GetConnectionString())
            .Options;

        return new AppDbContext(options);
    }
    
    [OneTimeSetUp]
    public async Task Setup()
    {
        Container = new PostgreSqlBuilder("postgres:16")
            .WithDatabase("NordicRccDemoAppTest")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilMessageIsLogged("database system is ready to accept connections"))
            .Build();

        await Container.StartAsync();
        
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(Container.GetConnectionString())
            .Options;
        
        await using var db = new AppDbContext(options);
        await db.Database.MigrateAsync();
    }
    
    [OneTimeTearDown]
    public async Task TearDown()
    {
        await Container.DisposeAsync();
    }
}