using Microsoft.Extensions.Logging;
using NodeDev.Core;
using NodeDev.Core.Class;
using NodeDev.Core.Types;

namespace NodeDev.Blazor.Services;

/// <summary>
/// Provides a UI-facing boundary for workspace mutations and execution requests.
/// </summary>
/// <remarks>
/// Components use this service instead of changing core model objects directly so failures are logged and can be presented consistently.
/// </remarks>
public sealed class WorkspaceCommandService(ProjectService projectService, ILogger<WorkspaceCommandService> logger)
{
	/// <summary>
	/// Gets the project currently managed by the active UI scope.
	/// </summary>
	public Project Project => projectService.Project;

	/// <summary>
	/// Replaces the active project with a default project.
	/// </summary>
	/// <returns>A result suitable for a status notification.</returns>
	public WorkspaceCommandResult CreateNewProject()
	{
		return Execute(() => projectService.ChangeProject(Project.CreateNewDefaultProject()), "New project created");
	}

	/// <summary>
	/// Retrieves saved project names from the configured projects directory.
	/// </summary>
	/// <returns>The saved names, or a failure result when they cannot be read.</returns>
	public WorkspaceCommandResult<IReadOnlyList<string>> GetSavedProjectNames()
	{
		return Execute(projectService.GetSavedProjectNames, "Saved projects loaded");
	}

	/// <summary>
	/// Loads a saved project into the active UI scope.
	/// </summary>
	/// <param name="projectName">The saved project name to load.</param>
	/// <returns>A task that completes with the operation outcome.</returns>
	public Task<WorkspaceCommandResult> OpenProjectAsync(string projectName)
	{
		return ExecuteAsync(() => projectService.LoadProjectAsync(projectName), $"Project '{projectName}' opened");
	}

	/// <summary>
	/// Persists the active project, using its existing name when no name is supplied.
	/// </summary>
	/// <param name="projectName">An optional project name to use for this save.</param>
	/// <param name="cancellationToken">Cancels the pending file operation.</param>
	/// <returns>A task that completes with the operation outcome.</returns>
	public Task<WorkspaceCommandResult> SaveProjectAsync(string? projectName = null, CancellationToken cancellationToken = default)
	{
		return ExecuteAsync(() => projectService.SaveProjectToFileAsync(projectName, cancellationToken), "Project saved");
	}

	/// <summary>
	/// Builds the active project without blocking the Blazor renderer.
	/// </summary>
	/// <param name="options">Compilation options to use for the build.</param>
	/// <param name="cancellationToken">Cancels the queued build before it starts.</param>
	/// <returns>The generated artifact path when the build succeeds.</returns>
	public Task<WorkspaceCommandResult<string>> BuildProjectAsync(BuildOptions options, CancellationToken cancellationToken = default)
	{
		return ExecuteBackgroundAsync(
			() => Project.Build(options),
			path => $"Project built successfully: {path}",
			cancellationToken);
	}

	/// <summary>
	/// Builds and runs the active project without hard debugging.
	/// </summary>
	/// <param name="options">Build options to use before execution.</param>
	/// <param name="cancellationToken">Cancels the queued execution before it starts.</param>
	/// <returns>The process exit code, or a failure result when execution cannot start.</returns>
	public async Task<WorkspaceCommandResult<object?>> RunProjectAsync(BuildOptions options, CancellationToken cancellationToken = default)
	{
		var result = await ExecuteBackgroundAsync(
			() => Project.Run(options),
			exitCode => $"Project exited with code {exitCode}",
			cancellationToken);

		if (result.Succeeded && result.Value is null)
		{
			return WorkspaceCommandResult<object?>.Failure("Project execution failed. See the debugger console for details.");
		}

		return result;
	}

	/// <summary>
	/// Builds and runs the active project with the hard debugger attached.
	/// </summary>
	/// <param name="options">Build options to use before execution.</param>
	/// <param name="cancellationToken">Cancels the queued execution before it starts.</param>
	/// <returns>The process exit code, or a failure result when debugging cannot start.</returns>
	public async Task<WorkspaceCommandResult<object?>> RunWithDebugAsync(BuildOptions options, CancellationToken cancellationToken = default)
	{
		var result = await ExecuteBackgroundAsync(
			() => Project.RunWithDebug(options),
			exitCode => $"Debug session ended with code {exitCode}",
			cancellationToken);

		if (result.Succeeded && result.Value is null)
		{
			return WorkspaceCommandResult<object?>.Failure("Debug execution failed. See the debugger console for details.");
		}

		return result;
	}

