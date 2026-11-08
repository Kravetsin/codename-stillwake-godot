# dev scenarios: demos for recordings and checks by frame

A dev scenario is what a run started with its argument plays (`godot --path game -- --store`): a class with
`[Dev("--store", "what it does")]`, deriving from `DevScenario` (`game/dev/DevScenario.cs`). `DevScenarios`
finds every one by reflection, so `Main` names none and a new demo touches no other file. Demos are cheats in
effect, so a release build has none of this folder and no `*Demo.cs` anywhere.

## Files

| File | Holds |
| --- | --- |
| `StoreScenario.cs` | `--store` |
| `AnimeScenario.cs` | `--anime`, `=bare`, `=bag`, `=walk`, `=door`, `=cave`, `=colours`, `=colours-dark`: the anime slice's frames (tools/anime-art) |
| `PlatformScenario.cs` | `--platform`: launch platforms, a run cabled and lit green, one uncabled and red, a piece alone; `--descent`: the pod down and back |
| `TestbedScenarios.cs` | `--testbed`, and the scenarios played on it: `--jetpack`, `--haul`, `--charge`, `--dust`, `--wheel`, `--signs` |
| `WreckScenarios.cs` | `--wreck`, `--wrecks`, `--harpoon` (seeded hosting too), `--grapple` |
| `GroundScenario.cs` | `--ground=<mode>` with its modifiers (`--axe`, `--precise`, `--take-at`, `--ground-offset`, `--sprint`) |
| `GroundHands.cs` | `--ground`'s hands: the tool held on the cell `GroundDemo` names |
| `HeroScenarios.cs` | the hero's maps and scripts: `--poses`, `--cape`, `--ledges`, `--ladders`, `--feel`, `--gym`, `--lone-hatch`, `--hills`, `--floor`, `--segments`, `--die`, `--breathe`, `--breathe-air`, `--suit` |
| `MachineScenarios.cs` | machines and items: `--furnace`, `--items`, `--menu-demo`, `--open`, `--near`, `--marks`, `--card`, `--charger`, `--arc`, `--bench`, `--tree`, `--board`, `--flash`, `--pod`, `--handbook`, `--tool-cards`, `--salvage` |
| `PlanScenarios.cs` | the plan's hand scripts: `--plan-demo`, `--mark`, `--layer-demo`, `--shimmer-demo`, `--driver-demo` |
| `DebrisScenarios.cs` | `--debris` (seeded hosting too), `--fly`, `--asteroid`, `--sortie`, `--net`, `--net-press` |
| `MeteorScenario.cs` | `--meteors`: the new game's wreck with its first meteor seen coming at once; `=N` its power |
| `PilesScenario.cs` | `--piles`: the hold with an axe in hand, a sledgehammer, the scanner worn; `--autopilot` breaks the piles. |
| `CapsuleScenario.cs` | The wreck with its trade station's capsule just gone or about to come home (--capsule) |
| `FlareScenario.cs` | `--flare`: the new game's wreck with a magnetic flare's warning begun at once; `=repair` a lamp half repaired |
| `PlanetScenarios.cs` | `--planet` (with `--valley`), `--strip` (with `--beam-back`), `--creatures` and its modifiers, `--nailer`, `--railgun`, `--hitboxes`, `--pickaxe`, `--ore-shine` (`=off` without the lamp), `--lake`, `--pour` |
| `TabletHands.cs` | A script's hands on the research tablet: `--tree`, `--board` |
| `CreatureHands.cs` | `--creatures`'s hands: the hero hunting the creatures with the axe, then the drill |
| `ClimbScenario.cs` | `--climb`: the first planet, the hero at the bottom of its deepest chasm, grappling gun in hand, the harpoon beside, creative (grapple-climbs) |
| `MeleeScenario.cs` | `--melee`: the hero in space by a hull, still pieces all round, the axe, pickaxe, sledgehammer and spear on 1-4, creative (swing-motions) |

## How a run is put together

Several scenarios may be active at once (`--testbed --open=crate --marks rock`). Each overrides only the hooks it
needs; `Main` asks the active ones in their order:

| Hook | Over the active scenarios |
| --- | --- |
| `Map` | the first that gives one; the wreck as drawn with none |
| `Create(modules)` | the first that gives one; a world made from the map with none |
| `Seed(world, seed)` | each in turn, through the save; hosting too where `Hosted` |
| `Script` | the first that gives one; `--autopilot` alone plays the wreck's |
| `Start` | the first that gives one, after `--edge` |
| `Attach(context)` | each in turn, once Main has made everything and the session runs: nodes, hand scripts |
| `Started(client)` | each in turn, as the session starts: commands such as opening a planet |
| `Frame(client, m, log)` | each in turn, every frame, before the hands |

Their order is `[Dev(…, Order = n)]`, lowest first, then the name. Where two give a map, world, script or start,
the lower wins: `--store` (0) before the testbed family (1); the hero's scripts before `--gym` and `--lone-hatch` (1),
those before `--hills` (2), those before the strip's (5); `--open` and `--near` (20) start the hero only when nothing
else does. No run is checked for two scenarios that both give one: the lower wins silently. A getter of
`DevContext` is null where the run made none (`Items` with `--perf`).

Not scenarios, so in `game/dev/Main.Dev.cs` and in release builds: networking (`--host`, `--join`, `--auto`,
`--doors`, `--toggle`, `--hatches`), measuring and the view (`--frames`, `--measure`, `--snap`, `--zoom`, `--sweep`,
`--fx-bench`…), the worlds only looked at (`--perf`, `--gallery`, `--world`), `--aside` and `--pose-sheet`.
Modifiers a scenario reads (`--manual`, `--autopilot`, `--pick`, `--axe`…) stay in `game/dev/logic/DevModes.cs`.

## Release builds

`game/Stillwake.Game.csproj` removes `dev/demos/**` and `**/*Demo.cs` under `ExportRelease`, and CI builds that
configuration, so kept code naming a demo type fails in the PR. A helper only demos use goes here or ends in
`Demo.cs`. Worlds that demos play (`Testbed`, `StoreWorld`) stay, unused, in a release build.

## Adding a demo

1. Its script or node in its feature (`features/<name>/<Name>Demo.cs`) or in `game/dev/` (`<Name>Demo.cs`).
2. A scenario here, in the file of its kind: `[Dev("--name", "what it shows (NameDemo)")]` and the hooks it needs.
   Its argument goes nowhere else; a modifier it reads goes into `DevModes.All`.
3. An `Order` only where it must win over, or yield to, another scenario's map, world, script or start.
4. Its row in the Files table above.
5. A frame of it: `tools/snap.ps1 out.png -- --name`. `DevModesTests` checks the argument; the CI build checks it
   is gone from a release.
