using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Proyectoandtask.Infrastructure.Context;

namespace Proyectandtask.Api.IntegrationTests;

[CollectionDefinition("API PostgreSQL integration tests", DisableParallelization = true)]
public sealed class ApiPostgreSqlCollection : ICollectionFixture<ApiPostgreSqlFixture>
{
    public const string Name = "API PostgreSQL integration tests";
}

public sealed class ApiPostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("proyectandtask_api_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private readonly Dictionary<string, string?> _previousEnvironmentValues = new();

    public ApiWebApplicationFactory Factory { get; private set; } = null!;

    public ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_database.GetConnectionString())
            .Options;
        return new ApplicationDbContext(options);
    }

    public async Task InitializeAsync()
    {
        await _database.StartAsync();

        SetEnvironmentValue("ConnectionStrings__DefaultConnection", _database.GetConnectionString());
        SetEnvironmentValue("Jwt__Issuer", "Proyectandtask.Api.IntegrationTests");
        SetEnvironmentValue("Jwt__Audience", "Proyectandtask.Api.IntegrationTests.Client");
        SetEnvironmentValue("Jwt__SecretKey", "Integration_Test_Jwt_Key_At_Least_32_Bytes_123!");
        SetEnvironmentValue("Jwt__AccessTokenMinutes", "15");

        // Aplicar migraciones antes de arrancar la API: al iniciar, Program ejecuta DemoDataSeeder.
        await using (var context = CreateContext())
        {
            await context.Database.MigrateAsync();
        }

        Factory = new ApiWebApplicationFactory();
        using var client = Factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await _database.DisposeAsync();

        foreach (var (key, value) in _previousEnvironmentValues)
        {
            Environment.SetEnvironmentVariable(key, value);
        }
    }

    private void SetEnvironmentValue(string key, string value)
    {
        _previousEnvironmentValues[key] = Environment.GetEnvironmentVariable(key);
        Environment.SetEnvironmentVariable(key, value);
    }
}

public sealed class ApiWebApplicationFactory : WebApplicationFactory<global::Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
    }
}
