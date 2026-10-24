# Plan: a smoother development loop

Goal: a fix or a new object touches only its own module or feature folder, plus data in `sim/Content`, and the
process is simple enough that a smaller model (Sonnet) can do routine work with no review by a larger one.
Started 2026-10-24 from a git-history analysis: the files most commits touched, the files merges fixed by hand,
and the CI failures. Delete this plan once its tasks are done, after what stays true moves into CLAUDE.md or
READMEs.

## Done

| PR | Change | What it removed |
| --- | --- | --- |
| #181 | A machine's map character and legend line live on `MachineSpec` (`Char`, `Means`) | `Legend.cs`'s two tables edited per machine |
| #182 | `sim/Directory.Build.props` sets a module's names and `InternalsVisibleTo` | boilerplate in each module csproj |
| #183 | `ContentTests` is one partial class across 7 files by subject | a 1334-line merge hot spot |
| #184 | Godot-free client code lives in its folder's `logic/`; `game.tests` compiles `../game/**/logic/**/*.cs` | 148 hand-written `<Compile Include>` lines |
| #185 | `World.Load` refuses unknown fields (`UnmappedMemberHandling.Disallow`) | format bumps, identity steps and fixtures for added fields |
| #186 | The asteroid property allows a start pushed past a hero's view | about half of the red CI runs |
| #187 | Each feature's strings in its `logic/<Name>.En.cs`; `En.cs` builds `Table` from them | `En.cs` as a file every task edits |

Also on `main` from parallel work: `Main` split by concern (`Main.cs`, `Main.Sessions.cs`, `dev/Main.Dev.cs`),
`DevModes` as the documented list of dev arguments, the build menu laid out from content.

Deliberately not done: sound cues per feature. It was built (branch `refactor/feature-sounds-full`, reference
only) but cues made from another file's cues need holder classes, since static initializers across partial
files run in no defined order. A later edit can break that silently with a null at run time, and `Sound.cs`
is touched half as often as `En.cs` was. It stays one catalogue.

## Next, in order

Before each task, check `git log` on its files: someone may have moved them since.

### 1. CI: format in parallel, a pre-commit format hook, timing tests with headroom

- Why: format takes 55 s of a ~4–7 min run on the critical path, and whitespace fixes still cost `style(...)`
  commits.
- What: `.github/workflows/ci.yml` runs `dotnet format --verify-no-changes` as its own job beside build+test.
  Add a hook that runs `dotnet format --include` on the staged `.cs` files: a git `pre-commit` (documented in
  README) or a Claude `PostToolUse` hook in `.claude/settings.json`. Timing tests (`BudgetTests` and other
  `Stopwatch` tests) get a ×3 limit in CI (an env var CI sets); strict limits run locally or on a schedule.
  Do not make them non-blocking: nobody watches a check that cannot fail.
- Note: a random property failure is a bug in the code or the test until shown otherwise. Replay it by its
  seed (`[Property(Replay = "a,b")]`) and fix it, as #186 did. Never loosen it to make CI pass.
- Who: any model. Effort: small.

### 2. Dev scenarios as classes; demos out of release builds

- Why: `game/dev/Main.Dev.cs` still holds ~100 `Has("--…")` checks. A new demo edits `DevModes.cs` and several
  spots in `Main.Dev.cs`: layout, world, seed, hero start, the demo node, frame hooks. Every demo ships in
  release builds, and they are cheats in effect.
- What: one class per scenario, found by reflection (or a static list in one file if trimming ever matters):
  ```csharp
  [Dev("--store", "a capacitor bank filling from a panel, then emptying into an arc smelter")]
  sealed class StoreScenario : DevScenario
  {
      public override Layout? Map => StoreWorld.Map;
      public override World Create(Mods m) => StoreWorld.Create(m);
      public override void Attach(DevContext c) => c.Add(new StoreDemo(c.Session));
  }
  ```
  `DevModes` keeps the arguments that are not scenarios (`--host`, `--zoom`…) and lists scenarios from their
  attributes, so `--help` and `DevModesTests` stay exact with no second list. Then main no longer names demo
  types, and release builds can drop them (`<Compile Remove>` on a dev folder or suffix under the export
  configuration). The title's Testbed button asks the registry.
