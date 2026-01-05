---
# Fill in the fields below to create a basic custom agent for your repository.
# The Copilot CLI can be used for local testing: https://gh.io/customagents/cli
# To make this agent available, merge this file into the default repository branch.
# For format details, see: https://gh.io/customagents/config

name: Basic dev agent
description: Used for general purpose NodeDev development
---

# NodeDev - Copilot Instructions

1) You must always read the documentation files when they are related to your current task. They are described in the "Documentation" section of this document.
2) You must always run the tests and make sure they are passing before you consider your job as completed, no matter how long you have been at the task or any following instruction to make it short or end the task early.
3) **CRITICAL: Disabling, removing, skipping, deleting, bypassing or converting to warnings ANY tests IS NOT ALLOWED and is not considered the right way of fixing a problematic test. The test must be functional and actually testing what it is intended to test. DO NOT REMOVE TESTS UNLESS EXPLICITLY INSTRUCTED TO DO SO BY THE USER.**
4) Document newly added content or concepts in this `.github/agents/basicAgent.agent.md` file or any related documentation file.
5) When the user corrects major mistakes done during your development, document them in this file to ensure it is never done again.
6) You must always install playwright BEFORE trying to run the tests. build the projects and install playwright. If you struggle (take multiple iterations to do it), document the steps you took in this file to make it easier next time.
7) **ALWAYS read the E2E testing documentation (`docs/e2e-testing.md`) BEFORE making any changes to E2E tests.** This documentation contains critical information about test patterns, selector strategies, and troubleshooting.
8) **When encountering E2E test issues (timeouts, element not found, etc.), ALWAYS use the Playwright MCP tools** to take screenshots and inspect the page state before assuming the test or functionality is broken. Use `playwright-browser_snapshot` and `playwright-browser_take_screenshot` to validate element visibility and page state.

## Programming style

1) Always use C# nullable
2) Prefer small polymorphic classes rather than big helper style classes
3) Write many tests for any newly added feature, whether in the core as unit tests or in the UI as e2e tests

## Overview
NodeDev is a visual programming environment built with Blazor and Blazor.Diagrams. It allows users to create software using a node-based visual interface instead of traditional text-based code. The system generates native IL code for near-native execution performance.

## Architecture

### Core Components
- **NodeDev.Core**: Core business logic, node definitions, graph management, type system, IL code generation, and debugging infrastructure (in `Debugger/` folder)
- **NodeDev.Blazor**: UI components built with Blazor (Razor components, diagrams, project explorer)
- **NodeDev.Blazor.Server**: Server-side Blazor hosting
- **NodeDev.Blazor.MAUI**: MAUI-based desktop application wrapper
- **NodeDev.Tests**: Unit tests for core functionality
- **NodeDev.EndToEndTests**: Playwright-based E2E tests with Reqnroll (SpecFlow successor)
- **NodeDev.ScriptRunner**: Console application that executes compiled user code as a separate process, serving as the target for the ICorDebug debugging infrastructure

### UI Structure
The main UI consists of:
- **AppBar**: Top toolbar with project controls (New, Open, Save, Options, Run, Run with Debug)
- **ProjectExplorer**: Left panel showing project structure (classes, methods, properties)
- **GraphCanvas**: Central canvas where nodes are placed and connected
- **ClassExplorer**: Shows details of the currently selected class
- **DebuggerConsolePanel**: Bottom panel with tabs for Console Output and Debug Callbacks

### Graph System
- Uses Blazor.Diagrams library for visual node editing
- **GraphNodeModel**: Represents a node in the diagram (wraps Core.Node)
- **GraphPortModel**: Represents input/output ports on nodes
- **GraphCanvas**: Main component managing the diagram, node creation, and connections

### Node System
- **Flow Nodes**: Control execution (Entry, Return, Branch, While, For, etc.) - have Exec connections
- **Data Nodes**: Compute values without flow (Math operations, comparisons) - no Exec connections
- **Mixed Nodes**: Both Exec and data (DeclareVariable, SetProperty, etc.)
- **Generic Types**: Automatically resolve based on connections (T, T1, T2 → concrete types)

## Testing
- E2E tests use Playwright with Reqnroll for BDD-style scenarios
- Tests start a Blazor Server instance and automate browser interactions
- Components use `data-test-id` attributes for test targeting
- Mouse event sequences critical for drag operations: MoveAsync → DownAsync → MoveAsync(with steps) → UpAsync

