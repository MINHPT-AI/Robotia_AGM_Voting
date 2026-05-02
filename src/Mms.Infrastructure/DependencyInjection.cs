using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Mms.Application.Common.Interfaces;
using Mms.Application.Interfaces;
using Mms.Infrastructure.Identity;
using Mms.Infrastructure.Persistence;
using Mms.Infrastructure.Services;

namespace Mms.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        // ── DbContext ──
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured");

        services.AddDbContext<MmsDbContext>(opts =>
            opts.UseNpgsql(connectionString));

        // DbContextFactory needed by AuditLogService (separate transaction)
        services.AddDbContextFactory<MmsDbContext>(opts =>
            opts.UseNpgsql(connectionString), ServiceLifetime.Scoped);

        // ── ASP.NET Core Identity ──
        services.AddIdentity<ApplicationUser, ApplicationRole>(opts =>
        {
            opts.Password.RequiredLength = 8;
            opts.Password.RequireUppercase = true;
            opts.Password.RequireDigit = true;
            opts.Password.RequireNonAlphanumeric = false;
            opts.Lockout.MaxFailedAccessAttempts = 5;
            opts.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        })
        .AddEntityFrameworkStores<MmsDbContext>()
        .AddDefaultTokenProviders();

        // BCrypt hasher — MUST come AFTER AddIdentity to override default PBKDF2
        services.AddScoped<IPasswordHasher<ApplicationUser>, BcryptPasswordHasher>();

        // ── JWT Authentication ──
        var jwtSecret = configuration["JWT_SECRET"]
            ?? "dev-only-secret-min-32-chars-replace-in-prod!!";

        // Identity sets Cookie as default; JWT added as secondary scheme for API
        services.AddAuthentication()
            .AddJwtBearer(opts =>
            {
                opts.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                    ValidateIssuer = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = configuration["Jwt:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                };
            });

        services.AddAuthorization();

        // ── Services ──
        services.AddScoped<JwtTokenService>();
        services.AddScoped<IAuditLogService, AuditLogService>();

        // ── Phase 06A: Letter generation services ──
        services.AddSingleton<IBarQrCodeGenerator, Documents.BarQrCodeGenerator>();
        services.AddTransient<ILetterDocxBuilder, Documents.LetterDocxBuilder>();
        services.AddTransient<ILibreOfficePdfConverter, Documents.LibreOfficePdfConverter>();
        services.AddTransient<Parsing.CpnRowMatcher>();

        // ── Phase 07: Template management services ──
        services.AddTransient<ITemplateFileService, Documents.TemplateFileService>();

        return services;
    }
}
