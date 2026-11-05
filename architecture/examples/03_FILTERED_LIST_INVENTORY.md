# Example: filtered list (Inventory)

**Load when:** building tabs, a filtered list, or a selection shared between panels.

Tabs filter one list, and a details panel renders the selection. All state is local, with no backend.

**Demonstrates:** tab elements from one class, a filtered list without allocations, shared selection.

Capability profile: `fastIoC`, `recyclableScroller` and `screenSystem` are `true`, and `uiFoundations` is `false`. For another profile, apply the [mapping](../OPTIONAL_PACKAGES.md#mapping).

`InventoryModel` is injected because its cells reference it, and pooled cells have no common parent with the screen (`ROOT-09`). A list whose cells need only their own data can keep its Model on the screen root.

## Hierarchy

```
InventoryScreen
├── Tabs
│   ├── WeaponsTab [InventoryTabView]                       inventoryTab = Weapons
│   │   ├── ActiveRoot
│   │   │   └── ActiveLabel (TextMeshProUGUI)
│   │   ├── InactiveRoot
│   │   │   └── InactiveLabel (TextMeshProUGUI)
│   │   └── SelectTabInput (Image: alpha 0) [SelectTabController]   inventoryTab = Weapons
│   ├── ArmorTab (same, inventoryTab = Armor)
│   └── ConsumablesTab (same, inventoryTab = Consumables)
├── ItemList [InventoryListView]
│   └── InventoryScroller [InventoryScroller]
└── Details [InventoryDetailsView]
    ├── EmptyRoot
    └── SelectedItemRoot
        ├── ItemNameText (TextMeshProUGUI)
        └── DropItemInput (Image: alpha 0) [DropItemController]

InventoryCell.prefab [InventoryCellModel, InventoryCellView]
├── ItemNameText (TextMeshProUGUI)
├── SelectedHighlight (Image)
└── SelectItemInput (Image: alpha 0) [SelectItemController]
```

The tabs filter one list, so no tab has its own content prefab.

## Model

```csharp
public enum InventoryTab { Weapons, Armor, Consumables }

[Serializable]
public class InventoryItemData
{
    public int id;
    public string displayName;
    public InventoryTab inventoryTab;
}

public class InventoryModel : IRecyclableDataSource<InventoryItemData>
{
    private const int NO_SELECTION = -1;

    public event Action OnSelectedTabChanged;
    public event Action OnVisibleItemsChanged;
    public event Action OnItemSelectionChanged;

    private readonly List<InventoryItemData> allItems = new();
    private readonly List<InventoryItemData> visibleItems = new();

    private InventoryTab selectedTab = InventoryTab.Weapons;
    private int selectedItemId = NO_SELECTION;

    public InventoryTab SelectedTab => selectedTab;
    public int SelectedItemId => selectedItemId;
    public int ItemCount => visibleItems.Count;
    public IReadOnlyList<InventoryItemData> VisibleItems => visibleItems;
    public InventoryItemData SelectedItem => FindItemById(selectedItemId);

    public InventoryItemData GetItem(int index)
    {
        return visibleItems[index];
    }

    public IReadOnlyList<InventoryItemData> GetAllItems()
    {
        return visibleItems;
    }

    public void ReplaceAllItems(List<InventoryItemData> newItems)
    {
        allItems.Clear();
        allItems.AddRange(newItems);
        RebuildVisibleItems();
    }

    public void SelectTab(InventoryTab inventoryTab)
    {
        if (selectedTab == inventoryTab)
            return;

        selectedTab = inventoryTab;
        OnSelectedTabChanged?.Invoke();
        ClearItemSelection();
        RebuildVisibleItems();
    }

    public void SelectItem(int itemId)
    {
        if (selectedItemId == itemId)
            return;

        selectedItemId = itemId;
        OnItemSelectionChanged?.Invoke();
    }

    public void DropSelectedItem()
    {
        InventoryItemData selectedItem = SelectedItem;

        if (selectedItem == null)
            return;

        allItems.Remove(selectedItem);
        ClearItemSelection();
        RebuildVisibleItems();
    }

    private void ClearItemSelection()
    {
        selectedItemId = NO_SELECTION;
        OnItemSelectionChanged?.Invoke();
    }

    private void RebuildVisibleItems()
    {
        visibleItems.Clear();

        for (int i = 0; i < allItems.Count; i++)
        {
            if (allItems[i].inventoryTab == selectedTab)
                visibleItems.Add(allItems[i]);
        }

        OnVisibleItemsChanged?.Invoke();
    }

    private InventoryItemData FindItemById(int itemId)
    {
        for (int i = 0; i < allItems.Count; i++)
        {
            if (allItems[i].id == itemId)
                return allItems[i];
        }

        return null;
    }
}
```

`DropSelectedItem` takes no parameter and validates itself, so `DropItemController` needs no state check (`CTRL-03`).

## Tabs

```csharp
public class InventoryTabView : MonoBehaviour
{
    [SerializeField] private GameObject activeRoot;
    [SerializeField] private GameObject inactiveRoot;
    [SerializeField] private InventoryTab inventoryTab;

    internal InventoryModel inventoryModel;

    private void OnEnable()
    {
        inventoryModel.OnSelectedTabChanged += RenderTabState;
        RenderTabState();
    }

    private void OnDisable()
    {
        inventoryModel.OnSelectedTabChanged -= RenderTabState;
    }

    private void RenderTabState()
    {
        bool isSelectedTab = inventoryModel.SelectedTab == inventoryTab;

        activeRoot.SetActive(isSelectedTab);
        inactiveRoot.SetActive(!isSelectedTab);
    }
}

public class SelectTabController : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private InventoryTab inventoryTab;

    internal InventoryModel inventoryModel;

    public void OnPointerClick(PointerEventData eventData)
    {
        inventoryModel.SelectTab(inventoryTab);
    }
}
```

## List and cell

```csharp
public class InventoryScroller : RecyclableScrollRect<InventoryItemData>
{
}

public class InventoryListView : MonoBehaviour
{
    [SerializeField] private InventoryScroller inventoryScroller;

    internal InventoryModel inventoryModel;

    private void OnEnable()
    {
        inventoryModel.OnVisibleItemsChanged += RenderVisibleItems;

        if (!inventoryScroller.IsInitialized)
            inventoryScroller.Initialize(inventoryModel);

        RenderVisibleItems();
    }

    private void OnDisable()
    {
        inventoryModel.OnVisibleItemsChanged -= RenderVisibleItems;
    }

    private void RenderVisibleItems()
    {
        inventoryScroller.Refresh();
    }
}

public class InventoryCellModel : MonoBehaviour, IInjectableForChildren, IRecyclableCell<InventoryItemData>
{
    private const float CELL_HEIGHT = 80f;

    public event Action OnItemDataChanged;

    public event Action OnItemSelectionChanged
    {
        add { inventoryModel.OnItemSelectionChanged += value; }
        remove { inventoryModel.OnItemSelectionChanged -= value; }
    }

    internal InventoryModel inventoryModel;

    private InventoryItemData itemData;

    public float DefaultCellSize => CELL_HEIGHT;
    public bool IsSelected => itemData != null && inventoryModel.SelectedItemId == itemData.id;

    public InventoryItemData Data
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

public class InventoryCellView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private Image selectedHighlight;

    private InventoryCellModel inventoryCellModel;

    private void Awake()
    {
        inventoryCellModel = GetComponentInParent<InventoryCellModel>();
    }

    private void OnEnable()
    {
        inventoryCellModel.OnItemDataChanged += RenderItem;
        inventoryCellModel.OnItemSelectionChanged += RenderItem;
        RenderItem();
    }

    private void OnDisable()
    {
        inventoryCellModel.OnItemDataChanged -= RenderItem;
        inventoryCellModel.OnItemSelectionChanged -= RenderItem;
    }

    private void RenderItem()
    {
        if (inventoryCellModel.Data == null)
            return;

        itemNameText.text = inventoryCellModel.Data.displayName;
        selectedHighlight.enabled = inventoryCellModel.IsSelected;
    }
}

public class SelectItemController : MonoBehaviour, IPointerClickHandler
{
    private InventoryCellModel inventoryCellModel;

    private void Awake()
    {
        inventoryCellModel = GetComponentInParent<InventoryCellModel>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        inventoryCellModel.SelectItem();
    }
}
```

## Details

```csharp
public class InventoryDetailsView : MonoBehaviour
{
    [SerializeField] private GameObject emptyRoot;
    [SerializeField] private GameObject selectedItemRoot;
    [SerializeField] private TextMeshProUGUI itemNameText;

    internal InventoryModel inventoryModel;

    private void OnEnable()
    {
        inventoryModel.OnItemSelectionChanged += RenderSelectedItem;
        RenderSelectedItem();
    }

    private void OnDisable()
    {
        inventoryModel.OnItemSelectionChanged -= RenderSelectedItem;
    }

    private void RenderSelectedItem()
    {
        InventoryItemData selectedItem = inventoryModel.SelectedItem;

        emptyRoot.SetActive(selectedItem == null);
        selectedItemRoot.SetActive(selectedItem != null);

        if (selectedItem != null)
            itemNameText.text = selectedItem.displayName;
    }
}

public class DropItemController : MonoBehaviour, IPointerClickHandler
{
    internal InventoryModel inventoryModel;

    public void OnPointerClick(PointerEventData eventData)
    {
        inventoryModel.DropSelectedItem();
    }
}
```

## Tests

```csharp
public class InventoryModelTests
{
    private InventoryModel inventoryModel;

    [SetUp]
    public void SetUp()
    {
        inventoryModel = new InventoryModel();
        inventoryModel.ReplaceAllItems(new List<InventoryItemData>
        {
            new() { id = 1, inventoryTab = InventoryTab.Weapons },
            new() { id = 2, inventoryTab = InventoryTab.Armor }
        });
    }

    [Test]
    public void SelectTab_FiltersItemsAndClearsSelection()
    {
        inventoryModel.SelectItem(1);

        inventoryModel.SelectTab(InventoryTab.Armor);

        Assert.AreEqual(1, inventoryModel.ItemCount);
        Assert.AreEqual(2, inventoryModel.GetItem(0).id);
        Assert.IsNull(inventoryModel.SelectedItem);
    }

    [Test]
    public void DropSelectedItem_WhenNothingSelected_ChangesNothing()
    {
        inventoryModel.DropSelectedItem();

        Assert.AreEqual(1, inventoryModel.ItemCount);
    }
}
```
