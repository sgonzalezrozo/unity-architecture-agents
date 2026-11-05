# C# standards

**Load when:** writing or reviewing C#.

These standards are mandatory in every project, with or without the optional packages.

`.editorconfig` covers formatting in the editor but doesn't stop compilation. The [analyzers](../analyzers) fail the build for `var`, LINQ, lambdas and `?.` on Unity objects. Code review enforces the rest.

Apply these rules to new code. Refactor old code only when asked.

## Naming

| Element | Style |
|---|---|
| Classes, public methods, properties, enums | `PascalCase` |
| Fields, locals, parameters | `camelCase`, no underscore |
| Constants | `ALL_CAPS` |
| Events | `OnSomethingChanged`, past tense |
| Async methods | Suffix `Async` |

Method prefixes state intent: `Get` (query), `Set` (assign), `Is`/`Has`/`Can` (bool query), `Try` (may fail, returns bool), `Calculate` (computes), `Update` (recomputes), `Render` (View draws Model state), `On` (event handler), `Validate` (throws if invalid).

## Class layout

Constants → events → injected fields → serialized fields → public fields → private fields → properties → constructor → Unity methods → other methods in order of use.

## Size

| | Target | Hard limit | At the limit |
|---|---|---|---|
| Lines in a method | 12 | 15 | Extract a named method |
| Lines in a class | 120 | 200 | Split by responsibility |
| Parameters | 2 | 3 | Create a parameter class |
| Line width | | 120 characters | Wrap |

Method lines count the body, without blank lines and lines holding only a brace. Class lines count everything in the class. The limits also apply to constructors and Unity methods. Only tests and generated code are exempt.

## Writing code

- Explicit types, never `var`.
- `new()` when the type is on the left: `List<string> names = new();`.
- Guard clauses and early returns instead of nested `if`/`else`. In loops, invert and `continue`.
- A single statement after `if`/`for`/`foreach` goes on the next line, indented, without braces.
- Opening braces on a new line. Expression-bodied methods put `=>` on a new line.
- No `#region`.
- No comments or `///` summaries unless explaining something the code cannot, such as an engine bug.
- No magic numbers (0, 1 and -1 in obvious contexts are fine). Name timeouts, retries, delays and thresholds.
- No hardcoded user-facing text. Use the localization system.
- No boolean flag parameters that select a code path; make two methods. `SetVisible(bool)` is fine, since the value is the state being set.
- Command-query separation. `Try*` is the accepted exception.
- Law of Demeter: `player.GetCaptainHealth()`, not `player.Team.Captain.Health`.
- Prefer polymorphism over a `switch` on type that chooses behavior. A `switch` expression for a simple mapping is fine.
- Prefer `private`, and `{ get; private set; }` over public setters.
- Remove unused usings, code and commented-out code.

## SOLID

Single responsibility per class, depend on interfaces at boundaries, prefer composition to inheritance. External systems (HTTP, files, SDKs) stay behind an interface we own, as Connectors do. A base class never checks for its derived types.

## Unity

- **Never use `?.`, `??` or `??=` on anything derived from `UnityEngine.Object`.** They bypass Unity's destroyed-object check. Use `if (x == null)`.
- **Never rename a serialized field in use** without `[FormerlySerializedAs("oldName")]`. The Inspector data is lost otherwise.
- Cache components once in `Awake`. Do not call `GetComponent` in `Update`.
- A GameObject does not call `SetActive` on itself; its parent controls visibility.
- Avoid `OnValidate` and decorative `[Header]` attributes.
- Data classes that must serialize are `[Serializable]` with public fields. Unity does not serialize auto-properties.
- Check your Unity version's C# level before using newer syntax. `record` and `init` need an `IsExternalInit` shim on older profiles, and `required` is not available at C# 9.

## Allocations

Treat these as forbidden in all client code, not only hot paths, so that hot paths never need auditing:

- No lambdas or anonymous methods, except in tests. A named method can also be unsubscribed.
- No LINQ.
- `for` loops on `List<T>`. `foreach` is fine on arrays, and on dictionaries outside hot paths.

Also:

- Reuse collections with `.Clear()` in methods called often. Return empty collections, not `null`.
- Use `StringBuilder` for string building in loops.
- Pool objects created often.
- Cache `WaitForSeconds` and similar yield instructions.
- Give DOTween and other callbacks a named method. Store parameters in fields.
- Use `MaterialPropertyBlock` for per-instance material changes.

## Async

- `async Task` always. The only `async void` in the project is `Forget` itself.
- Never `.Result` or `.Wait()`.
- Accept a `CancellationToken` in anything that can outlive its screen.
- Start from sync code with `.Forget("Context")`, never `_ = Task`:

```csharp
public static async void Forget(this Task task, string context)
{
    try
    {
        await task;
    }
    catch (OperationCanceledException)
    {
    }
    catch (Exception exception)
    {
        Debug.LogError($"[{context}] Background task failed: {exception}");
    }
}
```

- Use `ConfigureAwait(false)` only in code that touches no Unity API afterwards, and never in a MonoBehaviour.
- Guard concurrent runs with a state or flag, and reset it in `finally`.

## Errors

- **Throw** for what should not happen: broken preconditions, missing resources.
- **Return `Result<T>`** or a state for what can happen: network failure, invalid input, not enough currency.
- Catch specific exceptions. Never swallow one without logging it.
- Validate public API parameters. Do not null-check injected or cached references.
- A missing optional serialized component is a `LogWarning`, and the code degrades gracefully.

## Logging

- Prefix with context: `[TrophyRoom]`. Include IDs and reasons.
- `Log` for flow, `LogWarning` for recoverable issues, `LogError` for failures.
- Exceptions: `$"[Context] {ex.Message}\n{ex.StackTrace}"`.
- Remove verbose debug logs before committing.

## Resources

Clean up in `OnDestroy`, or `OnDisable` for subscriptions: cancel and dispose `CancellationTokenSource`, release Addressables, stop coroutines.

Pure C# classes that hold subscriptions implement `IDisposable`.

With `addressables`, load large textures and audio through it, and always release the handle.

## Tests

| ID | Rule |
|---|---|
| `TEST-01` | Anything that can be unit tested MUST have unit tests: Models, Modules, pure C# classes, data conversions and calculations. |
| `TEST-02` | Tests MUST cover the happy path, edge cases and errors. |
| `TEST-03` | Logic that can't be tested where it is MUST move to a testable class. It usually belongs in a Model. |
| `TEST-04` | A bug fix MUST include a test that fails without the fix. |
| `TEST-05` | Tests MUST be deterministic: no real time, randomness, network or dependence on run order. Fake them through interfaces. |
| `TEST-06` | Views and Controllers hold no logic, so they rely on manual QA. If one needs a test, that signals misplaced logic. |

Use NUnit and Arrange-Act-Assert, with one concern per test. Mock Connectors with NSubstitute. Name tests `{Method}_{Condition}_{Result}`. Writing the test first is encouraged, not mandatory.
