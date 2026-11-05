# Optional packages

**Load when:** generating code that depends on a package.

Packages change the implementation. The [architecture](../AGENTS.md#architecture-invariants), the [MVC layer rules](MVC.md) and the [C# standards](CSHARP_STANDARDS.md) stay mandatory in every configuration.

## Capability detection

Run this once per task. Read `Packages/manifest.json`, and set each capability from `dependencies`:

| Capability | `true` when `dependencies` contains |
|---|---|
| `fastIoC` | `com.sgonzalezrozo.fast-ioc` |
| `recyclableScroller` | `com.sgonzalezrozo.recyclable-scroller` |
| `screenSystem` | `com.sgonzalezrozo.screen-system` |
| `uiFoundations` | `com.sgonzalezrozo.ui-foundations` |
| `addressables` | `com.unity.addressables` |

Also set `otherContainer = true` if the project uses another dependency injection container, such as Zenject or VContainer. Check the manifest and the existing code.

| ID | Rule |
|---|---|
| `OPT-01` | Code MUST NOT reference a package whose capability is `false`. |
| `OPT-02` | If a capability is `true`, new code SHOULD use that package. |
| `OPT-03` | An agent MUST NOT add a package without approval. See [When to ask](AGENT_WORKFLOW.md#when-to-ask). |
| `OPT-04` | Match the code around the change. Don't mix both forms in one feature. |
| `OPT-05` | No configuration may break an `ARCH` rule or the C# standards. |

## Mapping

| Need | Package form | Unity form |
|---|---|---|
| Reference a Model | `fastIoC`: pure C# Model, injected `internal` field | [Model in the scene or prefab](#model-in-the-scene-or-prefab), found with `GetComponentInParent` |
| Click input | `uiFoundations`: subclass of `BaseClickController` | `MonoBehaviour, IPointerClickHandler` |
| Raycast target | `uiFoundations`: `BaseClickController`, a transparent `Graphic` | Transparent `Image` on the input GameObject |
| Disable clicks | `uiFoundations`: `Interactable = false` | Disable the input GameObject, or place it in an inactive state |
| Mutually exclusive looks | `uiFoundations`: `ViewStateMachine.SetState(stateName)` | View with one `GameObject` root per state |
| Dynamic list | `recyclableScroller`: `RecyclableScrollRect<T>` | [`ScrollRect` with reused cells](#list-without-recyclablescroller) |
| Screen shown | `screenSystem`: `IScreenShow.OnScreenShown()` | Controller `OnEnable`, on the screen root |
| Open a screen | `screenSystem`: `ScreenSystem` API | [Navigation Model](#navigation-without-screensystem) |
| Load a large asset | `addressables`: release the handle | Direct reference or `Resources` |

If `otherContainer = true`, use that container's injection in place of the `fastIoC` form. Keep every other rule.

The examples in this repository use this profile: `fastIoC`, `recyclableScroller` and `screenSystem` are `true`, and `uiFoundations` is `false`. Apply this mapping to adapt them.

## Model in the scene or prefab

Without a container, a Model is a MonoBehaviour that already exists in the scene or in a prefab, such as one loaded with Addressables. Nothing creates it at runtime. Views and Controllers on the same prefab find it once and cache it.

```
LivesScreen.prefab [LivesModel]
├── RemainingLivesText (TextMeshProUGUI) [LivesView]
└── LoseLifeInput (Image: alpha 0) [LoseLifeController]
```

```csharp
public class LivesModel : MonoBehaviour
{
    private const int MAX_LIVES = 3;

    public event Action OnLivesChanged;

    private int remainingLives = MAX_LIVES;

    public int RemainingLives => remainingLives;

    public void LoseLife()
    {
        if (remainingLives == 0)
            return;

        remainingLives--;
        OnLivesChanged?.Invoke();
    }
}

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
| `ROOT-01` | The Model MUST sit on the same GameObject or a parent of the Views and Controllers that use it. |
| `ROOT-02` | Views and Controllers MUST get it with `GetComponentInParent<TModel>()` in `Awake`, and cache it. Static singletons and service locators MUST NOT replace this. |
| `ROOT-03` | The Model MUST set its initial state in field initializers. A View's `OnEnable` can run before the Model's `Awake`. |
| `ROOT-04` | A Model is still a Model: it MUST NOT reference a View, a Controller or a non-Model component. |
| `ROOT-05` | Unit test it in EditMode with `new GameObject().AddComponent<TModel>()`. |

A project that wants pure C# Models without a container MAY use a composition root: a class that creates them and assigns the `internal` fields. It holds no logic, and it runs first with `[DefaultExecutionOrder]`.

## List without RecyclableScroller

The list View keeps one Cell Model per item and reuses them. It references the Cell Model, never a cell View (`VIEW-03`).

Keep the cell prefab's root inactive, so the cell receives its Model before `OnEnable`.

```csharp
public class InventoryListView : MonoBehaviour
{
    [SerializeField] private InventoryCellModel inventoryCellPrefab;
    [SerializeField] private RectTransform itemContainer;

    internal InventoryModel inventoryModel;

    private readonly List<InventoryCellModel> itemCells = new();

    private void OnEnable()
    {
        inventoryModel.OnVisibleItemsChanged += RenderVisibleItems;
        RenderVisibleItems();
    }

    private void OnDisable()
    {
        inventoryModel.OnVisibleItemsChanged -= RenderVisibleItems;
    }

    private void RenderVisibleItems()
    {
        IReadOnlyList<InventoryItemData> visibleItems = inventoryModel.VisibleItems;

        CreateMissingCells(visibleItems.Count);

        for (int i = 0; i < itemCells.Count; i++)
            RenderCell(itemCells[i], i, visibleItems);
    }

    private void CreateMissingCells(int requiredCellCount)
    {
        while (itemCells.Count < requiredCellCount)
        {
            InventoryCellModel itemCell = Instantiate(inventoryCellPrefab, itemContainer);
            itemCell.inventoryModel = inventoryModel;
            itemCells.Add(itemCell);
        }
    }

    private static void RenderCell(InventoryCellModel itemCell, int index, IReadOnlyList<InventoryItemData> visibleItems)
    {
        bool hasItem = index < visibleItems.Count;

        if (hasItem)
            itemCell.Data = visibleItems[index];

        itemCell.gameObject.SetActive(hasItem);
    }
}
```

This creates one cell per item, so it fits short lists. A long list needs virtualization: ask before adding a package.

## Navigation without ScreenSystem

A Model owns the current screen, and a View renders it. A Controller starts a load from `OnEnable`, because the screen root activates each time it is shown.

```csharp
public enum GameScreen { Home, Inventory }

public class NavigationModel
{
    public event Action OnCurrentScreenChanged;

    private GameScreen currentScreen = GameScreen.Home;

    public GameScreen CurrentScreen => currentScreen;

    public void OpenScreen(GameScreen screen)
    {
        if (currentScreen == screen)
            return;

        currentScreen = screen;
        OnCurrentScreenChanged?.Invoke();
    }
}

public class ScreenStackView : MonoBehaviour
{
    [SerializeField] private GameObject homeRoot;
    [SerializeField] private GameObject inventoryRoot;

    internal NavigationModel navigationModel;

    private void OnEnable()
    {
        navigationModel.OnCurrentScreenChanged += RenderCurrentScreen;
        RenderCurrentScreen();
    }

    private void OnDisable()
    {
        navigationModel.OnCurrentScreenChanged -= RenderCurrentScreen;
    }

    private void RenderCurrentScreen()
    {
        homeRoot.SetActive(navigationModel.CurrentScreen == GameScreen.Home);
        inventoryRoot.SetActive(navigationModel.CurrentScreen == GameScreen.Inventory);
    }
}
```

## Click Controller

Plain Unity:

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

UI Foundations:

```csharp
public class LoseLifeController : BaseClickController
{
    internal LivesModel livesModel;

    protected override void OnClick()
    {
        livesModel.LoseLife();
    }
}
```

`BaseClickController` derives from `Graphic`, so a Controller that cleans up in `OnDestroy` MUST use `protected override void OnDestroy()` and call `base.OnDestroy()`.

## Visual state View

Plain Unity:

```csharp
public class PurchaseStateView : MonoBehaviour
{
    [SerializeField] private GameObject availableRoot;
    [SerializeField] private GameObject lockedRoot;
    [SerializeField] private GameObject ownedRoot;

    internal StoreModel storeModel;

    private void OnEnable()
    {
        storeModel.OnSelectedItemStateChanged += RenderPurchaseState;
        RenderPurchaseState();
    }

    private void OnDisable()
    {
        storeModel.OnSelectedItemStateChanged -= RenderPurchaseState;
    }

    private void RenderPurchaseState()
    {
        PurchaseState state = storeModel.SelectedItemState;

        availableRoot.SetActive(state == PurchaseState.Available);
        lockedRoot.SetActive(state == PurchaseState.Locked);
        ownedRoot.SetActive(state == PurchaseState.Owned);
    }
}
```

UI Foundations:

```csharp
public class PurchaseStateView : MonoBehaviour
{
    [SerializeField] private ViewStateMachine purchaseStateMachine;

    internal StoreModel storeModel;

    private void OnEnable()
    {
        storeModel.OnSelectedItemStateChanged += RenderPurchaseState;
        RenderPurchaseState();
    }

    private void OnDisable()
    {
        storeModel.OnSelectedItemStateChanged -= RenderPurchaseState;
    }

    private void RenderPurchaseState()
    {
        purchaseStateMachine.SetState(storeModel.SelectedItemState.ToString());
    }
}
```

Each state in `ViewStateMachine` needs a `CanvasGroup`. State names MUST match the enum values exactly.
