# Unity Architecture Agents

**Architecture rules and a working process for AI coding assistants on Unity projects.**

[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

Unity Architecture Agents is a free, open-source `AGENTS.md` and `CLAUDE.md` rule set for AI coding agents on Unity 6 projects. It gives agents a strict MVC architecture for uGUI, Roslyn analyzers that turn the rules into compile errors, a batch-mode verify script, and a Claude Code hook that won't let an agent finish with a broken build. Templates cover Cursor, GitHub Copilot and Rider AI Assistant, and the rules say how to verify work through Unity MCP or the Unity CLI.

These are the rules I give Claude Code, Copilot and Rider's AI Assistant in my personal Unity projects. They're written for machines: each rule has a stable ID and a MUST or MUST NOT, and it says whether a compiler analyzer or a code review enforces it.

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
| [Optional packages](architecture/OPTIONAL_PACKAGES.md) | How an agent detects optional packages and adapts its code |
| [C# standards](architecture/CSHARP_STANDARDS.md) | Style, performance, async, logging and tests |
| [Editor tools](architecture/EDITOR_TOOLS.md) | How an agent verifies its work with Unity MCP, the Unity CLI or `verify.sh` |
| [Examples](architecture/examples) | Three complete features with invented names: a backend feature, custom input and a filtered list |
| [Templates](templates) | `.editorconfig`, Copilot, Cursor and Rider rules, `verify.sh` and a Claude Code Stop hook |
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
mkdir -p .cursor/rules
cp .ai/templates/cursor-rule.mdc .cursor/rules/unity-architecture.mdc
```

Commit these copies in your project. In Rider, set the rule's type to **Always**.

`.editorconfig` covers formatting, `var`, braces and naming in Rider and Visual Studio. It doesn't stop Unity from compiling.

### Verify every change automatically

An agent that can't check its work reports guesses. Copy the verify script and, for Claude Code, the Stop hook:

```bash
mkdir -p scripts .claude/hooks
cp .ai/templates/scripts/verify.sh scripts/
cp .ai/templates/claude/hooks/verify-on-stop.sh .claude/hooks/
cp .ai/templates/claude/settings.json .claude/
```

`scripts/verify.sh` compiles the project in batch mode, runs the EditMode tests and checks Fast IoC code generation. It fails on compiler and analyzer errors, and writes `Logs/verify/summary.json` for agents and CI. Exit codes: `0` passed, `1` failed, `2` couldn't run.

When Claude Code finishes, the hook runs `verify.sh` if `Assets`, `Packages` or `ProjectSettings` changed, and sends failures back to the agent. It gives up after three attempts in a row. Close the project in the Editor while the agent works, or the hook can't check anything.

### Unity MCP and the Unity CLI

With a Unity MCP server, such as the Unity CLI's `unity mcp` mode or [MCP for Unity](https://github.com/CoplayDev/unity-mcp), the agent reads the Console, runs tests and builds prefabs through the open Editor instead. [Editor tools](architecture/EDITOR_TOOLS.md) says which tool checks what, and what an agent may do through a bridge.

### Updating

To get the latest version, run this and commit the result:

```bash
git submodule update --remote .ai
```

## Project status

This is version `0.1.0`. The architecture and the code style are mandatory. [Fast IoC](https://github.com/sgonzalezrozo/unity-fast-ioc), [Recyclable Scroller](https://github.com/sgonzalezrozo/unity-recyclable-scroller), [Screen System](https://github.com/sgonzalezrozo/unity-screen-system) and [UI Foundations](https://github.com/sgonzalezrozo/unity-ui-foundations) are optional: agents detect them, and use plain Unity forms when they're missing.

## FAQ

### How do I make AI coding agents follow an architecture in Unity?

Give them rules they can't miss, and checks they can't skip. Add this repository as `.ai/`, point `AGENTS.md` and `CLAUDE.md` at it, install the [analyzers](analyzers), and add the [verify loop](#verify-every-change-automatically). Instructions guide the agent, and the compiler enforces the mechanical rules.

### Does it work with Claude Code, Cursor, Copilot and Rider?

I use it with Claude Code, Copilot and Rider's AI Assistant. Claude Code reads `CLAUDE.md`, and Copilot and Rider read the [templates](templates) you copy into their fixed paths. Cursor and Codex read the same kind of files (a Cursor rule template and `AGENTS.md`), but I haven't tested them yet.

### Does it work with Unity MCP?

It has rules for it, but they're not tested with a Unity MCP server yet. [Editor tools](architecture/EDITOR_TOOLS.md) tells an agent with the Unity CLI (`unity mcp`) or MCP for Unity to verify through the open Editor, and allows it to build prefabs with editor tools, never by writing prefab or scene YAML as text.

### Which architecture does it use?

Strict MVC for uGUI: Models own state, Views render, Controllers receive input, and state flows by pull through parameterless events. No `Button`, `Toggle` or `Slider`, because each is a View and a Controller in one. ECS gameplay is out of scope.

### Do I need Fast IoC or the other packages?

No. [Fast IoC](https://github.com/sgonzalezrozo/unity-fast-ioc), [Recyclable Scroller](https://github.com/sgonzalezrozo/unity-recyclable-scroller), [Screen System](https://github.com/sgonzalezrozo/unity-screen-system) and [UI Foundations](https://github.com/sgonzalezrozo/unity-ui-foundations) are optional. Agents detect them in `Packages/manifest.json` and use plain Unity forms when they're missing.

### Does it work with Unity 6, URP and HDRP?

It's written and tested against Unity 6 (`6000.3`). The rules cover uGUI code, so the render pipeline doesn't matter.

### How do I write an AGENTS.md or CLAUDE.md for a Unity project?

Start from this repository instead of a blank file. Add it as the `.ai/` submodule, and keep a two-line root `AGENTS.md` and `CLAUDE.md` that point to it, as [Use it in a project](#use-it-in-a-project) shows. Put rules that only apply to one project in that project's root `AGENTS.md`.

### Are there Cursor rules for Unity?

Yes. Copy [`templates/cursor-rule.mdc`](templates/cursor-rule.mdc) to `.cursor/rules/`. It loads the full rule set from `.ai/AGENTS.md`.

### Is it free for commercial use?

Yes. It's MIT licensed.

### Does anyone else use it?

Not yet. It's version `0.1.0` by a single author, used only in my personal projects so far, and the rules still change. Feedback and issues are very welcome.

### Is there a sample project?

[Unity Architecture Agents Sample](https://github.com/sgonzalezrozo/unity-architecture-agents-sample) runs agents against the real packages, with the verify hook installed.

## Changelog

See the [changelog](CHANGELOG.md).

## License

Released under the [MIT License](LICENSE).