	/// <summary>
	/// Stops the active hard-debugging session.
	/// </summary>
	/// <returns>A result suitable for a status notification.</returns>
	public WorkspaceCommandResult StopDebugging()
	{
		return Execute(Project.StopDebugging, "Debugging stopped");
	}

	/// <summary>
	/// Continues the active hard-debugging session after a breakpoint pause.
	/// </summary>
	/// <returns>A result suitable for a status notification.</returns>
	public WorkspaceCommandResult ResumeDebugging()
	{
		return Execute(Project.ContinueExecution, "Execution resumed");
	}

	/// <summary>
	/// Toggles live-debugging mode for the active project.
	/// </summary>
	/// <returns>A result that describes the resulting live-debugging state.</returns>
	public WorkspaceCommandResult ToggleLiveDebugging()
	{
		var isLiveDebuggingEnabled = Project.IsLiveDebuggingEnabled;
		return Execute(() =>
		{
			if (isLiveDebuggingEnabled)
			{
				Project.StopLiveDebugging();
			}
			else
			{
				Project.StartLiveDebugging();
			}
		}, isLiveDebuggingEnabled ? "Live debugging stopped" : "Live debugging started");
	}

	/// <summary>
	/// Creates a class in the active project after validating its name.
	/// </summary>
	/// <param name="name">The class name supplied by the user.</param>
	/// <param name="namespace">The namespace for the new class.</param>
	/// <returns>The created class when validation and insertion succeed.</returns>
	public WorkspaceCommandResult<NodeClass> CreateClass(string name, string @namespace = "MyApp")
	{
		return Execute(() =>
		{
			var nodeClass = new NodeClass(RequireName(name, "Class name"), @namespace, Project);
			Project.AddClass(nodeClass);
			return nodeClass;
		}, nodeClass => $"Class '{nodeClass.Name}' created successfully");
	}

	/// <summary>
	/// Renames a class after validating the supplied name.
	/// </summary>
	/// <param name="nodeClass">The class that belongs to the active project.</param>
	/// <param name="newName">The replacement name supplied by the user.</param>
	/// <returns>A result that reports whether the rename was accepted.</returns>
	public WorkspaceCommandResult RenameClass(NodeClass nodeClass, string newName)
	{
		return Execute(() => nodeClass.Rename(RequireName(newName, "Class name")), $"Class renamed to '{newName}'");
	}

	/// <summary>
	/// Removes a class from the active project after core reference validation succeeds.
	/// </summary>
	/// <param name="nodeClass">The class to remove.</param>
	/// <returns>A result that reports reference-validation failures to the UI.</returns>
	public WorkspaceCommandResult DeleteClass(NodeClass nodeClass)
	{
		return Execute(() => Project.RemoveClass(nodeClass), $"Class '{nodeClass.Name}' deleted");
	}

	/// <summary>
	/// Creates a method with a default void return type on the specified class.
	/// </summary>
	/// <param name="nodeClass">The class that owns the new method.</param>
	/// <param name="name">The method name supplied by the user.</param>
	/// <returns>The created method when insertion succeeds.</returns>
	public WorkspaceCommandResult<NodeClassMethod> CreateMethod(NodeClass nodeClass, string name)
	{
		return Execute(() =>
		{
			var method = new NodeClassMethod(nodeClass, RequireName(name, "Method name"), nodeClass.TypeFactory.Void);
			nodeClass.AddMethod(method, createEntryAndReturn: true);
			return method;
		}, method => $"Method '{method.Name}' created successfully");
	}

	/// <summary>
	/// Renames a method after validating the replacement name.
	/// </summary>
	/// <param name="method">The method to rename.</param>
	/// <param name="newName">The replacement name supplied by the user.</param>
	/// <returns>A result that reports whether the rename was accepted.</returns>
	public WorkspaceCommandResult RenameMethod(NodeClassMethod method, string newName)
	{
		return Execute(() => method.Rename(RequireName(newName, "Method name")), $"Method renamed to '{newName}'");
	}

	/// <summary>
	/// Deletes a method from its owning class.
	/// </summary>
	/// <param name="method">The method to delete.</param>
	/// <returns>A result that reports whether deletion succeeded.</returns>
	public WorkspaceCommandResult DeleteMethod(NodeClassMethod method)
	{
		return Execute(() => method.Class.RemoveMethod(method), $"Method '{method.Name}' deleted");
	}

