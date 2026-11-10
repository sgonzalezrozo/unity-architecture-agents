# Contributing to Unity Architecture Agents

Thanks for helping out. Bug reports, rule fixes and pull requests are welcome.

## Before you start

- Search the [existing issues](https://github.com/sgonzalezrozo/unity-architecture-agents/issues) first.
- Open an issue before starting a large change, so we can agree on the approach.
- Keep each pull request focused on one change.

## Make a change

- Each rule has one home and a stable ID. Edit the rule where it lives, and don't reuse or renumber IDs.
- Use the RFC 2119 keywords (MUST, SHOULD, MAY) as `AGENTS.md` defines them.
- Keep examples consistent with the rules they demonstrate.
- Don't leave internal notes in documents, such as planning phases or project names.
- Update `README.md` and `llms.txt` when the scope or the structure changes.
- Add an entry to `CHANGELOG.md`, and mark breaking changes with **Breaking**.

## Analyzers

The Roslyn analyzers live in `analyzers/`. With the .NET SDK installed, `analyzers/build.sh` runs their tests and builds the DLL. See `analyzers/README.md`. A new analyzer needs tests, and its rule ID must match the rule it enforces.

## Release checklist

1. Update the version in the README's Project status and FAQ sections.
2. Move the changelog entries under the new version.
3. Create the matching tag, such as `v0.2.0`.
