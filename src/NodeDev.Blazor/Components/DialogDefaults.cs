using MudBlazor;

namespace NodeDev.Blazor.Components;

internal static class DialogDefaults
{
	public static DialogOptions SmallForm { get; } = new() { MaxWidth = MaxWidth.Small, FullWidth = true };

	public static DialogOptions MediumForm { get; } = new() { MaxWidth = MaxWidth.Medium, FullWidth = true };

	public static DialogOptions LargeEditor { get; } = new() { MaxWidth = MaxWidth.Large, FullWidth = true };

	public static DialogOptions TypeSelector { get; } = new() { FullWidth = true };

	public static DialogOptions FullScreen { get; } = new() { FullScreen = true, FullWidth = true };

	public static DialogOptions Confirmation { get; } = new() { MaxWidth = MaxWidth.Small };
}
