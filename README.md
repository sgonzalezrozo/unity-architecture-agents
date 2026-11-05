# Unity Architecture Agents

**Architecture rules and a working process for AI coding assistants on Unity projects.**

[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

These are the rules I give coding agents such as Claude Code, Copilot and Rider's AI Assistant. They're written for machines: each rule has a stable ID and a MUST or MUST NOT, and it says whether a compiler analyzer or a code review enforces it. I use them in my personal Unity projects.

## What's included

| File | Purpose |
|---|---|
| [AGENTS.md](AGENTS.md) | Entry point: stack, philosophy and where to read next |
| [Developer overview](architecture/DEVELOPER_OVERVIEW.md) | One-file summary for human developers |
| [Agent workflow](architecture/AGENT_WORKFLOW.md) | Task classification, plan format, checklist, Definition of Done and Git |
| [MVC](architecture/MVC.md) | How Models, Views and Controllers work together |
| [Feature structure](architecture/FEATURE_STRUCTURE.md) | Folders, naming, and screen composition |
| [Dependency injection](architecture/DEPENDENCY_INJECTION.md) | Bindings, installers and references |
| [Backend integration](architecture/BACKEND_INTEGRATION.md) | How features talk to a backend |
| [UI patterns](architecture/UI_PATTERNS.md) | Clickable elements, visual states, tabs, lists and layouts |
| [Optional packages](architecture/OPTIONAL_PACKAGES.md) | How an agent detects UI Foundations and adapts its code |
| [C# standards](architecture/CSHARP_STANDARDS.md) | Style, performance, async, logging and tests |
| [Examples](architecture/examples) | Three complete features with invented names: a backend feature, custom input and a filtered list |
| [Templates](templates) | `.editorconfig`, Copilot instructions and a Rider AI Assistant rule |
| [Analyzers](analyzers) | Roslyn analyzers that turn the mechanical rules into compile errors |

## How the process scales

| Request | Process |
|---|---|
| Question, explanation, review | Answer directly |
| Small change inside existing classes | Make the change and summarize it |
| New feature, screen, class family or refactor | Plan first, wait for approval |

Before returning code, the agent runs the [pre-response checklist](architecture/AGENT_WORKFLOW.md#pre-response-checklist). The [analyzers](analyzers) turn the mechanical items into compile errors.

## Use it in a project

Add this repository as a Git submodule. Unity ignores a folder that starts with a dot, so `.ai/` is never imported and creates no `.meta` files.

```bash
git submodule add https://github.com/sgonzalezrozo/unity-architecture-agents.git .ai
```

Assistants look for their instructions at the project root, and a submodule can only add a folder. Add these two small files at the root to point to it.

`AGENTS.md`:

```markdown
# AGENTS.md

All rules live in the `.ai` submodule. Read `.ai/AGENTS.md` completely, then open only the document it points to.

If `.ai/` is empty, run `git submodule update --init`.
```

`CLAUDE.md`:

```text
@.ai/AGENTS.md
```

Keep rules that only apply to one project in that project's root `AGENTS.md`.

### Copilot, Rider and code style

These tools only read files from fixed paths in your project, so copy the ones you use:

```bash
cp .ai/templates/.editorconfig .
mkdir -p .github .aiassistant/rules
cp .ai/templates/copilot-instructions.md .github/
cp .ai/templates/rider-project-rule.md .aiassistant/rules/project.md
```

Commit these copies in your project. In Rider, set the rule's type to **Always**.

`.editorconfig` covers formatting, `var`, braces and naming in Rider and Visual Studio. It doesn't stop Unity from compiling.

### Updating

To get the latest version, run this and commit the result:

```bash
git submodule update --remote .ai
```

## Project status

This is version `0.1.0`. The rules assume the [Fast IoC](https://github.com/sgonzalezrozo/unity-fast-ioc), [Recyclable Scroller](https://github.com/sgonzalezrozo/unity-recyclable-scroller) and [Screen System](https://github.com/sgonzalezrozo/unity-screen-system) packages. [UI Foundations](https://github.com/sgonzalezrozo/unity-ui-foundations) is optional: without it, the rules use Unity's own event interfaces.

## Changelog

See the [changelog](CHANGELOG.md).

## License

Released under the [MIT License](LICENSE).
