# Feature structure

**Load when:** adding a feature or screen, placing a file, naming a type or describing a prefab hierarchy.

## Definition

A feature is usually one main screen plus modal popups. It may have several screens the user navigates between.

## Folders

`Assets/Client/` has one folder per feature. A feature has one folder per screen, plus `Services`.

```
Assets/
├── Client/
│   ├── TrophyRoom/
│   │   ├── MainRoom/
│   │   ├── EditRoom/
│   │   ├── DetailsTrophy/
│   │   ├── DetailsTrophySnapshot/
│   │   └── Services/
│   ├── Store/
│   ├── Inventory/
│   ├── HomePage/
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

### Inside `Services`

| File | Role |
|---|---|
| `{Feature}Module.cs` | Pure C# Model that wraps the feature's backend connection ([Backend integration](BACKEND_INTEGRATION.md)) |
| `I{Feature}Connector.cs`, `{Feature}Connector.cs` | Connector interface and implementation |
| `{Feature}Data.cs` | Data classes from the backend |
| `{Feature}Installer.cs` | Fast IoC bindings. Not needed without a container ([Dependency injection](DEPENDENCY_INJECTION.md)) |
| `ScreenDefinition` asset(s) | With `screenSystem`. One if the feature has a single screen. If there are several, see below |

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
| Input GameObject | `{Action}Input` | `ClaimTrophyInput` |
| State root GameObject | `{State}Root` | `LoadingRoot` |
| Rendering method | `Render{Subject}` | `RenderTrophyName` |
| Data | `{Name}Data` | `TrophyData` |
| Connector | `I{Feature}Connector`, `{Feature}Connector` | `ITrophyRoomConnector` |
| Installer | `{Feature}Installer` | `TrophyRoomInstaller` |
| Prefab | Same as its main element | `MainRoom.prefab`, `TrophyCell.prefab` |

**Namespaces:** `Client.{Feature}`, flat for the whole feature. Folder names MUST NOT contain spaces.

## Screen composition

```
TrophyRoomMain [MainRoomLoadController, MainRoomStateView]
├── Header
│   ├── TitleText (TextMeshProUGUI)
│   └── CloseInput (Image: alpha 0) [CloseController]
├── Content (static elements, tab content or a scroller)
└── Footer
```

**Hierarchy notation:**

| Form | Meaning |
|---|---|
| `Name (UnityType)` | GameObject with a Unity component |
| `Name (UnityType: setting)` | The same, with a required setting |
| `Name [ProjectType]` | GameObject with project MonoBehaviours |
| `Name (UnityType) [ProjectType]` | GameObject with both |

Pure C# Models never appear in a hierarchy, because they aren't on any GameObject.

Rules:

- Separate static elements from dynamic ones. Dynamic content is either tab content or a scroller.
- A screen has its Views at the root of the scope they render, or lower when a section is independent. Interactive elements carry their own Controller.
- Screen prefabs have no Canvas of their own. They sit under an existing Canvas, which owns the `CanvasScaler`.

## Modals and navigation

Popups are separate prefabs in the screen folder that uses them. A modal's state lives in a Model like any other state.

**Not specified yet.** With `screenSystem`, the `ScreenSystem` and `ScreenDefinition` APIs, modal stacking, returning a result from a modal, and passing context between screens have no documented API. Agents MUST read how existing features do it, and MUST ask if there is no precedent.

One rule already applies: nothing is pushed to a screen. If screen B needs the selected trophy, screen A sets it on a shared Model and B reads it there. The [backend example](examples/01_BACKEND_FEATURE_TROPHY_ROOM.md) does this with `TrophyRoomModule.SelectTrophy`.

Scene transitions MUST go through one project scene loader, never `SceneManager` from feature code. Showing a menu screen is not the same as leaving a gameplay scene.

## Assembly definitions

```
Assets/Client/Client.asmdef                  All client code
Assets/Client/Shared/Client.Shared.asmdef    Optional
```

Place an `.asmdef` inside the folder it should compile. An `.asmdef` directly in `Assets/` would capture the whole project.

Give a feature its own `.asmdef` only when it has heavy dependencies, external integrations, or more than about 100 files.
