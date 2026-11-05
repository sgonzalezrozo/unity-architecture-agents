# Unity Architecture Agents

**Architecture rules and a working process for AI coding assistants on Unity projects.**

[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

These are the rules and examples I give assistants such as Claude Code, Copilot and Rider's AI Assistant. They keep generated code consistent with a strict MVC architecture, and they scale the process to the size of the task. I use them in my personal Unity projects.

## What's included

| File | Purpose |
|---|---|
| [AGENTS.md](AGENTS.md) | Entry point: stack, philosophy and where to read next |
| [Development Bible](architecture/DEV_BIBLE.md) | One-file summary for human developers |
| [AI workflow](architecture/AI_WORKFLOW.md) | Process by task size, plan format, self-check, Definition of Done and Git |
| [MVC](architecture/MVC.md) | How Models, Views and Controllers work together |
| [Feature structure](architecture/FEATURE_STRUCTURE.md) | Folders, naming, and screen composition |
| [Dependency injection](architecture/DEPENDENCY_INJECTION.md) | Bindings, installers and references |
| [Backend](architecture/BACKEND.md) | How features talk to a backend |
| [UI patterns](architecture/UI_PATTERNS.md) | Clickable elements, visual states, tabs, lists and layouts |
| [C# standards](architecture/CSHARP_STANDARDS.md) | Style, performance, async, logging and tests |
| [Examples](architecture/examples) | Three complete features with invented names: Trophy Room, Settings and Inventory |

## How the process scales

| Request | Process |
|---|---|
| Question, explanation, review | Answer directly |
| Small change inside existing classes | Make the change and summarize it |
| New feature, screen, class family or refactor | Plan first, wait for approval |

Before replying with code, the assistant runs the [self-check](architecture/AI_WORKFLOW.md#self-check), a short list of questions that catch architecture violations.

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

To get the latest version later, run this and commit the result:

```bash
git submodule update --remote .ai
```

## Project status

The rules assume the [Fast IoC](https://github.com/sgonzalezrozo/unity-fast-ioc), [Recyclable Scroller](https://github.com/sgonzalezrozo/unity-recyclable-scroller) and [Screen System](https://github.com/sgonzalezrozo/unity-screen-system) packages. `BaseClickController`, `ViewStateMachine` and `BaseStartScene` aren't published yet.

## License

Released under the [MIT License](LICENSE).
