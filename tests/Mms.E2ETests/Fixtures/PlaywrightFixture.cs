using Microsoft.Playwright;

namespace Mms.E2ETests.Fixtures;

/// <summary>
/// Shared Playwright fixture for all E2E tests.
/// Runs against the live docker-compose stack (NOT WebApplicationFactory).
/// Set MMS_E2E_URL env var to override the default http://localhost:8080.
/// </summary>
public class PlaywrightFixture : IAsyncLifetime
{
    public IPlaywright Playwright { get; private set; } = null!;
    public IBrowser Browser { get; private set; } = null!;
    public string BaseUrl { get; } =
        Environment.GetEnvironmentVariable("MMS_E2E_URL") ?? "http://localhost:8080";

    public async Task InitializeAsync()
    {
        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
            SlowMo = 0,
        });

        await WaitForAppReady();
    }

    /// <summary>
    /// Retries GET /login for up to 60 seconds until the app is up.
    /// </summary>
    private async Task WaitForAppReady()
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        for (int i = 0; i < 30; i++)
        {
            try
            {
                var resp = await http.GetAsync($"{BaseUrl}/login");
                if (resp.IsSuccessStatusCode) return;
            }
            catch { /* app not ready yet */ }
            await Task.Delay(2000);
        }
        throw new Exception($"App not ready at {BaseUrl} after 60 seconds");
    }

    /// <summary>
    /// Creates a new browser context + page for test isolation.
    /// </summary>
    public async Task<IPage> NewPageAsync()
    {
        var context = await Browser.NewContextAsync();
        return await context.NewPageAsync();
    }

    public async Task DisposeAsync()
    {
        await Browser.DisposeAsync();
        Playwright.Dispose();
    }
}
