# End-to-End Testing Guide

This document describes the E2E testing capabilities for NodeDev, specifically focused on testing node manipulation and connections in the visual programming interface.

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

Connections are created using the same drag-and-drop mechanism as node movement:
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
