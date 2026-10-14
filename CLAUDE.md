# Stillwake

A 2D side-view space game: an always-moving ship salvages debris and turns it into automated
production. PC (Steam), online co-op for 4+ players in host-owned worlds. The game is described in
`docs/concept.md`; why things are the way they are, in `docs/decisions/`.

The game moved from TypeScript to Godot and C# ([0025](docs/decisions/0025-godot-and-csharp.md)).
Until the port is done, work follows [`docs/plans/port.md`](docs/plans/port.md): the next task is the
first open one there. The TypeScript repo, `Kravetsin/codename-stillwake` (checked out beside this
one as `../codename-stillwake`), is the specification: its module READMEs, its rules and its tests.

## Stack

Godot 4.7 (.NET build) · C# 12 on .NET 8 · plain C# assemblies for the simulation · Godot 2D
(TileMapLayer, 2D lights, shaders) and Control nodes for UI · Godot physics on the host only ·
our lockstep over ENet, GodotSteam in release · xUnit and FsCheck. Why: [0025](docs/decisions/0025-godot-and-csharp.md).

## Engineering principles

Authors apply these when designing, reviewers when reviewing. If a rule below contradicts a principle,
the principle wins and the rule gets revisited.

1. **Design before code.** A non-trivial feature starts as a short design (`design-author`), cut down by
   `design-reviewer`, then a plan. Rewriting the same code three times means the first two passes were
   design done in the most expensive medium.
2. **No abstraction without two callers today.** An interface, generic parameter, registry, strategy or
   option needs two existing callers, in the same change or already merged. "Later" does not count.
3. **Trade-offs are explicit.** "We do not handle X because Y" is a complete answer. A design without
   its list of deliberate omissions is unfinished.
4. **Handle what players can actually do.** Before adding a guard, fallback or recovery path, name the
   sequence of player actions that produces the case. If it takes imagination, write it down as an
   accepted limitation instead.
5. **Reuse first.** Before a new module, operation, command or dependency, say in one sentence why the
   existing ones do not cover the case.
6. **Trust matches the threat model.** Co-op among friends: no anti-cheat, client-authoritative hero
   movement, a trusted host. Revisit only when the product changes.
7. **Tunables are data.** Every number a designer may change lives in `sim/Content`.

## Layout

| Folder           | Holds                                                                                 |
| ---------------- | ------------------------------------------------------------------------------------- |
| `sim/Content`    | game data as C# records and constants: materials, machines, rates, ASCII maps; no logic |
| `sim/Core`       | the simulation core: modules, phases, commands, saves, hashes, the world's Rng, layouts |
| `sim/<Module>`   | one assembly per sim module (`Grid`, `Rooms`, …), referencing only what it depends on  |
| `sim/Registry`   | every module of a world wired through constructors, in run order; the wreck layout     |
| `sim/Analyzers`  | our Roslyn analyzer: the determinism rules BannedApiAnalyzers cannot express          |
| `sim/Tests`      | xUnit and FsCheck: scenario, invariant and core tests                                 |
| `net/`           | the session (`Net`): host, client, protocol, playout, hash checks; its tests (`Tests`) |
| `game/`          | the Godot project: rendering, UI, input, the hero controller, the ENet transport      |
| `docs/`          | concept, decisions (ADRs), plans, playtests                                           |

Assemblies reference one way only: `Content` → `Core` → modules → `Registry` → `game`, and
`Core` → `Net` → `game`. Nothing under `sim/` or `net/` references Godot.

## Sim modules

Every gameplay feature is a module: an assembly `sim/<Module>/` with a class deriving from
`Module<TState>` ([0004](docs/decisions/0004-modules-and-tests.md)), its state class public with
internal members, so other assemblies can name it but never read or write it.

```csharp
public sealed class RoomsModule : Module<RoomsState>
{
    public override string Id => "rooms";
    public RoomsModule(IGridQueries grid)            // dependencies as interfaces, through the constructor
    {
        Handles<Seal>(c => Problem(c), c => Apply(c));   // a command: validate, then apply
        System(Phase.Derivation, () => { ... });         // a system in its phase
    }
    protected override RoomsState Init(Layout layout) => ...;   // the starting slice, from the map
    public override void Rebuild() { ... }                      // derived state, after create and load
    public int? RoomAt(int cell) => ...;                        // queries are public methods
}
```

- A module writes only its own slice. Others read it through its query interface (`IGridQueries`) and
  change it only through its operation interface (`IGridOps`), which it receives only when it
  declares the dependency in its constructor. The compiler enforces both.
- Queries return read-only data (`ReadOnlySpan<T>`, `IReadOnlyList<T>`, records); a caller never
  casts one back to change it.
- A tick runs in phases — commands, construction, derivation, simulation, events — and within a phase
  modules run in the order they are registered, every module after its dependencies.
- Derived state is marked `[JsonIgnore]` and rebuilt in `Rebuild()`; it is never saved or hashed, so
  it may hold anything. The module keeps it current while the world runs.
- Commands are records deriving from `Command`, validated when they apply, on their stamped tick. A
  refused command returns a reason, an i18n key `<moduleId>.<why>`, and leaves state untouched.
  Validate game rules, not payload shapes. Throwing is for broken invariants — our bugs.
