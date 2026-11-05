# Example: custom input (Settings)

**Load when:** replacing `Toggle` or `Slider`, or building drag input.

One toggle prefab is reused for every on/off setting. Adding a setting takes one enum value, one array entry and one prefab instance.

**Demonstrates:** reusable prefab driven by config values, visual state roots, drag input.

Code uses plain Unity APIs. If `uiFoundations = true`, apply [Optional packages](../OPTIONAL_PACKAGES.md).

## Hierarchy

```
SettingsScreen
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

public class SettingsModel
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

    internal SettingsModel settingsModel;

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

    internal SettingsModel settingsModel;

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

    internal SettingsModel settingsModel;

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
    internal SettingsModel settingsModel;

    private RectTransform barRectTransform;

    private void Awake()
    {
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

    internal SettingsModel settingsModel;

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
    internal SettingsModel settingsModel;

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
    [Test]
    public void ToggleSetting_ChangesValueAndMarksUnsavedChanges()
    {
        SettingsModel settingsModel = new();

        settingsModel.ToggleSetting(SettingKey.Vibration);

        Assert.IsTrue(settingsModel.IsSettingEnabled(SettingKey.Vibration));
        Assert.IsTrue(settingsModel.HasUnsavedChanges);
    }

    [Test]
    public void SetNormalizedVolume_WhenOutOfRange_ClampsValue()
    {
        SettingsModel settingsModel = new();

        settingsModel.SetNormalizedVolume(2f);

        Assert.AreEqual(1f, settingsModel.NormalizedVolume);
    }

    [Test]
    public void SaveSettings_WhenNoUnsavedChanges_DoesNotNotify()
    {
        SettingsModel settingsModel = new();
        int notificationCount = 0;
        settingsModel.OnSettingsChanged += () => notificationCount++;

        settingsModel.SaveSettings();

        Assert.AreEqual(0, notificationCount);
    }
}
```