- Pitfalls: order matters today. The world chain picks the first matching flag (`--store` before `--testbed`),
  and `AddChild` order is draw order (the furnace before the light). Make both explicit (`Order`) instead of
  relying on discovery order. Few flags have tests, so check each migrated scenario by a frame before and after
  (`tools/snap.ps1 out.png -- --<flag>`).
- Who: a larger model designs `DevScenario` and migrates 2–3 different demos as examples. Sonnet migrates the
  rest by example in batches, each checked by frames. Effort: medium.

### 3. World views by interface

- Why: each per-surface view in `Main` is a field, a free and a creation in the surface rebuild. Many also
  appear in the plan-fade list and in a `LightView` `Func` property. `LightView` grows a property per light
  source feature.
- What: `ISurfaceView { int Order; Node Make(SurfaceContext) }`, made and freed in one loop; marker interfaces
  `ILightSource` (its lights) and `IFadesWithPlan`, which `LightView` and the fade gather from the views.
- Read `Main.cs` first: the split may have changed where views are made.
- Who: larger model for the interfaces and first two views, then Sonnet. Effort: medium.

### 4. ADR names by date and slug

- Why: eight renumberings in five days, from two developers taking the same next number. Each renaming
  breaks `(0081)` references in code and READMEs.
- What: new ADRs are `docs/decisions/YYYY-MM-DD-<slug>.md`, referred to by slug; numbered ones stay.
  `DocsGuardTests.AdrIndex` and `tools/adr-index.ps1` accept both forms. Consider `merge=union` in
  `.gitattributes` for append-only lists such as `docs/decisions/index.md`.
- Open: whether #185 (unknown fields refused, no bump for added ones) gets an ADR amending 0031; it could be
  the first in the new form.
- Who: any model. Effort: small.

### 5. Recipes: `/new-machine`, `/new-module`, `/new-demo`

- Why: the "add a thing" checklists live in people's heads. A recipe with a short checklist and the files to
  touch lets a smaller model do it right the first time.
- What: `.claude/commands/new-machine.md` (and the others): the steps, the files, and the checks before a PR.
  Checks: README rows, no hand-written list to extend, the save rule (CLAUDE.md, Sim modules). Write each
  recipe from a real recent example (the capacitor bank, `39f6ed5c`; the fauna module, `10c8ea1d`), re-walked
  on today's code. `/new-demo` waits for task 2.
- Who: larger model writes them; test each by having Sonnet add a throwaway machine with it. Effort: small–medium.

### 6. Smaller items

- README 8 KB cap: `building` (8062 B) and `crew` (8046 B) are near it again. Trimming them has started to cut
  meaning to save bytes. Decide with both developers: raise the cap, or move tables elsewhere.
- Light review preset: determinism, tests and correctness by default; all eight reviewers for a new mechanic.
  Keep `design-reviewer` mandatory before a new mechanic: it catches real mistakes.
- `IXOps : IXQueries`, so a module takes `IPowerOps power` once instead of `IPowerQueries, IPowerOps`.
- A split `DesyncTests.cs` by subject (as #183 did for `ContentTests`).
- Big classes a smaller model struggles to hold: `HeroView` (held tools as `IHeldTool` strategies) and
  `InventoryPanel` (machine windows by kind). Check what the `Main`/`HeroView` split already did.

## Delegating to Sonnet: what worked

Three tasks were run as Sonnet sub-agents and reviewed (#181, #184, #187).
- Mechanical work with cheap checks went well: moving 148 files with their `.uid`s, 570 strings moved verbatim.
  Cheap checks are a script comparing before and after, the compiler, and the docs guard.
- Write the goal as an outcome ("adding a machine must need one line of data"), not only the steps. Without
  it, Sonnet rebuilt the removed second table inside a test.
- Name the traps known in advance: `.uid` files beside `.cs`, the README cap, paths named in other projects.
  End with a self-check list.
- Design forks (the sound cues' initialization order) went to clever but fragile code. Decide them in the
  brief, or keep them for a larger model.
- Delegation saves money only on large batches. On a small task, writing the brief and reviewing cost as much
  as doing it.
