# Example: backend feature (Trophy Room)

**Load when:** a feature calls the backend.

Two screens share one Module. `MainRoom` lists trophies. `DetailsTrophy` renders the selected trophy, with a countdown and a claim command.

**Demonstrates:** folder layout, Module, Connector, load states, recyclable list, async command, timer, tests.

Capability profile: `fastIoC`, `recyclableScroller` and `screenSystem` are `true`, and `uiFoundations` is `false`. For another profile, apply the [mapping](../OPTIONAL_PACKAGES.md#mapping). Screen navigation is omitted because that API is [not specified yet](../FEATURE_STRUCTURE.md#modals-and-navigation).

## Files

```
Assets/Client/TrophyRoom/
├── MainRoom/
│   ├── MainRoom.prefab
│   ├── TrophyCell.prefab
│   ├── MainRoomLoadController.cs
│   ├── MainRoomStateView.cs
│   ├── TrophyListView.cs
│   ├── TrophyScroller.cs
│   ├── TrophyCellModel.cs
│   ├── TrophyCellView.cs
│   └── SelectTrophyController.cs
├── DetailsTrophy/
│   ├── DetailsTrophy.prefab
│   ├── DetailsTrophyView.cs
│   ├── ClaimAreaView.cs
│   └── ClaimTrophyController.cs
└── Services/
    ├── TrophyRoomModule.cs
    ├── TrophyData.cs
    ├── ITrophyRoomConnector.cs
    ├── TrophyRoomConnector.cs
    ├── TrophyRoomInstaller.cs
    ├── MainRoom.asset              ScreenDefinition
    └── DetailsTrophy.asset         ScreenDefinition
```

Trophy icons stay in `Assets/Art/`.

## Services

```csharp
[Serializable]
public class TrophyData
{
    public int id;
    public string displayName;
    public bool isClaimed;
    public long expiresAtUnixSeconds;
}

public interface ITrophyRoomConnector
{
    Task<List<TrophyData>> FetchTrophiesAsync(CancellationToken cancellationToken);
    Task<TrophyData> ClaimTrophyAsync(int trophyId, CancellationToken cancellationToken);
}
```

`TrophyRoomConnector` implements both calls with the project's backend client. It returns data and holds no state. The installer is in [Dependency injection](../DEPENDENCY_INJECTION.md#installers).

```csharp
public enum TrophyLoadState { Idle, Loading, Loaded, Failed }
public enum TrophyClaimState { NoSelection, Claimable, Claiming, Claimed }

public class TrophyRoomModule : IRecyclableDataSource<TrophyData>
{
    private const int NO_SELECTION = -1;

    public event Action OnLoadStateChanged;
    public event Action OnTrophiesChanged;
    public event Action OnTrophySelectionChanged;

    private readonly ITrophyRoomConnector trophyRoomConnector;
    private readonly List<TrophyData> trophies = new();

    private TrophyLoadState loadState = TrophyLoadState.Idle;
    private int selectedTrophyId = NO_SELECTION;
    private bool isClaiming;

    public TrophyLoadState LoadState => loadState;
    public int ItemCount => trophies.Count;
    public int SelectedTrophyId => selectedTrophyId;
    public TrophyData SelectedTrophy => FindTrophyById(selectedTrophyId);

    public TrophyClaimState SelectedTrophyClaimState
    {
        get
        {
            TrophyData selectedTrophy = SelectedTrophy;

            if (selectedTrophy == null)
                return TrophyClaimState.NoSelection;
            if (selectedTrophy.isClaimed)
                return TrophyClaimState.Claimed;

            return isClaiming ? TrophyClaimState.Claiming : TrophyClaimState.Claimable;
        }
    }

    public long SelectedTrophySecondsRemaining
    {
        get
        {
            TrophyData selectedTrophy = SelectedTrophy;

            if (selectedTrophy == null)
                return 0;

            long nowUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return Math.Max(0, selectedTrophy.expiresAtUnixSeconds - nowUnixSeconds);
        }
    }

    public TrophyRoomModule(ITrophyRoomConnector trophyRoomConnector)
    {
        this.trophyRoomConnector = trophyRoomConnector;
    }

    public TrophyData GetItem(int index)
    {
        return trophies[index];
    }

    public IReadOnlyList<TrophyData> GetAllItems()
    {
        return trophies;
    }

    public void SelectTrophy(int trophyId)
    {
        if (selectedTrophyId == trophyId)
            return;

        selectedTrophyId = trophyId;
        OnTrophySelectionChanged?.Invoke();
    }

    public async Task LoadTrophiesAsync(CancellationToken cancellationToken)
    {
        if (loadState == TrophyLoadState.Loading)
            return;

        TrophyLoadState previousLoadState = loadState;
        SetLoadState(TrophyLoadState.Loading);

        try
        {
            StoreFetchedTrophies(await trophyRoomConnector.FetchTrophiesAsync(cancellationToken));
        }
        catch (OperationCanceledException)
        {
            SetLoadState(previousLoadState);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[TrophyRoom] Load failed: {exception.Message}\n{exception.StackTrace}");
            SetLoadState(TrophyLoadState.Failed);
        }
    }

    public async Task ClaimSelectedTrophyAsync(CancellationToken cancellationToken)
    {
        if (SelectedTrophyClaimState != TrophyClaimState.Claimable)
            return;

        SetIsClaiming(true);

        try
        {
            ReplaceTrophy(await trophyRoomConnector.ClaimTrophyAsync(selectedTrophyId, cancellationToken));
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
            SetIsClaiming(false);
        }
    }

    private void StoreFetchedTrophies(List<TrophyData> fetchedTrophies)
    {
        trophies.Clear();
        trophies.AddRange(fetchedTrophies);
        OnTrophiesChanged?.Invoke();
        SetLoadState(TrophyLoadState.Loaded);
    }

    private void SetLoadState(TrophyLoadState newLoadState)
    {
        loadState = newLoadState;
        OnLoadStateChanged?.Invoke();
    }

    private void SetIsClaiming(bool newIsClaiming)
    {
        isClaiming = newIsClaiming;
        OnTrophySelectionChanged?.Invoke();
    }

    private void ReplaceTrophy(TrophyData updatedTrophy)
    {
        for (int i = 0; i < trophies.Count; i++)
        {
            if (trophies[i].id != updatedTrophy.id)
                continue;

            trophies[i] = updatedTrophy;
            OnTrophiesChanged?.Invoke();
            return;
        }
    }

    private TrophyData FindTrophyById(int trophyId)
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

The claim replaces the entry with the backend's response (`BE-01`). It never sets `isClaimed` locally.

## MainRoom screen

```
MainRoom [MainRoomStateView, MainRoomLoadController]
├── LoadingRoot
│   └── LoadingSpinner (Image)
├── LoadedRoot [TrophyListView]
│   └── TrophyScroller [TrophyScroller]
└── FailedRoot
    └── FailureMessageText (TextMeshProUGUI)

TrophyCell.prefab [TrophyCellModel, TrophyCellView]
├── TrophyNameText (TextMeshProUGUI)
├── ClaimedBadge (Image)
├── SelectedHighlight (Image)
└── SelectTrophyInput (Image: alpha 0) [SelectTrophyController]
```

The Controller starts the load because a shown screen is input (`CTRL-06`). It owns the token (`BE-07`).

```csharp
public class MainRoomLoadController : MonoBehaviour, IScreenShow
{
    internal TrophyRoomModule trophyRoomModule;

    private readonly CancellationTokenSource destroyCancellation = new();

    public void OnScreenShown()
    {
        trophyRoomModule.LoadTrophiesAsync(destroyCancellation.Token).Forget("TrophyRoom.LoadTrophies");
    }

    private void OnDestroy()
    {
        destroyCancellation.Cancel();
        destroyCancellation.Dispose();
    }
}

public class MainRoomStateView : MonoBehaviour
{
    [SerializeField] private GameObject loadingRoot;
    [SerializeField] private GameObject loadedRoot;
    [SerializeField] private GameObject failedRoot;

    internal TrophyRoomModule trophyRoomModule;

    private void OnEnable()
    {
        trophyRoomModule.OnLoadStateChanged += RenderLoadState;
        RenderLoadState();
    }

    private void OnDisable()
    {
        trophyRoomModule.OnLoadStateChanged -= RenderLoadState;
    }

    private void RenderLoadState()
    {
        TrophyLoadState loadState = trophyRoomModule.LoadState;

        loadingRoot.SetActive(loadState == TrophyLoadState.Loading);
        loadedRoot.SetActive(loadState == TrophyLoadState.Loaded);
        failedRoot.SetActive(loadState == TrophyLoadState.Failed);
    }
}

public class TrophyScroller : RecyclableScrollRect<TrophyData>
{
}

public class TrophyListView : MonoBehaviour
{
    [SerializeField] private TrophyScroller trophyScroller;

    internal TrophyRoomModule trophyRoomModule;

    private void OnEnable()
    {
        trophyRoomModule.OnTrophiesChanged += RenderTrophies;

        if (!trophyScroller.IsInitialized)
            trophyScroller.Initialize(trophyRoomModule);

        RenderTrophies();
    }

    private void OnDisable()
    {
        trophyRoomModule.OnTrophiesChanged -= RenderTrophies;
    }

    private void RenderTrophies()
    {
        trophyScroller.Refresh();
    }
}
```

The Cell Model exposes only what its View needs. The View never reaches through to the Module. The custom `add` and `remove` forward the subscription, so the Cell Model needs no lifecycle code.

```csharp
public class TrophyCellModel : MonoBehaviour, IInjectableForChildren, IRecyclableCell<TrophyData>
{
    private const float CELL_HEIGHT = 96f;

    public event Action OnTrophyDataChanged;

    public event Action OnTrophySelectionChanged
    {
        add { trophyRoomModule.OnTrophySelectionChanged += value; }
        remove { trophyRoomModule.OnTrophySelectionChanged -= value; }
    }

    internal TrophyRoomModule trophyRoomModule;

    private TrophyData trophyData;

    public float DefaultCellSize => CELL_HEIGHT;
    public bool IsSelected => trophyData != null && trophyRoomModule.SelectedTrophyId == trophyData.id;

    public TrophyData Data
    {
        get => trophyData;
        set
        {
            trophyData = value;
            OnTrophyDataChanged?.Invoke();
        }
    }

    public void SelectTrophy()
    {
        if (trophyData != null)
            trophyRoomModule.SelectTrophy(trophyData.id);
    }
}

public class TrophyCellView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI trophyNameText;
    [SerializeField] private Image claimedBadge;
    [SerializeField] private Image selectedHighlight;

    private TrophyCellModel trophyCellModel;

    private void Awake()
    {
        trophyCellModel = GetComponentInParent<TrophyCellModel>();
    }

    private void OnEnable()
    {
        trophyCellModel.OnTrophyDataChanged += RenderTrophy;
        trophyCellModel.OnTrophySelectionChanged += RenderSelection;
        RenderTrophy();
    }

    private void OnDisable()
    {
        trophyCellModel.OnTrophyDataChanged -= RenderTrophy;
        trophyCellModel.OnTrophySelectionChanged -= RenderSelection;
    }

    private void RenderTrophy()
    {
        if (trophyCellModel.Data == null)
            return;

        trophyNameText.text = trophyCellModel.Data.displayName;
        claimedBadge.enabled = trophyCellModel.Data.isClaimed;
        RenderSelection();
    }

    private void RenderSelection()
    {
        selectedHighlight.enabled = trophyCellModel.IsSelected;
    }
}

