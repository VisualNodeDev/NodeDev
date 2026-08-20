---
name: coding-standard
description: >
  Apply NodeDev coding standards. Use when writing, editing, reviewing, or formatting any code in this repository.
---

# Coding Standard

Apply these rules to all C# code created or modified in this repository.

## Braces and control flow

Always put control-flow bodies on their own lines and enclose them in braces.
Do not use single-line `if`, `else`, loop, `lock`, `try`, `catch`, or
similar statements.

```csharp
if (condition)
{
    DoWork();
}
```

## Usings
Always use the new `using var xxx = ...` syntax whenever possible. Avoid `using(...) { ... }` blocks.

## Methods

Use block-bodied methods. Do not use expression-bodied methods, including
single-line methods such as `public void Something() => SomethingElse();`.

Every method must have XML documentation using triple-slash syntax. The
documentation must explain intent, behavior, constraints, side effects, or
other useful context; do not restate the method name or obvious information. Add relevant `<param>`,
`<returns>`, `<exception>`, or `<remarks>` elements when they add useful
information.

```csharp
/// <summary>
/// Rebuilds the graph index so connection lookups reflect the current nodes.
/// </summary>
/// <remarks>
/// Call this after bulk graph mutations, before resolving connections.
/// </remarks>
public void RebuildIndex()
{
    _index = BuildIndex(_nodes);
}
```

Avoid meaningless descriptions such as `Clears the values` for a method named
`ClearValues`; they do not provide information beyond the identifier.

## Properties

Expression-bodied syntax is allowed only for small, simple properties.

```csharp
public int Prop => _otherThing;
```

## Blazor

Do not put services and general classes alongside Blazor components.
For example, "Components" and "Services" should be separate folders in the project structure.