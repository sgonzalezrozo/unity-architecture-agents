# Project rules

Set this rule's type to **Always** in the Rider editor.

All project rules live in `.ai/AGENTS.md`. Read it before suggesting code, then open the document it points to for your task.

Non-negotiable, in case nothing else is loaded:

- Strict MVC: Models know only Models. Views know only Models and rendering components. Controllers know only Models and input interfaces.
- Events are `public event Action OnSomethingChanged;` with no parameters. Listeners pull from the Model.
- No `Button`, `Toggle`, `Slider` or `Dropdown`. Controllers inherit `BaseClickController` or implement `IPointer*` interfaces.
- MonoBehaviours receive dependencies through `internal` fields with no attribute (FastIoC). Pure C# classes use the constructor.
