# UI patterns

**Load when:** building clickable elements, visual states, tabs, lists or layouts.

Examples use plain Unity APIs. If `uiFoundations = true`, apply the mapping in [Optional packages](OPTIONAL_PACKAGES.md).

## Selectable replacements

| Do not use | Use |
|---|---|
| `Button` | Graphic plus click Controller |
| `Toggle` | View with On and Off roots, plus click Controller |
| `Slider` | `Image` fill plus Controller with `IPointerDownHandler` and `IDragHandler` |
| `Dropdown` | Modal with a list |
| `TMP_InputField` | Allowed. The one exception to `ARCH-05` |

## Clickable elements

```
CloseButton
├── Visuals
│   ├── Background (Image, raycastTarget off)
│   └── Label (TextMeshProUGUI, raycastTarget off)
└── CloseInput (Image: alpha 0, raycastTarget on) [CloseController]
```

| ID | Rule |
|---|---|
| `INPUT-01` | The Controller MUST sit on a dedicated child named `{Action}Input`, separate from the visuals. |
| `INPUT-02` | `{Action}Input` MUST have its own raycast target that fills the element, and MUST be the last sibling. Pointer events bubble to parents, not to siblings. |
| `INPUT-03` | Visual-only Graphics MUST have `raycastTarget` off. |
| `INPUT-04` | In a multi-state element, `{Action}Input` MUST be inside the state that accepts input, so other states can't be clicked. |
| `INPUT-05` | A screen-level Controller MUST handle only lifecycle events, never input from a child element. |

## Visual states

Use for two or more mutually exclusive looks of one element, such as Loading, Loaded and Failed.

```
PurchaseArea [PurchaseStateView]
├── AvailableRoot
│   ├── PriceText (TextMeshProUGUI)
│   └── BuyInput (Image: alpha 0) [BuyController]
├── LockedRoot
│   └── LockIcon (Image)
└── OwnedRoot
    └── OwnedBadge (Image)
```

| ID | Rule |
|---|---|
| `STATE-01` | The Model MUST expose the state, preferably as an enum. |
| `STATE-02` | The View MUST render every root from that state in one method. See [the code](OPTIONAL_PACKAGES.md#visual-state-view). |
| `STATE-03` | One state set per concern. Nest them if needed. |
| `STATE-04` | A View inside an inactive root is disabled, so it MUST read the Model again in `OnEnable`. |
| `STATE-05` | To show or hide one element, use `CanvasGroup` or the View's own logic. |

## Tabs

Tab content is static: nested in the prefab at edit time, never instantiated at runtime.

```
InventoryScreen
├── Tabs
│   ├── WeaponsTab [InventoryTabView]
│   │   ├── ActiveRoot
│   │   ├── InactiveRoot
│   │   └── SelectWeaponsInput (Image: alpha 0) [SelectTabController]
│   └── ArmorTab (same components, different serialized tab)
└── Content [TabContentView]
    ├── WeaponsContent (CanvasGroup)
    └── ArmorContent (CanvasGroup)
```

- One Model MUST hold the selected tab, preferably as an enum.
- Tab elements MUST share one View class and one Controller class. A serialized enum selects the tab.
- Each tab has its own prefab, nested in the screen.
- Hide tabs with `CanvasGroup` (`alpha`, `interactable`, `blocksRaycasts`) so their Views stay subscribed. Use `SetActive` when a hidden tab is expensive.
- Tabs that filter one list need no separate prefabs: see [Filtered list](examples/03_FILTERED_LIST_INVENTORY.md).

## Lists

All dynamic lists and grids MUST use `RecyclableScrollRect<T>`. It is abstract, so declare one concrete class per data type:

```csharp
public class ItemScroller : RecyclableScrollRect<ItemData>
{
}
```

```
ItemList [ItemListView]
└── ItemScroller [ItemScroller]
    └── Viewport (RectMask2D)
        └── Container (RectTransform)

ItemCell.prefab [ItemCellModel, ItemCellView]
├── ItemIcon (Image)
├── ItemNameText (TextMeshProUGUI)
└── SelectItemInput (Image: alpha 0) [SelectItemController]
```

### Data source

The Model that owns the list implements `IRecyclableDataSource<T>`:

```csharp
public int ItemCount => items.Count;

public ItemData GetItem(int index)
{
    return items[index];
}

public IReadOnlyList<ItemData> GetAllItems()
{
    return items;
}
```

The list View initializes the scroller once and refreshes it on each change:

```csharp
private void OnEnable()
{
    inventoryModel.OnItemsChanged += RenderItems;

    if (!itemScroller.IsInitialized)
        itemScroller.Initialize(inventoryModel);

    RenderItems();
}

private void RenderItems()
{
    itemScroller.Refresh();
}
```

### Cell Model

The only MonoBehaviour Model. The scroller requires a component on the prefab.

```csharp
public class ItemCellModel : MonoBehaviour, IInjectableForChildren, IRecyclableCell<ItemData>
{
    private const float CELL_HEIGHT = 120f;

    public event Action OnItemDataChanged;

    internal InventoryModel inventoryModel;

    private ItemData itemData;

    public float DefaultCellSize => CELL_HEIGHT;
    public bool IsSelected => itemData != null && inventoryModel.SelectedItemId == itemData.id;

    public ItemData Data
    {
        get => itemData;
        set
        {
            itemData = value;
            OnItemDataChanged?.Invoke();
        }
    }

    public void SelectItem()
    {
        if (itemData != null)
            inventoryModel.SelectItem(itemData.id);
    }
}
```

| ID | Rule |
|---|---|
| `LIST-01` | `IInjectableForChildren` gives the cell its `internal` fields. `IRecyclableCell<T>` is how the scroller sets `Data`. |
| `LIST-02` | The `Data` setter MUST fire an event. A recycled cell can get data while active, so `OnEnable` alone would show stale content. This is the one accepted push, and it MUST stop at the Cell Model. |
| `LIST-03` | Cell Views and Controllers MUST get the Cell Model with `GetComponentInParent<TCellModel>()` in `Awake`. |
| `LIST-04` | Cells MUST NOT be instantiated by hand or call `Initialize(data)`. |

### Scroller API

| Member | Use |
|---|---|
| `Initialize(source)` | Call once |
| `SetDataSource(source)` | Replace the source |
| `Refresh()` | Redraw after data changes. Call once per batch |
| `ScrollToItem(index)`, `ScrollToStart()`, `ScrollToEnd()` | Jump to a position |
| `ForceStopScrolling()`, `Clear()` | Stop momentum, remove all cells |
| `IsInitialized`, `VisibleStartIndex`, `VisibleEndIndex` | Read state |

## Layouts

```
ScreenRoot (fills parent, VerticalLayoutGroup)
├── Header (fixed or preferred height)
├── Content (flexible, fills the remainder)
└── Footer (fixed or preferred height)
```

- Layouts MUST fill the available space and adapt to their container.
- Use `LayoutGroup` with preferred and flexible sizes, not fixed positions.
- More than about 5 hierarchy levels is a warning sign.
- Test at several aspect ratios.
