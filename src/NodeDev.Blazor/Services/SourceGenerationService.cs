using NodeDev.Core;
using NodeDev.Core.Class;

namespace NodeDev.Blazor.Services;

/// <summary>
/// Generates the C# preview shown by SourceViewer without blocking the Blazor renderer. Generation is
/// serialized because building mutates project compiler state, and every build receives an isolated output directory.
/// </summary>
internal sealed class SourceGenerationService
{
	private readonly SemaphoreSlim GenerationGate = new(1, 1);

	/// <summary>
	/// Builds the method's project on a worker thread and returns the generated C# for the requested method.
	/// Cancellation prevents queued work and suppresses stale results, although an active compiler invocation
	/// must finish before its temporary output can be cleaned up.
	/// </summary>
	public async Task<string?> GenerateCSharpAsync(NodeClassMethod method, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(method);
		await GenerationGate.WaitAsync(cancellationToken);
		try
		{
			return await Task.Run(() => GenerateCSharp(method, cancellationToken), cancellationToken);
		}
		finally
		{
			GenerationGate.Release();
		}
	}

	/// <summary>
	/// Performs generation in a unique temporary directory and removes all generated artifacts afterward.
	/// </summary>
	private static string? GenerateCSharp(NodeClassMethod method, CancellationToken cancellationToken)
	{
		var outputPath = Path.Combine(Path.GetTempPath(), $"nodedev-source-{Guid.NewGuid():N}");
		try
		{
			cancellationToken.ThrowIfCancellationRequested();
			method.Graph.Project.Build(BuildOptions.Debug with { OutputPath = outputPath });
			cancellationToken.ThrowIfCancellationRequested();
			return method.Graph.Project.GetGeneratedCSharpCode(method);
		}
		finally
		{
			try
			{
				if (Directory.Exists(outputPath))
					Directory.Delete(outputPath, recursive: true);
			}
			catch (IOException)
			{
				// Best-effort cleanup. A process may briefly retain a generated output file.
			}
			catch (UnauthorizedAccessException)
			{
				// Best-effort cleanup. The generated source result should still be usable.
			}
		}
	}
}
