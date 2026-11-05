# Backend integration

**Load when:** a feature calls the backend. Complete code: [Backend feature](examples/01_BACKEND_FEATURE_TROPHY_ROOM.md).

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

A Module is a Model, so all [Model rules](MVC.md#model-rules) apply. It is the only Model that MAY reference a Connector.

There is one Module per feature, shared by all of that feature's screens. That is what lets several screens work on the same data.

**What goes where:**

| State | Lives in |
|---|---|
| Data from the backend, load state, selection shared between screens | Module |
| State of one screen only, such as a sort order or an edit in progress | That screen's Model |

Screen Models may read the Module and call its commands, like any Model-to-Model dependency.

## Rules

| ID | Rule |
|---|---|
| `BE-01` | Backend data MUST NOT be mutated. To apply a change, replace the entry with the backend's response. |
| `BE-02` | Timestamps MUST be stored as Unix seconds, and remaining time computed when read. |
| `BE-03` | Each operation MUST have a state a View can render. An empty list is a valid result, not a failure. |
| `BE-04` | Concurrent loads MUST be prevented with the state itself: return early if already loading. |
| `BE-05` | A cancelled operation is not a failure. Restore the previous state and log no error. |
| `BE-06` | Only Controllers MAY start async commands, with `.Forget("Context")` and a `CancellationToken` they own. |
| `BE-07` | The Controller MUST cancel and dispose its token source in `OnDestroy`. |

## Starting a load

A Controller calls the Module from the screen-shown event: `OnScreenShown()` with `screenSystem`, otherwise `OnEnable`. Never from `Start()`, because a screen can be shown more than once.

## Network failures

- Timeouts: 15 seconds per attempt, 3 attempts at most.
- Retries: exponential backoff with jitter, written once in a shared helper that every Connector uses.
- Never leave a task running after a timeout.
- Show the user that something is happening during long operations, and allow cancelling them.

## Not specified yet

Real-time updates (WebSocket, polling), caching policy, request batching and rate limiting have no agreed pattern. Agents MUST ask.
