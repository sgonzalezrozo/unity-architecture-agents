# MVC

**Load when:** writing any Model, View or Controller.

Implements `ARCH-01` to `ARCH-04` from [AGENTS.md](../AGENTS.md).

## Layer definitions

| Term | Definition |
|---|---|
| **Model** | Class that owns state and business rules. Pure C# with a container, or a MonoBehaviour in the scene or prefab without one |
| **Module** | Model that wraps a feature's backend access. See [Backend integration](BACKEND_INTEGRATION.md) |
| **Cell Model** | MonoBehaviour Model on a list cell prefab, in every configuration |
| **Connector** | Stateless class that performs backend calls |
| **View** | MonoBehaviour that reads Models and renders |
| **Controller** | MonoBehaviour that converts one input into one Model command |
| **Query** | Read-only member. Changes no state |
| **Command** | Method that changes state. Validates its own input |
| **Config value** | Serialized enum or number that selects behavior. Never a component or Model reference |

## Dependency matrix

| Layer | Models | Connector | Rendering components | Input interfaces | Views | Controllers |
|---|---|---|---|---|---|---|
| **Model** | Yes | Module only | No | No | No | No |
| **View** | Yes | No | Yes | No | No | No |
| **Controller** | Yes | No | No | Yes | No | No |

## Reference acquisition

| Layer | Serialized field | Assigned `internal` field | `GetComponentInParent` | Constructor |
|---|---|---|---|---|
| **Pure C# Model** | No | No | No | Yes |
| **Cell Model** | No | Models, with a container | Parent Models, without a container | No |
| **View** | Rendering components, state roots, config values | Models, with a container | Cell Model. Any Model without a container | No |
| **Controller** | Config values | Models, with a container | Cell Model. Any Model without a container | No |

A container assigns the `internal` fields. Without one, Models live in the scene or prefab: see [Optional packages](OPTIONAL_PACKAGES.md#model-in-the-scene-or-prefab). `Initialize(model)` on a MonoBehaviour is method injection and MUST NOT be used (`UA0006`).

## Model rules

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

| ID | Rule |
|---|---|
| `MODEL-01` | With a container, a Model MUST be pure C#, unless it is a Cell Model. Without one, a Model is a MonoBehaviour in the scene or prefab. |
| `MODEL-02` | A method MUST be a query or a command, not both. `Try*` is the accepted exception. |
| `MODEL-03` | A command MUST validate its input and ignore invalid calls. Business rules MUST NOT live in a Controller. |
| `MODEL-04` | Commands MAY take parameters. Events MUST NOT (`UA0004`). |
| `MODEL-05` | Events MUST use `public event Action OnSomethingChanged;`, in the past tense. |
| `MODEL-06` | A Model MUST fire an event only when state changed. |
| `MODEL-07` | A Model MAY subscribe to another Model. It MUST use a named method and unsubscribe in `Dispose()`. |
| `MODEL-08` | A Model SHOULD have one responsibility. More than about 5 commands signals a split. |

## View rules

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

| ID | Rule |
|---|---|
| `VIEW-01` | A View MUST subscribe in `OnEnable`, render immediately, and unsubscribe in `OnDisable`. The Model may have changed while the View was inactive. |
| `VIEW-02` | A View MUST NOT call a command, sync or async. |
| `VIEW-03` | A View MUST NOT reference another View, even a child (`UA0001`). Each View reads its own Model. |
| `VIEW-04` | A View MUST NOT implement input interfaces (`UA0002`). |
| `VIEW-05` | A View MAY change activeness, color, size, text, alpha and sprites, and MAY format values for display. |
| `VIEW-06` | A View MAY use `Update()` only for time-based values, throttled as in the [countdown example](examples/01_BACKEND_FEATURE_TROPHY_ROOM.md#detailstrophy-screen). |

Name rendering methods `Render{Subject}`.

## Controller rules

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

| ID | Rule |
|---|---|
| `CTRL-01` | A Controller MUST handle one action and sit on the element it acts on. |
| `CTRL-02` | A Controller MUST call one command per input. If a click does two things, that is one command on a Model. |
| `CTRL-03` | A Controller MUST NOT check state or hold a rule. |
| `CTRL-04` | A Controller MAY convert raw input to a domain value, such as a pointer position to 0 to 1. |
| `CTRL-05` | A Controller MUST NOT serialize a reference to a Unity object (`UA0003`). |
| `CTRL-06` | A screen lifecycle event is input. A Controller MAY start work from it. See [the mapping](OPTIONAL_PACKAGES.md#mapping). |

For the `BaseClickController` form, see [Optional packages](OPTIONAL_PACKAGES.md).

## Violation patterns

| Violation | Correct form | Rule |
|---|---|---|
| `event Action<int> OnScoreChanged` | `event Action OnScoreChanged`, then read `scoreModel.Score` | `ARCH-04` |
| `public Action OnChanged { get; set; }` | `public event Action OnChanged;` | `MODEL-05` |
| `model.OnSelected?.Invoke()` in a Controller | `model.SelectItem(itemId)` | `ARCH-04` |
| `view.Initialize(model)` | Injected field, or `GetComponentInParent` without a container | `UA0006` |
| `parentView.Show(data)` or `itemViews[i].Render()` | The child View reads its own Model | `VIEW-03` |
| `view.RefreshButton.onClick += ...` | A Controller implementing `IPointerClickHandler` | `ARCH-05` |
| `if (model.State == Active) model.Buy();` in a Controller | `model.Buy()` validates internally | `CTRL-03` |
| View calls `module.LoadAsync()` | A Controller calls it and the View reads the result | `VIEW-02` |
| `internal DataService dataService` in a View | The View reads a Model | `ARCH-01` |