	/// <summary>
	/// Creates a double-typed property on the specified class.
	/// </summary>
	/// <param name="nodeClass">The class that owns the new property.</param>
	/// <param name="name">The property name supplied by the user.</param>
	/// <returns>The created property when insertion succeeds.</returns>
	public WorkspaceCommandResult<NodeClassProperty> CreateProperty(NodeClass nodeClass, string name)
	{
		return Execute(() =>
		{
			var property = new NodeClassProperty(nodeClass, RequireName(name, "Property name"), nodeClass.TypeFactory.Get<double>());
			nodeClass.Properties.Add(property);
			return property;
		}, property => $"Property '{property.Name}' created successfully");
	}

	/// <summary>
	/// Renames a property after validating the replacement name.
	/// </summary>
	/// <param name="property">The property to rename.</param>
	/// <param name="newName">The replacement name supplied by the user.</param>
	/// <returns>A result that reports whether the rename was accepted.</returns>
	public WorkspaceCommandResult RenameProperty(NodeClassProperty property, string newName)
	{
		return Execute(() => property.Rename(RequireName(newName, "Property name")), $"Property renamed to '{newName}'");
	}

	/// <summary>
	/// Replaces a property's declared type.
	/// </summary>
	/// <param name="property">The property to update.</param>
	/// <param name="type">The selected type.</param>
	/// <returns>A result that reports whether the type change was accepted.</returns>
	public WorkspaceCommandResult ChangePropertyType(NodeClassProperty property, TypeBase type)
	{
		return Execute(() => property.ChangeType(type), $"Property type changed to '{type.FriendlyName}'");
	}

	/// <summary>
	/// Adds the core model's default parameter to a method.
	/// </summary>
	/// <param name="method">The method to update.</param>
	/// <returns>A result that reports whether the parameter was added.</returns>
	public WorkspaceCommandResult AddDefaultParameter(NodeClassMethod method)
	{
		return Execute(method.AddDefaultParameter, "Parameter added");
	}

	/// <summary>
	/// Renames a method parameter after validating the replacement name.
	/// </summary>
	/// <param name="parameter">The parameter to rename.</param>
	/// <param name="newName">The replacement name supplied by the user.</param>
	/// <returns>A result that reports whether the rename was accepted.</returns>
	public WorkspaceCommandResult RenameParameter(NodeClassMethodParameter parameter, string newName)
	{
		return Execute(() => parameter.Rename(RequireName(newName, "Parameter name")), $"Parameter renamed to '{newName}'");
	}

	/// <summary>
	/// Changes whether a parameter is emitted as an output parameter.
	/// </summary>
	/// <param name="parameter">The parameter to update.</param>
	/// <param name="isOut">Whether the parameter should be marked <see langword="out"/>.</param>
	/// <returns>A result that reports whether the update was accepted.</returns>
	public WorkspaceCommandResult SetParameterIsOut(NodeClassMethodParameter parameter, bool isOut)
	{
		return Execute(() => parameter.SetIsOut(isOut), "Parameter updated");
	}

	/// <summary>
	/// Moves a parameter earlier in its method signature.
	/// </summary>
	/// <param name="parameter">The parameter to move.</param>
	/// <returns>A result that reports whether the move was accepted.</returns>
	public WorkspaceCommandResult MoveParameterUp(NodeClassMethodParameter parameter)
	{
		return Execute(parameter.MoveUp, "Parameter moved");
	}

	/// <summary>
	/// Moves a parameter later in its method signature.
	/// </summary>
	/// <param name="parameter">The parameter to move.</param>
	/// <returns>A result that reports whether the move was accepted.</returns>
	public WorkspaceCommandResult MoveParameterDown(NodeClassMethodParameter parameter)
	{
		return Execute(parameter.MoveDown, "Parameter moved");
	}

	/// <summary>
	/// Removes a parameter from its method signature.
	/// </summary>
	/// <param name="parameter">The parameter to remove.</param>
	/// <returns>A result that reports whether removal was accepted.</returns>
	public WorkspaceCommandResult RemoveParameter(NodeClassMethodParameter parameter)
	{
		return Execute(parameter.Remove, "Parameter removed");
	}

	/// <summary>
	/// Replaces a method parameter's declared type.
	/// </summary>
	/// <param name="parameter">The parameter to update.</param>
	/// <param name="type">The selected type.</param>
	/// <returns>A result that reports whether the type change was accepted.</returns>
	public WorkspaceCommandResult ChangeParameterType(NodeClassMethodParameter parameter, TypeBase type)
	{
		return Execute(() => parameter.ChangeType(type), $"Parameter type changed to '{type.FriendlyName}'");
	}

