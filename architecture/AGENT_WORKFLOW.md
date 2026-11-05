# Agent workflow

**Load when:** starting any task.

Match the process to the task size. Run [capability detection](OPTIONAL_PACKAGES.md#capability-detection) before generating UI code.

## Task classification

| Request | Process |
|---|---|
| Question, explanation, review | Answer directly |
| Small change inside existing classes, with no architectural decision | Make the change and summarize it |
| New feature, new screen, new class family, refactor, anything touching `Shared/` | Plan first, wait for approval |

## Plan

Before planning, read a similar existing feature and the relevant docs. The plan is short:

```
Feature: PlayerStats
Location: Assets/Client/PlayerStats/
New files: list
Changed files: list
Reference wiring: Fast IoC scene "PlayerStats", priority 200
Tests: PlayerStatsModelTests
New dependencies: none
Capabilities: fastIoC, recyclableScroller, screenSystem, uiFoundations
Open questions: list
```

## When to ask

Ask when the docs and the existing code don't settle a decision. Don't ask what they already answer.

Agents MUST ask before:

- Adding any package or dependency
- Changing `Shared/`, core patterns or public behavior of existing code
- Using an API that is not documented and has no precedent in the code
- Introducing a new pattern
- Breaking any rule in `AGENTS.md`, even if it seems justified

Never fill a gap with a guess presented as fact. If you assumed something, say so.

## Determinism

Agent output varies between runs. A deterministic tool gives the same result every time, so it MUST carry the work whenever it can.

| ID | Rule |
|---|---|
| `DET-01` | If a compiler, analyzer, test, script or generator can decide something, the agent MUST use that tool and MUST NOT rely on its own judgment. |
| `DET-02` | A claim about code MUST come from a tool result, not from memory. If no tool can check it, report it as unverified. |
| `DET-03` | Mechanical work repeated across files SHOULD become a script or generator, and the output SHOULD be reproducible. |
| `DET-04` | A rule that can be checked mechanically SHOULD become an analyzer or a test, not only a sentence in a document. |
| `DET-05` | Runtime logic MUST be reproducible: the same state and input produce the same result. |
| `DET-06` | Models MUST NOT read time, randomness or external state directly. They MUST receive them through an interface, so tests control them. See [the example](examples/01_BACKEND_FEATURE_TROPHY_ROOM.md#services). |
| `DET-07` | Logic MUST NOT depend on undefined order, such as `Dictionary` enumeration order or the Unity `Awake` order of unrelated objects. |

## Working in Unity

- Do not hand-edit `.prefab`, `.unity`, `.asset` or `.meta` files. Describe the hierarchy with the [notation](FEATURE_STRUCTURE.md#screen-composition), plus the Inspector steps for the developer.
- Unity generates `.meta` files. A moved file must move with its `.meta`.
- Do not change generated code or external packages.
- Never rename serialized fields without `[FormerlySerializedAs]`.
- Stay within the agreed scope. Mention other problems you notice, but do not fix them unasked.

## Pre-response checklist

Run this before returning code. Item numbers are stable: the [analyzers](../analyzers) refer to them.

1. Does any Model know a View, a Controller or a Unity component?
2. Does any View handle input, call a command, or know another View or a service?
3. Does any Controller change a visual, hold a rule, or serialize a reference?
4. Does any event have parameters? Is anything pushed that could be pulled?
5. Is there a `Button`, `Toggle`, `Slider`, `Dropdown`, `onClick` or `Initialize(model)`?
   Does the code reference a package whose capability is `false`?
6. Are assigned fields `internal`, with no attribute under Fast IoC, and everything else `private`?
7. Is every file in the right folder, and is source art outside the feature?
8. Are there lambdas, LINQ, `var`, `?.` on Unity objects, or hardcoded text?
9. Could a deterministic tool have decided this? Does a Model read time or randomness directly?
10. Is everything that can be unit tested covered by a test?

## Reporting back

State what changed, which tool verified it, and what wasn't verified. If you couldn't compile or run the code, say so.

## Definition of Done

**Every change**

- [ ] Follows `AGENTS.md` and the checklist above
- [ ] Compiles with no new warnings
- [ ] Existing tests pass, and everything that can be unit tested has tests
- [ ] A bug fix includes a test that fails without the fix
- [ ] No unused code, no debug logs

**UI changes**

- [ ] Strings use localization
- [ ] Layout adapts to the target resolutions
- [ ] Subscriptions, tokens and Addressables are cleaned up

**Before release**

- [ ] Tested on iOS and Android devices, checked with IL2CPP
- [ ] Memory and frame rate checked on target devices

## Git

- `develop` is the integration branch. `main` is updated only from `release/*`.
- Branches: `feature/{name}`, `bugfix/{name}`, `release/{version}`.
- One feature or fix per PR. A feature PR touches its own folder and installer.
- Commit messages describe the change: `Add trophy details timer`.
- PRs state what changed, why, how it was tested, and any breaking change.
- At least one approval, and CI must pass before merging.
