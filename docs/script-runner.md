# ScriptRunner - Debugging Infrastructure

## Overview

ScriptRunner is a console application that serves as the target process for debugging in NodeDev. It executes user-compiled DLLs in a separate process, providing the foundation for advanced debugging features via the ICorDebug API.

## Architecture

### Process Separation

NodeDev uses a two-process architecture for code execution:

```
┌─────────────────────────────┐
│   Host Process (IDE)        │
│  - NodeDev.Blazor.Server    │
│  - NodeDev.Blazor.MAUI      │
│  - Compiles user code       │
│  - Manages UI               │
└──────────┬──────────────────┘
           │ Launches
           ▼
┌─────────────────────────────┐
│  Target Process             │
│  - NodeDev.ScriptRunner     │
│  - Loads compiled DLL       │
│  - Executes user code       │
│  - Reports output/errors    │
└─────────────────────────────┘
```

### Benefits

1. **Process Isolation**: User code runs in a separate process, protecting the IDE from crashes
2. **Debugging Support**: Enables ICorDebug attachment for advanced debugging features
3. **Resource Management**: Easier to manage memory and resources of user code
4. **Security**: Sandboxing opportunities for untrusted code execution

## How It Works

### 1. Project Compilation

When `Project.Run()` is called:

```csharp
var assemblyPath = Build(options);  // Compiles to bin/Debug/project.exe
```

The build process creates:
- `project.exe` (or `project.dll`) - The compiled user code
- `project.pdb` - Debug symbols
- `project.runtimeconfig.json` - Runtime configuration

### 2. ScriptRunner Launch

The IDE launches ScriptRunner with the compiled DLL:

```bash
dotnet NodeDev.ScriptRunner.dll "/absolute/path/to/project.exe"
```

### 3. Assembly Loading

ScriptRunner loads the assembly:

```csharp
Assembly assembly = Assembly.LoadFrom(dllPath);
```

### 4. Entry Point Discovery

ScriptRunner searches for entry points in this order:

1. **Static Program.Main method** (in any namespace)
   ```csharp
   public static class Program
   {
       public static int Main() { ... }
   }
   ```

2. **IRunnable implementation** (future extensibility)
   ```csharp
   public class MyClass : IRunnable
   {
       public void Run() { ... }
   }
   ```

### 5. Execution

ScriptRunner invokes the entry point with proper exception handling:

```csharp
try
{
    int exitCode = InvokeEntryPoint(assembly, userArgs);
    return exitCode;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Fatal error: {ex.GetType().Name}: {ex.Message}");
    Console.Error.WriteLine($"Stack trace:\n{ex.StackTrace}");
    return 3;
}
```

## Command-Line Interface

### Usage

```bash
NodeDev.ScriptRunner <path-to-dll> [args...]
```

### Arguments

- `<path-to-dll>`: **Required**. Absolute or relative path to the compiled DLL to execute
- `[args...]`: **Optional**. Arguments to pass to the entry point (if it accepts `string[] args`)

### Exit Codes

| Code | Meaning |
|------|---------|
| 0    | Success - Program executed successfully |
| 1    | Invalid usage - Missing DLL path argument |
| 2    | File not found - DLL doesn't exist at specified path |
| 3    | Fatal error - Unhandled exception during execution |
| 4    | No entry point - No valid entry point found in assembly |
| N    | User-defined - Return value from `Program.Main()` |

### Examples

```bash
# Execute a simple program
dotnet NodeDev.ScriptRunner.dll /path/to/myprogram.dll

# Execute with arguments
dotnet NodeDev.ScriptRunner.dll /path/to/myprogram.dll arg1 arg2 "arg with spaces"
```

## Build Integration

### MSBuild Targets

ScriptRunner is automatically copied to dependent projects using MSBuild targets:

**NodeDev.Core/NodeDev.Core.csproj:**
```xml
<ItemGroup>
  <ProjectReference Include="..\NodeDev.ScriptRunner\NodeDev.ScriptRunner.csproj">
    <ReferenceOutputAssembly>false</ReferenceOutputAssembly>
  </ProjectReference>
</ItemGroup>

<Target Name="CopyScriptRunner" AfterTargets="Build">
  <ItemGroup>
    <ScriptRunnerFiles Include="..\NodeDev.ScriptRunner\bin\$(Configuration)\$(TargetFramework)\**\*.*" />
  </ItemGroup>
  <Copy SourceFiles="@(ScriptRunnerFiles)" DestinationFolder="$(OutputPath)%(RecursiveDir)" />
</Target>
```

