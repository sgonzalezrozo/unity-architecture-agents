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
| Model with no common parent, or that persists | `fastIoC`: pure C#, injected `internal` field. See [Where a Model lives](MVC.md#where-a-model-lives) | Pure C# singleton (`ROOT-07`) |
| Configuration asset for a Model | `fastIoC`: `Bind<T>().FromInstance("Path").AsSingle()`, injected `internal` field | Load it by address or path (`MODEL-10`) |
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

## List without RecyclableScroller

The list View only creates cells and shows as many as there are items. It holds GameObjects, never a cell View (`VIEW-03`). Each cell finds the Model and pulls its own item by position, so no data is pushed (`ARCH-04`).

```csharp
public class InventoryListView : MonoBehaviour
{
    [SerializeField] private GameObject inventoryCellPrefab;
    [SerializeField] private RectTransform itemContainer;

    private readonly List<GameObject> itemCells = new();

    private InventoryModel inventoryModel;

    private void Awake()
    {
        inventoryModel = GetComponentInParent<InventoryModel>();
    }

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
        CreateMissingCells(inventoryModel.ItemCount);

        for (int i = 0; i < itemCells.Count; i++)
            itemCells[i].SetActive(i < inventoryModel.ItemCount);
    }

    private void CreateMissingCells(int requiredCellCount)
    {
        while (itemCells.Count < requiredCellCount)
            itemCells.Add(Instantiate(inventoryCellPrefab, itemContainer));
    }
}

public class InventoryCellView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI itemNameText;

    private InventoryModel inventoryModel;

    private void Awake()
    {
        inventoryModel = GetComponentInParent<InventoryModel>();
    }

    private void OnEnable()
    {
        inventoryModel.OnVisibleItemsChanged += RenderItem;
        RenderItem();
    }

    private void OnDisable()
    {
        inventoryModel.OnVisibleItemsChanged -= RenderItem;
    }

    private void RenderItem()
    {
        int itemIndex = transform.GetSiblingIndex();

        if (itemIndex < inventoryModel.ItemCount)
            itemNameText.text = inventoryModel.GetItem(itemIndex).displayName;
    }
}
```

This creates one cell per item, so it fits short lists. A long list needs virtualization: ask before adding a package.

## Navigation without ScreenSystem

A Model owns the current screen, and a View renders it. A Controller starts a load from `OnEnable`, because the screen root activates each time it is shown.

If navigation must persist across scenes, it is row 2 in [Where a Model lives](MVC.md#where-a-model-lives).

```csharp
public enum GameScreen { Home, Inventory }

public class NavigationModel : MonoBehaviour
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

    private NavigationModel navigationModel;

    private void Awake()
    {
        navigationModel = GetComponentInParent<NavigationModel>();
    }

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

These forms differ only in the base class. The `internal` field shows an injected Model. Otherwise, use `GetComponentInParent`: see [Where a Model lives](MVC.md#where-a-model-lives).

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

These forms differ only in how the state is rendered. The `internal` field shows an injected Model. Otherwise, use `GetComponentInParent`: see [Where a Model lives](MVC.md#where-a-model-lives).

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