### Installing Playwright for E2E Tests
Before running E2E tests, Playwright browsers must be installed:

1. Build the test project first:
   ```bash
   dotnet build src/NodeDev.EndToEndTests/NodeDev.EndToEndTests.csproj
   ```

2. Install Playwright Chromium browser:
   ```bash
   cd src/NodeDev.EndToEndTests
   pwsh bin/Debug/net10.0/playwright.ps1 install chromium
   ```

3. The browser will be installed to `~/.cache/ms-playwright/`

**Important**: Playwright must be reinstalled after:
- Cleaning the build output
- Updating the Playwright NuGet package
- Running tests in a fresh CI environment

Without Playwright installed, all E2E tests will fail with browser launch errors.

### ICorDebug Testing Requirements
The debugger integration tests require the `dbgshim` library to be available. This is obtained automatically via the **Microsoft.Diagnostics.DbgShim** NuGet package which is referenced by NodeDev.Core.

**How it works:**
- The `Microsoft.Diagnostics.DbgShim` NuGet package (v9.0.652701) is included as a dependency in NodeDev.Core
- When the package is restored, it downloads platform-specific native libraries:
  - `microsoft.diagnostics.dbgshim.linux-x64` for Linux x64
  - `microsoft.diagnostics.dbgshim.win-x64` for Windows x64
  - And other platform variants
- The `DbgShimResolver` class automatically finds the library in the NuGet packages cache

**Location of dbgshim library:**
- NuGet cache: `~/.nuget/packages/microsoft.diagnostics.dbgshim.linux-x64/[version]/runtimes/linux-x64/native/libdbgshim.so`
- Windows: `~/.nuget/packages/microsoft.diagnostics.dbgshim.win-x64/[version]/runtimes/win-x64/native/dbgshim.dll`

**Running the debugger tests:**
```bash
# Build and restore packages
dotnet build src/NodeDev.Tests/NodeDev.Tests.csproj

# Run debugger tests
dotnet test src/NodeDev.Tests/NodeDev.Tests.csproj --filter "DebuggerCoreTests"
```

The tests demonstrate:
1. Finding dbgshim from the NuGet package
2. Loading the dbgshim library
3. Creating a NodeDev project with nodes
4. Building the project
5. Launching the process in suspended mode
6. Registering for CLR runtime startup
7. Enumerating CLRs in the target process
8. Attaching to the process and obtaining ICorDebug interface

## Documentation
Detailed topic-specific documentation is maintained in the `docs/` folder:

- `docs/e2e-testing.md` - End-to-end testing patterns, node interaction, connection testing, and screenshot validation
- `docs/node-types-and-connections.md` - Comprehensive guide to node types, connection system, port identification, and testing strategies
- `docs/script-runner.md` - ScriptRunner architecture, usage, and ICorDebug debugging infrastructure

## Debugging Infrastructure

