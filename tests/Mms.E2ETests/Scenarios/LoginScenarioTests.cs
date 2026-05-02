using FluentAssertions;
using Mms.E2ETests.Fixtures;
using Mms.E2ETests.Pages;

namespace Mms.E2ETests.Scenarios;

/// <summary>
/// E2E Login scenarios — runs against live docker-compose stack.
/// Prerequisites:
///   1. docker-compose up -d --build
///   2. App running at http://localhost:8080 (or set MMS_E2E_URL)
///   3. Playwright browsers installed: pwsh bin/.../playwright.ps1 install chromium
/// </summary>
public class LoginScenarioTests : IClassFixture<PlaywrightFixture>
{
    private readonly PlaywrightFixture _fixture;

    public LoginScenarioTests(PlaywrightFixture fixture) => _fixture = fixture;

    // TC-01: Happy path — admin/Admin@123 → redirects to change-password or dashboard
    [Fact]
    public async Task Login_ValidCredentials_RedirectsToDashboardOrChangePassword()
    {
        var page = await _fixture.NewPageAsync();
        var loginPage = new LoginPage(page, _fixture.BaseUrl);

        await loginPage.NavigateAsync();
        await loginPage.FillCredentialsAsync("admin", "Admin@123");
        await loginPage.SubmitAsync();

        // Should redirect away from /login
        await page.WaitForURLAsync(url => !url.Contains("/login"),
            new() { Timeout = 10_000 });

        var url = page.Url;
        // Either goes to change-password (first login) or dashboard
        var validRedirect = url.Contains("/change-password") ||
                           url.EndsWith("/") ||
                           url.Contains("/dashboard");
        validRedirect.Should().BeTrue($"Expected redirect but got: {url}");
    }

    // TC-02: Wrong password → error message displayed
    [Fact]
    public async Task Login_WrongPassword_ShowsError()
    {
        var page = await _fixture.NewPageAsync();
        var loginPage = new LoginPage(page, _fixture.BaseUrl);

        await loginPage.NavigateAsync();
        await loginPage.FillCredentialsAsync("admin", "WrongPassword!");
        await loginPage.SubmitAsync();

        // Should stay on /login page
        page.Url.Should().Contain("/login");

        // Should show error
        var hasError = await loginPage.HasErrorAsync();
        hasError.Should().BeTrue("Error message should be displayed for wrong password");
    }
}
