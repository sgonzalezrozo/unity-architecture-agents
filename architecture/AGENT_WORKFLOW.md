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
FastIoC scope: scene "PlayerStats", priority 200
Tests: PlayerStatsModelTests
New dependencies: none
Optional packages: uiFoundations = true | false
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
   Is `UIFoundations` referenced while `uiFoundations = false`?
6. Are injected fields `internal` with no attribute, and everything else `private`?
7. Is every file in the right folder, and is source art outside the feature?
8. Are there lambdas, LINQ, `var`, `?.` on Unity objects, or hardcoded text?

## Reporting back

State what changed, how it was verified, and what was not verified. If you could not compile or run the code, say so.

## Definition of Done

**Every change**

- [ ] Follows `AGENTS.md` and the checklist above
- [ ] Compiles with no new warnings
- [ ] Existing tests pass; new Model and Module logic has tests
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
