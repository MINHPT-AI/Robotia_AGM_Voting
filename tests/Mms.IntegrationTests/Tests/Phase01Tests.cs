using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mms.Infrastructure.Identity;
using Mms.IntegrationTests.Fixtures;

namespace Mms.IntegrationTests.Tests;

public class Phase01Tests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;

    public Phase01Tests(DatabaseFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Migration_AppliesSuccessfully_AllCoreTablesExist()
    {
        var db = _fixture.DbContext;

        // Query information_schema for table names in public schema
        var tables = new List<string>();
        using var cmd = db.Database.GetDbConnection().CreateCommand();
        cmd.CommandText = "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public'";
        await db.Database.OpenConnectionAsync();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tables.Add(reader.GetString(0));
        }

        tables.Should().Contain("companies");
        tables.Should().Contain("meetings");
        tables.Should().Contain("shareholders");
        tables.Should().Contain("ballots");
        tables.Should().Contain("audit_logs");
        tables.Should().Contain("meeting_resolutions");
        tables.Should().Contain("meeting_candidates");
        tables.Should().Contain("AspNetUsers");
        tables.Should().Contain("AspNetRoles");
    }

    [Fact]
    public async Task SeedData_Creates4Roles_And1AdminUser()
    {
        using var scope = _fixture.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        (await roleManager.RoleExistsAsync("admin")).Should().BeTrue();
        (await roleManager.RoleExistsAsync("operator")).Should().BeTrue();
        (await roleManager.RoleExistsAsync("viewer")).Should().BeTrue();
        (await roleManager.RoleExistsAsync("checkin")).Should().BeTrue();

        var admin = await userManager.FindByNameAsync("admin");
        admin.Should().NotBeNull();
        admin!.MustChangePassword.Should().BeTrue();
        admin.FullName.Should().Be("System Administrator");

        var roles = await userManager.GetRolesAsync(admin);
        roles.Should().Contain("admin");
    }

    [Fact]
    public void JwtTokenService_GeneratesValidToken_WithCorrectClaims()
    {
        var jwt = _fixture.Services.GetRequiredService<JwtTokenService>();
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "test-user",
            MustChangePassword = true,
        };

        var token = jwt.GenerateAccessToken(user, ["admin"]);

        token.Should().NotBeNullOrEmpty();

        var handler = new JwtSecurityTokenHandler();
        var parsed = handler.ReadJwtToken(token);

        parsed.Claims.Should().Contain(c =>
            c.Type == "must_change_password" && c.Value == "true");
        parsed.Claims.Should().Contain(c =>
            c.Type == ClaimTypes.Role && c.Value == "admin");
        parsed.Claims.Should().Contain(c =>
            c.Type == JwtRegisteredClaimNames.Name && c.Value == "test-user");
    }
}