- A command says who sent it: the host stamps `From` with the peer's id; a peer has one hero. Queries
  never see it. Modules have no clock: one that needs time counts ticks in its own slice.
- A module's `README.md` is the source of truth for what it owns, depends on and guarantees. Keep it
  current in the same change.

## Client

`game/` holds a scene and script per feature (`game/features/<name>/`), mirroring a sim module: what
it draws in the world, its panel, its overlay. A feature reads the sim through public queries, changes
it only by sending commands, and never reaches into another feature. Game rules live in the sim only;
the client shows what the sim decided. Panels read on a timer, never every frame. What is always on
screen speaks without words — colour, light, motion; text is on demand, in overlays and by the
pointer.

- Cells are drawn with `TileMapLayer`, never a node per cell; light occluders are merged per wall
  run. A node per cell costs CPU time with every light (0025).
- Floats are fine in the client: only the sim must be deterministic.

## Co-op

Host-owned worlds over Steam; single-player is a host without guests. The host stamps every command
with the tick it applies on and broadcasts it, and every peer runs the same simulation. Heroes are
client-authoritative and replicated about 20 times a second; physics runs on the host only and reaches
the sim as commands. Details: [0002](docs/decisions/0002-coop-model.md).

## Determinism

Every peer must compute the same world from the same commands. In `sim/`:

- No floating point (`float`, `double`, `decimal`, `Math` on them): quantities and time are integers,
  units and ticks. Where an amount is divided, the rounding direction is chosen and written down.
- No `Random`, clocks (`DateTime`, `Stopwatch`, `Environment.TickCount`), threads or `async`: the
  world's `Rng`, ticks, one thread.
- No iteration in unguaranteed order: never `foreach` over a `Dictionary` or `HashSet`; iterate lists
  or sorted keys. Never let `GetHashCode` decide an order. Strings compare ordinally.
- Nothing reaches the sim except commands. A hero's position enters as commands from `crew`; physics
  results enter as commands from the host.

The analyzers fail the build on these (`sim/BannedSymbols.txt`, `sim/Analyzers`); the desync tests
catch the rest.

## Budgets

On the reference world (256×128 cells, every layer filled) on a mid-range PC: 60 fps with the sim
running, and a sim tick under 2 ms. Measure render time with Godot's own monitors
(`RenderingServer.ViewportGetMeasuredRenderTimeCpu`) and the sim with a `Stopwatch` in a test, never
in the sim.

## Tests

- **Scenario tests** are the default: `new Scenario(map)` in `sim/Tests` builds a world from an ASCII
  map (characters per `Data.Legend`), then `.Do(command)`, `.Run(ticks)` and assertions through the
  modules' queries and `.Refusals`. Every rule of a module and every refusal reason gets one. Port a
  module's TypeScript tests with it, one for one.
- **Invariant tests** (FsCheck `[Property]`) for what must always hold; draw maps that reach every
  limit, and prove the test bites by breaking the rule once and watching it fail.
- **Generic suites** (`ModulesTests`) cover every registered module: save, load and play on, two
  worlds kept alike, and every module with commands drawing them in its `Fuzz` override (refused
  ones too). Do not copy them per module.
- **Client:** scripted runs (`-- --autopilot` and the like) recorded with `--write-movie` and read
  frame by frame; logic such as the hero controller in plain C# with unit tests.
- A bug fix starts with a failing test. Tests never touch real time; time is ticks.

## Workflow

- Small commits straight to `main` while the port runs; CI must stay green. Once the second developer
  works here too, one task, one branch, one PR.
- A non-trivial feature: `design-author` → `design-reviewer` → a plan in `docs/plans/` → `/review-plan`
  → implementation → `/review-implementation` → human review.
- Docs: decisions in `docs/decisions/` (numbered; a changed decision gets a new ADR that supersedes the
  old one), the game in `docs/concept.md`, plans in `docs/plans/` — deleted once done, after what stays
  true moves into an ADR, a module README or the concept.
- Keep this file short. Detail about a module belongs in its README.

## Commands

```
dotnet build Stillwake.sln                 # everything, analyzers included
dotnet test Stillwake.sln                  # the sim's tests
dotnet format Stillwake.sln --verify-no-changes
godot --path game                          # play; --headless --import once after new assets
godot --path game --write-movie /tmp/f/shot.png --fixed-fps 20 --quit-after 100 -- --autopilot
```

`godot` is `Godot_v4.7.2-stable_mono_win64_console.exe` (on this machine in
`C:\Users\Kravets\Tools\godot-4.7.2\`).

## Conventions

- English in code, comments, commits and docs. Player-facing text lives in one table of i18n keys,
  shown through `T(key)`; refusal reasons use the sim's keys.
- Comments only for a non-obvious why, at most two lines; `///` summaries on public members say what
  they are for. Longer reasoning goes to `docs/`.
- Commits follow Conventional Commits with the module as scope: `feat(atmosphere): vent rooms open to
space`. Imperative, subject under 72 characters.
- C#: PascalCase types and members, `_camelCase` private fields, file-scoped namespaces, one public type
  per file unless small records belong together. A module's assembly is `Stillwake.Sim.<Module>`, its
  id the camelCase name (`lifeSupport`).

## Out of scope

Phones, web releases, consoles, dedicated servers, host migration, anti-cheat.
