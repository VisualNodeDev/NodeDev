using Microsoft.AspNetCore.Builder;
using Microsoft.Playwright;
using System.Diagnostics;

namespace NodeDev.EndToEndTests.Hooks;

[Binding]
public class Hooks
{
    public IPage User { get; private set; } = null!; //-> We'll call this property in the tests

    private IBrowser OpenedBrowser = null!;

    private static Process App = null!;
    private static StreamWriter StdOutput = null!;
    private static StreamWriter StdError = null!;
    private static IPlaywright PlaywrightInstance = null!;

    private const int Port = 5166;

    [BeforeFeature]
    public static async Task StartServer()
    {
        StdOutput = new StreamWriter(File.Open("../../../../NodeDev.Blazor.Server/logs_std.txt", FileMode.Create), leaveOpen: false);
        StdError = new StreamWriter(File.Open("../../../../NodeDev.Blazor.Server/logs_err.txt", FileMode.Create), leaveOpen: false);

        // start the server using either a environment variable set by the CI, or a default path.
        // The default path will work if you're running the tests from Visual Studio.
        App = new Process();
        App.StartInfo = new ProcessStartInfo()
        {
            FileName = "dotnet",
            Arguments = $"run --no-build -- --urls http://localhost:{Port}",
            WorkingDirectory = "../../../../NodeDev.Blazor.Server",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        App.OutputDataReceived += App_OutputDataReceived;
        App.ErrorDataReceived += App_ErrorDataReceived;

        App.Start();
        App.BeginOutputReadLine();
        App.BeginErrorReadLine();

        await Task.Delay(1000);

        if(App.HasExited)
        {
            StdOutput.Flush();
            StdError.Flush();
            throw new Exception("Failed to start the server: " + App.ExitCode);
        }

        //Initialise Playwright
        PlaywrightInstance ??= await Playwright.CreateAsync();
    }

    private static void App_ErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (e.Data != null)
            StdError.WriteLine(e.Data);
    }

    private static void App_OutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        if(e.Data != null)
            StdOutput.WriteLine(e.Data);
    }


    [BeforeScenario] // -> Notice how we're doing these steps before each scenario
    public async Task RegisterSingleInstancePractitioner()
    {
        //Initialise a browser - 'Chromium' can be changed to 'Firefox' or 'Webkit'
        OpenedBrowser = await PlaywrightInstance.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = Environment.GetEnvironmentVariable("HEADLESS") == "true" // -> Use this option to be able to see your test running
        });

        //Setup a browser context
        var context1 = await OpenedBrowser.NewContextAsync(new()
        {
            ScreenSize = new() { Width = 1900, Height = 1000 },
            ViewportSize = new() { Width = 1900, Height = 1000 }
        });

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
                if (i == 60)
                {
                    StdOutput.Flush();
                    StdError.Flush();
                    throw;
                }
            }

            await Task.Delay(1000);
        }
    }

    [AfterScenario]
    public async Task AfterScenario()
    {
        await OpenedBrowser.CloseAsync();
        await OpenedBrowser.DisposeAsync();
    }

    [AfterFeature] // -> Notice how we're doing these steps after each scenario
    public static async Task StopServer()
    {
        App.Kill(true);
        while (!App.HasExited)
        {
            await Task.Delay(100);
        }

        StdOutput.Close();
        StdError.Close();

        StdError.Dispose();
        StdError.Dispose();
    }
}
