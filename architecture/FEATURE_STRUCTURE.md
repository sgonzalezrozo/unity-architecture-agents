# Feature structure

## What a feature is

A feature is usually one main screen plus some modal popups over it. Sometimes it has a few screens the user navigates through, linearly or not.

## Folders

`Assets/Client/` has one folder per feature. Inside a feature, there is one folder per screen plus `Services And definitions`.

```
Assets/
├── Client/
│   ├── TrophyRoom/
│   │   ├── MainRoom/
│   │   ├── EditRoom/
│   │   ├── DetailsTrophy/
│   │   ├── DetailsTrophySnapshot/
│   │   └── Services And definitions/
│   ├── Season Store/
│   ├── Inventory/
│   ├── Home Page/
│   └── Shared/
├── Art/        Source art: textures, sprites, materials, VFX, animations
├── Audio/      Source audio: SFX, Music, Voice
├── Fonts/
├── Scenes/     Scenes, each next to its own scripts
├── Editor/     Editor-only tools
├── Plugins/    Third-party SDKs
└── Tests/      EditMode and PlayMode test assemblies, one folder per feature
```

### Inside a screen folder

The C# for that screen and the prefabs built with it, side by side. No `Scripts/`, `Prefabs/` or `Views/` subfolders.

Art source files stay in `Assets/Art/` and `Assets/Audio/`. "Created assets" (prefabs) live with the code; "source assets" (`.png`, `.mp3`) do not.

A very large screen may use subfolders by section (`Tabs/`, `Members/`), never by file type.

### Inside `Services And definitions`

| File | Role |
|---|---|
| `{Feature}Module.cs` | Pure C# Model that wraps the feature's backend connection ([Backend](BACKEND.md)) |
| `I{Feature}Connector.cs`, `{Feature}Connector.cs` | Connector interface and implementation |
| `{Feature}Data.cs` | Data classes from the backend |
| `{Feature}Installer.cs` | FastIoC bindings ([Dependency injection](DEPENDENCY_INJECTION.md)) |
| `ScreenDefinition` asset(s) | One if the feature has a single screen. If there are several, see below |

### Multi-screen features

- Each screen has its own folder.
- A screen that other features can open keeps its `ScreenDefinition` in the feature where it was first created, which is its main usage. It never moves to `Shared/`.
- Other features refer to that screen; they do not copy it.

### `Shared/`

Only for things used by two or more features that belong to none of them: reusable prefabs, dialogs, formatters. The same subfolders-by-purpose rule applies.

## Naming

| Type | Pattern | Example |
|---|---|---|
| Screen Model | `{Screen}Model` | `EditRoomModel` |
| Feature Module | `{Feature}Module` | `TrophyRoomModule` |
| View | `{Name}View` | `TrophyCellView` |
| Controller | `{Action}Controller` | `ClaimTrophyController` |
| Data | `{Name}Data` | `TrophyData` |
| Connector | `I{Feature}Connector`, `{Feature}Connector` | `ITrophyRoomConnector` |
| Installer | `{Feature}Installer` | `TrophyRoomInstaller` |
| Prefab | Same as its main element | `MainRoom.prefab`, `TrophyCell.prefab` |

**Namespaces:** `Client.{Feature}`, flat for the whole feature. Folder names with spaces do not map to namespaces: in Rider, turn off the *Namespace Provider* property on `Services And definitions`.

## Screen composition

```
TrophyRoomMain [MainRoomLoadController, MainRoomStateView]
├── Header
│   ├── Title (TextMeshProUGUI)
│   └── Actions (Close) [CloseController]
├── Content (static elements, tab content or a scroller)
└── Footer
```

**Hierarchy notation** used everywhere in these docs:

- `Name (Type)` is a GameObject with a Unity component.
- `Name [ComponentA, ComponentB]` is a GameObject with our MonoBehaviours.
- Pure C# Models never appear in a hierarchy, because they are not on any GameObject.

Rules:

- Separate static elements from dynamic ones. Dynamic content is either tab content or a scroller.
- A screen has its Views at the root of the scope they render, or lower when a section is independent. Interactive elements carry their own Controller.
- Screen prefabs have no Canvas of their own. They are Addressable prefabs instantiated under an existing Canvas, which owns the `CanvasScaler`.

## Modals and navigation

Popups are separate prefabs in the screen folder that uses them. A modal's state lives in a Model like any other state.

**Not specified yet.** `ScreenSystem`, `ScreenDefinition`, modal stacking, returning a result from a modal, and passing context between screens have no documented API. Assistants must read how existing features do it, and must ask if there is no precedent.

One rule already applies: nothing is pushed to a screen. If screen B needs to know which trophy was selected, A sets it on a shared Model and B reads it there. [Trophy Room](examples/01_TROPHY_ROOM.md) does this with `TrophyRoomModule.Select`.

Scene transitions always go through the project's scene loader, never `SceneManager` directly. It prepares the Screen System for the scene change and shows the splash screen while the next scene loads. Showing a menu screen is not the same as leaving a match scene.

## Assembly definitions

```
Assets/Client/Client.asmdef                  All client code
Assets/Client/Shared/Client.Shared.asmdef    Optional
```

Place an `.asmdef` inside the folder it should compile. An `.asmdef` directly in `Assets/` would capture the whole project.

Give a feature its own `.asmdef` only when it has heavy dependencies, external integrations, or more than about 100 files.
