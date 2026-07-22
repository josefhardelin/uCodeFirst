# Introduction

uCodeFirst syncs C# class definitions into an Umbraco 17+ database on startup. Document types, element types, media types, compositions, languages, templates and dictionary items are all declared as plain C# — attributes and interfaces — instead of being clicked together in the backoffice.

## Why

- **One source of truth.** The C# class *is* the schema. No drift between what the code expects and what the backoffice actually has.
- **Code review for schema changes.** Adding a property is a diff, not a screenshot.
- **No generate step.** Unlike ModelsBuilder, there's nothing to run after changing a class — the model *is* the definition.

## How it fits together

1. `CodeFirstComposer` (auto-discovered by Umbraco's `IComposer` mechanism) registers the package and wires a startup handler.
2. On startup, the handler scans all loaded assemblies for classes/interfaces decorated with `[DocumentType]`, `[ElementType]`, `[MediaType]`, or `[CompositionType]`.
3. A validation pass checks for duplicate aliases/GUIDs, duplicate property aliases, and dangling references — sync aborts with a single aggregated error rather than partially applying a broken schema.
4. Two sync engines run in order: one ensures shared Umbraco data types exist for every property editor attribute in use, then one creates/updates the content types themselves.

This only ever runs in development. Production environments promote schema via [uSync](https://github.com/KevinJump/uSync), using `.uSync` files exported from a dev environment where uCodeFirst has already run — see [Dev vs production](getting-started.md#dev-vs-production).

For the full attribute and property-editor reference, see the [API Reference](../api/index.md).
