# Developer overview

The summary for human developers, in one sitting. Agents use [AGENTS.md](../AGENTS.md), which holds the normative rules and their IDs.

The architecture and the code style are mandatory. Packages such as Fast IoC are optional, and code samples here use them: see [Optional packages](OPTIONAL_PACKAGES.md).

## 1. Philosophy

We use MVC, strictly. These rules are not preferences: code that breaks them is wrong even if it works.

1. **Knowledge goes one way.** Views and Controllers know Models. Models know only Models. Nobody knows a View or a Controller.
2. **Models own the truth.** All data and all state live in Models.
3. **Views render. Controllers receive input.** Neither one does the other's job, and neither one decides anything.
4. **Pull, never push.** If A knows B, A takes what it needs from B. B never hands it over.
5. **No `Button`.** It is a View and a Controller in one component, so it breaks rule 3. The same goes for `Toggle`, `Slider` and `Dropdown`. `TMP_InputField` is the only exception. Controllers implement `IPointer*` interfaces.
6. **A feature lives in one folder.** Code sits next to the prefabs built from it. Source art does not.

```
Input → Controller → Model → (event, no data) → View reads Model → View renders
```

The result: you always know where something is. A wrong value is a Model bug, a wrong pixel is a View bug, and a dead click is a Controller bug.

## 2. Who knows what

| | Models | Rendering components | Input interfaces | Views | Controllers |
|---|---|---|---|---|---|
| **Model** | Yes | No | No | No | No |
| **View** | Yes | Yes | No | No | No |
| **Controller** | Yes | No | Yes | No | No |

The only other exception: a feature's **Module**, a Model that wraps its backend calls, also knows its Connector.

## 3. The three layers

### Model

Pure C#. Holds state, applies rules, announces changes. Knows nothing about Unity UI.

```csharp
public class LivesModel
{
    private const int MAX_LIVES = 3;

    public event Action OnLivesChanged;

    private int remainingLives = MAX_LIVES;

    public int RemainingLives => remainingLives;
    public bool IsAlive => remainingLives > 0;

    public void LoseLife()
    {
        if (!IsAlive)
            return;

        remainingLives--;
        OnLivesChanged?.Invoke();
    }
}
```

- Commands change state and validate themselves. Queries read it. A method is one or the other.
- Commands may take parameters. **Events never do.**
- One responsibility. More than about 5 commands means: check whether to split it.
- Only list cells are MonoBehaviour Models, because the scroller needs them on the prefab.

### View

A MonoBehaviour that reads Models and paints. Always this same shape:

```csharp
public class LivesView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI remainingLivesText;

    internal LivesModel livesModel;

    private void OnEnable()
    {
        livesModel.OnLivesChanged += RenderRemainingLives;
        RenderRemainingLives();
    }

    private void OnDisable()
    {
        livesModel.OnLivesChanged -= RenderRemainingLives;
    }

    private void RenderRemainingLives()
    {
        remainingLivesText.text = livesModel.RemainingLives.ToString();
    }
}
```

- Subscribe and render in `OnEnable`. Unsubscribe in `OnDisable`.
- Serializes rendering components only. Never calls a Model command. Never references another View.
- `Update()` only for values that change with time, such as countdowns, and throttled.

### Controller

A MonoBehaviour that turns one input into one command.

```csharp
public class LoseLifeController : MonoBehaviour, IPointerClickHandler
{
    internal LivesModel livesModel;

    public void OnPointerClick(PointerEventData eventData)
    {
        livesModel.LoseLife();
    }
}
```

- One Controller per action, placed on an `{Action}Input` child of the element it acts on.
- Implement `IPointerClickHandler`, `IPointerDownHandler`, `IDragHandler` and so on. If the project has UI Foundations, clicks can use `BaseClickController`: see [Optional packages](OPTIONAL_PACKAGES.md).
- May convert a pointer position to a value. May not check state or decide anything.
- No serialized references. A plain config value, such as which tab it opens, is allowed.

## 4. Getting references

| | How |
|---|---|
| MonoBehaviour | With Fast IoC, an injected `internal` field. Without a container, the Model lives in the scene or prefab: use `GetComponentInParent`, cached once |
| Pure C# class | Constructor |
| Inside a list cell | `GetComponentInParent<CellModel>()`, cached once |

`internal` is reserved for injected fields; everything else is `private`. Forbidden: `[Inject]`, `Initialize(model)`, service locators. Do not null-check injected fields.

One installer per feature. Bind Models, Modules and Connectors, never Views or Controllers.

## 5. Size limits

