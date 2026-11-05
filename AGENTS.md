# AGENTS.md

Entry point for developers and AI assistants. Read this file completely, then open only the document that matches your task.

Human developers: start with the [Development Bible](architecture/DEV_BIBLE.md), a one-file summary of everything below.

Each rule has one home. If two documents ever disagree, the document listed for that topic in [Where to read next](#where-to-read-next) wins, and the other one is a bug to fix.

## Stack

- Unity, C#, uGUI, TextMeshPro
- FastIoC for dependency injection (not Zenject)
- RecyclableScroller for lists, ViewStateMachine for visual states, Addressables for large assets
- Git flow: `feature/*` and `bugfix/*` branch from `develop`

## Philosophy

We use MVC, strictly. The rules below are not preferences; code that breaks them is wrong even if it works.

1. **Knowledge is one-way.**
   - A Model knows only other Models.
   - A View knows only Models, plus the Unity rendering components it displays.
   - A Controller knows only Models, plus the Unity input interfaces it implements.
   - Views and Controllers never know each other.
2. **Models own data and state.** All of it. Views and Controllers hold no state beyond cached references.
3. **Views render, Controllers receive input.** A View never handles input and never calls a command on a Model. A Controller never touches anything visual.
4. **Pull, never push.** If A knows B, A takes what it needs from B. B never hands it over. Events therefore have no parameters: the event says "something changed", the listener reads the Model.
5. **No Selectable components.** `Button`, `Toggle`, `Slider` and `Dropdown` are a View and a Controller in one component, so we do not use them. Controllers implement `IPointer*` / `IDrag*` interfaces instead. `TMP_InputField` is the only exception.
6. **Everything for a feature lives together.** Code and the prefabs built from it share a folder. Art source files do not.

```
Input → Controller → Model → (event, no data) → View reads Model → View renders
```

## Where to read next

| Task | Read |
|---|---|
| Any Model, View or Controller | [MVC](architecture/MVC.md) |
| New feature, new screen, modal, where a file goes, naming | [Feature structure](architecture/FEATURE_STRUCTURE.md) |
| Bindings, installers, how a class gets its references | [Dependency injection](architecture/DEPENDENCY_INJECTION.md) |
| Anything that talks to the backend | [Backend](architecture/BACKEND.md) |
| Clickable elements, visual states, tabs, lists, layouts | [UI patterns](architecture/UI_PATTERNS.md) |
| C# style, performance, async, logging, tests | [C# standards](architecture/CSHARP_STANDARDS.md) |
| Working as an AI assistant, Definition of Done, Git | [AI workflow](architecture/AI_WORKFLOW.md) |

Complete examples (copy from these first):

| Example | Shows |
|---|---|
| [Trophy Room](architecture/examples/01_TROPHY_ROOM.md) | Real folder layout, Module, Connector, list, async action, timer, tests |
| [Settings](architecture/examples/02_SETTINGS.md) | Replacing Toggle and Slider, one reusable prefab, drag input |
| [Inventory](architecture/examples/03_INVENTORY.md) | Tabs, filtered list, selection, details panel, no backend |

## Project APIs: never invent them

These are project or package types (the `com.sgonzalezrozo.*` packages). Their documented surface is in the linked docs. For anything else, read the source or ask. Never guess a method name.

`BaseClickController`, `ViewStateMachine`, `RecyclableScrollRect`, `IRecyclableCell<T>`, `IRecyclableDataSource<T>`, `IInjectableForChildren`, `BindingRecorder`, `MonoInstaller`, `ScreenSystem`, `ScreenDefinition`, `IScreenShow`, and `IPrefabInstantiator`.
