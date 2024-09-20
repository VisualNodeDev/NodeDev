using Microsoft.AspNetCore.Builder;
using Microsoft.Playwright;
using System.Diagnostics;

namespace NodeDev.EndToEndTests.Hooks;

[Binding]
public class Hooks
{
    public IPage User { get; private set; } = null!; //-> We'll call this property in the tests

    private static Process App;

    private const int Port = 5166;

    [BeforeFeature]
    public static async Task StartServer()
    {
        // start the server using either a environment variable set by the CI, or a default path.
        // The default path will work if you're running the tests from Visual Studio.
        App = Process.Start(new ProcessStartInfo()
        {
            CreateNoWindow = false,
            FileName = "dotnet",
            Arguments = "run --no-build",
            WorkingDirectory = Environment.GetEnvironmentVariable("NodeDevServerPath") ?? @"..\..\..\..\NodeDev.Blazor.Server",
        })!;
    }

    [BeforeScenario] // -> Notice how we're doing these steps before each scenario
    public async Task RegisterSingleInstancePractitioner()
    {
        //Initialise Playwright
        var playwright = await Playwright.CreateAsync();
        //Initialise a browser - 'Chromium' can be changed to 'Firefox' or 'Webkit'
        var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = false // -> Use this option to be able to see your test running
        });
        //Setup a browser context
        var context1 = await browser.NewContextAsync();

        //Initialise a page on the browser context.
        User = await context1.NewPageAsync();

        for (int i = 0; ; ++i)
        {
            try
            {
                await User.GotoAsync($"http://localhost:{Port}");
                break;
            }
            catch
            {
                if (i == 5)
                    throw;
            }

            await Task.Delay(100);
        }
    }


    [AfterScenario] // -> Notice how we're doing these steps after each scenario
    public static async Task StopServer()
    {
        App.Kill(true);
        while (!App.HasExited)
        {
            await Task.Delay(100);
        }
    }
}
