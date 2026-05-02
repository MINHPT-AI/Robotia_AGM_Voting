using Microsoft.Playwright;

namespace Mms.E2ETests.Pages;

/// <summary>
/// Page Object for the Dashboard (/) — verifies successful login.
/// </summary>
public class DashboardPage
{
    private readonly IPage _page;
    private readonly string _baseUrl;

    public DashboardPage(IPage page, string baseUrl)
    {
        _page = page;
        _baseUrl = baseUrl;
    }

    public async Task<bool> IsVisibleAsync()
    {
        // Dashboard should have the main layout visible
        try
        {
            await _page.WaitForSelectorAsync(".mud-layout", new() { Timeout = 5_000 });
            return true;
        }
        catch
        {
            return false;
        }
    }

    public Task<string> GetCurrentUrlAsync() => Task.FromResult(_page.Url);
}