These limits exist to keep us fast: small pieces are read, changed and reviewed quickly. **The target is what you write. The limit is a hard stop in code review.**

| | Target | Hard limit | When you reach the limit |
|---|---|---|---|
| Lines in a method | 12 | **15** | Extract a named method |
| Lines in a class | 120 | **200** | Split by responsibility |
| Parameters | 2 | **3** | Create a parameter class |
| Line width | | **120 characters** | Wrap |

- **Counting:** count the lines of the body, without blank lines and lines holding only a brace. For a class, count every line in the class.
- The limits apply to constructors and Unity methods too.
- Only tests and generated code are exempt.

## 6. How every class is laid out

Always in this order, so anyone can find anything:

1. Constants
2. Events
3. Injected fields
4. Serialized fields
5. Public fields (avoid)
6. Private fields
7. Properties
8. Constructor
9. Unity methods
10. Other methods, in order of use

## 7. How we write C#

**Naming**

| Element | Style | Example |
|---|---|---|
| Class, method, property | `PascalCase` | `TrophyRoomModule` |
| Field, local, parameter | `camelCase`, no underscore | `selectedTrophyId` |
| Constant | `ALL_CAPS` | `MAX_LIVES` |
| Event | `On` + past tense | `OnLivesChanged` |
| Async method | `Async` suffix | `LoadAsync` |

Classes are nouns, methods are verbs. Method prefixes state intent: `Get`, `Set`, `Is`/`Has`/`Can`, `Try`, `Calculate`, `Render`, `On`.

**Style**

`.editorconfig` flags most of this in Rider and Visual Studio, and the [analyzers](../analyzers) fail the build for the mechanical rules.

```csharp
public void Process(List<ItemData> items)
{
    if (items.Count == 0)
        return;

    for (int i = 0; i < items.Count; i++)
    {
        if (!items[i].isActive)
            continue;

        Apply(items[i]);
    }
}
```

- Guard clauses and early returns. No `else` after a return, no nesting pyramids.
- One statement after `if` or `for`: next line, indented, no braces.
- Braces on their own line.
- Explicit types, never `var`. `new()` when the type is on the left.
- No comments or `///`. Names explain. Comment only what code cannot, such as an engine bug.
- No `#region`. No magic numbers. No hardcoded user text.
- No boolean flag parameters that pick a code path; write two methods.
- `a.GetB()` is fine; `a.B.C.D` is not.

**Forbidden in client code**

| Never | Instead |
|---|---|
| Lambdas | Named methods |
| LINQ | `for` loops |
| `foreach` on `List<T>` | `for` |
| `?.`, `??`, `??=` on Unity objects | `if (x == null)`; they bypass Unity's destroyed check |
| `Action<T>` or `{ get; set; }` events | `public event Action OnChanged;` |
| `async void`, `.Result`, `.Wait()` | `async Task`, and `.Forget("Context")` from sync code |
| `SceneManager` directly | The project's scene loader |
| Renaming a serialized field | `[FormerlySerializedAs("oldName")]` |

**Async and errors**

- Only Controllers start async commands, with a `CancellationToken` they cancel in `OnDestroy`.
- Throw for what should not happen. Return a state or `Result<T>` for what can.
- Never swallow an exception without logging it. Prefix logs with `[FeatureName]`.

**Tests**

Anything that can be unit tested has tests: NUnit, Arrange-Act-Assert, one concern per test. Models don't read time or randomness directly, so tests can control them. Views and Controllers have no logic to test.

## 8. Where files go

```
Assets/Client/TrophyRoom/
├── MainRoom/                     Screen: its C# and prefabs, side by side
├── DetailsTrophy/
└── Services/                     Module, Connector, data, installer, ScreenDefinition
```

- No `Scripts/` or `Prefabs/` folders inside a feature.
- Source art stays in `Assets/Art/` and `Assets/Audio/`.
- A screen used by other features keeps its `ScreenDefinition` where it was first created.

## 9. Before you open a PR

- [ ] Could a Model compile without any View or Controller existing?
- [ ] Does any View handle input or call a command?
- [ ] Does any Controller touch a visual or check a rule?
- [ ] Does any event carry data? Is anything pushed that could be pulled?
- [ ] Any `Button`, `Toggle`, `Slider`, `Dropdown` or `onClick`?
- [ ] Every method 15 lines or fewer, every class 200 or fewer, 3 parameters at most?
- [ ] Class layout in the right order?
- [ ] Subscriptions and tokens cleaned up?
- [ ] Everything that can be unit tested covered?
- [ ] Could a deterministic tool have decided this?
