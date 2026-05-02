using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Common.Behaviours;
using Mms.Application.Companies.Queries;
using Mms.Infrastructure;
using Mms.Infrastructure.Handlers.Companies;
using Mms.Infrastructure.Logging;
using Mms.Infrastructure.Persistence;
using Mms.Web.Components;
using Mms.Infrastructure.Hubs;
using MudBlazor.Services;
using Serilog;

// ── Bootstrap Serilog early (before builder) ──
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var connectionString = configuration.GetConnectionString("Default")!;
SerilogConfiguration.Configure(configuration, connectionString);

try
{
    Log.Information("Starting Mms.Web");
    AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    // Infrastructure (EF Core, Identity, JWT)
    builder.Services.AddInfrastructure(builder.Configuration);

    // DataProtection — persist keys so antiforgery tokens survive container restarts
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo("/app/keys"))
        .SetApplicationName("Mms");

    // MediatR — scan Application (commands/queries) and Infrastructure (handlers)
    builder.Services.AddMediatR(cfg =>
    {
        cfg.RegisterServicesFromAssembly(typeof(GetCompanyQuery).Assembly);
        cfg.RegisterServicesFromAssembly(typeof(GetCompanyHandler).Assembly);
    });

    // FluentValidation — scan Application assembly for all validators
    builder.Services.AddValidatorsFromAssembly(typeof(GetCompanyQuery).Assembly);

    // ValidationBehaviour pipeline — runs validators before each handler
    builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));

    // HttpClient — used by ImageUploader.razor to POST files to /api/uploads/image
    builder.Services.AddScoped(sp =>
    {
        var config = sp.GetRequiredService<IConfiguration>();
        var baseUrl = config["AppBaseUrl"] ?? "http://localhost:8080/";
        return new HttpClient { BaseAddress = new Uri(baseUrl) };
    });

    // Cookie auth config (Identity default scheme)
    builder.Services.ConfigureApplicationCookie(opts =>
    {
        opts.LoginPath = "/login";
        opts.LogoutPath = "/account/logout";
        opts.AccessDeniedPath = "/login";
        opts.ExpireTimeSpan = TimeSpan.FromHours(8);
        opts.SlidingExpiration = true;
        opts.Cookie.HttpOnly = true;
        opts.Cookie.SameSite = SameSiteMode.Strict;
    });

    // Required for auth state in Blazor components (.NET 8)
    builder.Services.AddCascadingAuthenticationState();
    builder.Services.AddHttpContextAccessor();

    // Blazor Server + MudBlazor
    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();
    builder.Services.AddMudServices();

    // API Controllers (auth endpoints)
    builder.Services.AddControllers();

    // SignalR for real-time check-in updates
    builder.Services.AddSignalR();

    var app = builder.Build();

    // ── Auto-migrate on startup ──
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<MmsDbContext>();
        db.Database.Migrate();
    }

    // ── Seed data (4 roles + admin user) ──
    await SeedData.EnsureSeededAsync(app.Services);

    // ── Middleware pipeline ──
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
    }

    app.UseStaticFiles();

    app.UseRouting();

    app.Use(async (ctx, next) =>
    {
        ctx.Response.Headers["X-Frame-Options"] = "DENY";
        ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
        await next();
    });

    app.UseSerilogRequestLogging(opts =>
        opts.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000}ms");

    app.UseAuthentication();
    app.UseAuthorization();
    app.UseAntiforgery();

    app.MapControllers();
    app.MapHub<CheckinHub>("/hubs/checkin");
    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Required for WebApplicationFactory in integration tests
public partial class Program { }
