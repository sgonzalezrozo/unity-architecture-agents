# MVC

The philosophy is in [AGENTS.md](../AGENTS.md). This document is how to apply it.

## Who knows what

| | Models | Connector | Rendering components | Input interfaces | Views | Controllers |
|---|---|---|---|---|---|---|
| **Model** | Yes | Module only | No | No | No | No |
| **View** | Yes | No | Yes | No | No | No |
| **Controller** | Yes | No | No | Yes | No | No |

A class may know several Models. A View never references another View, even a child one: each View gets its own Model and refreshes itself.

## How each layer gets its references

| | Serialized fields | Injected `internal` fields | `GetComponentInParent` | Constructor |
|---|---|---|---|---|
| **Pure C# Model** | No | No | No | Yes |
| **Cell Model** (MonoBehaviour) | No | Yes | No | No |
| **View** | Rendering components and config values | Models | Cell Model | No |
| **Controller** | Config values only | Models | Cell Model | No |

- **Config value:** a plain enum, int or similar that selects behavior, such as which tab a controller opens. It never references a component or a Model.
- `Initialize(model)` methods on MonoBehaviours are method injection and are forbidden.

## Model

Holds data and state, applies business rules, notifies changes.

```csharp
public class LivesModel
{
    private const int MAX_LIVES = 3;

    public event Action OnLivesChanged;

    private int lives = MAX_LIVES;

    public int Lives => lives;
    public bool IsAlive => lives > 0;

    public void LoseLife()
    {
        if (!IsAlive)
            return;

        lives--;
        OnLivesChanged?.Invoke();
    }
}
```

- Pure C# by default. A Model is a MonoBehaviour only when it is a list cell, because the scroller requires it ([Lists](UI_PATTERNS.md#lists)).
- Exposes **queries** (read-only properties, `IReadOnlyList<T>`) and **commands** (methods that change state). A method does one or the other, not both.
- Commands validate their own input and ignore invalid calls. Rules live here, never in a Controller.
- Commands may take parameters. Parameters are the caller telling the Model what to do. Only events are parameterless.
- Fires an event only when something actually changed.
- One responsibility, one reason to change. There is no fixed method count, but more than about 5 commands is a signal to check whether it should be split.
- May subscribe to other Models. Unsubscribe in `Dispose()`, and use a named method, never a lambda.

**Events are always `public event Action OnXChanged;`.** The `event` keyword matters: only the owning class can fire it, and no one can overwrite other subscribers. Name them in the past tense.

## View

Renders Model state. Nothing else.

```csharp
public class LivesView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI livesText;

    internal LivesModel lives;

    private void OnEnable()
    {
        lives.OnLivesChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        lives.OnLivesChanged -= Refresh;
    }

    private void Refresh()
    {
        livesText.text = lives.Lives.ToString();
    }
}
```

- **Subscribe in `OnEnable`, call `Refresh()` immediately, unsubscribe in `OnDisable`.** The immediate call matters: the Model may have changed while the View was inactive.
- Serializes rendering components only: `Image`, `TextMeshProUGUI`, `RectTransform`, `CanvasGroup`, `ViewStateMachine`, `RecyclableScrollRect`.
- May change activeness, color, size, text, alpha and sprites. May format values for display.
- Calls no commands, sync or async. It only reads.
- `Update()` is allowed only for values that change with time, such as countdowns, throttled as in [the example](examples/01_TROPHY_ROOM.md#detailstrophy).

## Controller

Turns input into one Model command.

```csharp
public class LoseLifeController : BaseClickController
{
    internal LivesModel lives;

    protected override void OnClick()
    {
        lives.LoseLife();
    }
}
```

- Click input: inherit `BaseClickController` and override `OnClick()`. Its `InitializeController()` hook is where a Controller caches a cell Model.
- Other input: implement `IPointerDownHandler`, `IDragHandler` and similar directly.
- A screen lifecycle event is also input: a Controller may implement `IScreenShow` to start a load. See [the example](examples/01_TROPHY_ROOM.md#mainroom).
- One Controller per action, placed on the element it acts on ([Clickable elements](UI_PATTERNS.md#clickable-elements)).
- May convert raw input into a domain value, such as a pointer position into 0 to 1. May not decide anything: no state checks, no business conditions.
- If a click must do two things, that is one command on a Model, not two calls from the Controller.

## Wrong and right

| Wrong | Right | Why |
|---|---|---|
| `Action<int> OnScoreChanged` | `event Action OnScoreChanged`, then read `model.Score` | Pull, never push |
| `public Action OnChanged { get; set; }` | `public event Action OnChanged;` | Anyone could fire or replace it |
| `model.OnSelected?.Invoke()` from a Controller | `model.Select(id)` | Only the Model fires its events |
| `view.Initialize(model)` | `internal` injected field | Method injection |
| `parentView.Show(data)` or `itemViews[i].Refresh()` | Child View listens to its own Model | Views do not know Views |
| `view.RefreshButton.onClick += ...` | `RefreshController : BaseClickController` | Controllers do not know Views; no Button |
| `if (model.State == Active) model.Buy();` in a Controller | `model.Buy()` validates internally | Rules belong in Models |
| View calls `module.LoadAsync()` | Controller calls it, View reads the result | Views only read |
| `internal DataService service` in a View | View reads a Model | Views know only Models |