public class SelectTrophyController : MonoBehaviour, IPointerClickHandler
{
    private TrophyCellModel trophyCellModel;

    private void Awake()
    {
        trophyCellModel = GetComponentInParent<TrophyCellModel>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        trophyCellModel.SelectTrophy();
    }
}
```

## DetailsTrophy screen

`DetailsTrophy` receives nothing from `MainRoom`. It reads the selection from the Module (`ARCH-04`).

```
DetailsTrophy [DetailsTrophyView]
├── TrophyNameText (TextMeshProUGUI)
├── SecondsRemainingText (TextMeshProUGUI)
└── ClaimArea [ClaimAreaView]
    ├── ClaimableRoot
    │   └── ClaimTrophyInput (Image: alpha 0) [ClaimTrophyController]
    ├── ClaimingRoot
    │   └── ClaimingSpinner (Image)
    └── ClaimedRoot
        └── ClaimedBadge (Image)
```

```csharp
public class DetailsTrophyView : MonoBehaviour
{
    private const float COUNTDOWN_RENDER_INTERVAL_SECONDS = 1f;

    [SerializeField] private TextMeshProUGUI trophyNameText;
    [SerializeField] private TextMeshProUGUI secondsRemainingText;

    internal TrophyRoomModule trophyRoomModule;

