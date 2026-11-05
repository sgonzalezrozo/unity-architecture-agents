# Copilot instructions

All project rules live in `.ai/AGENTS.md`. Read it before suggesting code, then open the document it points to for your task.

Non-negotiable, in case nothing else is loaded:

- Strict MVC: Models know only Models. Views know only Models and rendering components. Controllers know only Models and input interfaces.
- Events are `public event Action OnSomethingChanged;` with no parameters. Listeners pull from the Model.
- No `Button`, `Toggle`, `Slider` or `Dropdown`. Controllers implement `IPointer*` interfaces.
- Packages are optional. Before generating code, run capability detection from `.ai/architecture/OPTIONAL_PACKAGES.md`.
- Models sit on a common parent of their Views and Controllers, which cache them with `GetComponentInParent`. Only a Model with no common parent, or one that persists, is injected through an `internal` field. Without a container, it is a singleton. Pure C# classes use the constructor.
