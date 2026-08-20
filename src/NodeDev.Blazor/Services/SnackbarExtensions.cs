using MudBlazor;

namespace NodeDev.Blazor.Services;

/// <summary>
/// Provides consistent snackbar rendering for workspace command outcomes.
/// </summary>
public static class SnackbarExtensions
{
	/// <summary>
	/// Adds the command message to a snackbar with a severity derived from its outcome.
	/// </summary>
	/// <param name="snackbar">The snackbar service that displays the notification.</param>
	/// <param name="result">The command outcome to display.</param>
	/// <param name="successSeverity">The severity to use when <paramref name="result"/> succeeded.</param>
	public static void ShowCommandResult(this ISnackbar snackbar, WorkspaceCommandResult result, Severity successSeverity = Severity.Success)
	{
		snackbar.Add(result.Message, result.Succeeded ? successSeverity : Severity.Error);
	}
}
