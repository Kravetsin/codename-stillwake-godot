---
description: Add a sim module — its assembly, state, commands, wiring, save step, tests, client feature and docs.
argument-hint: <Module> "<what it simulates>"
---

User arguments: `$ARGUMENTS` — the module's name (PascalCase, `Fauna`; its id camelCase, `fauna`) and what it simulates.
Ask for either if missing, and stop.

A module is a new mechanic: it starts as a design (`design-author` → `design-reviewer` → a plan in `docs/plans/`,
CLAUDE.md Workflow). Do not start here without that plan; if there is none, say so and stop. This recipe is the wiring
the plan does not repeat. Walked last on `10c8ea1d` (the fauna module) and on today's code.

## 1. The assembly — `sim/<Module>/`

- `<Module>.csproj`: only `ProjectReference`s to the modules it depends on (`../Crew/Crew.csproj`). `sim/Directory.Build.props`
  gives it its names, `InternalsVisibleTo` the tests and the determinism analyzers. `dotnet sln add sim/<Module>/<Module>.csproj`.
- `<Module>.cs`: `public sealed partial class <Module>Module : Module<<Module>State>`, `Id => "<id>"`, its dependencies as
  query and operation interfaces through the constructor, `Handles<Command>(Problem, Apply)` per command, `System(Phase.…)`
  per system, `Init(Layout)`, `Rebuild()`; queries as public methods returning read-only data.
- `State.cs`: the state class public, its members internal; derived state `[JsonIgnore]`, rebuilt in `Rebuild()`.
- Its commands: records deriving `Command`; a refusal returns the key `<id>.<why>` and changes nothing.
- `Fuzz.cs`: the `Fuzz` override drawing every command, refused ones too (the generic suites in `ModulesTests` use it).
- Its tunables in `sim/Content/<Module>.cs`, never in the module.
- The determinism rules (CLAUDE.md): integers, the world's `Rng`, no `Dictionary`/`HashSet` iteration. The build fails on most.

## 2. Wiring — `sim/Registry/`

`Registry.csproj` references it; `Registry.cs` makes it after its dependencies, in run order, and lists it in the modules
of a surface (`Module[] surface = [...]`). Saved slices follow that order, so a new one goes last unless the plan says why.

## 3. The save — a new module bumps the format

1. Before the bump, write this format's fixture: `STILLWAKE_WRITE_FIXTURE=1 dotnet test sim/Tests --filter MigrationsTests`.
2. Bump `World.SaveFormat` in `sim/Core/Sim.cs`.
3. Add its step to `Migrations.Steps` in `sim/Registry/Migrations.cs`: its slice added by hand, aboard and on every surface,
   empty where a running world allows it (fauna's `Creatures` step is the example), never a silent default.
4. `tools/test.sh Migrations`: every format since `Migrations.Oldest` upgrades, loads and plays on.

## 4. Tests — `sim/Tests/`

- A scenario test (`new Scenario(map)`) for every rule and every refusal reason; an FsCheck `[Property]` for what must
  always hold, proved to bite by breaking the rule once.
- `ModulesTests` covers save, load, two worlds alike and fuzzing for every registered module: do not copy them.
- Commands that cross peers: a case in `net/Tests/DesyncTests.cs`. A system that runs every tick on a big world: a case in
  `BudgetTests`.

## 5. The client — `game/features/<id>/`

A view per thing it draws (a line in `game/SurfaceViews.cs` for a view of the surface), its panel, its overlay; it reads
the module's queries and sends its commands, nothing else. Its strings, refusal reasons included, in
`logic/<Module>.En.cs`. A demo, if it needs one: `/new-demo`.

## 6. Docs

- `sim/<Module>/README.md`: `## Files` first (`DocsGuardTests` checks the table), then what it owns, depends on and
  guarantees, under 8 KB. The same for the feature folder.
- `docs/map.md`: its line under its domain (`DocsGuardTests` fails on a module folder it does not name).
- An ADR, `docs/decisions/YYYY-MM-DD-two-words.md`, then `tools/adr-index.ps1`. `docs/glossary.md` for its words.

## Checks before the PR

- `dotnet build Stillwake.sln` (analyzers) and `tools/test.sh` whole.
- A frame or a recording of it in play (`tools/snap.ps1`).
- The plan in `docs/plans/` deleted, what stays true moved into the README and the ADR.
- Commit `feat(<id>): …`.
