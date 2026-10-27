# Effects on their maker's layer

Every spark, ember, arc, ring and trail is drawn at `Layers.Effects` (23), over the whole world: a furnace's embers over
the hero before it, the arc drill's coil arcs over the hand that holds it. After this plan each effect draws on the layer
of what makes it, with a few chosen exceptions over the heroes.

## Rules (the user's)

- **An effect draws on its maker's layer**: "every effect on the same layer as the object that makes it". The maker is
  the thing drawn in the world that the effect comes off: the furnace for its embers, the machine for its arcs, the
  creature for its sparks, the drill in the hand for its coil.
- **We choose the layer per effect.** The default is the maker's; these draw over the heroes, at `Layers.Effects`,
  because the player should always see them:
  - **hits**: an axe, a nail, the drill's bite or a harpoon's bite on a wall, a growth, a creature or a wreck, the ground
    dug (with its cracks, 0077), and the burst of what such a hit breaks or kills;
  - **a blow on a hero**: a creature's ram, the sentry's arc striking (its whole strike), a relic's or resonator's zap;
  - **venting air** (`AirView`, `VentLight`).
- **What a hero holds** draws over its torso and the tool, under its front arm: the arc drill's coil (all of
  `DrillLight`) and the harpoon's muzzle glint and gas. Every hero's figure alike.
- **Unchanged:** how effects look (0075, arcs-are-hairlines), the light they give the world, the sim.

## Design

- **A stage** (`render/logic/Stage.cs`): its own `Glims`, `Flares`, `Bolts` and `HitBurst`, each with today's caps, and
  one `EffectsView` drawing them. **A world stage is keyed by z** (`Stages.Of(Layers.Fire)`): names sharing a z share a
  stage (`Lava`, `Flora`, `Fixtures`; `Fire`, `Shimmer`; `Creatures`, `FloorItems`). Its view is made on first use with
  that z, absolute.
- **Where world stages live.** One container node, `Stages.Root`, added by `Main` to the world; its stage views are its
  children. `ShowWorld` moves the container to the end of its parent after re-adding the feature views (every enter,
  surface change and load), so at a shared z a stage draws over that layer's objects. `Stages.Clear()` empties every
  pool (the clears in `Main.cs` and `Main.Sessions.cs`); `Stages.Reset()` drops every stage and figure entry, called as
  `Main` leaves the tree (exit to title reloads the scene).
- **`Stages.All`**: world stages in z order, then figure stages in the order they were made. **`LightView` merges them**:
  every stage's flashes, then every stage's arcs, then every stage's particles, under its one `Fx.MostLights` cap, as
  today's order across one pool.
- **`Glims.Shared`, `Flares.Shared`, `Bolts.Shared`, `HitBurst.Shared` and `EffectsView()` are removed**, so the
  compiler lists every emitter. `EffectsView.Count` sums `Stages.All`. `EffectsView` keeps its passes and its
  `_lowMatter` child at the absolute `LowDust` z, and skips drawing while its pools are empty. `ToolPicture` keeps its
  own pools and view, untouched.
- **A figure's held stage** (`Figure.Held`): made on first use, not keyed in `Stages.Of`; its view a child between
  `_held` and `frontArm` at relative z 0 (so the figure's own z), its global transform set to identity each frame so it
  draws in world pixels as the figure turns and flips; floor dust it throws keeps `LowDust`. In `Stages.All` while the
  figure is in the tree, out in its `_ExitTree`. A body, a seated hero or the title's figures never make one unless
  they throw a held effect. `DrillLight` takes it from its figure; `RodLight.Fired(stage, …)` takes the shooter's,
  which both hero views reach through the figure they draw.
- **Caps are per stage.** About 12 world stages and one per figure can hold far more than today's 1600 particles; in
  play only a few are busy at once. Accepted, checked with `--fx-bench` against the frame budget.

| Emitter | Stage |
| --- | --- |
| `FurnaceView` | `Fire` |
| `SmelterHeat`, `BenchView`, `ArcCrackle`, `PodView`, `LampSparks`, `light/Crackle` | `Machines` |
| `DoorLight` | `Doors` |
| `DebrisView` (drift, burner): wrecks drift behind the hull, their effects with them | `Debris` |
| `CreaturesView` (but `Die`), `TickBodies` own moments (`Sparks` for its `Cut`) | `Creatures` |
| `FloraView`, `CaveView`, `HomesView` (but `Felled` and its zap), `SpiresView`, `RelicSparks` (but `Zap`) | `Flora` |
| `LavaView` | `Lava` (= `Flora`) |
| `DroneLight` | `Drones` |
| `JetPlume`; `RodLight` flight, bead, zip home | `Tethers` |
| `Figure` boot dust, landing ring, glove sparks; `AirSwirl` (the hero's breath, round its body) | `Hero` own / `OtherHeroes` |
| `DrillLight`, `RodLight.Fired` | the figure's held stage |
| `BuildGlow`: molten spot, line, band, tip sparks, finish motes and glint, weld smoke, edge motes | `PlanHands` |
| hits: `ShotEnds`, `HeroView.Struck`, `DigView`, `RodLight.Bit`, `TickBodies` blows (`Sparks` for a blow), `BreakView`, `HomesView.Felled`, `CreaturesView.Die` | `Effects` |
| a blow on a hero: `TickBodies` hero hit, `ShotsView` an arc's strike, `RelicSparks.Zap`, the resonator's zap | `Effects` |
| `AirView`, `VentLight`; `PickUpFlights` (it flies to the HUD) | `Effects` |
| `FxBench` | `Effects`: it measures the cost of one full stage |

## Deliberately not handled

- **A particle keeps its stage for life**: an ember rising past a hero's helmet stays behind it.
- **No splits inside one object** but the figure's: a helix's back half, a creature's far legs. The screwdriver's tip
  sparks stay on `PlanHands`, over the hero: `BuildGlow` cannot reach the figure without one feature reaching into another.
- **Order between objects sharing a z** is tree order, as today; two creatures' sparks draw over both.
- **Effects under `Shimmer` (10)** are copied with the screen and wobble with the heat, as their machines do.

## Tasks

- **X-1** Stages: `Stage`, `Stages` (`Of`, `All`, `Clear`, `Reset`, `Root`), the `Shared` pools removed, every emitter on
  its stage per the table (`TickBodies.Sparks` and `RelicSparks.Zap` take a stage), `LightView` merged, `ShowWorld`
  moving `Root` last, the clears, `EffectsView.Count`, dev counts. Tests (`LuminousTests`): a stage's pools are its own;
  `Stages.Of` gives one stage per z; `Clear` empties every stage; `Reset` drops them; `All` is in z order. Frames:
  `--testbed` with a hero before the furnace (embers behind it), after a surface change too; an axe on a wall (sparks
  over the hero); `--fx-bench` within the budget.
- **X-2** The figure's held stage: `Figure.Held`, `DrillLight`, `RodLight.Fired`; a figure's stage leaves `All` with the
  figure (test). Frames: an arc drill crackling at zoom 3, the front arm over its arcs, own hero and a friend's (`--join`).
- **Done when:** suite and format green; `game/render/README.md`, `game/features/effects/README.md` current; an ADR
  (effects-on-their-layer) amending 0075's "over everything"; this plan deleted.