This ensures ScriptRunner is available wherever NodeDev.Core is built.

### Location at Runtime

The `FindScriptRunnerExecutable()` method locates ScriptRunner:

1. Same directory as NodeDev.Core assembly (production)
2. Sibling directory in build output (development)

```csharp
private static string FindScriptRunnerExecutable()
{
    string coreDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    string scriptRunnerDll = Path.Combine(coreDirectory, "NodeDev.ScriptRunner.dll");
    
    if (File.Exists(scriptRunnerDll))
        return scriptRunnerDll;
    
    // Try sibling directories for development scenarios
    // ...
}
```

## Testing

### Unit Tests

ScriptRunner functionality is tested in `NodeDev.Tests/ScriptRunnerTests.cs`:

1. **ScriptRunner_ShouldExecuteSimpleProgram** - Verifies basic execution and console output
2. **ScriptRunner_ShouldHandleExceptions** - Tests graceful error handling
3. **ScriptRunner_ShouldReturnExitCode** - Validates exit code propagation

### Test Pattern

```csharp
[Fact]
public void ScriptRunner_ShouldExecuteSimpleProgram()
{
    // Arrange - Create a project with a WriteLine node
    var project = Project.CreateNewDefaultProject(out var mainMethod);
    // ... add nodes, connect them ...
    
    // Act - Run via ScriptRunner
    var result = project.Run(BuildOptions.Debug);
    
    // Assert - Verify output and behavior
    Assert.NotEmpty(consoleOutput);
    Assert.Contains(consoleOutput, line => line.Contains("ScriptRunner Test Output"));
}
```

## Future: ICorDebug Integration

ScriptRunner lays the groundwork for advanced debugging features:

### Planned Features

1. **Breakpoints**: Set breakpoints on visual nodes
2. **Step Execution**: Step through node execution one at a time
3. **Variable Inspection**: Inspect connection values at runtime
4. **Call Stack**: View the execution path through the graph
5. **Exception Catching**: Catch and inspect exceptions in the debugger

### ICorDebug API

The ICorDebug API provides:
- Process creation and attachment
- Thread control (suspend, resume)
- Breakpoint management
- Stack walking
- Variable inspection
- Exception handling

### Implementation Approach

```csharp
// Future debugging implementation (pseudocode)
var debugger = new ICorDebug();
var process = debugger.CreateProcess("dotnet", "NodeDev.ScriptRunner.dll project.exe");
process.OnBreakpoint += (sender, e) => {
    // Highlight the corresponding node in the UI
    // Show connection values
    // Enable step controls
};
```

## Troubleshooting

### ScriptRunner Not Found

**Error:** `FileNotFoundException: ScriptRunner executable not found`

**Solution:** Rebuild the solution to trigger the MSBuild copy target:
```bash
dotnet build
```

### Assembly Load Errors

**Error:** `Could not load file or assembly`

**Cause:** The compiled DLL might have missing dependencies

**Solution:** Ensure all dependencies are in the output directory with the DLL

### No Entry Point Found

**Error:** `No entry point found. Expected: - Static method Program.Main()`

**Cause:** The compiled assembly doesn't have a valid entry point

**Solution:** Verify the project has a class named "Program" with a static "Main" method

## Development Guidelines

### Adding New Entry Point Types

To add support for new entry point patterns:

1. Add detection logic to `InvokeEntryPoint()`:
   ```csharp
   // Strategy 3: Look for custom entry point
   Type? customType = assembly.GetTypes()
       .FirstOrDefault(t => t.GetCustomAttribute<EntryPointAttribute>() != null);
   ```

2. Add invocation logic
3. Update documentation and tests

### Modifying Execution Behavior

When modifying ScriptRunner:
- Keep it lightweight and fast
- Preserve exit code semantics
- Maintain backward compatibility
- Add corresponding tests
- Update this documentation

## See Also

- [Project.cs](../src/NodeDev.Core/Project.cs) - `Run()` and `FindScriptRunnerExecutable()` methods
- [Program.cs](../src/NodeDev.ScriptRunner/Program.cs) - ScriptRunner implementation
- [ScriptRunnerTests.cs](../src/NodeDev.Tests/ScriptRunnerTests.cs) - Test suite
