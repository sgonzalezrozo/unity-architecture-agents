# Changelog

All notable changes to this project are documented in this file.

## [Unreleased]

### Added

- Roslyn analyzers for the mechanical rules in the checklist.
- `OPTIONAL_PACKAGES.md`, with capability detection, and list and navigation forms for projects without the packages.
- Stable rule IDs, RFC 2119 keywords and the enforcement of each invariant.
- A scope: the architecture rules cover client code, except ECS gameplay, packages and tools.
- `HAND` rules: what an agent must give a person who builds a prefab.
- `SCOPE` rules: a scope is one root GameObject, and a feature can hold several.
- `ARCH-09` to `ARCH-11`: no Model cycles, no abstraction without a need, and the minimum that finishes the need.
- `PERF` rules, `FEAT-01`, `MODEL-09` to `MODEL-11`, `LIST-05` and `TEST-07` to `TEST-10`.
- `ARCH-07` and the `DET` rules: anything that can be deterministic must be.
- `ARCH-08` and the `TEST` rules: anything that can be unit tested must be.

### Changed

- **Breaking:** Tests for Model behavior are written first, and `MODEL-03` separates reachable conditions from bugs.
- **Breaking:** Fast IoC, Recyclable Scroller, Screen System and UI Foundations are optional. The architecture and code style stay mandatory.
- Models and Views sit on their scope root, and a Model moves up only when shared. Injection is the exception, for Models with no common parent or that persist. Without a container, those Models are singletons.
- Examples use `IPointerClickHandler` and `GameObject` state roots.
- The analyzers apply Fast IoC rules only when Fast IoC is in the compilation.
- **Breaking:** Renamed documents, the `Services And definitions` folder to `Services`, and `Actions` GameObjects to match their Controller.
- **Breaking:** Rendering methods are now `Render{Subject}`, and examples use full, specific member names.

### Fixed

- `ViewStateMachine.SetState` replaces `SetActiveState`, which doesn't exist.
- Lists use a concrete `RecyclableScrollRect<T>` subclass and the real `IRecyclableDataSource<T>` members.
- `MonoInstaller` takes scene names, not a subscene.

## [0.1.0] - 2026-11-05

### Added

- `AGENTS.md`, the architecture documents and three complete examples.
- Templates for Copilot and Rider's AI Assistant, and the `.editorconfig` the documents refer to.
