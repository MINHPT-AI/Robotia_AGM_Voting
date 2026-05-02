using Microsoft.Playwright;

namespace Mms.E2ETests.Pages;

/// <summary>
/// Page Object for the Login page (/login).
/// Uses Blazor Server SSR form with standard input names.
/// </summary>
public class LoginPage
{
    private readonly IPage _page;
    private readonly string _baseUrl;

    public LoginPage(IPage page, string baseUrl)
    {
        _page = page;
        _baseUrl = baseUrl;
    }

    public async Task NavigateAsync()
    {
        await _page.GotoAsync($"{_baseUrl}/login");
        // Wait for the login form to render
        await _page.WaitForSelectorAsync("form", new() { Timeout = 10_000 });
    }

    public async Task FillCredentialsAsync(string username, string password)
    {
        await _page.FillAsync("input[name='Input.Username']", username);
        await _page.FillAsync("input[name='Input.Password']", password);
    }

    public async Task SubmitAsync()
    {
        await _page.ClickAsync("button[type='submit']");
        // Wait for navigation or error
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task<bool> HasErrorAsync()
    {
        var errorEl = _page.Locator(".mud-alert-text-error, .mud-alert-filled-error, [class*='error']");
        return await errorEl.CountAsync() > 0;
    }

    public async Task<string?> GetErrorTextAsync()
    {
        var errorEl = _page.Locator(".mud-alert-text-error, .mud-alert-filled-error").First;
        if (await errorEl.CountAsync() == 0) return null;
        return await errorEl.TextContentAsync();
    }
}
