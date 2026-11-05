# Optional packages

**Load when:** writing click handling or visual states.

These rules work with plain Unity APIs. An optional package changes the implementation, never the architecture.

## Capability detection

Run this once per task, before generating UI code:

1. Read `Packages/manifest.json`.
2. If `dependencies` contains `com.sgonzalezrozo.ui-foundations`, set `uiFoundations = true`.
3. Otherwise, set `uiFoundations = false`.

| ID | Rule |
|---|---|
| `OPT-01` | If `uiFoundations = false`, code MUST NOT reference the `UIFoundations` namespace. |
| `OPT-02` | If `uiFoundations = true`, new code SHOULD use the package. |
| `OPT-03` | Code MUST NOT add the package without approval. See [When to ask](AGENT_WORKFLOW.md#when-to-ask). |
| `OPT-04` | Match the code around the change. Don't mix both forms in one feature. |

The examples in this repository use plain Unity APIs. Apply the mapping below when `uiFoundations = true`.

## Mapping

| Need | `uiFoundations = false` | `uiFoundations = true` |
|---|---|---|
| Click input | `MonoBehaviour, IPointerClickHandler` | Subclass of `BaseClickController` |
| Click handler | `public void OnPointerClick(PointerEventData eventData)` | `protected override void OnClick()` |
| Raycast target | Transparent `Image` on the input GameObject | `BaseClickController`, which is a transparent `Graphic` |
| Disable clicks | Disable the input GameObject or put it in an inactive state | `Interactable = false` |
| Cache a cell Model in a Controller | `Awake()` | `protected override bool InitializeController()` |
| Mutually exclusive looks | View with one `GameObject` root per state | `ViewStateMachine` |
| Change the look | `SetActive` on each root | `SetState(string stateName)` |

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