    private float nextCountdownRenderTime;

    private void OnEnable()
    {
        trophyRoomModule.OnTrophySelectionChanged += RenderSelectedTrophy;
        trophyRoomModule.OnTrophiesChanged += RenderSelectedTrophy;
        RenderSelectedTrophy();
    }

    private void OnDisable()
    {
        trophyRoomModule.OnTrophySelectionChanged -= RenderSelectedTrophy;
        trophyRoomModule.OnTrophiesChanged -= RenderSelectedTrophy;
    }

    private void Update()
    {
        if (Time.unscaledTime < nextCountdownRenderTime)
            return;

        nextCountdownRenderTime = Time.unscaledTime + COUNTDOWN_RENDER_INTERVAL_SECONDS;
        RenderCountdown();
    }

    private void RenderSelectedTrophy()
    {
        TrophyData selectedTrophy = trophyRoomModule.SelectedTrophy;

        trophyNameText.text = selectedTrophy == null ? string.Empty : selectedTrophy.displayName;
        RenderCountdown();
    }

    private void RenderCountdown()
    {
        secondsRemainingText.text = trophyRoomModule.SelectedTrophySecondsRemaining.ToString();
    }
}

public class ClaimAreaView : MonoBehaviour
{
    [SerializeField] private GameObject claimableRoot;
    [SerializeField] private GameObject claimingRoot;
    [SerializeField] private GameObject claimedRoot;