	/// <summary>
	/// Runs a synchronous mutation and converts exceptions into a logged failure result.
	/// </summary>
	/// <param name="action">The mutation to run.</param>
	/// <param name="successMessage">The message to return when the mutation completes.</param>
	/// <returns>A successful result, or a failure result that retains the thrown exception.</returns>
	private WorkspaceCommandResult Execute(Action action, string successMessage)
	{
		try
		{
			action();
			return WorkspaceCommandResult.Success(successMessage);
		}
		catch (Exception ex)
		{
			return Failure(ex);
		}
	}

	/// <summary>
	/// Runs a synchronous query or mutation that returns a value.
	/// </summary>
	/// <typeparam name="T">The value returned by <paramref name="action"/>.</typeparam>
	/// <param name="action">The operation to run.</param>
	/// <param name="successMessage">The message to return when the operation completes.</param>
	/// <returns>The operation value in a successful result, or a logged failure result.</returns>
	private WorkspaceCommandResult<T> Execute<T>(Func<T> action, string successMessage)
	{
		return Execute(action, _ => successMessage);
	}

	/// <summary>
	/// Runs a synchronous query or mutation and creates its success message from the returned value.
	/// </summary>
	/// <typeparam name="T">The value returned by <paramref name="action"/>.</typeparam>
	/// <param name="action">The operation to run.</param>
	/// <param name="successMessage">Creates the message associated with the returned value.</param>
	/// <returns>The operation value in a successful result, or a logged failure result.</returns>
	private WorkspaceCommandResult<T> Execute<T>(Func<T> action, Func<T, string> successMessage)
	{
		try
		{
			var value = action();
			return WorkspaceCommandResult<T>.Success(value, successMessage(value));
		}
		catch (Exception ex)
		{
			return Failure<T>(ex);
		}
	}

	/// <summary>
	/// Awaits an asynchronous operation and converts failures into a logged result.
	/// </summary>
	/// <param name="action">The asynchronous operation to run.</param>
	/// <param name="successMessage">The message to return when the operation completes.</param>
	/// <returns>A task that completes with a success or failure result.</returns>
	private async Task<WorkspaceCommandResult> ExecuteAsync(Func<Task> action, string successMessage)
	{
		try
		{
			await action();
			return WorkspaceCommandResult.Success(successMessage);
		}
		catch (Exception ex)
		{
			return Failure(ex);
		}
	}

	/// <summary>
	/// Runs a blocking operation on a worker thread and converts exceptions into a logged result.
	/// </summary>
	/// <typeparam name="T">The value returned by <paramref name="action"/>.</typeparam>
	/// <param name="action">The blocking operation to run.</param>
	/// <param name="successMessage">Creates the message associated with the returned value.</param>
	/// <param name="cancellationToken">Cancels the queued worker operation before it starts.</param>
	/// <returns>A task that completes with the operation result.</returns>
	private async Task<WorkspaceCommandResult<T>> ExecuteBackgroundAsync<T>(Func<T> action, Func<T, string> successMessage, CancellationToken cancellationToken)
	{
		try
		{
			var value = await Task.Run(action, cancellationToken);
			return WorkspaceCommandResult<T>.Success(value, successMessage(value));
		}
		catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
		{
			return WorkspaceCommandResult<T>.Failure("The operation was canceled.", ex);
		}
		catch (Exception ex)
		{
			return Failure<T>(ex);
		}
	}

	/// <summary>
	/// Logs an exception and turns it into a non-generic failure result.
	/// </summary>
	/// <param name="exception">The exception raised by a workspace operation.</param>
	/// <returns>A result that preserves the exception for callers that need to inspect it.</returns>
	private WorkspaceCommandResult Failure(Exception exception)
	{
		logger.LogError(exception, "Workspace command failed");
		return WorkspaceCommandResult.Failure(exception.Message, exception);
	}

	/// <summary>
	/// Logs an exception and turns it into a typed failure result.
	/// </summary>
	/// <typeparam name="T">The value type expected by the failed operation.</typeparam>
	/// <param name="exception">The exception raised by a workspace operation.</param>
	/// <returns>A typed result with no value and the preserved exception.</returns>
	private WorkspaceCommandResult<T> Failure<T>(Exception exception)
	{
		logger.LogError(exception, "Workspace command failed");
		return WorkspaceCommandResult<T>.Failure(exception.Message, exception);
	}

	/// <summary>
	/// Validates and normalizes a user-supplied model member name.
	/// </summary>
	/// <param name="name">The raw user input.</param>
	/// <param name="label">The user-facing name for the value being validated.</param>
	/// <returns>The trimmed, non-empty name.</returns>
	/// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty or whitespace.</exception>
	private static string RequireName(string name, string label)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			throw new ArgumentException($"{label} cannot be empty.", nameof(name));
		}

		return name.Trim();
	}
}
