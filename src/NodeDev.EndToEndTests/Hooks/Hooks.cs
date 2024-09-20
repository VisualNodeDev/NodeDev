﻿using Microsoft.AspNetCore.Builder;
using Microsoft.Playwright;
using System.Diagnostics;

namespace NodeDev.EndToEndTests.Hooks;

[Binding]
public class Hooks
{
    public IPage User { get; private set; } = null!; //-> We'll call this property in the tests

    private static Process App = null!;
    private static StreamWriter StdOutput = null!;
    private static StreamWriter StdError = null!;

    private const int Port = 5166;

    [BeforeFeature]
    public static async Task StartServer()
    {
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

        StdOutput = new StreamWriter(File.Open("../../../../NodeDev.Blazor.Server/logs_std.txt", FileMode.Create));
        StdError = new StreamWriter(File.Open("../../../../NodeDev.Blazor.Server/logs_err.txt", FileMode.Create));
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
        //Initialise Playwright
        var playwright = await Playwright.CreateAsync();
        //Initialise a browser - 'Chromium' can be changed to 'Firefox' or 'Webkit'
        var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = Environment.GetEnvironmentVariable("HEADLESS") == "true" // -> Use this option to be able to see your test running
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
                {
                    StdOutput.Flush();
                    StdError.Flush();
                    throw;
                }
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


        StdOutput.Flush();
        StdError.Flush();

        StdError.Dispose();
        StdError.Dispose();
    }
}
