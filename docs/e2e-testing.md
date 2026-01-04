# End-to-End Testing Guide

This document describes the E2E testing capabilities for NodeDev, specifically focused on testing node manipulation and connections in the visual programming interface.

## ⚠️ CRITICAL: Always Validate Test Logs

**IMPORTANT**: When running E2E tests, you MUST always check the test output logs to ensure tests actually ran correctly. 

### How to Validate Tests Properly

1. **Check for "No matching step definition" warnings** - These indicate step definitions are missing
2. **Verify step execution** - Look for `-> done:` messages showing each step executed
3. **Check position changes** - Validate `Current position`, `Position after drag`, and `Movement delta` in logs
4. **Verify connection operations** - Look for `Connecting ports:` messages with port coordinates
5. **Ensure no steps were skipped** - Look for `-> skipped because of previous errors` which indicates failures
6. **Monitor browser console errors** - Tests now capture and report console errors during test execution

### Example of Proper Test Log Validation

```
✅ CORRECT - Test actually ran:
Current position of Return: (370, 168)
Position after drag: (670, 168)
Movement delta: (300, 0)
Connecting ports: Entry.Exec -> Return.Exec
-> done: NodeManipulationStepDefinitions.WhenIConnectTheOutputToTheInput("Entry", "Exec", "Return", "Exec") (1.2s)
  Passed CreateConnectionBetweenEntryAndReturnNodes [5 s]

❌ INCORRECT - Test was skipped:
When I connect the 'Entry' 'Exec' output to the 'Return' 'Exec' input
-> skipped because of previous errors
  Skipped CreateConnectionBetweenEntryAndReturnNodes [0 s]
```

## .NET 10 Upgrade Findings

### Changes Required for .NET 10 Compatibility

The upgrade to .NET 10 required the following changes:
- Updated all project files from `net9.0` to `net10.0`
- Changed `MudDialogInstance` to `IMudDialogInstance` in all dialog components (MudBlazor API change)
- Updated test dependencies and Playwright browser version (1148 → 1200)
- **CRITICAL**: Must reinstall Playwright browsers after upgrade: `pwsh bin/Debug/net10.0/playwright.ps1 install --with-deps`

### Known Issues After Upgrade

1. **SignalR Connection Errors**: After test completion, Blazor SignalR shows connection errors when the server shuts down. These are expected and don't affect test validity.
   - Error: `Connection disconnected with error 'Error: WebSocket closed with status code: 1006'`
   - These occur AFTER tests complete and can be ignored

2. **Browser Console Monitoring**: New test scenario added to detect frontend errors during method opening. The test passes, confirming no errors occur during normal UI operations.

3. **Playwright Browser Compatibility**: The .NET 10 upgrade changes Playwright browser version. After upgrading, you MUST run:
   ```bash
   cd src/NodeDev.EndToEndTests
   pwsh bin/Debug/net10.0/playwright.ps1 install --with-deps
   ```

### Test Coverage for .NET 10

**Core Functionality Tests:**
- ✅ All existing tests pass on .NET 10
- ✅ Node movement and dragging works correctly
- ✅ Connection creation between ports works correctly  
- ✅ Method opening in UI works without console errors
- ✅ Graph canvas renders properly after method opening

**NEW: Comprehensive UI Tests (16 scenarios):**
- ✅ Class operations and selection
- ✅ Method listing and text display integrity
- ✅ Text overlap detection
- ✅ Multiple method opening
- ✅ Class switching
- ✅ Console error monitoring during all operations
- ⚠️ Node adding/deletion (requires implementation)
- ⚠️ Connection deletion (requires implementation)
- ⚠️ Generic type color changes (requires implementation)
- ⚠️ Class renaming (requires implementation)

**Test Results:** 12/16 passing (75% coverage)
- 4 tests skip functionality not yet implemented in test infrastructure
- No actual bugs found in .NET 10 upgrade

### UI Rendering Investigation

**Reported Issue:** Text overlap in method names (screenshot showed "PropMain0" overlapped)

**Test Results:** 
- Method display integrity test PASSES
- No text overlap detected in headless mode
- Method names display correctly: "int Main ()"
- Screenshots show proper rendering

**Possible Causes if Issue Persists:**
1. **Browser-specific rendering** - Issue may be specific to non-headless Chrome
2. **Timing/race condition** - UI might update incorrectly under certain conditions
3. **Cache/state issue** - May require browser cache clear
4. **Font rendering** - Different font rendering between environments

**Recommendation:** If visual bugs appear:
1. Clear browser cache and reload
2. Check browser console for errors (test monitors this)
3. Try in headless mode to verify functionality
4. Take screenshot at exact moment of issue
5. Check if issue is reproducible across multiple sessions

## Overview

NodeDev uses Playwright with Reqnroll (successor to SpecFlow) for end-to-end testing. Tests automate browser interactions to verify the entire application stack from UI to backend.

## Testing Node Movement

### Available Helper Methods

The `HomePage` class provides methods for interacting with nodes on the graph canvas:

#### `GetGraphNode(string nodeName)` 
Returns a Playwright locator for a node by its name.

#### `HasGraphNode(string nodeName)` 
Checks if a node with the given name exists on the canvas.

#### `DragNodeTo(string nodeName, int x, int y)` 
Drags a node to specific screen coordinates. Uses smooth mouse movements with multiple steps for reliable drag operations.

#### `GetNodePosition(string nodeName)` 
Returns the current (x, y) position of a node on screen.

#### `TakeScreenshot(string fileName)` 
Captures a screenshot of the current page state for visual validation.

### Example Test

```gherkin
Scenario: Move a Return node on the canvas
    Given I load the default project
    And I open the 'Main' method in the 'Program' class
    When I drag the 'Return' node by 200 pixels to the right and 100 pixels down
    Then The 'Return' node should have moved from its original position
```

