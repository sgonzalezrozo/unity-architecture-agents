# AGENTS.md

Normative entry point for coding agents working on a Unity project. Read this file completely, then load only the documents the routing table selects for the task.

- **Requirement keywords:** MUST, MUST NOT, SHOULD and MAY are used as defined in RFC 2119.
- **Rule IDs:** every rule has a stable ID. Cite the ID when you report or discuss a violation.
- **Source of truth:** each rule has one home. If two documents disagree, the document routed for that topic wins, and the conflict is a defect to report.
- **Human summary:** [Developer overview](architecture/DEVELOPER_OVERVIEW.md).

## Scope

The architecture rules cover all client code except gameplay simulation, which is usually written in ECS. Code after a wrapper that translates ECS to view code does follow them, such as a View that uses an ECS-based tween tool. Packages and tools, such as Recyclable Scroller, don't have to follow them, and how a tool works internally is not a violation.

## What is mandatory

| Scope | Status |
|---|---|
| Architecture invariants in this file | **Mandatory in every project** |
| [MVC](architecture/MVC.md) layer rules | **Mandatory in every project** |
| [C# standards](architecture/CSHARP_STANDARDS.md): code form, style and format | **Mandatory in every project** |
| Unity, C#, uGUI, TextMeshPro | Required runtime |
| Fast IoC, Recyclable Scroller, Screen System, UI Foundations, Addressables | **Optional.** They change the implementation, never the architecture |

Run [capability detection](architecture/OPTIONAL_PACKAGES.md#capability-detection) before generating code, and [editor tool detection](architecture/EDITOR_TOOLS.md#capability-detection) before verifying it.

## Architecture invariants

Strict MVC. Code that violates an invariant is incorrect even if it compiles and runs.

| ID | Rule | Enforced by |
|---|---|---|
| `ARCH-01` | Dependencies are one-way. A Model MAY reference only Models and configuration assets. A View MAY reference only Models and rendering components. A Controller MAY reference only Models and input interfaces. Views and Controllers MUST NOT reference each other. | `UA0001` |
| `ARCH-02` | Models own all data and state. Views and Controllers MUST hold no state beyond cached references. | Review |
| `ARCH-03` | Views render and Controllers receive input. A View MUST NOT handle input or call a Model command. A Controller MUST NOT change anything visual. | `UA0002`, `UA0003`, review |
| `ARCH-04` | State flows by pull. If A references B, A reads from B and B never sends data to A. If A knows B, A MUST do all the work to get what it needs from B, and B MUST NOT prepare, format or deliver data for A. Events MUST have no parameters: they signal a change, and the listener reads the Model. | `UA0004`, review |
| `ARCH-05` | `Selectable` components MUST NOT be used: `Button`, `Toggle`, `Slider` and `Dropdown` combine View and Controller. Controllers implement `IPointer*` and `IDrag*` interfaces. `TMP_InputField` is the only exception. | `UA0005` |
| `ARCH-06` | Feature code and the prefabs built from it share a folder. Source art does not. | Review |
| `ARCH-07` | Anything that can be deterministic MUST be. Prefer a deterministic tool to agent judgment, and keep runtime logic reproducible. See [Determinism](architecture/AGENT_WORKFLOW.md#determinism). | `verify.sh`, review |
| `ARCH-08` | Anything that can be unit tested MUST have unit tests. Logic that can't be tested where it is MUST move to where it can. See [Tests](architecture/CSHARP_STANDARDS.md#tests). | `verify.sh`, review |
| `ARCH-09` | Model dependencies MUST be acyclic. If two Models need each other, what they share MUST move to a new Model that needs neither. A shared Model MUST NOT reference a feature's Model. | `UA0014` |
| `ARCH-10` | Code MUST NOT add an interface or abstraction without a second use or a test need. | Review |
| `ARCH-11` | Do the minimum that completely finishes the need. A feature not in use MUST cost nothing at runtime. See [Performance](architecture/CSHARP_STANDARDS.md#performance). | Review |

```
Input event → Controller → Model command → state change → parameterless event → View reads Model → View renders
```

## Document routing

| Task | Load |
|---|---|
| Any Model, View or Controller, scopes, where a Model lives | [MVC](architecture/MVC.md) |
| Clicks, visual states, tabs, lists, layouts | [UI patterns](architecture/UI_PATTERNS.md) |
| Any dependency on a package, or code that differs by package | [Optional packages](architecture/OPTIONAL_PACKAGES.md) |
| New feature or screen, file location, naming, prefab hierarchy | [Feature structure](architecture/FEATURE_STRUCTURE.md) |
| Reference acquisition, Fast IoC bindings | [Dependency injection](architecture/DEPENDENCY_INJECTION.md) |
| Backend calls | [Backend integration](architecture/BACKEND_INTEGRATION.md) |
| C# style, performance, async, logging, tests | [C# standards](architecture/CSHARP_STANDARDS.md) |
| Task process, checklist, Definition of Done, Git | [Agent workflow](architecture/AGENT_WORKFLOW.md) |
| Verifying a change, Unity MCP or Unity CLI tools, `verify.sh` | [Editor tools](architecture/EDITOR_TOOLS.md) |

Read the closest example before generating a feature:

| Example | Demonstrates |
|---|---|
| [Backend feature](architecture/examples/01_BACKEND_FEATURE_TROPHY_ROOM.md) | Folder layout, Module, Connector, list, async command, timer, tests |
| [Custom input](architecture/examples/02_CUSTOM_INPUT_SETTINGS.md) | `Toggle` and `Slider` replacements, reusable prefab, drag input |
| [Filtered list](architecture/examples/03_FILTERED_LIST_INVENTORY.md) | Tabs, filtered list, selection, details panel, no backend |

## Completion

A task is complete only when it passes the [pre-response checklist](architecture/AGENT_WORKFLOW.md#pre-response-checklist) and the [Definition of Done](architecture/AGENT_WORKFLOW.md#definition-of-done). Report what each tool verified and what wasn't verified ([Editor tools](architecture/EDITOR_TOOLS.md#reporting-back)).

## API policy

MUST NOT invent an API. The documented surface of project packages is in the linked documents. For anything else, read the package source or ask. Verify each symbol below before use:

`RecyclableScrollRect<T>`, `IRecyclableCell<T>`, `IRecyclableDataSource<T>`, `IInjectableForChildren`, `BindingRecorder`, `MonoInstaller`, `ScreenSystem`, `ScreenDefinition`, `IScreenShow`, `IPrefabInstantiator`, `BaseClickController`, `ViewStateMachine`.
