# Current approach vs. code-first

A comparison written for a mixed audience — **developers** and **editors** are different stakeholders
with different stakes, so each is called out.

## The current approach (backoffice-first + ModelsBuilder)

1. A developer opens the Umbraco backoffice and clicks to create document types, tabs, properties,
   data types, etc.
2. ModelsBuilder generates C# models **from** that schema.
3. The developer writes code against the generated models.
4. To move schema between environments, the team uses uSync (export to disk, import on deploy).

### Pros
- **Visual, discoverable.** Non-developers can see and reason about the content model.
- **Editors can adjust** some schema themselves without a deployment.
- **No build step** to change schema — it's live in the DB immediately.
- **Mature tooling** (ModelsBuilder, uSync) that the whole ecosystem understands.

### Cons
- **Slow developer loop.** Click → generate → code. You must run the site and use the UI *before* you
  can write code. Painful with AI-assisted development, where the AI would rather just write the class.
- **Schema lives in a database**, not in source. The C# models are a *generated projection*, not the
  truth. Review/diff/PR of schema changes is awkward (uSync files help, but they're machine output).
- **Drift between environments** is a recurring chore (uSync exists precisely to fight this).
- **Two sources of truth in practice:** the DB (real schema) and the generated models (must be kept in
  sync). Forgetting to regenerate is a classic bug.

## The code-first approach (this plan)

1. A developer writes a C# class with attributes describing the document type and its properties.
2. On startup (in development), the package syncs that schema into the local DB.
3. The *same class* is the runtime model — no generate step.
4. uSync serializes the resulting schema; deployment promotes it to production.

### Pros
- **Fast developer loop.** Write the class, start the site, verify. No clicking, no generate step.
  This is the Episerver experience and is ideal for AI-assisted development.
- **Schema is source code.** It lives in git, gets code-reviewed, diffed, branched, and is the single
  source of truth. Schema changes travel *with* the feature code that needs them.
- **Strongly-typed by construction.** The class you define is the class you query.
- **Deterministic deployment.** Stable GUIDs make uSync exports clean and reviewable.

### Cons
- **Less visual.** The model isn't browsable as a diagram until the site runs; you read C# instead.
- **A rename is a breaking operation** unless a stable GUID is pinned first (mitigated: GUIDs are
  explicit and required — see design-decisions Q3).
- **Editors lose the ability to change *schema*** in the backoffice — by design (see below).
- **New, less-proven tooling** vs. the mature backoffice-first path.

## The editor angle (important)

The instinctive editor objection is *"so editors can't change anything anymore?"* The honest answer
distinguishes two very different things:

- **Content** (pages, text, images, the actual site) — **still 100% editor-owned, in the database.**
  Code-first changes *nothing* here. Editors create and edit content exactly as today.
- **Schema/structure** (what fields exist, what a document type is) — **becomes developer-owned, in code.**

This is exactly the division Episerver used, and it's *why* editors stayed happy there: editors were
never in the business of defining the content model; they were in the business of filling it. Defining
structure is a developer concern that benefits from version control, review, and testing.

### What protects editors in this plan
- **Content is never touched** by schema sync. Removing a *field* could lose that field's data, but
  pages, media, and content remain.
- **Production schema sync is disabled.** In production, code-first does nothing live; schema changes
  arrive only via a deliberate **uSync** import on deploy — the same controlled process teams use today.
- **A dry-run/preview** (roadmap) lets you show editors exactly what schema will change *before* a
  deploy, so there are no "my field vanished" surprises.

### Honest trade-off for editors
Editors (and editor-leaning developers) **lose the ability to tweak the content model in the
backoffice** — add a property, reorder a tab, retune a data type. Under code-first that's a code change
and a deploy. For teams where editors actively shape the model, this is a real change in workflow and
must be agreed up front. For teams where developers already own the model (the common case), it's pure
upside.

## When to choose which

| Situation | Recommended |
|---|---|
| Developer/AI-driven, developers own the model | **Code-first** |
| Editors actively shape the content model in the backoffice | **Current approach** (or code-first with explicit buy-in) |
| Greenfield project, fast iteration wanted | **Code-first** |
| Large existing site with established backoffice workflows | **Current approach**, migrate incrementally |