    internal TrophyRoomModule trophyRoomModule;

    private void OnEnable()
    {
        trophyRoomModule.OnTrophySelectionChanged += RenderClaimState;
        trophyRoomModule.OnTrophiesChanged += RenderClaimState;
        RenderClaimState();
    }

    private void OnDisable()
    {
        trophyRoomModule.OnTrophySelectionChanged -= RenderClaimState;
        trophyRoomModule.OnTrophiesChanged -= RenderClaimState;
    }

    private void RenderClaimState()
    {
        TrophyClaimState claimState = trophyRoomModule.SelectedTrophyClaimState;

        claimableRoot.SetActive(claimState == TrophyClaimState.Claimable);
        claimingRoot.SetActive(claimState == TrophyClaimState.Claiming);
        claimedRoot.SetActive(claimState == TrophyClaimState.Claimed);
    }
}

public class ClaimTrophyController : MonoBehaviour, IPointerClickHandler
{
    internal TrophyRoomModule trophyRoomModule;

    private readonly CancellationTokenSource destroyCancellation = new();

    public void OnPointerClick(PointerEventData eventData)
    {
        trophyRoomModule.ClaimSelectedTrophyAsync(destroyCancellation.Token).Forget("TrophyRoom.ClaimTrophy");
    }

    private void OnDestroy()
    {
        destroyCancellation.Cancel();
        destroyCancellation.Dispose();
    }
}
```

## Tests

```csharp
public class TrophyRoomModuleTests
{
    private ITrophyRoomConnector trophyRoomConnector;
    private TrophyRoomModule trophyRoomModule;

    [SetUp]
    public void SetUp()
    {
        trophyRoomConnector = Substitute.For<ITrophyRoomConnector>();
        trophyRoomModule = new TrophyRoomModule(trophyRoomConnector);
    }

    [Test]
    public async Task LoadTrophiesAsync_WhenFetchSucceeds_ExposesTrophies()
    {
        List<TrophyData> fetchedTrophies = new() { new TrophyData { id = 1 } };
        trophyRoomConnector.FetchTrophiesAsync(Arg.Any<CancellationToken>()).Returns(fetchedTrophies);

        await trophyRoomModule.LoadTrophiesAsync(CancellationToken.None);

        Assert.AreEqual(TrophyLoadState.Loaded, trophyRoomModule.LoadState);
        Assert.AreEqual(1, trophyRoomModule.ItemCount);
    }

    [Test]
    public async Task LoadTrophiesAsync_WhenListIsEmpty_IsLoadedNotFailed()
    {
        trophyRoomConnector.FetchTrophiesAsync(Arg.Any<CancellationToken>()).Returns(new List<TrophyData>());

        await trophyRoomModule.LoadTrophiesAsync(CancellationToken.None);

        Assert.AreEqual(TrophyLoadState.Loaded, trophyRoomModule.LoadState);
    }

    [Test]
    public async Task ClaimSelectedTrophyAsync_WhenClaimSucceeds_ReplacesEntry()
    {
        TrophyData originalTrophy = new() { id = 1 };
        TrophyData claimedTrophy = new() { id = 1, isClaimed = true };
        trophyRoomConnector.FetchTrophiesAsync(Arg.Any<CancellationToken>()).Returns(new List<TrophyData> { originalTrophy });
        trophyRoomConnector.ClaimTrophyAsync(1, Arg.Any<CancellationToken>()).Returns(claimedTrophy);
        await trophyRoomModule.LoadTrophiesAsync(CancellationToken.None);
        trophyRoomModule.SelectTrophy(1);

        await trophyRoomModule.ClaimSelectedTrophyAsync(CancellationToken.None);

        Assert.AreEqual(TrophyClaimState.Claimed, trophyRoomModule.SelectedTrophyClaimState);
        Assert.IsFalse(originalTrophy.isClaimed);
    }
}
```
