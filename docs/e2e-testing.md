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

## Troubleshooting

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
