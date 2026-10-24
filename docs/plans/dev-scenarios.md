# Plan: dev scenarios as classes, demos out of release builds

The "Dev scenarios as classes" task of [dev-workflow.md](dev-workflow.md). Reviewed once by `design-reviewer`.

## Problem

A demo today is wired in up to nine places of `game/dev/Main.Dev.cs`, each a chain of `Has("--…")`: the map
(`DevLayout`, last wins), the base world and the seeds (`Alone`), the hero's script (`DevScript`, first wins),
its start (`PlaceHero`), nodes (`AddDebrisDemos`, `AddDevDemos`), hand scripts (`StartHandDemos`, with five
fields in `Main.cs` and their calls in `ShowHands`), and `PlayDev`/`Host`. Its argument also goes into
`DevModes.All`. `ItemsView` makes `ItemsDemo` itself (`demo:`). Every demo ships in release builds, where
they are cheats.

## Where it is heading

Users: the ~45 demo flags there are now, and each new demo (the `/new-demo` recipe). Nothing else.

## Design

A **scenario** is a class for one flag, or a few that share code (`--wreck`, `--wrecks`, `--harpoon`). Several
can be active in one run, as now (`--testbed --open=crate --marks rock`); each overrides what it needs.

```csharp
[Dev("--store", "a capacitor bank filling from a panel, then emptying into an arc smelter")]
sealed class StoreScenario(DevArgs a) : DevScenario(a)
{
    public override World? Create(IReadOnlyList<Module> m) => StoreWorld.Create(m);
    public override Layout? Map => StoreWorld.Map;
    public override void Attach(DevContext c) => c.Add(new StoreDemo(c.World));
}
```

`DevScenario(DevArgs args)` hooks, each virtual with a default of nothing:

| Hook | Replaces | Over the active scenarios |
| --- | --- | --- |
| `Layout? Map` | `DevLayout`, `PlayDev`'s aim | the first that gives one |
| `World? Create(modules)` | `Alone`'s base world | the first that gives one |
| `World Seed(World, DevSeed)` | `Alone`'s seeds; `Host`'s with `Hosted` | each in turn; `DevSeed` has `Modules` and `M` |
| `bool Hosted` | `Host` seeding debris, wrecks | seeds when hosting too |
| `HeroAutopilot? Script` | `DevScript` | the first that gives one |
| `Func<Registry,int>? Start` | `PlaceHero` | the first that gives one, after `--edge`; `--open` and `--near` are scenarios of `Order = 20`, so last |
| `Attach(DevContext)` | `AddDebrisDemos`, `StartHandDemos`, `AddDevDemos`, `PosesDemo`, `ItemsView`'s demo | each in turn, once Main has made everything |
| `Started(Client)` | `PlayDev`'s planet and strip | each in turn |
| `Frame(Client, Registry, Action<string>)` | `ShowHands`' five demo calls, `BeamBack` | each in turn, every frame |

`DevContext` holds what scenarios use, as getters, null where not made (`Items` with `--perf`): `Ui`, `Hands`,
`Debris`, `Items`, `Fabricator`, `Menu`, `Suits` (`MenuDemo`), `DebrisPage` (`HandbookDemo`), `World`
(`(Client, Registry)?`), `M`, `Layout`, `Hero`, `Add(Node)`. A getter goes in when a scenario needs it.

**Order.** One `Order` per scenario, then its name: "the first that gives one" and "each in turn" both follow
it. The chains disagree today (`--hills` beats `--gym`'s map, `--gym` beats `--hills`' script), which is
harmless only because no run combines them; the runs that combine scenarios are the ones under Checks.
The testbed family (`--testbed`, `--jetpack`, `--charge`, `--dust`) shares a `TestbedScenario` base.

**Registry.** `DevScenarios` finds every `DevScenario` with `[Dev]` in the game assembly by reflection, once,
sorted by `Order` and name, and makes the active ones (`Flag` by `Has`, the others by `Given`/`Value`).
Main calls each hook over them. A static list would mean a second file to touch per demo.

**Arguments.** `[Dev(name, doc, Shape = DevShape.Flag, Order = 0)]` is a scenario's entry; `DevModes.All`
keeps the other arguments. `DevArgs` takes both, the scenarios' handed in by Main. `game.tests` cannot
reflect over Godot code, so `DevModesTests` reads `[Dev("--…"` from the source with its shape and adds those
names to the listed ones; a misspelt `Has("--hils")` still fails it.

**Release builds.** Demo-only code is `**/*Demo.cs`, or under `game/dev/demos/` (scenarios, `GroundHands`,
`FaunaHands`, and whatever else only demos use). The csproj removes both under `ExportRelease`;
Main names no demo type. CI builds `-c ExportRelease` as well, so a demo type named from kept code fails
there and not at release. The title's Testbed button stays `OS.IsDebugBuild()`.

## Stays in Main, not scenarios

- Modifier arguments that scenarios read: `--manual`, `--autopilot`, `--pick`, `--into`, `--axe`, `--precise`,
  `--arc-drill`, `--open-at`…
- Networking and desync tools: `--host`, `--join`, `--relay`, `--cheat`, `--auto`, `--doors`, `--toggle`,
  `--hatches` (`DevTick` keeps them: they use `_doors`, `_start`, `_role`).
- Measuring and view: `--frames`, `--measure`, `--allocs`, `--snap`, `--for`, `--sweep`, `--lit`, `--dark`,
  `--zoom`, `--view`, `--no-homes`, `--slots`. They stay in release builds: a friend's `--frames` log is how
  a slow machine is diagnosed.
- Worlds looked at, not played: `--perf` (also hosted), `--gallery`, `--world`; and `--aside`, for a co-op window.
  A `Walked` hook for `--perf` and `--gallery` alone was cut.
- `--pose-sheet` (makes nothing else) stays an early return in `_Ready`; `PoseSheet` stays in `game/dev/`.

## Deliberately not done

- No check for two scenarios that both give a map or world: the first wins, as the chains decide silently now.
- `HeroAutopilot` and its maps stay in `features/crew`: the hero's tests use them. `StoreWorld` and `Testbed`
  stay too (not `*Demo.cs`), dead code in a release build, harmless.

## Checks

- A frame per flag before and after (taken on `main` before the change), and the combinations the docs
  name: `--testbed --open=…`, `--testbed --near=furnace --shimmer-demo`, `--ground=hit-… --arc-drill --axe`,
  `--strip --fauna=… --fauna-ram`, `--planet --fauna`.
- `dotnet build -c ExportRelease` passes with no demo in it.

## Steps

1. Done: the framework (`game/dev/DevScenario.cs`, `DevScenarios.cs`), `--store`, the `--testbed` family,
   `--ground` and the `--wreck` family in `game/dev/demos/`; `Main` asks the scenarios first, then its old
   chains. Frames of the 9 flags and 6 testbed combinations match `main`'s.
2. Done: the rest, by kind, in `game/dev/demos/` (`Hero`, `Machine`, `Plan`, `Debris`, `Planet` scenarios);
   `Main.Dev.cs` keeps only what stays in Main, and `ItemsView` no longer makes `ItemsDemo`. Done here rather
   than by Sonnet: every batch edits `Main.Dev.cs` and `DevModes.cs`, so batches could not run apart.
3. The old chains gone, `ExportRelease` exclusion and its CI build, the `/new-demo` checklist in this file.
