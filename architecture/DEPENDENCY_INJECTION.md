# Dependency injection

We use **FastIoC**. Bindings are defined per feature and resolved without reflection at runtime.

## Getting a dependency

| Class | How |
|---|---|
| MonoBehaviour | `internal` field, no attribute |
| Pure C# class | Constructor |
| Inside a list cell | `GetComponentInParent<TCellModel>()`, cached once |

```csharp
public class TrophyCountView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI countText;

    internal TrophyRoomModule module;
}
```

```csharp
public class TrophyRoomModule
{
    private readonly ITrophyRoomConnector connector;

    public TrophyRoomModule(ITrophyRoomConnector connector)
    {
        this.connector = connector;
    }
}
```

Injected fields are ready before `Awake`.

Forbidden: `[Inject]`, `public` injected fields, `Initialize(...)` on a MonoBehaviour, constructors on a MonoBehaviour, and resolving from `ContextRuntime` as a service locator. FastIoC flags this in the Editor and development builds.

Do not null-check injected fields. If injection fails, it should fail loudly.

### The trap of attribute-free injection

FastIoC matches by **type**: the field name does not matter, and there is no attribute marking intent. So:

- Use `internal` only for injected fields. Everything else is `private`.
- Never bind primitives such as `Bind<int>()` or `Bind<string>()`. Put configuration in a typed class and bind that.
- Bind each type once per scope.

## Installers

One `BindingRecorder` per feature, in `Services And definitions/`.

```csharp
using FastIoC.API;

namespace Client.TrophyRoom
{
    [MonoInstaller("Bootgame", "TrophyRoom", Priority = 200)]
    public class TrophyRoomInstaller : BindingRecorder
    {
        public override void InstallBindings()
        {
            Bind<ITrophyRoomConnector>().To<TrophyRoomConnector>().AsSingle();
            Bind<TrophyRoomModule>().AsSingle();
        }
    }
}
```

`[MonoInstaller(scene, subscene, Priority = n)]`: scene name, optional subscene or zone, and install order (higher first).

| Priority | Use |
|---|---|
| 400 | Configuration |
| 300 | Shared services |
| 200 | Features |
| 100 and below | Feature details |

## What to bind

Bind Models, Modules and Connectors only. Never bind Views or Controllers: they are components on prefabs and receive their fields.

| Binding | Use |
|---|---|
| `Bind<T>().AsSingle()` | One shared instance, created on first use. Use this unless another lifetime is needed |
| `Bind<IT>().To<T>().AsSingle()` | Interface to implementation |
| `Bind<T>().AsCached()` | One instance per scene, released when the scene unloads |
| `Bind<T>().AsTransient()` | A new instance per injection. This is also the lifetime when none is given |
| `Bind<T>().FromInstance(x)` | An existing instance, shared as a singleton |
| `Bind<T>().FromInstance("Path").AsSingle()` | A prefab or ScriptableObject loaded by name or `Assets/...` path |

FastIoC has no factory delegates. When creation needs custom logic, bind a factory class and inject it.

## Scope

- Use a feature subscene for anything that lives and dies with the feature.
- A dependency needed by several features goes in a shared scope with a higher priority.

## Runtime-created objects

Cells spawned by the scroller receive injection through `IInjectableForChildren` on their Model. See [Lists](UI_PATTERNS.md#lists).

Never call `Instantiate` directly for a prefab with injected fields. Use `IPrefabInstantiator`, or the fields stay empty.
