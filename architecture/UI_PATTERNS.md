# UI patterns

## Clickable elements

We do not use `Selectable` components. Replace them like this:

| Instead of | Use |
|---|---|
| `Button` | Graphic + Controller on `BaseClickController` |
| `Toggle` | `ViewStateMachine` with On and Off + click Controller |
| `Slider` | `Image` fill + Controller with `IPointerDownHandler`, `IDragHandler` |
| `Dropdown` | A modal with a list |
| `TMP_InputField` | Allowed. The one exception |

```
CloseButton
├── Visuals
│   ├── Background (Image, raycastTarget off)
│   └── Label (TextMeshProUGUI, raycastTarget off)
└── Actions (Close) [CloseController]
```

- The Controller sits on a child named `Actions`, separate from the visuals. The text in parentheses is only a description of the action in these docs.
- **`Actions` needs a raycast target of its own.** Click events travel up to parents, not across to siblings, so a sibling `Visuals` will not trigger it. Give `Actions` a transparent Graphic that fills the element, and place it last so it is on top.
- Turn off `raycastTarget` on the purely visual Graphics.
- An element with several visual states has its `Actions` inside the state that accepts input, so a locked state is unclickable by construction.

Screen-level Controllers exist only for lifecycle events, never for input from a child element.

## Visual states

Use `ViewStateMachine` for two or more mutually exclusive looks of the same element: Loading/Loaded/Failed, Locked/Available/Owned, On/Off.

```
PurchaseArea [ViewStateMachine, PurchaseStateView]
├── Available
│   ├── Price (TextMeshProUGUI)
│   └── Actions (Buy) [BuyController]
├── Locked
│   └── LockIcon (Image)
└── Owned
    └── OwnedBadge (Image)
```

```csharp
public class PurchaseStateView : MonoBehaviour
{
    [SerializeField] private ViewStateMachine states;

    internal StoreModel store;

    private void OnEnable()
    {
        store.OnStateChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        store.OnStateChanged -= Refresh;
    }

    private void Refresh()
    {
        states.SetActiveState(store.SelectedItemState.ToString());
    }
}
```

- The Model holds an enum. Child GameObjects are named exactly as the enum values.
- `enum.ToString()` allocates, which is fine in an event handler and not in `Update()`.
- For state that is not an enum, use `private const string STATE_ON = "On";`, never a literal in the call.
- One `ViewStateMachine` per concern. Nest them if needed.
- For a simple show or hide of one element, use `CanvasGroup` or the View's own logic.
- Remember that a View inside a state is disabled while that state is off. It re-reads the Model in `OnEnable`.

## Tabs

Tab content is **static**. It is nested in the prefab at edit time, never instantiated at runtime.

```
InventoryScreen
├── Tabs
│   ├── WeaponsTab [ViewStateMachine, InventoryTabView]
│   │   ├── Active
│   │   ├── Inactive
│   │   └── Actions (SelectWeapons) [InventoryTabController]
│   └── ArmorTab (same components, different serialized tab)
└── Content [TabContentView]
    ├── WeaponsContent (CanvasGroup)
    └── ArmorContent (CanvasGroup)
```

- One Model holds the selected tab, preferably as an enum.
- Tab buttons share one View class and one Controller class. A serialized enum says which tab each one is.
- Each tab has its own prefab, nested in the screen.
- Hide tabs with `CanvasGroup` (`alpha`, `interactable`, `blocksRaycasts`) so their Views stay subscribed. Use `SetActive` when a hidden tab is expensive to keep running.
- Tab content that filters one list does not need separate prefabs: see [Inventory](examples/03_INVENTORY.md).

## Lists

Use `RecyclableScrollRect` for all dynamic lists and grids.

```
ItemList [ItemListView]
└── Scroller (RecyclableScrollRect)
    └── Viewport (RectMask2D only)
        └── Container (RectTransform)

ItemCell.prefab [ItemCellModel, ItemCellView]
├── Icon (Image)
├── Label (TextMeshProUGUI)
└── Actions (Select) [ItemCellController]
```

### The list's data source

The Model that owns the list implements `IRecyclableDataSource<T>`. No extra container class is needed.

```csharp
public int Count => items.Count;

public ItemData GetItem(int index)
{
    return items[index];
}
```

The list View initializes the scroller once and refreshes it when the Model changes:

```csharp
private void OnEnable()
{
    inventory.OnItemsChanged += Refresh;

    if (!scroller.IsInitialized)
        scroller.Initialize(inventory);

    Refresh();
}

private void Refresh()
{
    scroller.Refresh();
}
```

### The cell Model

The one case where a Model is a MonoBehaviour: the scroller needs a component on the prefab.

```csharp
public class ItemCellModel : MonoBehaviour, IInjectableForChildren, IRecyclableCell<ItemData>
{
    private const float CELL_HEIGHT = 120f;

    public event Action OnDataChanged;

    internal InventoryModel inventory;

    private ItemData data;

    public float DefaultCellSize => CELL_HEIGHT;
    public bool IsSelected => data != null && inventory.SelectedItemId == data.Id;

    public ItemData Data
    {
        get => data;
        set
        {
            data = value;
            OnDataChanged?.Invoke();
        }
    }

    public void Select()
    {
        if (data != null)
            inventory.SelectItem(data.Id);
    }
}
```

- `IInjectableForChildren` gives the cell its `internal` fields. `IRecyclableCell<T>` is how the scroller sets `Data`. That assignment is the one push we accept, since it is the library's contract, and it stops at the cell Model.
- **The `Data` setter fires an event.** A recycled cell can get new data while it stays active, so `OnEnable` alone would show stale content.
- Cell Views and Controllers find it with `GetComponentInParent<ItemCellModel>()`: Views in `Awake`, Controllers in `InitializeController()`.
- Cells never call `Initialize(data)`, and nothing instantiates them by hand.

Complete cells: [Trophy Room](examples/01_TROPHY_ROOM.md#mainroom) and [Inventory](examples/03_INVENTORY.md).

### Scroller reference

| Member | Use |
|---|---|
| `Initialize(source)` | Once |
| `SetDataSource(source)` | Replace the source |
| `Refresh()` | Redraw after data changes. Call it once per batch |
| `ScrollToItem(index)`, `ScrollToStart()`, `ScrollToEnd()` | Jump |
| `ForceStopScrolling()`, `Clear()` | Stop momentum, remove all |
| `IsInitialized`, `VisibleStartIndex`, `VisibleEndIndex` | Read state |

Inspector settings for tuning: *Estimated Cell Height*, *Pool Percentage* (1.4 is 40% extra), *Cells Per Frame* (lower for heavy cells), *Grid Count* or *Use Dynamic Grid Count*.

## Layouts

All layouts fill the available space and adapt when their container changes.

```
ScreenRoot (fills parent, vertical layout)
├── Header (fixed or preferred height)
├── Content (flexible, fills the remainder)
└── Footer (fixed or preferred height)
```

- Use `LayoutGroup` with preferred and flexible sizes rather than fixed positions.
- Keep hierarchies shallow: more than about 5 levels is a warning sign.
- Test at several aspect ratios.
