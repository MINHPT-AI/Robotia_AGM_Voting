using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mms.Application.Common.Behaviours;
using Mms.Application.Companies.Queries;
using Mms.Infrastructure;
using Mms.Infrastructure.Handlers.Companies;
using Mms.Infrastructure.Identity;
using Mms.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace Mms.IntegrationTests.Fixtures;

public class DatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("mms_test")
        .WithUsername("mms_test")
        .WithPassword("test_password")
        .Build();

    public MmsDbContext DbContext { get; private set; } = null!;
    public IServiceProvider Services { get; private set; } = null!;

    /// <summary>
    /// Creates a new scoped DbContext — use for read-after-write verification 
    /// to avoid EF Core cache returning stale data.
    /// </summary>
    public MmsDbContext CreateFreshDbContext()
    {
        var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<MmsDbContext>();
    }

    /// <summary>
    /// Creates a scoped ISender (MediatR) for sending commands/queries.
    /// </summary>
    public ISender CreateMediator()
    {
        var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<ISender>();
    }

    public async Task InitializeAsync()
    {
        // Match Program.cs — allow DateTime.Kind=Unspecified for Npgsql
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        await _container.StartAsync();

        var services = new ServiceCollection();

        // Need logging for Identity
        services.AddLogging();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = _container.GetConnectionString(),
                ["Jwt:Issuer"] = "test-issuer",
                ["Jwt:Audience"] = "test-audience",
                ["JWT_SECRET"] = "test-secret-minimum-32-characters-long-enough",
            })
            .Build();

        services.AddSingleton<IConfiguration>(config);
        services.AddInfrastructure(config);

        // MediatR — mirrors Program.cs registration
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(GetCompanyQuery).Assembly);       // Application
            cfg.RegisterServicesFromAssembly(typeof(GetCompanyHandler).Assembly);     // Infrastructure (handlers)
        });

        // FluentValidation
        services.AddValidatorsFromAssembly(typeof(GetCompanyQuery).Assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));

        Services = services.BuildServiceProvider();

        DbContext = Services.GetRequiredService<MmsDbContext>();
        await DbContext.Database.MigrateAsync();
        await SeedData.EnsureSeededAsync(Services);
    }

    public async Task DisposeAsync()
    {
        await DbContext.DisposeAsync();
        await _container.DisposeAsync();
    }
}
