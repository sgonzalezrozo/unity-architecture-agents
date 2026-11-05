# Example: Trophy Room

A backend feature with two screens that share one Module. `MainRoom` lists trophies. `DetailsTrophy` shows the selected one, with a countdown and a claim action.

Shows: folder layout, Module, Connector, loading states, recyclable list, async action, timer, tests.

Screen navigation is left out because that API is [not specified yet](../FEATURE_STRUCTURE.md#modals-and-navigation).

## Folders

```
Assets/Client/TrophyRoom/
├── MainRoom/
│   ├── MainRoom.prefab
│   ├── TrophyCell.prefab
│   ├── MainRoomLoadController.cs
│   ├── MainRoomStateView.cs
│   ├── TrophyListView.cs
│   ├── TrophyCellModel.cs
│   ├── TrophyCellView.cs
│   └── SelectTrophyController.cs
├── DetailsTrophy/
│   ├── DetailsTrophy.prefab
│   ├── DetailsTrophyView.cs
│   └── ClaimTrophyController.cs
└── Services And definitions/
    ├── TrophyRoomModule.cs
    ├── TrophyData.cs
    ├── ITrophyRoomConnector.cs
    ├── TrophyRoomConnector.cs
    ├── TrophyRoomInstaller.cs
    ├── MainRoom.asset              ScreenDefinition
    └── DetailsTrophy.asset         ScreenDefinition
```

Trophy icons stay in `Assets/Art/`.

## Services And definitions

```csharp
[Serializable]
public class TrophyData
{
    public int id;
    public string name;
    public bool isClaimed;
    public long expiresAtUnixSeconds;
}

public interface ITrophyRoomConnector
{
    Task<List<TrophyData>> FetchTrophiesAsync(CancellationToken token);
    Task<TrophyData> ClaimTrophyAsync(int trophyId, CancellationToken token);
}
```

`TrophyRoomConnector : ITrophyRoomConnector` implements those two calls with the project's backend client. It returns data and holds nothing.

The installer is shown in [Dependency injection](../DEPENDENCY_INJECTION.md#installers).

```csharp
public enum TrophyLoadState { Idle, Loading, Loaded, Failed }
public enum TrophyClaimState { None, Claimable, Claiming, Claimed }

public class TrophyRoomModule : IRecyclableDataSource<TrophyData>
{
    private const int NO_SELECTION = -1;

    public event Action OnLoadStateChanged;
    public event Action OnTrophiesChanged;
    public event Action OnSelectionChanged;

    private readonly ITrophyRoomConnector connector;
    private readonly List<TrophyData> trophies = new();

    private TrophyLoadState loadState = TrophyLoadState.Idle;
    private int selectedTrophyId = NO_SELECTION;
    private bool isClaiming;

    public TrophyLoadState LoadState => loadState;
    public int Count => trophies.Count;
    public int SelectedTrophyId => selectedTrophyId;
    public TrophyData SelectedTrophy => FindTrophy(selectedTrophyId);

    public TrophyClaimState SelectedClaimState
    {
        get
        {
            TrophyData trophy = SelectedTrophy;

            if (trophy == null)
                return TrophyClaimState.None;
            if (trophy.isClaimed)
                return TrophyClaimState.Claimed;

            return isClaiming ? TrophyClaimState.Claiming : TrophyClaimState.Claimable;
        }
    }

    public long SelectedSecondsRemaining
    {
        get
        {
            TrophyData trophy = SelectedTrophy;

            if (trophy == null)
                return 0;

            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return Math.Max(0, trophy.expiresAtUnixSeconds - now);
        }
    }

    public TrophyRoomModule(ITrophyRoomConnector connector)
    {
        this.connector = connector;
    }

    public TrophyData GetItem(int index)
    {
        return trophies[index];
    }

    public void Select(int trophyId)
    {
        if (selectedTrophyId == trophyId)
            return;

        selectedTrophyId = trophyId;
        OnSelectionChanged?.Invoke();
    }

    public async Task LoadAsync(CancellationToken token)
    {
        if (loadState == TrophyLoadState.Loading)
            return;

        TrophyLoadState previousState = loadState;
        SetLoadState(TrophyLoadState.Loading);

        try
        {
            ApplyTrophies(await connector.FetchTrophiesAsync(token));
        }
        catch (OperationCanceledException)
        {
            SetLoadState(previousState);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[TrophyRoom] Load failed: {exception.Message}\n{exception.StackTrace}");
            SetLoadState(TrophyLoadState.Failed);
        }
    }

    public async Task ClaimSelectedAsync(CancellationToken token)
    {
        if (SelectedClaimState != TrophyClaimState.Claimable)
            return;

        SetClaiming(true);

        try
        {
            TrophyData claimed = await connector.ClaimTrophyAsync(selectedTrophyId, token);
            ReplaceTrophy(claimed);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            Debug.LogError($"[TrophyRoom] Claim failed, trophyId: {selectedTrophyId}: {exception.Message}");
        }
        finally
        {
            SetClaiming(false);
        }
    }

    private void ApplyTrophies(List<TrophyData> result)
    {
        trophies.Clear();
        trophies.AddRange(result);
        OnTrophiesChanged?.Invoke();
        SetLoadState(TrophyLoadState.Loaded);
    }

    private void SetLoadState(TrophyLoadState newState)
    {
        loadState = newState;
        OnLoadStateChanged?.Invoke();
    }

    private void SetClaiming(bool claiming)
    {
        isClaiming = claiming;
        OnSelectionChanged?.Invoke();
    }

    private void ReplaceTrophy(TrophyData updated)
    {
        for (int i = 0; i < trophies.Count; i++)
        {
            if (trophies[i].id != updated.id)
                continue;

            trophies[i] = updated;
            OnTrophiesChanged?.Invoke();
            return;
        }
    }

    private TrophyData FindTrophy(int trophyId)
    {
        for (int i = 0; i < trophies.Count; i++)
        {
            if (trophies[i].id == trophyId)
                return trophies[i];
        }

        return null;
    }
}
```

Note that the claim replaces the entry with the backend's response. It never sets `isClaimed` locally.

## MainRoom

```
MainRoom [ViewStateMachine, MainRoomStateView, MainRoomLoadController]
├── Loading
│   └── Spinner (Image)
├── Loaded [TrophyListView]
│   └── Scroller (RecyclableScrollRect)
├── Failed
│   └── Message (TextMeshProUGUI)
└── Idle

TrophyCell.prefab [TrophyCellModel, TrophyCellView]
├── Name (TextMeshProUGUI)
├── ClaimedBadge (Image)
├── SelectedHighlight (Image)
└── Actions (Select) [SelectTrophyController]
```

The Controller starts the load because a screen being shown is an input. It owns the token.

```csharp
public class MainRoomLoadController : MonoBehaviour, IScreenShow
{
    internal TrophyRoomModule module;

    private readonly CancellationTokenSource lifetime = new();

    public void OnScreenShown()
    {
        module.LoadAsync(lifetime.Token).Forget("TrophyRoom.Load");
    }

    private void OnDestroy()
    {
        lifetime.Cancel();
        lifetime.Dispose();
    }
}

public class MainRoomStateView : MonoBehaviour
{
    [SerializeField] private ViewStateMachine states;

    internal TrophyRoomModule module;

    private void OnEnable()
    {
        module.OnLoadStateChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        module.OnLoadStateChanged -= Refresh;
    }

    private void Refresh()
    {
        states.SetActiveState(module.LoadState.ToString());
    }
}

public class TrophyListView : MonoBehaviour
{
    [SerializeField] private RecyclableScrollRect scroller;

    internal TrophyRoomModule module;

    private void OnEnable()
    {
        module.OnTrophiesChanged += Refresh;

        if (!scroller.IsInitialized)
            scroller.Initialize(module);

        Refresh();
    }

    private void OnDisable()
    {
        module.OnTrophiesChanged -= Refresh;
    }

    private void Refresh()
    {
        scroller.Refresh();
    }
}
```

The cell exposes only what its View needs. The View never reaches through to the Module.

The custom `add`/`remove` forward the subscription, so the cell Model needs no lifecycle code of its own.

```csharp
public class TrophyCellModel : MonoBehaviour, IInjectableForChildren, IRecyclableCell<TrophyData>
{
    private const float CELL_HEIGHT = 96f;

    public event Action OnDataChanged;

    public event Action OnSelectionChanged
    {
        add { module.OnSelectionChanged += value; }
        remove { module.OnSelectionChanged -= value; }
    }

    internal TrophyRoomModule module;

    private TrophyData data;

    public float DefaultCellSize => CELL_HEIGHT;
    public bool IsSelected => data != null && module.SelectedTrophyId == data.id;

    public TrophyData Data
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
            module.Select(data.id);
    }
}

public class TrophyCellView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Image claimedBadge;
    [SerializeField] private Image selectedHighlight;

    private TrophyCellModel cell;

    private void Awake()
    {
        cell = GetComponentInParent<TrophyCellModel>();
    }

    private void OnEnable()
    {
        cell.OnDataChanged += Refresh;
        cell.OnSelectionChanged += RefreshSelection;
        Refresh();
    }

    private void OnDisable()
    {
        cell.OnDataChanged -= Refresh;
        cell.OnSelectionChanged -= RefreshSelection;
    }

    private void Refresh()
    {
        if (cell.Data == null)
            return;

        nameText.text = cell.Data.name;
        claimedBadge.enabled = cell.Data.isClaimed;
        RefreshSelection();
    }

    private void RefreshSelection()
    {
        selectedHighlight.enabled = cell.IsSelected;
    }
}

public class SelectTrophyController : BaseClickController
{
    private TrophyCellModel cell;

    protected override bool InitializeController()
    {
        cell = GetComponentInParent<TrophyCellModel>();
        return true;
    }

    protected override void OnClick()
    {
        cell.Select();
    }
}
```

## DetailsTrophy

`DetailsTrophy` is a different screen, yet it needs nothing passed to it. It reads the selection from the Module.

```
DetailsTrophy [DetailsTrophyView]
├── Name (TextMeshProUGUI)
├── Timer (TextMeshProUGUI)
└── ClaimArea [ViewStateMachine]
    ├── None
    ├── Claimable
    │   └── Actions (Claim) [ClaimTrophyController]
    ├── Claiming
    │   └── Spinner (Image)
    └── Claimed
        └── Badge (Image)
```

```csharp
public class DetailsTrophyView : MonoBehaviour
{
    private const float TIMER_REFRESH_SECONDS = 1f;

    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private ViewStateMachine claimStates;

    internal TrophyRoomModule module;

    private float nextTimerRefresh;

    private void OnEnable()
    {
        module.OnSelectionChanged += Refresh;
        module.OnTrophiesChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        module.OnSelectionChanged -= Refresh;
        module.OnTrophiesChanged -= Refresh;
    }

    private void Update()
    {
        if (Time.unscaledTime < nextTimerRefresh)
            return;

        nextTimerRefresh = Time.unscaledTime + TIMER_REFRESH_SECONDS;
        RefreshTimer();
    }

    private void Refresh()
    {
        TrophyData trophy = module.SelectedTrophy;

        nameText.text = trophy == null ? string.Empty : trophy.name;
        claimStates.SetActiveState(module.SelectedClaimState.ToString());
        RefreshTimer();
    }

    private void RefreshTimer()
    {
        timerText.text = module.SelectedSecondsRemaining.ToString();
    }
}

public class ClaimTrophyController : BaseClickController
{
    internal TrophyRoomModule module;

    private readonly CancellationTokenSource lifetime = new();

    protected override void OnClick()
    {
        module.ClaimSelectedAsync(lifetime.Token).Forget("TrophyRoom.Claim");
    }

    private void OnDestroy()
    {
        lifetime.Cancel();
        lifetime.Dispose();
    }
}
```

If `BaseClickController` declares its own `OnDestroy`, override it and call the base instead of the private method above.

## Tests

```csharp
public class TrophyRoomModuleTests
{
    private ITrophyRoomConnector connector;
    private TrophyRoomModule module;

    [SetUp]
    public void SetUp()
    {
        connector = Substitute.For<ITrophyRoomConnector>();
        module = new TrophyRoomModule(connector);
    }

    [Test]
    public async Task LoadAsync_Success_ExposesTrophies()
    {
        List<TrophyData> trophies = new() { new TrophyData { id = 1 } };
        connector.FetchTrophiesAsync(Arg.Any<CancellationToken>()).Returns(trophies);

        await module.LoadAsync(CancellationToken.None);

        Assert.AreEqual(TrophyLoadState.Loaded, module.LoadState);
        Assert.AreEqual(1, module.Count);
    }

    [Test]
    public async Task LoadAsync_EmptyList_IsLoadedNotFailed()
    {
        connector.FetchTrophiesAsync(Arg.Any<CancellationToken>()).Returns(new List<TrophyData>());

        await module.LoadAsync(CancellationToken.None);

        Assert.AreEqual(TrophyLoadState.Loaded, module.LoadState);
    }

    [Test]
    public async Task ClaimSelectedAsync_Success_ReplacesEntry()
    {
        TrophyData original = new() { id = 1 };
        TrophyData claimed = new() { id = 1, isClaimed = true };
        connector.FetchTrophiesAsync(Arg.Any<CancellationToken>()).Returns(new List<TrophyData> { original });
        connector.ClaimTrophyAsync(1, Arg.Any<CancellationToken>()).Returns(claimed);
        await module.LoadAsync(CancellationToken.None);
        module.Select(1);

        await module.ClaimSelectedAsync(CancellationToken.None);

        Assert.AreEqual(TrophyClaimState.Claimed, module.SelectedClaimState);
        Assert.IsFalse(original.isClaimed);
    }
}
```
