# Changelog

All notable changes to this project are documented in this file.

## [Unreleased]

### Added

- Roslyn analyzers for the mechanical rules in the checklist.
- `OPTIONAL_PACKAGES.md`, with capability detection for UI Foundations.
- Stable rule IDs, RFC 2119 keywords and the enforcement of each invariant.

### Changed

- **Breaking:** UI Foundations is optional. Examples use `IPointerClickHandler` and `GameObject` state roots.
- **Breaking:** Renamed documents, the `Services And definitions` folder to `Services`, and `Actions` GameObjects to `{Action}Input`.
- **Breaking:** Rendering methods are now `Render{Subject}`, and examples use full, specific member names.

### Fixed

- `ViewStateMachine.SetState` replaces `SetActiveState`, which doesn't exist.
- Lists use a concrete `RecyclableScrollRect<T>` subclass and the real `IRecyclableDataSource<T>` members.
- `MonoInstaller` takes scene names, not a subscene.

## [0.1.0] - 2026-11-05

### Added

- `AGENTS.md`, the architecture documents and three complete examples.
- Templates for Copilot and Rider's AI Assistant, and the `.editorconfig` the documents refer to.
