# NodeDev Node Types and Connection System

## Overview
NodeDev uses a visual node-based programming system where nodes have inputs and outputs that can be connected. Understanding the different types of nodes and their connection types is critical for E2E testing.

## Node Categories

### 1. Flow Nodes
Flow nodes control program execution flow and always have **Exec** connections.

#### Entry Node
- **Purpose**: Starting point of method execution
- **Connections**:
  - Output: `Exec` (execution flow)
  - Outputs: One output per method parameter (e.g., if method has int x, there's an "x" output)
- **Color**: Red title

#### Return Node
- **Purpose**: End of method execution, returns a value
- **Connections**:
  - Input: `Exec` (execution flow)
  - Input: `Return` (value to return) - only present if method return type is not void
- **Color**: Red title

#### Other Flow Nodes
- **Branch**: If/else logic (Exec input, True/False outputs)
- **WhileNode**: Loop execution
- **ForNode**: Counted loop
- **ForeachNode**: Iterate over collection
- **TryCatchNode**: Exception handling

### 2. Data Flow Nodes (NoFlowNode)
These nodes compute values without execution flow. They are executed when their output is needed.

#### Math Operations
- **Add, Subtract, Multiply, Divide, Modulo**
  - Inputs: `a` (T1), `b` (T2)
  - Output: `c` (T3)
  - Generic types are automatically resolved based on connected types
  
- **Comparison Nodes** (BiggerThan, SmallerThan, Equals, NotEquals, etc.)
  - Inputs: Two values to compare
  - Output: boolean result

#### Variable Operations
- **DeclareVariableNode**
  - Input: `InitialValue` (T)
  - Output: `Variable` (T)
  - Has Exec input/output (it's a NormalFlowNode)
  - Generic type T is resolved based on InitialValue connection

- **GetPropertyOrField**
  - Gets a property/field value from an object
  - Inputs: Object instance
  - Output: Property/field value

- **SetPropertyOrField**
  - Sets a property/field value on an object
  - Has Exec connections (mutates state)

### 3. Mixed Nodes (NormalFlowNode)
These have both Exec and data connections.

## Connection Types

### Exec Connections
- **Visual**: Special styling (animated dashes in CSS)
- **Purpose**: Control program execution order
- **Direction**: Always output → input
- **Rule**: Only ONE Exec output can connect to ONE Exec input
- **Location**: Usually at top of node ports

### Data Connections
- **Visual**: Different colors based on type
- **Purpose**: Pass values between nodes
- **Direction**: Always output → input
- **Rule**: One output can connect to MULTIPLE inputs, but one input accepts only ONE output
- **Type System**: 
  - Strongly typed (int, string, bool, custom types, etc.)
  - Generic types (T, T1, T2, etc.) that resolve based on connections
  - UndefinedGenericType becomes concrete when connected

## Port Identification in E2E Tests

### CSS Structure
```
.diagram-node
  └─ .my-node
      └─ .row
          ├─ .col.input    (left side)
          │   ├─ .name (contains port name text)
          │   └─ .diagram-port.left
          └─ .col.output   (right side)
              ├─ .name (contains port name text)
              └─ .diagram-port.right
```

### Test Helper Method
```csharp
public ILocator GetGraphPort(string nodeName, string portName, bool isInput)
{
    var node = GetGraphNode(nodeName);
    var portType = isInput ? "input" : "output";
    return node.Locator($".col.{portType}")
               .Filter(new() { HasText = portName })
               .Locator(".diagram-port")
               .First;
}
```

## Connection Creation in E2E Tests

### Method
```csharp
public async Task ConnectPorts(string sourceNode, string sourcePort, 
                               string targetNode, string targetPort)
{
    // 1. Find source output port
    // 2. Find target input port
    // 3. Drag from source to target using mouse events
    //    - Mouse.MoveAsync to source
    //    - Mouse.DownAsync (pointerdown)
    //    - Mouse.MoveAsync to target with steps (pointermove)
    //    - Mouse.UpAsync (pointerup)
}
```

## Blazor.Diagrams Connection Handling

### DragNewLinkBehavior
Located in: `Blazor.Diagrams.Core/Behaviors/DragNewLinkBehavior.cs`

- Subscribes to PointerDown on ports
- Creates temporary link while dragging
- On PointerUp, validates and creates permanent connection
- Checks port compatibility (output→input, type matching)

### Connection Validation
- Source must be an output port
- Target must be an input port
- Type compatibility check (unless UndefinedGenericType)
- No duplicate connections to same input

## Testing Strategy

### Basic Connection Test
1. Create/Load project with nodes
2. Optionally move nodes apart for visibility
3. Take screenshot BEFORE connection
4. Drag from source output to target input
5. Take screenshot AFTER connection
6. Verify connection visually or through DOM

### Complex Connection Test
1. Test different node types (Flow + Data)
2. Test generic type resolution (connect int to T)
3. Test multiple connections from one output
4. Test that second connection to input replaces first

## Common Node Combinations for Testing

### Example 1: Variable Declaration and Return
```
Entry (Exec) → DeclareVariable (Exec)
DeclareVariable (InitialValue) ← [Constant/Input]
DeclareVariable (Variable) → Return (Return value)
DeclareVariable (Exec) → Return (Exec)
```

### Example 2: Math Operation
```
Entry outputs → Add (a, b)
Add (c) → Return (Return value)
Entry (Exec) → Return (Exec)
```

## Files to Reference

- Node definitions: `/src/NodeDev.Core/Nodes/`
- Connection class: `/src/NodeDev.Core/Connections/Connection.cs`
- Type system: `/src/NodeDev.Core/Types/`
- Blazor.Diagrams behaviors: `/src/Blazor.Diagrams/src/Blazor.Diagrams.Core/Behaviors/`

## Key Insights for E2E Testing

1. **Exec connections are special** - they control flow, styled differently, only one per input
2. **Generic types resolve dynamically** - connecting an int to T makes T become int
3. **Port colors indicate type** - visual feedback for type compatibility
4. **Grid snapping applies** - node positions snap to 30px grid
5. **Event sequence matters** - PointerDown→PointerMove→PointerUp must fire in order
6. **Steps parameter crucial** - Mouse.MoveAsync needs steps > 1 for proper drag detection

## Future Testing Considerations

- Test invalid connections (output→output, incompatible types)
- Test connection removal (click and drag away, or delete key)
- Test reconnection (dragging existing connection to new port)
- Test multi-select and bulk operations
- Test undo/redo of connections