### Hard Debugging (ICorDebug)
NodeDev now supports "Hard Debugging" via the ICorDebug API (.NET's unmanaged debugging interface). This provides low-level debugging capabilities including:
- Process attachment and management
- Debug event callbacks (process creation, module loading, thread creation, etc.)
- Future support for breakpoints and step-through execution

**Running with Debug:**
The UI provides two run modes:
1. **Run** - Normal execution without debugger attachment
2. **Run with Debug** - Executes with ICorDebug debugger attached

**Important**: "Run with Debug" requires successful debugger attachment. If attachment fails for any reason (DbgShim not found, CLR enumeration fails, etc.), the operation will fail with an error dialog showing the specific issue. There is no fallback to normal execution.

**Debug State Management:**
- `Project.IsHardDebugging` - Boolean property indicating active debug session
- `Project.DebuggedProcessId` - Process ID of debugged process (null when not debugging)
- `Project.HardDebugStateChanged` - Observable stream for debug state changes (true when attached, false when detached)
- `Project.DebugCallbacks` - Observable stream of `DebugCallbackEventArgs` for all debug events

**UI Visual Feedback:**
- "Run with Debug" button changes color (warning) and shows PID when debugging
- Button is disabled during active debug session
- DebuggerConsolePanel shows two tabs:
  - "Console Output" - Standard output from the program
  - "Debug Callbacks" - Real-time debug events with timestamps

**Implementation Pattern:**
```csharp
// Running with debug in Project.cs
try 
{
    var result = project.RunWithDebug(BuildOptions.Debug);
}
catch (InvalidOperationException ex)
{
    // Handle debug attachment failure
    // Show error dialog to user with ex.Message
    Console.WriteLine($"Debug failed: {ex.Message}");
}

// Subscribing to debug callbacks
project.DebugCallbacks.Subscribe(callback => {
    Console.WriteLine($"{callback.CallbackType}: {callback.Description}");
});

// Subscribing to debug state changes
project.HardDebugStateChanged.Subscribe(isDebugging => {
    if (isDebugging)
        Console.WriteLine("Debugging started");
    else
        Console.WriteLine("Debugging stopped");
});
```

### Debugger Module (NodeDev.Core.Debugger)
The debugging infrastructure is located in `src/NodeDev.Core/Debugger/` and provides ICorDebug API access via the ClrDebug NuGet package:

- **DbgShimResolver**: Cross-platform resolution for the dbgshim library from NuGet packages or system paths
- **DebugSessionEngine**: Main debugging engine with process launch, attach, and callback handling
- **ManagedDebuggerCallbacks**: Implementation of ICorDebugManagedCallback interfaces via ClrDebug
- **DebugEngineException**: Custom exception type for debugging errors
- **NodeBreakpointInfo**: Maps nodes to their generated source code locations for breakpoint resolution
- **BreakpointMappingInfo**: Collection of all breakpoint information for a compiled project

**Dependencies:**
- `ClrDebug` (v0.3.4): C# wrappers for the unmanaged ICorDebug API
- `Microsoft.Diagnostics.DbgShim` (v9.0.652701): Native dbgshim library for all platforms

### Breakpoint System
NodeDev supports setting breakpoints on nodes during debugging. The system tracks node-to-source-line mappings during compilation:

**Infrastructure:**
1. **Node Marking**: Nodes are marked with `BreakpointDecoration` (only non-inlinable nodes support breakpoints)
2. **Line Tracking**: `RoslynGraphBuilder.BuildStatementsWithBreakpointTracking()` tracks which source line each node generates
3. **Compilation**: `RoslynNodeClassCompiler` collects all breakpoint mappings into `BreakpointMappingInfo`
4. **Storage**: Project stores breakpoint mappings after build for use during debugging
5. **Debug Engine**: `DebugSessionEngine` receives breakpoint mappings and attempts to set breakpoints after modules load

**Implementation:**
- ✅ Node breakpoint marking and persistence
- ✅ #line directives with virtual line numbers for stable mapping
- ✅ PDB sequence point reading for accurate IL offset resolution
- ✅ Breakpoint mapping storage in compilation results
- ✅ Debug engine infrastructure for breakpoint management
- ✅ Actual ICorDebug breakpoint setting with `ICorDebugFunction.CreateBreakpoint()`
- ✅ Execution pauses at breakpoints and resumes with Continue()

**How It Works:**
1. UI allows toggling breakpoints on nodes (F9 or toolbar button)
2. Breakpoints persist across save/load
3. Compilation adds #line directives with virtual line numbers (10000, 11000, 12000...)
4. PDB sequence points are read to map virtual lines to exact IL offsets
5. Debug engine creates actual ICorDebug breakpoints at precise locations
6. Execution pauses when breakpoints are hit
7. User can resume with Continue()

### ScriptRunner
NodeDev includes a separate console application called **ScriptRunner** that serves as the target process for debugging. This architecture supports "Hard Debugging" via the ICorDebug API.

**Architecture:**
- **Host Process**: The Visual IDE (NodeDev.Blazor.Server or NodeDev.Blazor.MAUI)
- **Target Process**: ScriptRunner - a separate console application that executes the user's compiled code

**ScriptRunner Features:**
- Accepts a DLL path as command-line argument
- Optional `--wait-for-debugger` flag to pause execution until debugger attaches
- Loads assemblies using `Assembly.LoadFrom()`
- Finds and invokes entry points:
  - Static `Program.Main` method (in any namespace)
  - Types implementing `IRunnable` interface (future extensibility)
- Wraps execution in try/catch blocks to print exceptions to console
- Proper exit code handling for CI/CD integration

**Build System:**
- ScriptRunner is automatically built with NodeDev.Core
- MSBuild targets copy ScriptRunner to the output directory of dependent projects
- The `Project.Run()` method automatically locates and launches ScriptRunner
- The `Project.RunWithDebug()` method launches ScriptRunner and attaches debugger
- The `Project.GetScriptRunnerPath()` method returns the ScriptRunner location for debugging infrastructure
