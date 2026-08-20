namespace NodeDev.Blazor.Services;

/// <summary>
/// Represents a workspace command outcome that can be presented by the UI.
/// </summary>
/// <param name="Succeeded">Whether the requested operation completed successfully.</param>
/// <param name="Message">A user-facing description of the operation outcome.</param>
/// <param name="Exception">The underlying failure when the operation did not succeed.</param>
public record WorkspaceCommandResult(bool Succeeded, string Message, Exception? Exception = null)
{
	/// <summary>
	/// Creates a successful command result.
	/// </summary>
	/// <param name="message">The user-facing success message.</param>
	/// <returns>A successful result without an exception.</returns>
	public static WorkspaceCommandResult Success(string message)
	{
		return new WorkspaceCommandResult(true, message);
	}

	/// <summary>
	/// Creates a failed command result while preserving the underlying exception.
	/// </summary>
	/// <param name="message">The user-facing failure message.</param>
	/// <param name="exception">The exception that caused the operation to fail.</param>
	/// <returns>A failed result.</returns>
	public static WorkspaceCommandResult Failure(string message, Exception? exception = null)
	{
		return new WorkspaceCommandResult(false, message, exception);
	}
}

/// <summary>
/// Represents a workspace command outcome that may include a value.
/// </summary>
/// <typeparam name="T">The type returned by a successful command.</typeparam>
/// <param name="Succeeded">Whether the requested operation completed successfully.</param>
/// <param name="Message">A user-facing description of the operation outcome.</param>
/// <param name="Value">The value produced by a successful command.</param>
/// <param name="Exception">The underlying failure when the operation did not succeed.</param>
public sealed record WorkspaceCommandResult<T>(bool Succeeded, string Message, T? Value, Exception? Exception = null)
	: WorkspaceCommandResult(Succeeded, Message, Exception)
{
	/// <summary>
	/// Creates a successful result that carries the command value.
	/// </summary>
	/// <param name="value">The value produced by the command.</param>
	/// <param name="message">The user-facing success message.</param>
	/// <returns>A successful typed result.</returns>
	public static WorkspaceCommandResult<T> Success(T value, string message)
	{
		return new WorkspaceCommandResult<T>(true, message, value);
	}

	/// <summary>
	/// Creates a failed typed result with no command value.
	/// </summary>
	/// <param name="message">The user-facing failure message.</param>
	/// <param name="exception">The exception that caused the operation to fail.</param>
	/// <returns>A failed typed result.</returns>
	public new static WorkspaceCommandResult<T> Failure(string message, Exception? exception = null)
	{
		return new WorkspaceCommandResult<T>(false, message, default, exception);
	}
}
