# C# Coding Guidelines

Deviations from Microsoft C# standards only.

## Assignments

- No `var`; always explicit types.
- No expression-bodied members.
- Use concrete type, not interface, when the class knows (depends on) the implementing type.
  - Do not do: `IBar foo = new Bar()`
- No discard operator `_` on the left side of an assignment.

## Documentation

- No `<inheritdoc/>`; repeat the full XML doc comment on every member.
- Comment all constants, fields, properties, enums, enum values, events, methods, and nested types with `<summary>`; all method parameters with `<param>`; non-void return values with `<returns>`. End every tag content with a period.
- `/* */` for lengthy explanations only; `//` for all other comments.
- Single-line `///` if ≤ 160 chars incl. tags; otherwise multi-line `///` with one tag per line.
- In `<summary>` use `<see cref="..."/>` for types and events.
- Use `<see langword="..."/>` for `true`, `false`, `null`.
- TODOs: `// TODO <username> <explanation>`.

## Naming

- Avoid abbreviations.
- Abbreviations in strict camelCase; second letter always lowercase (e.g. `commandId`).
- Event-handling methods: `<EventSource><EventName>`, no underscore (e.g. `MqttAdapterMessageReceived`).
- Dictionaries named `<Value>By<Key>` (e.g. `commandsById`).

## Structure

- Group members in regions: `Constants → Fields → Constructors → Destructor/Finalizer → Events → Enums → Properties → Operators → Public Methods → Protected Methods → Private Methods → Structs → Classes`.
- Multi-line logical conditions: place `&&` / `||` at the start of each continuation line.
- Single `return` per method; use a local result variable if needed.
- Primary constructors for record types only.
- Check arguments in public/protected/internal methods; not in private methods except for callbacks/event handlers.

## Testing

- Use `Assert.ThrowsExactly<T>()`, not `[ExpectedException]`.
- Test methods: `When<Method>And<Condition>_Then<Result>`.