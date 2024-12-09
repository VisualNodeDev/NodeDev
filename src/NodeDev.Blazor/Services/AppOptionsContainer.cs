using System.Text.Json;

namespace NodeDev.Blazor.Services;

public class AppOptionsContainer
{
	private readonly string OptionsFileName;

	private AppOptions appOptions = new();
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
		if (!string.IsNullOrWhiteSpace(OptionsFileName))
		{
			LoadOptions();
		}
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
