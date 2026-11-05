# Dependency injection

**Load when:** writing bindings or installers, or getting a reference.

Most Views and Controllers don't use injection: they find a Model on a common parent. Injection is for the cases in [Where a Model lives](MVC.md#where-a-model-lives). A static singleton or a service locator MUST NOT be used in any case.

| Capability | Mechanism for injected Models |
|---|---|
| `fastIoC` | Injected `internal` fields. The rest of this document applies |
| `otherContainer` | That container's injection |
| Neither | Ask (`ROOT-07`) |

## Reference acquisition

| Class | How |
|---|---|
| View or Controller, Model on a common parent | `GetComponentInParent<TModel>()`, cached once |
| View or Controller, no common parent or persistent Model | `internal` field |
| Inside a list cell | `GetComponentInParent<TCellModel>()`, cached once |
| MonoBehaviour Model, Model outside MVC | `internal` field |
| Pure C# class | Constructor |

```csharp
public class TrophyCountView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI countText;

    internal TrophyRoomModule trophyRoomModule;
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

With `fastIoC`, injected fields are assigned before `Awake`, and these rules apply:

| ID | Rule |
|---|---|
| `DI-01` | `[Inject]` MUST NOT be used with Fast IoC (`UA0007`). |
| `DI-02` | MonoBehaviour fields MUST be `private` or `internal` (`UA0008`). With Fast IoC, `internal` fields MUST have no attribute. |
| `DI-03` | `Initialize(...)` with parameters MUST NOT exist on a MonoBehaviour (`UA0006`). |
| `DI-04` | A MonoBehaviour MUST NOT have a constructor. |
| `DI-05` | Code MUST NOT resolve from `ContextRuntime` as a service locator. FastIoC flags this in the Editor and development builds. |
| `DI-06` | Injected fields MUST NOT be null-checked. A failed injection should fail loudly. |
| `DI-07` | Primitives MUST NOT be bound. Put configuration in a typed class and bind that. |
| `DI-08` | A type MUST be bound once per scope. |

FastIoC matches by **type**. The field name doesn't matter and no attribute marks intent, so `internal` is the only signal that a field is injected.

## Fast IoC installers

One `BindingRecorder` per feature, in `Services/`.

```csharp
using FastIoC.API;

namespace Client.TrophyRoom
{
    [MonoInstaller("MainMenu", "TrophyRoom", Priority = 200)]
    public class TrophyRoomInstaller : BindingRecorder
    {
        public override void InstallBindings()
        {
            Bind<ITrophyRoomConnector>().To<TrophyRoomConnector>().AsSingle();
            Bind<IClock>().To<SystemClock>().AsSingle();
            Bind<TrophyRoomModule>().AsSingle();
        }
    }
}
```

`[MonoInstaller(sceneNames, Priority = n)]`: the scenes the installer applies to, and the install order (higher runs first). With no scene names, the installer is global.

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
| `Bind<T>().AsCached()` | One cached instance, cleared when a scene unloads |
| `Bind<T>().AsTransient()` | A new instance per injection. This is also the lifetime when none is given |
| `Bind<T>().FromInstance(x)` | An existing instance, shared as a singleton |
| `Bind<T>().FromInstance("Path").AsSingle()` | A prefab or ScriptableObject loaded by name or `Assets/...` path |

FastIoC has no factory delegates. When creation needs custom logic, bind a factory class and inject it.

## Scope

- Use the feature's scene for anything that lives and dies with the feature.
- A dependency needed by several features goes in a shared scope with a higher priority.
- A scene-specific `AsSingle()` instance stays cached after its scene unloads. Use `AsCached()` to tie an instance to a scene.

## Runtime-created objects

Scroller cells receive injection through `IInjectableForChildren` on their Cell Model. See [Lists](UI_PATTERNS.md#lists).

Code MUST NOT call `Instantiate` directly for a prefab with injected fields. Use `IPrefabInstantiator`, or the fields stay empty.