### Implementation Details

- Node positions are validated by comparing coordinates before and after drag operations
- A minimum movement threshold (50 pixels) accounts for grid snapping
- Screenshots are automatically captured during drag operations for debugging
- All drag operations include delays to ensure the UI has time to process events

## Testing Connections

### Available Helper Methods

#### `GetGraphPort(string nodeName, string portName, bool isInput)` 
Returns a Playwright locator for a specific port on a node.

#### `ConnectPorts(string sourceNodeName, string sourcePortName, string targetNodeName, string targetPortName)` 
Creates a connection between two ports by dragging from source output to target input.

### Port Identification

Ports are identified using:
- **Node name**: The name displayed in the node's title
- **Port name**: The label shown next to the port
- **Input/Output**: Whether the port is on the left (input) or right (output) side

### Connection Testing Approach

### Connection Testing Approach

Connections are created using the same drag-and-drop mechanism as node movement:
1. Locate the source port (output)
2. Locate the target port (input)
3. Perform mouse drag from source to target
4. Verify connection was established

### Example Connection Test

```gherkin
Scenario: Create connection between Entry and Return nodes
    Given I load the default project
    And I open the 'Main' method in the 'Program' class
    When I move the 'Return' node away from 'Entry' node
    And I take a screenshot named 'nodes-separated'
    When I connect the 'Entry' 'Exec' output to the 'Return' 'Exec' input
    Then I take a screenshot named 'after-connection'
```

### Validating Connection Tests

**ALWAYS check test logs** for connection operations:

```
✅ Connection succeeded - Look for these in logs:
Connecting ports: Entry.Exec -> Return.Exec
Port positions: (422.4375, 231.01562) -> (671, 231.01562)
-> done: NodeManipulationStepDefinitions.WhenIConnectTheOutputToTheInput(...) (1.2s)

✅ Movement succeeded - Look for these in logs:
Current position of Return: (370, 168)
Position after drag: (670, 168)
Movement delta: (300, 0)
-> done: NodeManipulationStepDefinitions.WhenIMoveTheNodeAwayFromNode(...) (1.3s)
```

Without these log entries, the test may have been skipped or failed silently.
1. Locate the source port (output)
2. Locate the target port (input)
3. Perform mouse drag from source to target
4. Verify connection was established

## Test Data and Setup

### Default Project Structure

When loading the default project:
- A `Program` class is created
- Contains a `Main` method
- The method graph includes:
  - An `Entry` node (execution start point)
  - A `Return` node (execution end point)

### Test IDs

Components are marked with `data-test-id` attributes for reliable selection:
- `graph-canvas`: The main graph canvas
- `graph-node`: Individual nodes (with `data-test-node-name` for the node name)
- Graph ports are located by CSS class and port name

### MudBlazor Component Selectors

**IMPORTANT**: MudBlazor components like `MudTabPanel` do NOT forward custom attributes like `data-test-id` to the rendered HTML. For these components, use CSS classes instead:

```razor
<!-- WRONG - data-test-id won't work on MudTabPanel -->
<MudTabPanel Text="Console Output" data-test-id="consoleOutputTab">

<!-- CORRECT - use Class attribute instead -->
<MudTabPanel Text="Console Output" Class="consoleOutputTab">
```

In tests, select by CSS class:
```csharp
// Use CSS class selector for MudBlazor components that don't forward data-test-id
var consoleOutputTab = Page.Locator(".consoleOutputTab");
```

**Always verify your selectors work** by using Playwright tools to inspect the page:
1. Use `playwright-browser_snapshot` to see the accessibility tree
2. Use `playwright-browser_take_screenshot` to visually inspect the page
3. If a selector doesn't find elements, the attribute may not be rendered - check the actual HTML

## Running Tests

### Locally
```bash
cd src/NodeDev.EndToEndTests
HEADLESS=true dotnet test --verbosity normal
```

### In CI
Tests run automatically in GitHub Actions with headless mode enabled.

## Best Practices

1. **Use descriptive node names**: Tests rely on node names for identification
2. **Allow sufficient delays**: UI updates are async, include appropriate waits
3. **Validate with screenshots**: Capture screenshots during critical operations
4. **Test incrementally**: Start with simple movements before complex scenarios
5. **Account for grid snapping**: Node positions may snap to grid, use tolerance in assertions
6. **ALWAYS read this documentation BEFORE modifying E2E tests**
7. **NEVER skip, disable, or remove tests** - fix the underlying issue instead

## Troubleshooting

### ⚠️ IMPORTANT: Always Use Playwright Tools First

**When encountering any E2E test issues (timeout, element not found, assertion failures), ALWAYS use the Playwright MCP tools to diagnose before assuming the test or functionality is broken:**

1. **`playwright-browser_snapshot`** - Get accessibility tree of current page state
2. **`playwright-browser_take_screenshot`** - Capture visual screenshot to see actual UI state
3. **`playwright-browser_navigate`** - Manually navigate to test the UI
4. **`playwright-browser_click`** - Test interactions manually

These tools help you:
- Verify elements exist and are visible
- See the actual HTML/CSS classes rendered (important for MudBlazor components)
- Understand timing issues by inspecting state at specific moments
- Validate selectors before assuming they're correct

### Nodes Don't Move
- Verify the node name matches exactly (case-sensitive)
- Check if node is visible on canvas before dragging
- Increase delays in drag operation if UI is slow to respond

### Ports Not Found
- Ensure port name matches the displayed label
- Verify the node has the expected ports (check node definition)
- Use screenshot to verify port visibility

### Tests Timeout
- Increase Playwright timeout settings if needed
- Check server startup logs for errors
- Verify network connectivity to localhost server
