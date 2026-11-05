# MVC

**Load when:** writing any Model, View or Controller.

Implements `ARCH-01` to `ARCH-04` and `ARCH-09` from [AGENTS.md](../AGENTS.md).

## Layer definitions

| Term | Definition |
|---|---|
| **Model** | Class that owns state and business rules. See [Where a Model lives](#where-a-model-lives) |
| **Outside-MVC Model** | Model created by the container that no View or Controller uses |
| **Module** | Model that wraps a feature's backend access. See [Backend integration](BACKEND_INTEGRATION.md) |
| **Cell Model** | MonoBehaviour Model on a list cell prefab |
| **Connector** | Stateless class that performs backend calls |
| **View** | MonoBehaviour that reads Models and renders |
| **Controller** | MonoBehaviour that converts one input into one Model command |
| **Query** | Read-only member. Changes no state |
| **Command** | Method that changes state. Validates its own input |
| **Config value** | Enum or number serialized on a View or Controller that selects behavior. Never a component or Model reference |
| **Configuration asset** | `ScriptableObject` with serialized values, read by a Model |
| **Scope** | Self-contained part of a feature with one root GameObject. See [Scopes](#scopes) |

## Dependency matrix

| Layer | Models | Connector | Rendering components | Input interfaces | Views | Controllers |
|---|---|---|---|---|---|---|
| **Model** | Yes | Module only | No | No | No | No |
| **View** | Yes | No | Yes | No | No | No |
| **Controller** | Yes | No | No | Yes | No | No |

A Model MAY also reference a configuration asset (`MODEL-10`).

## Scopes

A scope is a self-contained part of a feature with one root GameObject, usually a prefab root. A feature is not a scope: a large feature is divided into several.

```
InventoryScreen.prefab [InventoryModel, InventoryView]      main scope
├── ItemList [InventoryListView]                            scope
└── ActionButtons [ActionButtonsView]                       scope
    ├── ApplyController (Image: alpha 0) [ApplyController]
    └── RevertController (Image: alpha 0) [RevertController]
```

| ID | Rule |
|---|---|
| `SCOPE-01` | Nesting a prefab inside another MUST NOT change its scope. Its root stays the root. |
| `SCOPE-02` | A scope's Model and View MUST sit on its root. Controllers stay on their input objects (`INPUT-01`). |
| `SCOPE-03` | A Model MUST stay on its scope root. It moves to the lowest common parent only when another scope or feature needs it. |
| `SCOPE-04` | A View MUST render only inside its scope. Its serialized references MUST NOT point outside it. |
| `SCOPE-05` | A View or Controller MAY read a Model outside its scope, by looking up the hierarchy or through injection. |

## Where a Model lives

This applies to every project, with or without a container. Use the first row that fits.

| Order | Case | Where it lives | How a View or Controller gets it |
|---|---|---|---|
| 1 | Its Views and Controllers can share a parent | MonoBehaviour in the scene or prefab, on the lowest common parent, even if that is a root GameObject of the scene | `GetComponentInParent<TModel>()` |
| 2 | They can't share a parent, or it must persist | Pure C#, created by the container. Without one, a singleton | Injected `internal` field. Without a container, `TModel.Instance` |
| 3 | Outside MVC: no View or Controller uses it | Created by the container. Without one, a singleton | It doesn't. Only Models reference it |

```
LivesScreen.prefab [LivesModel, LivesView]
├── RemainingLivesText (TextMeshProUGUI)
└── LoseLifeController (Image: alpha 0) [LoseLifeController]
```

| ID | Rule |
|---|---|
| `ROOT-01` | A Model MUST sit on the lowest common parent of the Views and Controllers that use it, whenever one can exist. That is usually its scope root (`SCOPE-03`). Prefabs loaded with Addressables count. |
| `ROOT-02` | Those Views and Controllers MUST get it with `GetComponentInParent<TModel>()` in `Awake`, and cache it. A singleton or a service locator MUST NOT replace this. |
| `ROOT-03` | A MonoBehaviour Model MUST set its initial state in field initializers. A View's `OnEnable` can run before the Model's `Awake`. |
| `ROOT-04` | A MonoBehaviour Model is still a Model: it MUST NOT reference a View, a Controller or a non-Model component. |
| `ROOT-05` | Unit test a MonoBehaviour Model in EditMode with `new GameObject().AddComponent<TModel>()`. |
| `ROOT-06` | Injection is the exception. A Model MUST NOT be injected into a View or Controller when a common parent can exist. |
| `ROOT-07` | Without a container, a Model in row 2 or 3 MUST be a pure C# singleton with a public constructor. Views and Controllers cache `TModel.Instance` in `Awake`. With a container, singletons MUST NOT be used. |
| `ROOT-08` | Views and Controllers MUST NOT reference an Outside-MVC Model (row 3). |
| `ROOT-09` | A test MUST create its own instance and MUST NOT use `TModel.Instance`, because static state carries over between tests. |

```csharp
public class CoinBalanceModel
{
    public static CoinBalanceModel Instance { get; } = new CoinBalanceModel();
}
```

## Reference acquisition

| Layer | Serialized field | Injected `internal` field | `GetComponentInParent` | Constructor |
|---|---|---|---|---|
| **Pure C# Model** | No | No | No | Yes |
| **MonoBehaviour Model** | No | Models | Parent Models | No |
| **View** | Rendering components, state roots, config values | Models, row 2 only | Models, row 1 only | No |
| **Controller** | Config values | Models, row 2 only | Models, row 1 only | No |

`Initialize(model)` on a MonoBehaviour is method injection and MUST NOT be used (`UA0006`).

## Model rules

```csharp
public class LivesModel : MonoBehaviour
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
| `MODEL-01` | A Model MUST live where [Where a Model lives](#where-a-model-lives) says. |
| `MODEL-02` | A method MUST be a query or a command, not both. `Try*` is the accepted exception. |
| `MODEL-03` | A command MUST validate its input. A condition the player can reach, such as too few coins, is ignored or returns a state. A condition only a bug can reach, such as an unknown ID, throws. Business rules MUST NOT live in a Controller. |
| `MODEL-04` | Commands MAY take parameters. Events MUST NOT (`UA0004`). |
| `MODEL-05` | Events MUST use `public event Action OnSomethingChanged;`, in the past tense. |
| `MODEL-06` | A Model MUST fire an event only when state changed. |
| `MODEL-07` | A Model MAY subscribe to another Model. It MUST use a named method and unsubscribe in `OnDestroy()` on a MonoBehaviour, or `Dispose()` in pure C#. |
| `MODEL-08` | A Model SHOULD have one responsibility. More than about 5 commands signals a split. |
| `MODEL-09` | A Model MUST NOT have serialized fields (`UA0013`). |
| `MODEL-10` | A value set outside code MUST come from an injected configuration asset or from backend JSON. Everything else is a constant. Without a container, load the asset by address or path. |
| `MODEL-11` | A Model MUST finish changing state before it fires an event. A subscriber MUST NOT call a command on the Model that fired the event. |

## View rules

```csharp
public class LivesView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI remainingLivesText;

    private LivesModel livesModel;

    private void Awake()
    {
        livesModel = GetComponentInParent<LivesModel>();
    }

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
    private LivesModel livesModel;

    private void Awake()
    {
        livesModel = GetComponentInParent<LivesModel>();
    }

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
| `view.Initialize(model)` | `GetComponentInParent`, or an injected field for row 2 | `UA0006` |
| `parentView.Show(data)` or `itemViews[i].Render()` | The child View reads its own Model | `VIEW-03` |
| `view.RefreshButton.onClick += ...` | A Controller implementing `IPointerClickHandler` | `ARCH-05` |
| `if (model.State == Active) model.Buy();` in a Controller | `model.Buy()` validates internally | `CTRL-03` |
| View calls `module.LoadAsync()` | A Controller calls it and the View reads the result | `VIEW-02` |
| `internal DataService dataService` in a View | The View reads a Model | `ARCH-01` |
