using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NodeDev.Blazor;

public class AppOptionsContainer
{
    private readonly string OptionsFileName;

    private AppOptions appOptions = new AppOptions();
    public AppOptions AppOptions
    {
        get => appOptions;
        set
        {
            appOptions = value;
            SaveOptions();
        }
    }

    public AppOptionsContainer(string optionFileName)
    {
        OptionsFileName = optionFileName;
        LoadOptions();
    }

    public void SaveOptions()
    {
        File.WriteAllText(OptionsFileName, JsonSerializer.Serialize(appOptions, new JsonSerializerOptions()
        {
            WriteIndented = true
        }));
    }

    public void LoadOptions()
    {
        if (File.Exists(OptionsFileName))
        {
            appOptions = JsonSerializer.Deserialize<AppOptions>(File.ReadAllText(OptionsFileName)) ?? new AppOptions();
        }
    }
}
