# NodeDev.Blazor maintainability audit

Audit date: 2026-08-19

This document records the follow-up work from the maintainability review of `src/NodeDev.Blazor`.

## Architectural decision

`ProjectService` intentionally remains a singleton. The application treats the project as persistent application state so that reloading the page does not create a new project. Do not change this lifetime without first introducing another persistence/session-restoration design.

## Addressed in the initial cleanup

- `SourceViewer` owns and disposes its graph subscription and cancellation state. Source generation runs outside the renderer thread, is serialized, and cleans its temporary build directory.
- Class and method deletion now mutate the core project model. Deletion is refused when it would leave known class or method references dangling.
- Class rename now goes through a domain operation rather than assigning `NodeClass.Name` in the component.
- Project paths are constructed by `ProjectService`, reject invalid/traversal names, stay under the configured projects directory, and are written atomically. Save As commits the new project name only after a successful write.
- `GraphCanvas` was split into the requested collaborators:
  - `GraphDiagramProjection`
  - `GraphDiagramSynchronizer`
  - `GraphPopupState`
  - `GraphDebugVisualizer`
- The rest of the graph interaction logic remains in `GraphCanvas`.

## Deferred high-value improvements

### Canvas performance and coupling

- `GraphDiagramSynchronizer` still rebuilds the full diagram for graph changes that request a UI refresh. Introduce indexed, incremental updates using mappings such as `Node -> GraphNodeModel`, `Connection -> GraphPortModel`, and `ScopeId -> LambdaGroupModel`.
- `Graph.GraphCanvas` still points from the domain graph to a live UI adapter. Consider replacing this with domain change notifications or a separately owned adapter.
- Open method tabs are not closable and `KeepPanelsAlive` keeps every opened canvas and its subscriptions alive. Add tab closing and decide whether inactive canvases should remain active.
- Replace the fixed `Task.Delay(100)` used before initial canvas projection with an explicit diagram/container-ready signal if the diagrams library exposes one.

### Application state and commands

- Project changes currently force `NavigationManager.Refresh(true)`. Replace the hard reload with an explicitly reset workspace state keyed by project identity.
- Toolbar, dialogs, and explorers directly invoke core operations and each implement their own error/snackbar behavior. A workspace command service could centralize build, run, save, rename, and delete commands.
- `ProjectToolbar` creates raw `Thread` instances for run/debug, while Build is synchronous on the UI thread. Replace these with managed tasks, busy state, cancellation, concurrency guards, and consistent exception reporting.

### Reactive and lifecycle cleanup

- Replace or thoroughly test the custom `Utility.AcceptThenSample` operator. Its timer, stopwatch, and flags are not serialized and can race when source and timer callbacks overlap.
- Audit diagram/link/vertex event handlers for explicit detachment during disposal, especially anonymous handlers that cannot currently be unsubscribed.
- `GraphNodeModel.OnNodeExecuting` uses a delayed fire-and-forget animation without cancellation. Give debug visualization a lifetime token so delayed work cannot update disposed models.
- `DebuggedPathView` should marshal service event notifications through `InvokeAsync` and await its event callbacks.

### Console rendering

- `DebuggerConsolePanel` can render up to 10,000 spans every 100 ms. Virtualize the output or cap the rendered window while retaining a larger backing buffer.
- Make console buffer mutation thread-safe.
- Parse `\n`, `\r\n`, and multiple lines per input chunk correctly.
- Avoid replacing the process-wide `Console.Out`; route output through the project's existing console observable.

### Duplication and component cleanup

- Consolidate the duplicated breakpoint-toggle implementation in `GraphCanvas`.
- Consolidate type-selector result handling used by `ClassExplorer` and `EditMethodMenu` around a single `TypeBase` result contract.
- Extract the repeated graph popup overlay markup into one component or one overlay branch.
- Consider a shared small name-entry dialog for create-class, create-method, and rename flows while leaving domain validation in core commands.
- Remove obsolete or unused members, including `ClassExplorer.ShowAddMethodMenu`, `ProjectExplorer.HoveredClass`, unused selector position parameters, unused component parameters/injections, and unused path-highlight methods if no planned caller exists.
- Placeholder toolbar actions (Add, Export, Pause) should either be implemented, disabled with an explicit explanation, or removed from the primary toolbar.

### Styling and standards

- Move repeated inline styles and popup geometry into component-scoped `.razor.css` files. The initial audit found 51 inline style declarations.
- Add a root `.editorconfig` and adopt one naming/formatting convention for C# and Razor.
- Replace obsolete synchronous MudBlazor `DialogService.Show` calls with `ShowAsync`.
- Resolve MudBlazor analyzer warnings for unsupported `Title` attributes, typically using tooltips or supported accessibility attributes.
- Once the warning baseline is clean, enforce warnings in CI.

### Tests to add

- Component/controller tests for subscription disposal and project switching.
- Diagram projection and synchronization tests, including suppression restoration after exceptions.
- Popup state-transition tests.
- Explorer tests proving deletion/rename behavior remains synchronized with serialization.
- Source-generation cancellation and cleanup tests.
- Console multiline parsing and concurrent-write tests.
- Tests for `AcceptThenSample` boundary timing and concurrent emissions.
- Large-graph update benchmarks or performance tests.

## Baseline from the audit

- `src/NodeDev.Blazor` originally contained 39 files and approximately 4,631 lines.
- `GraphCanvas.razor.cs` originally contained 1,153 lines, approximately 25% of the project.
- The project built successfully before the cleanup.
- The pre-cleanup unit baseline was 143 passing tests.
