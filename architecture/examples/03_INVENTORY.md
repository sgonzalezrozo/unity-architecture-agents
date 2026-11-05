# Example: Inventory

A screen with tabs that filter one list, plus a details panel. Everything is local: no backend.

Shows: tab buttons from one class, a filtered list without allocations, selection shared between list and details.

## Hierarchy

```
InventoryScreen
├── Tabs
│   ├── WeaponsTab [ViewStateMachine, InventoryTabView]   tab = Weapons
│   │   ├── Active
│   │   │   └── Label (TextMeshProUGUI)
│   │   ├── Inactive
│   │   │   └── Label (TextMeshProUGUI)
│   │   └── Actions (SelectTab) [SelectTabController]     tab = Weapons
│   ├── ArmorTab (same, tab = Armor)
│   └── ConsumablesTab (same, tab = Consumables)
├── ItemList [InventoryListView]
│   └── Scroller (RecyclableScrollRect)
└── Details [ViewStateMachine, InventoryDetailsView]
    ├── Empty
    └── Selected
        ├── ItemName (TextMeshProUGUI)
        └── Actions (Drop) [DropItemController]

InventoryCell.prefab [InventoryCellModel, InventoryCellView]
├── ItemName (TextMeshProUGUI)
├── Highlight (Image)
└── Actions (Select) [SelectItemController]
```

The tab is one list filtered three ways, so there is no content prefab per tab.

## Model

```csharp
public enum InventoryTab { Weapons, Armor, Consumables }

[Serializable]
public class InventoryItemData
{
    public int id;
    public string name;
    public InventoryTab tab;
}

public class InventoryModel : IRecyclableDataSource<InventoryItemData>
{
    private const int NO_SELECTION = -1;

    public event Action OnTabChanged;
    public event Action OnItemsChanged;
    public event Action OnSelectionChanged;

    private readonly List<InventoryItemData> allItems = new();
    private readonly List<InventoryItemData> visibleItems = new();

    private InventoryTab selectedTab = InventoryTab.Weapons;
    private int selectedItemId = NO_SELECTION;

    public InventoryTab SelectedTab => selectedTab;
    public int SelectedItemId => selectedItemId;
    public int Count => visibleItems.Count;
    public InventoryItemData SelectedItem => FindItem(selectedItemId);

    public InventoryItemData GetItem(int index)
    {
        return visibleItems[index];
    }

    public void SetItems(List<InventoryItemData> items)
    {
        allItems.Clear();
        allItems.AddRange(items);
        RebuildVisibleItems();
    }

    public void SelectTab(InventoryTab tab)
    {
        if (selectedTab == tab)
            return;

        selectedTab = tab;
        OnTabChanged?.Invoke();
        ClearSelection();
        RebuildVisibleItems();
    }

    public void SelectItem(int itemId)
    {
        if (selectedItemId == itemId)
            return;

        selectedItemId = itemId;
        OnSelectionChanged?.Invoke();
    }

    public void DropSelected()
    {
        InventoryItemData item = SelectedItem;

        if (item == null)
            return;

        allItems.Remove(item);
        ClearSelection();
        RebuildVisibleItems();
    }

    private void ClearSelection()
    {
        selectedItemId = NO_SELECTION;
        OnSelectionChanged?.Invoke();
    }

    private void RebuildVisibleItems()
    {
        visibleItems.Clear();

        for (int i = 0; i < allItems.Count; i++)
        {
            if (allItems[i].tab == selectedTab)
                visibleItems.Add(allItems[i]);
        }

        OnItemsChanged?.Invoke();
    }

    private InventoryItemData FindItem(int itemId)
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

`DropSelected` takes no parameter and validates itself, so `DropItemController` needs no state check.

## Tabs

```csharp
public class InventoryTabView : MonoBehaviour
{
    private const string STATE_ACTIVE = "Active";
    private const string STATE_INACTIVE = "Inactive";

    [SerializeField] private ViewStateMachine states;
    [SerializeField] private InventoryTab tab;

    internal InventoryModel inventory;

    private void OnEnable()
    {
        inventory.OnTabChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        inventory.OnTabChanged -= Refresh;
    }

