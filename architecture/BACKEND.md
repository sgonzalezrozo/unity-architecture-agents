# Backend

Use this for every feature that talks to the backend. Complete code is in [Trophy Room](examples/01_TROPHY_ROOM.md).

```
Backend ← Connector ← Module → (event) → View reads Module
                        ↑
                    Controller calls commands
```

| Piece | Type | Job |
|---|---|---|
| `{Feature}Data` | Data class | The backend response as received |
| `I{Feature}Connector` | Interface | The calls the feature can make; lets tests fake the backend |
| `{Feature}Connector` | Class implementing `I{Feature}Connector` | Performs the calls, returns data, holds no state |
| `{Feature}Module` | Pure C# Model | Wraps the Connector and holds the feature's backend data |

## The Module

A Module is a Model, so all Model rules apply. It has one exception: it is the only Model allowed to know a Connector.

There is one Module per feature, shared by all of that feature's screens. That is what lets several screens work on the same data.

**What goes where:**

| State | Lives in |
|---|---|
| Data from the backend, load state, selection shared between screens | Module |
| State of one screen only, such as a sort order or an edit in progress | That screen's Model |

Screen Models may read the Module and call its commands, like any Model-to-Model dependency.

## Rules

- Backend data is never mutated. To apply a change, replace the entry with what the backend returned.
- Store timestamps as Unix seconds and compute remaining time when read.
- Each operation has a state a View can render. An empty list is a valid result, not a failure.
- Prevent concurrent loads with the state itself: return early if already loading.
- A cancelled operation is not a failure: return to the previous state and do not log an error.
- Only Controllers start async commands, using `.Forget("Context")` and a `CancellationToken` they own.
- Cancel and dispose that token source in `OnDestroy`.

## Starting a load

A Controller implements `IScreenShow` and calls the Module from `OnScreenShown()`. Never from `Start()`, because a screen can be shown more than once.

## Network failures

- Timeouts: 15 seconds per attempt, 3 attempts at most.
- Retries: exponential backoff with jitter, written once in a shared helper that every Connector uses.
- Never leave a task running after a timeout.
- Show the user that something is happening during long operations, and allow cancelling them.

## Not specified yet

Real-time updates (WebSocket, polling), caching policy, request batching and rate limiting have no agreed pattern. Assistants must ask.
