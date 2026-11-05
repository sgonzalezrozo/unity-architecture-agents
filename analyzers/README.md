# Analyzers

**Roslyn analyzers that turn the mechanical architecture rules into compile errors.**

An instruction is a request, not a guarantee. These rules fail the build, whether a person or an agent wrote the code.

| Rule | Checklist item | Fails when |
|---|---|---|
| `UA0001` | 1, 2 | A Model knows a View, a Controller or a Unity component, or a View or Controller knows a View or Controller |
| `UA0002` | 2 | A View implements an `EventSystems` input interface |
| `UA0003` | 3 | A Controller serializes a reference to a Unity object |
| `UA0004` | 4 | An event has parameters |
| `UA0005` | 5 | Code uses `Button`, `Toggle`, `Slider`, `Dropdown` or another `Selectable` |
| `UA0006` | 5 | A MonoBehaviour has `Initialize` with parameters |
| `UA0007` | 6 | Code uses `[Inject]` in a project with Fast IoC |
| `UA0008` | 6 | A MonoBehaviour field isn't `private` or `internal`. With Fast IoC, an `internal` field has an attribute |
| `UA0009` | 8 | Code uses a lambda or anonymous method |
| `UA0010` | 8 | Code uses LINQ |
| `UA0011` | 8 | Code uses `var` |
| `UA0012` | 8 | Code uses `?.` on a Unity object |

Layers come from class names: `*Model` and `*Module`, `*View`, and `*Controller`. A `BaseClickController` subclass also counts as a Controller, if the project has UI Foundations. `TMP_InputField` is allowed.

The [checklist](../architecture/AGENT_WORKFLOW.md#pre-response-checklist) also covers rules a compiler can't judge, such as a View calling a command, a Controller holding a rule, file locations and hardcoded text. Those stay in code review.

## Install

You need the [.NET SDK](https://dotnet.microsoft.com/download). Close the project in Unity, then run:

```bash
.ai/analyzers/build.sh .
```

This runs the analyzer tests, builds the DLL and installs it in `Assets/Client/Analyzers`, with the `RoslynAnalyzer` label already set. Commit both files.

Unity applies an analyzer to the assembly it sits in, so the rules cover `Assets/Client` but not your tests, where lambdas are allowed. To use another assembly, pass its folder as the second argument.
