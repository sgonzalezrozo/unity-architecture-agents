# Example: custom input (Settings)

**Load when:** replacing `Toggle` or `Slider`, or building drag input.

One toggle prefab is reused for every on/off setting. Adding a setting takes one enum value, one array entry and one prefab instance.

**Demonstrates:** reusable prefab driven by config values, visual state roots, drag input.

Capability profile: `fastIoC`, `recyclableScroller` and `screenSystem` are `true`, and `uiFoundations` is `false`. For another profile, apply the [mapping](../OPTIONAL_PACKAGES.md#mapping).

All Views and Controllers sit under one screen, so `SettingsModel` lives on the screen root (row 1 in [Where a Model lives](../MVC.md#where-a-model-lives)). No installer is needed.

## Hierarchy

```
SettingsScreen [SettingsModel]
├── SettingRows
│   ├── MusicRow (SettingToggle.prefab: settingKey = Music)
│   ├── SoundRow (SettingToggle.prefab: settingKey = Sound)
│   ├── VibrationRow (SettingToggle.prefab: settingKey = Vibration)
│   └── VolumeRow [VolumeBarView]
│       ├── VolumeTrack (Image)
│       ├── VolumeFill (Image: type Filled)
│       └── DragVolumeInput (Image: alpha 0) [DragVolumeController]
└── Footer [SaveAreaView]
    └── UnsavedChangesRoot
        └── SaveSettingsInput (Image: alpha 0) [SaveSettingsController]

SettingToggle.prefab [SettingToggleView]
├── EnabledRoot
│   └── EnabledBackground (Image)
├── DisabledRoot
│   └── DisabledBackground (Image)
└── ToggleSettingInput (Image: alpha 0) [ToggleSettingController]
```

`ToggleSettingInput` sits outside the state roots because both states accept clicks. `DragVolumeController` uses its own `RectTransform` as the bar.

## Model

One enum and one array replace three identical classes.

```csharp
public enum SettingKey { Music, Sound, Vibration }

public class SettingsModel : MonoBehaviour
{
    private const float DEFAULT_VOLUME = 0.8f;

    public event Action OnSettingsChanged;

    private readonly bool[] enabledSettings = { true, true, false };

    private float normalizedVolume = DEFAULT_VOLUME;
    private bool hasUnsavedChanges;

    public float NormalizedVolume => normalizedVolume;
    public bool HasUnsavedChanges => hasUnsavedChanges;

    public bool IsSettingEnabled(SettingKey settingKey)
    {
        return enabledSettings[(int)settingKey];
    }

    public void ToggleSetting(SettingKey settingKey)
    {
        enabledSettings[(int)settingKey] = !enabledSettings[(int)settingKey];
        MarkUnsavedChanges();
    }

    public void SetNormalizedVolume(float newNormalizedVolume)
    {
        float clampedVolume = Mathf.Clamp01(newNormalizedVolume);

        if (Mathf.Approximately(normalizedVolume, clampedVolume))
            return;

        normalizedVolume = clampedVolume;
        MarkUnsavedChanges();
    }

    public void SaveSettings()
    {
        if (!hasUnsavedChanges)
            return;

        hasUnsavedChanges = false;
        OnSettingsChanged?.Invoke();
    }

    private void MarkUnsavedChanges()
    {
        hasUnsavedChanges = true;
        OnSettingsChanged?.Invoke();
    }
}
```

## Toggle

The serialized `SettingKey` is a config value, so both classes MAY have it. They never reference each other: they query the same key on the Model.

```csharp
public class SettingToggleView : MonoBehaviour
{
    [SerializeField] private GameObject enabledRoot;
    [SerializeField] private GameObject disabledRoot;
    [SerializeField] private SettingKey settingKey;

    private SettingsModel settingsModel;

    private void Awake()
    {
        settingsModel = GetComponentInParent<SettingsModel>();
    }

    private void OnEnable()
    {
        settingsModel.OnSettingsChanged += RenderSettingState;
        RenderSettingState();
    }

    private void OnDisable()
    {
        settingsModel.OnSettingsChanged -= RenderSettingState;
    }

    private void RenderSettingState()
    {
        bool isSettingEnabled = settingsModel.IsSettingEnabled(settingKey);

        enabledRoot.SetActive(isSettingEnabled);
        disabledRoot.SetActive(!isSettingEnabled);
    }
}

public class ToggleSettingController : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private SettingKey settingKey;

    private SettingsModel settingsModel;

    private void Awake()
    {
        settingsModel = GetComponentInParent<SettingsModel>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        settingsModel.ToggleSetting(settingKey);
    }
}
```

## Volume bar

The Controller only converts a pointer position to 0 to 1 (`CTRL-04`). Clamping and change detection belong to the Model.

```csharp
public class VolumeBarView : MonoBehaviour
{
    [SerializeField] private Image volumeFill;

    private SettingsModel settingsModel;

    private void Awake()
    {
        settingsModel = GetComponentInParent<SettingsModel>();
    }

    private void OnEnable()
    {
        settingsModel.OnSettingsChanged += RenderVolume;
        RenderVolume();
    }

    private void OnDisable()
    {
        settingsModel.OnSettingsChanged -= RenderVolume;
    }

    private void RenderVolume()
    {
        volumeFill.fillAmount = settingsModel.NormalizedVolume;
    }
}

public class DragVolumeController : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    private SettingsModel settingsModel;
    private RectTransform barRectTransform;

    private void Awake()
    {
        settingsModel = GetComponentInParent<SettingsModel>();
        barRectTransform = (RectTransform)transform;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        SetVolumeFromPointer(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        SetVolumeFromPointer(eventData);
    }

    private void SetVolumeFromPointer(PointerEventData eventData)
    {
        bool isPointerOnBarPlane = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            barRectTransform, eventData.position, eventData.pressEventCamera, out Vector2 localPointerPosition);

        if (!isPointerOnBarPlane)
            return;

        settingsModel.SetNormalizedVolume(Rect.PointToNormalized(barRectTransform.rect, localPointerPosition).x);
    }
}
```

## Save

```csharp
public class SaveAreaView : MonoBehaviour
{
    [SerializeField] private GameObject unsavedChangesRoot;

    private SettingsModel settingsModel;

    private void Awake()
    {
        settingsModel = GetComponentInParent<SettingsModel>();
    }

    private void OnEnable()
    {
        settingsModel.OnSettingsChanged += RenderSaveState;
        RenderSaveState();
    }

    private void OnDisable()
    {
        settingsModel.OnSettingsChanged -= RenderSaveState;
    }

    private void RenderSaveState()
    {
        unsavedChangesRoot.SetActive(settingsModel.HasUnsavedChanges);
    }
}

public class SaveSettingsController : MonoBehaviour, IPointerClickHandler
{
    private SettingsModel settingsModel;

    private void Awake()
    {
        settingsModel = GetComponentInParent<SettingsModel>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        settingsModel.SaveSettings();
    }
}
```

## Tests

```csharp
public class SettingsModelTests
{
    private SettingsModel settingsModel;

    [SetUp]
    public void SetUp()
    {
        settingsModel = new GameObject().AddComponent<SettingsModel>();
    }

    [TearDown]
    public void TearDown()
    {
        UnityEngine.Object.DestroyImmediate(settingsModel.gameObject);
    }

    [Test]
    public void ToggleSetting_ChangesValueAndMarksUnsavedChanges()
    {
        settingsModel.ToggleSetting(SettingKey.Vibration);

        Assert.IsTrue(settingsModel.IsSettingEnabled(SettingKey.Vibration));
        Assert.IsTrue(settingsModel.HasUnsavedChanges);
    }

    [Test]
    public void SetNormalizedVolume_WhenOutOfRange_ClampsValue()
    {
        settingsModel.SetNormalizedVolume(2f);

        Assert.AreEqual(1f, settingsModel.NormalizedVolume);
    }

    [Test]
    public void SaveSettings_WhenNoUnsavedChanges_DoesNotNotify()
    {
        int notificationCount = 0;
        settingsModel.OnSettingsChanged += () => notificationCount++;

        settingsModel.SaveSettings();

        Assert.AreEqual(0, notificationCount);
    }
}
```
