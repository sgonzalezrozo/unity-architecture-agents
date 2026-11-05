# AGENTS.md

Normative entry point for coding agents working on a Unity project. Read this file completely, then load only the documents the routing table selects for the task.

- **Requirement keywords:** MUST, MUST NOT, SHOULD and MAY are used as defined in RFC 2119.
- **Rule IDs:** every rule has a stable ID. Cite the ID when you report or discuss a violation.
- **Source of truth:** each rule has one home. If two documents disagree, the document routed for that topic wins, and the conflict is a defect to report.
- **Human summary:** [Developer overview](architecture/DEVELOPER_OVERVIEW.md).

## What is mandatory

| Scope | Status |
|---|---|
| Architecture invariants in this file | **Mandatory in every project** |
| [MVC](architecture/MVC.md) layer rules | **Mandatory in every project** |
| [C# standards](architecture/CSHARP_STANDARDS.md): code form, style and format | **Mandatory in every project** |
| Unity, C#, uGUI, TextMeshPro | Required runtime |
| Fast IoC, Recyclable Scroller, Screen System, UI Foundations, Addressables | **Optional.** They change the implementation, never the architecture |

Run [capability detection](architecture/OPTIONAL_PACKAGES.md#capability-detection) before generating code.

## Architecture invariants

Strict MVC. Code that violates an invariant is incorrect even if it compiles and runs.

| ID | Rule | Enforced by |
|---|---|---|
| `ARCH-01` | Dependencies are one-way. A Model MAY reference only Models. A View MAY reference only Models and rendering components. A Controller MAY reference only Models and input interfaces. Views and Controllers MUST NOT reference each other. | `UA0001` |
| `ARCH-02` | Models own all data and state. Views and Controllers MUST hold no state beyond cached references. | Review |
| `ARCH-03` | Views render and Controllers receive input. A View MUST NOT handle input or call a Model command. A Controller MUST NOT change anything visual. | `UA0002`, `UA0003`, review |
| `ARCH-04` | State flows by pull. If A references B, A reads from B and B never sends data to A. Events MUST have no parameters: they signal a change, and the listener reads the Model. | `UA0004` |
| `ARCH-05` | `Selectable` components MUST NOT be used: `Button`, `Toggle`, `Slider` and `Dropdown` combine View and Controller. Controllers implement `IPointer*` and `IDrag*` interfaces. `TMP_InputField` is the only exception. | `UA0005` |
| `ARCH-06` | Feature code and the prefabs built from it share a folder. Source art does not. | Review |

```
Input event → Controller → Model command → state change → parameterless event → View reads Model → View renders
```

## Document routing

| Task | Load |
|---|---|
| Any Model, View or Controller | [MVC](architecture/MVC.md) |
| Clicks, visual states, tabs, lists, layouts | [UI patterns](architecture/UI_PATTERNS.md) |
| Any dependency on a package, or code that differs by package | [Optional packages](architecture/OPTIONAL_PACKAGES.md) |
| New feature or screen, file location, naming, prefab hierarchy | [Feature structure](architecture/FEATURE_STRUCTURE.md) |
| Reference acquisition, composition roots, Fast IoC bindings | [Dependency injection](architecture/DEPENDENCY_INJECTION.md) |
| Backend calls | [Backend integration](architecture/BACKEND_INTEGRATION.md) |
| C# style, performance, async, logging, tests | [C# standards](architecture/CSHARP_STANDARDS.md) |
| Task process, checklist, Definition of Done, Git | [Agent workflow](architecture/AGENT_WORKFLOW.md) |

Read the closest example before generating a feature:

| Example | Demonstrates |
|---|---|
| [Backend feature](architecture/examples/01_BACKEND_FEATURE_TROPHY_ROOM.md) | Folder layout, Module, Connector, list, async command, timer, tests |
| [Custom input](architecture/examples/02_CUSTOM_INPUT_SETTINGS.md) | `Toggle` and `Slider` replacements, reusable prefab, drag input |
| [Filtered list](architecture/examples/03_FILTERED_LIST_INVENTORY.md) | Tabs, filtered list, selection, details panel, no backend |

## Completion

A task is complete only when it passes the [pre-response checklist](architecture/AGENT_WORKFLOW.md#pre-response-checklist) and the [Definition of Done](architecture/AGENT_WORKFLOW.md#definition-of-done). Report what was verified and what wasn't.

## API policy

MUST NOT invent an API. The documented surface of project packages is in the linked documents. For anything else, read the package source or ask. Verify each symbol below before use:

`RecyclableScrollRect<T>`, `IRecyclableCell<T>`, `IRecyclableDataSource<T>`, `IInjectableForChildren`, `BindingRecorder`, `MonoInstaller`, `ScreenSystem`, `ScreenDefinition`, `IScreenShow`, `IPrefabInstantiator`, `BaseClickController`, `ViewStateMachine`.
