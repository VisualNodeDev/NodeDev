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
3) Disabling, removing, skipping, deleting, bypassing or converting to warnings ANY tests IS NOT ALLOWED and is not considered the right way of fixing a problematic test. The test must be functional and actually testing what it is intended to test.
4) Document newly added content or concepts in this `.github/agents/basicAgent.agent.md` file or any related documentation file.
5) When the user corrects major mistakes done during your development, document them in this file to ensure it is never done again.

## Programming style

1) Always use C# nullable
2) Prefer small polymorphic classes rather than big helper style classes
3) Write many tests for any newly added feature, whether in the core as unit tests or in the UI as e2e tests

## Overview
NodeDev is a visual programming environment built with Blazor and Blazor.Diagrams. It allows users to create software using a node-based visual interface instead of traditional text-based code. The system generates native IL code for near-native execution performance.

## Architecture

### Core Components
- **NodeDev.Core**: Core business logic, node definitions, graph management, type system, and IL code generation
- **NodeDev.Blazor**: UI components built with Blazor (Razor components, diagrams, project explorer)
- **NodeDev.Blazor.Server**: Server-side Blazor hosting
- **NodeDev.Blazor.MAUI**: MAUI-based desktop application wrapper
- **NodeDev.Tests**: Unit tests for core functionality
- **NodeDev.EndToEndTests**: Playwright-based E2E tests with Reqnroll (SpecFlow successor)
- **NodeDev.ScriptRunner**: Console application that executes compiled user code as a separate process, serving as the target for the ICorDebug debugging infrastructure

### UI Structure
The main UI consists of:
- **AppBar**: Top toolbar with project controls (New, Open, Save, Options)
- **ProjectExplorer**: Left panel showing project structure (classes, methods, properties)
- **GraphCanvas**: Central canvas where nodes are placed and connected
- **ClassExplorer**: Shows details of the currently selected class

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

## Documentation
Detailed topic-specific documentation is maintained in the `docs/` folder:

- `docs/e2e-testing.md` - End-to-end testing patterns, node interaction, connection testing, and screenshot validation
- `docs/node-types-and-connections.md` - Comprehensive guide to node types, connection system, port identification, and testing strategies
- `docs/script-runner.md` - ScriptRunner architecture, usage, and ICorDebug debugging infrastructure

## Debugging Infrastructure

### ScriptRunner
NodeDev includes a separate console application called **ScriptRunner** that serves as the target process for debugging. This architecture is being developed to support "Hard Debugging" via the ICorDebug API (.NET's unmanaged debugging interface).

**Architecture:**
- **Host Process**: The Visual IDE (NodeDev.Blazor.Server or NodeDev.Blazor.MAUI)
- **Target Process**: ScriptRunner - a separate console application that executes the user's compiled code

**ScriptRunner Features:**
- Accepts a DLL path as command-line argument
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

**Future: ICorDebug Integration**
This infrastructure prepares NodeDev for implementing advanced debugging features:
- Breakpoints in visual graphs
- Step-through execution
- Variable inspection at runtime
- Exception handling and catching
- Live debugging across process boundaries
