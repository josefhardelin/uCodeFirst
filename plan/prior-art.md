# Prior art — does code-first Umbraco already exist?

**Short answer: partially. The *idea* is proven, but there is no actively-maintained package doing
it the Episerver way for modern Umbraco (v14+), and nothing native in v18.** There is a real gap.

## The landscape

### uSiteBuilder (Vega IT) — the closest precedent
Literally code-first Umbraco, explicitly modeled on Episerver: define document types as C# classes
with attributes, and it builds/synchronizes the doctypes into the DB on startup.
- **Proven the concept** for Umbraco v7/v8 and was popular.
- **Has not kept up** with modern Umbraco (v10+, and certainly not the v14+ backoffice rewrite).
- **Takeaway:** the strongest reference point for *what good looks like*; the implementation is stale.

### ModelsBuilder (ships with Umbraco) — the exact inverse
Generates strongly-typed C# models **from** document types you define in the backoffice
(**schema → code**). What we want is **code → schema**. So ModelsBuilder is not competition — it's the
thing we *invert*. In a code-first world, **ModelsBuilder is disabled**: the hand-written class *is*
the model. (See design-decisions Q5.)

Relevant source: `src/Umbraco.Infrastructure/ModelsBuilder`, `src/Umbraco.Web.Common/ModelsBuilder`.

### uSync — often mistaken for code-first, but it isn't
**DB ⇄ disk serialization**: you build schema in the backoffice, export it to files for source control
and environment promotion. The **backoffice remains the source of truth**; the files are a projection.
`uSync.Migrations` imports from other systems — still not "C# class as truth."

**Why it matters here:** uSync is the *wrong home to build inside* (opposite philosophy), but the
*right partner for deployment*. Our plan uses uSync to promote code-first schema from dev to prod.
(See design-decisions Q7.)

### Older "Our.Umbraco.CodeFirst"
Attribute-based, v6/v7 era, abandoned.

## Conclusion
- No native code-first in Umbraco v18.
- No actively-maintained package doing the Episerver-style code → schema sync for v14+.
- uSiteBuilder proves the idea; ModelsBuilder is the inverse to replace; uSync is the deployment partner.

A focused, modern package has a clear, unoccupied niche.