    private void Refresh()
    {
        states.SetActiveState(inventory.SelectedTab == tab ? STATE_ACTIVE : STATE_INACTIVE);
    }
}

public class SelectTabController : BaseClickController
{
    [SerializeField] private InventoryTab tab;

    internal InventoryModel inventory;

    protected override void OnClick()
    {
        inventory.SelectTab(tab);
    }
}
```

## List and cell

```csharp
public class InventoryListView : MonoBehaviour
{
    [SerializeField] private RecyclableScrollRect scroller;

    internal InventoryModel inventory;

    private void OnEnable()
    {
        inventory.OnItemsChanged += Refresh;

        if (!scroller.IsInitialized)
            scroller.Initialize(inventory);

        Refresh();
    }

    private void OnDisable()
    {
        inventory.OnItemsChanged -= Refresh;
    }

    private void Refresh()
    {
        scroller.Refresh();
    }
}

public class InventoryCellModel : MonoBehaviour, IInjectableForChildren, IRecyclableCell<InventoryItemData>
{
    private const float CELL_HEIGHT = 80f;

    public event Action OnDataChanged;

    public event Action OnSelectionChanged
    {
        add { inventory.OnSelectionChanged += value; }
        remove { inventory.OnSelectionChanged -= value; }
    }

    internal InventoryModel inventory;

    private InventoryItemData data;

    public float DefaultCellSize => CELL_HEIGHT;
    public bool IsSelected => data != null && inventory.SelectedItemId == data.id;

    public InventoryItemData Data
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
            inventory.SelectItem(data.id);
    }
}

public class InventoryCellView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI itemName;
    [SerializeField] private Image highlight;

    private InventoryCellModel cell;

    private void Awake()
    {
        cell = GetComponentInParent<InventoryCellModel>();
    }

    private void OnEnable()
    {
        cell.OnDataChanged += Refresh;
        cell.OnSelectionChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        cell.OnDataChanged -= Refresh;
        cell.OnSelectionChanged -= Refresh;
    }

    private void Refresh()
    {
        if (cell.Data == null)
            return;

        itemName.text = cell.Data.name;
        highlight.enabled = cell.IsSelected;
    }
}

public class SelectItemController : BaseClickController
{
    private InventoryCellModel cell;

    protected override bool InitializeController()
    {
        cell = GetComponentInParent<InventoryCellModel>();
        return true;
    }

    protected override void OnClick()
    {
        cell.Select();
    }
}
```

## Details

```csharp
public class InventoryDetailsView : MonoBehaviour
{
    private const string STATE_SELECTED = "Selected";
    private const string STATE_EMPTY = "Empty";

    [SerializeField] private ViewStateMachine states;
    [SerializeField] private TextMeshProUGUI itemName;

    internal InventoryModel inventory;

    private void OnEnable()
    {
        inventory.OnSelectionChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        inventory.OnSelectionChanged -= Refresh;
    }

    private void Refresh()
    {
        InventoryItemData item = inventory.SelectedItem;

        states.SetActiveState(item == null ? STATE_EMPTY : STATE_SELECTED);

        if (item != null)
            itemName.text = item.name;
    }
}

public class DropItemController : BaseClickController
{
    internal InventoryModel inventory;

    protected override void OnClick()
    {
        inventory.DropSelected();
    }
}
```

## Tests

```csharp
public class InventoryModelTests
{
    private InventoryModel inventory;

    [SetUp]
    public void SetUp()
    {
        inventory = new InventoryModel();
        inventory.SetItems(new List<InventoryItemData>
        {
            new() { id = 1, tab = InventoryTab.Weapons },
            new() { id = 2, tab = InventoryTab.Armor }
        });
    }

    [Test]
    public void SelectTab_FiltersItemsAndClearsSelection()
    {
        inventory.SelectItem(1);

        inventory.SelectTab(InventoryTab.Armor);

        Assert.AreEqual(1, inventory.Count);
        Assert.AreEqual(2, inventory.GetItem(0).id);
        Assert.IsNull(inventory.SelectedItem);
    }

    [Test]
    public void DropSelected_NothingSelected_ChangesNothing()
    {
        inventory.DropSelected();

        Assert.AreEqual(1, inventory.Count);
    }
}
```
