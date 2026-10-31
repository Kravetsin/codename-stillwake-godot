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
| #189 | CI formats in its own job beside build+test, a gate job keeps the required check's name; a pre-commit hook (`tools/hooks`) formats staged `.cs`; budget bounds are a desk's, ×5 in CI (`STILLWAKE_BUDGET_SLACK`) | a minute on CI's critical path, `style(...)` commits, budget bounds loosened by hand after red runs |
| #190 | A dev demo is a scenario class with `[Dev("--name", …)]` in `game/dev/demos/`, found by reflection; `Main.Dev.cs` keeps only its tools; release builds drop the demos and CI builds one (`game/dev/demos/README.md`) | a new demo editing `DevModes.cs` and several spots of `Main.Dev.cs`; cheats in release builds |
| #191 | The views of a surface are a list in draw order (`game/SurfaceViews.cs`), made and freed in one loop, a flag for those fading with the plan; a view giving light is an `ILightSource` that `LightView.Shining` gathers | a new view's field, free, make and fade lines in `Main.cs` and a `Func` in `LightView` |
| #192 | New ADRs are `YYYY-MM-DD-two-words.md`, titled `# Title` and named by their slug, the numbered ones kept; the index accepts both and is `merge=union`; the first, for #185, amends 0031 | renumbering ADRs when two developers took the same next number |
| #194 | Recipes `/new-machine`, `/new-module`, `/new-demo` in `.claude/commands/`, walked on today's code; `/new-machine` tested by Sonnet adding a throwaway machine; `MachineArtTests` catches a machine with no picture | the add-a-thing checklists in people's heads; a missing picture crashing the first frame |
| #311 | CI builds and tests with `-c Release`, `TieredPGO` off, the budgets' slack ×2, `lscpu` in the log ([2026-10-31-ci-tests-in-release](../decisions/2026-10-31-ci-tests-in-release.md)) | half of `dotnet test`'s time on CI |

Also on `main` from parallel work: `Main` split by concern (`Main.cs`, `Main.Sessions.cs`, `dev/Main.Dev.cs`),
`DevModes` as the documented list of dev arguments, the build menu laid out from content.

Deliberately not done: sound cues per feature. It was built (branch `refactor/feature-sounds-full`, reference
only) but cues made from another file's cues need holder classes, since static initializers across partial
files run in no defined order. A later edit can break that silently with a null at run time, and `Sound.cs`
is touched half as often as `En.cs` was. It stays one catalogue.

## Next, in order

Before each task, check `git log` on its files: someone may have moved them since.

### 1. Smaller items

- README 8 KB cap: `building` (8062 B) and `crew` (8046 B) are near it again. Trimming them has started to cut
  meaning to save bytes. Decide with both developers: raise the cap, or move tables elsewhere.
- Light review preset: determinism, tests and correctness by default; all eight reviewers for a new mechanic.
  Keep `design-reviewer` mandatory before a new mechanic: it catches real mistakes.
- `IXOps : IXQueries`, so a module takes `IPowerOps power` once instead of `IPowerQueries, IPowerOps`.
- A split `DesyncTests.cs` by subject (as #183 did for `ContentTests`).
- Big classes a smaller model struggles to hold: `HeroView` (held tools as `IHeldTool` strategies) and
  `InventoryPanel` (machine windows by kind). Check what the `Main`/`HeroView` split already did.

### 2. CI speed

Tests in Release halved `dotnet test` ([2026-10-31-ci-tests-in-release](../decisions/2026-10-31-ci-tests-in-release.md)), and it
is still most of a run. Measured 2026-10-31, by what each would save:

- A PR runs only what its paths can break, `main` everything. Of the last 100 PRs 9% touched only docs (`DocsGuardTests` is
  enough) and about a quarter only the client (no `Sim.Tests` or `Net.Tests`, but `sim/Tests` links `game/Content/logic/Light.cs`).
- The budget tests in a job of their own: they run alone, after the others, about 30% of `Sim.Tests`' time.
- Planets shared between tests: three tests grow the first planet on the same 50 seeds, 27% of the summed test time.
- The slowest tests in the run's summary, from a trx: the step grew from 15 s to 150–400 s in two weeks, one test at a time.
- `format`, about 2 minutes beside `build, tests`, is a run's floor once that gets as short. `dotnet format whitespace --folder`
  takes 6 s against 35 on a desk and catches whitespace, but not `IDE0161` (file-scoped namespaces).
- x64 or `ubuntu-24.04-arm`, once `lscpu` in the logs shows how often a run draws x64's slowest machines (see the ADR).

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
