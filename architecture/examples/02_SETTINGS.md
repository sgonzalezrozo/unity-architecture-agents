# Example: Settings

Replaces `Toggle` and `Slider` with our own Views and Controllers. One toggle prefab is reused for every on/off setting.

Shows: reusable prefab driven by config values, `ViewStateMachine`, drag input.

## Hierarchy

```
SettingsScreen
├── Rows
│   ├── MusicRow (SettingToggle.prefab, key = Music)
│   ├── SoundRow (SettingToggle.prefab, key = Sound)
│   ├── VibrationRow (SettingToggle.prefab, key = Vibration)
│   └── VolumeRow [VolumeBarView]
│       ├── Track (Image)
│       ├── Fill (Image, type Filled)
│       └── Actions (Drag) [VolumeDragController]
└── Footer [ViewStateMachine, SaveAreaView]
    ├── Clean
    └── Dirty
        └── Actions (Save) [SaveSettingsController]

SettingToggle.prefab [ViewStateMachine, SettingToggleView]
├── On
│   └── Background (Image, green)
├── Off
│   └── Background (Image, gray)
└── Actions (Toggle) [ToggleSettingController]
```

`Actions` sits outside the states because both On and Off accept clicks, and `VolumeDragController` uses the `Actions` RectTransform as the bar.

## Model

One enum and one array replace three identical classes (`MusicToggleModel`, `SoundToggleModel`, `VibrateToggleModel`).

```csharp
public enum SettingKey { Music, Sound, Vibration }

public class SettingsModel
{
    private const float DEFAULT_VOLUME = 0.8f;

    public event Action OnSettingsChanged;

    private readonly bool[] enabledSettings = { true, true, false };

    private float volume = DEFAULT_VOLUME;
    private bool isDirty;

    public float Volume => volume;
    public bool IsDirty => isDirty;

    public bool IsEnabled(SettingKey key)
    {
        return enabledSettings[(int)key];
    }

    public void Toggle(SettingKey key)
    {
        enabledSettings[(int)key] = !enabledSettings[(int)key];
        MarkDirty();
    }

    public void SetVolume(float normalizedVolume)
    {
        float clamped = Mathf.Clamp01(normalizedVolume);

        if (Mathf.Approximately(volume, clamped))
            return;

        volume = clamped;
        MarkDirty();
    }

    public void Save()
    {
        if (!isDirty)
            return;

        isDirty = false;
        OnSettingsChanged?.Invoke();
    }

    private void MarkDirty()
    {
        isDirty = true;
        OnSettingsChanged?.Invoke();
    }
}
```

## Toggle

The serialized `SettingKey` is a config value, so it is allowed on both the View and the Controller. They never talk to each other; they just ask the Model about the same key.

```csharp
public class SettingToggleView : MonoBehaviour
{
    private const string STATE_ON = "On";
    private const string STATE_OFF = "Off";

    [SerializeField] private ViewStateMachine states;
    [SerializeField] private SettingKey key;

    internal SettingsModel settings;

    private void OnEnable()
    {
        settings.OnSettingsChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        settings.OnSettingsChanged -= Refresh;
    }

    private void Refresh()
    {
        states.SetActiveState(settings.IsEnabled(key) ? STATE_ON : STATE_OFF);
    }
}

public class ToggleSettingController : BaseClickController
{
    [SerializeField] private SettingKey key;

    internal SettingsModel settings;

    protected override void OnClick()
    {
        settings.Toggle(key);
    }
}
```

## Volume bar

Only converting a pointer position into 0 to 1 happens here. Clamping and change detection are the Model's.

```csharp
public class VolumeBarView : MonoBehaviour
{
    [SerializeField] private Image fill;

    internal SettingsModel settings;

    private void OnEnable()
    {
        settings.OnSettingsChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        settings.OnSettingsChanged -= Refresh;
    }

    private void Refresh()
    {
        fill.fillAmount = settings.Volume;
    }
}

public class VolumeDragController : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    internal SettingsModel settings;

    private RectTransform bar;

    private void Awake()
    {
        bar = (RectTransform)transform;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        ApplyPointer(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        ApplyPointer(eventData);
    }

    private void ApplyPointer(PointerEventData eventData)
    {
        bool isInsidePlane = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            bar, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);

        if (!isInsidePlane)
            return;

        settings.SetVolume(Rect.PointToNormalized(bar.rect, localPoint).x);
    }
}
```

## Save

```csharp
public class SaveAreaView : MonoBehaviour
{
    private const string STATE_DIRTY = "Dirty";
    private const string STATE_CLEAN = "Clean";

    [SerializeField] private ViewStateMachine states;

    internal SettingsModel settings;

    private void OnEnable()
    {
        settings.OnSettingsChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        settings.OnSettingsChanged -= Refresh;
    }

    private void Refresh()
    {
        states.SetActiveState(settings.IsDirty ? STATE_DIRTY : STATE_CLEAN);
    }
}

public class SaveSettingsController : BaseClickController
{
    internal SettingsModel settings;

    protected override void OnClick()
    {
        settings.Save();
    }
}
```

## Tests

```csharp
public class SettingsModelTests
{
    [Test]
    public void Toggle_ChangesValueAndMarksDirty()
    {
        SettingsModel settings = new();

        settings.Toggle(SettingKey.Vibration);

        Assert.IsTrue(settings.IsEnabled(SettingKey.Vibration));
        Assert.IsTrue(settings.IsDirty);
    }

    [Test]
    public void SetVolume_OutOfRange_IsClamped()
    {
        SettingsModel settings = new();

        settings.SetVolume(2f);

        Assert.AreEqual(1f, settings.Volume);
    }

    [Test]
    public void Save_WhenClean_DoesNotNotify()
    {
        SettingsModel settings = new();
        int notifications = 0;
        settings.OnSettingsChanged += () => notifications++;

        settings.Save();

        Assert.AreEqual(0, notifications);
    }
}
```

Adding a setting takes one enum value, one array entry and one prefab instance. No new classes.
