# NodeDev - Copilot Instructions

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

## Testing
- E2E tests use Playwright with Reqnroll for BDD-style scenarios
- Tests start a Blazor Server instance and automate browser interactions
- Components use `data-test-id` attributes for test targeting

## Documentation
Detailed topic-specific documentation is maintained in the `docs/` folder:

- `docs/e2e-testing.md` - End-to-end testing patterns, node interaction, connection testing, and screenshot validation
