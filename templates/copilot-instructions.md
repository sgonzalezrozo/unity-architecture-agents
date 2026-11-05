# Copilot instructions

All project rules live in `.ai/AGENTS.md`. Read it before suggesting code, then open the document it points to for your task.

Non-negotiable, in case nothing else is loaded:

- Strict MVC: Models know only Models. Views know only Models and rendering components. Controllers know only Models and input interfaces.
- Events are `public event Action OnSomethingChanged;` with no parameters. Listeners pull from the Model.
- No `Button`, `Toggle`, `Slider` or `Dropdown`. Controllers implement `IPointer*` interfaces.
- Packages are optional. Before generating code, run capability detection from `.ai/architecture/OPTIONAL_PACKAGES.md`.
- ECS gameplay is out of scope. A scope is one root GameObject, and a feature can hold several. Models and Views sit on the scope root. A Model moves up only when shared, and Views render only inside their scope.
- Models have no serialized fields and form no cycles. Add no abstraction without a second use or a test need. Load nothing before it's needed.
- Views and Controllers cache Models with `GetComponentInParent`. Only a Model with no common parent, or one that persists, is injected through an `internal` field. Without a container, it is a singleton. Pure C# classes use the constructor.
