# Copilot instructions

All project rules live in `.ai/AGENTS.md`. Read it before suggesting code, then open the document it points to for your task.

Non-negotiable, in case nothing else is loaded:

- Strict MVC: Models know only Models. Views know only Models and rendering components. Controllers know only Models and input interfaces.
- Events are `public event Action OnSomethingChanged;` with no parameters. Listeners pull from the Model.
- No `Button`, `Toggle`, `Slider` or `Dropdown`. Controllers implement `IPointer*` interfaces.
- Packages are optional. Before generating code, run capability detection from `.ai/architecture/OPTIONAL_PACKAGES.md`.
- With a container, MonoBehaviours receive Models through `internal` fields. Without one, they cache them with `GetComponentInParent`. Pure C# classes use the constructor.
