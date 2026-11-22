# Crew

## Files

| File | Purpose |
| --- | --- |
| `Crew.cs` | The module. |
| `Fuzz.cs` | Commands drawn. |
| `Harm.cs` | `Harm`, what hurt. |
| `Signal.cs` | `Signal`, a tracker's. |
| `State.cs` | `Hero`, `CrewState`. |

Heroes: where (as each client says), breathing, passing out.

- **Depends on:** `Atmosphere` (`Breathe`, `AiredAt`); `Grid` (`Width`, `Height`, `Zone`, `WallAt`, `BackAt`, `Solid`, the core, op `Clear`); `Items` (`Equipped`, `WornIn`,
  `ShortOf`, `Creative`; ops `Take`, `Drain`, `StopRunning`, `LeaveBody`, `Kit`); `Players` (`Worn`, `HasDrone`, ops); `Sky` (`Now`).
- **State** (`CrewState`, all saved): `Spawn` — the first lifepod's hatch (`Content.Crew.PodHatch`), else the
  first `S`, else cell 0; `Pod` — its top-left or null (`Fixed`); `Clock`; `Heroes` — by peer id, in id
  order.

## Rules

- **Air** (air-segments): `Hero.Air`, "the suit", in parts (`PartsOf`): lungs, the helmet's `Tank`, food's lungs;
  spent last first, a part gone taking its fill (`Shape`). Recut first each tick, awake.
- **Enter and report**: `Enter(Cell)` puts the sender's hero at `Cell`, making it full (bare, empty on a peer's
  first) if none. Sent on a new room or air side, every `Crew.ReportEvery` ticks while cells differ (`ReportThreatened` threatened).
- **The breath**: every `Crew.BreathEvery` ticks, in id order, each awake hero breathes: by the
  pod (`ByPod`) up to `Crew.PodRefill` of its endless air, then `Breathe` the rest of a unit and `FillOf(capacity)` for
  the suit, which pays any shortfall.
- **Thin air** (0076): `ThinAir` n: the suit pays the shortfall every n-th breath in full, exertion too; off the
  beat `Owed` moves only where air covers it.
- **Colours, looks, skins** (looks-are-printed, hero-skins), kept by `Players`: a hero coming in wears the first unworn of
  `Crew.Colours`, else the least worn; a peer's first gets the kit (`IItemsOps.Kit`). `PickColour` a free
  colour; `ChooseLook` the next print's look; `ChooseSkins` skins, worn at once.
- **Leave** (host, a peer gone): the hero goes, colour freed, tool stopped, inventory kept.
- **Surfaces** (`ICarries`): `Depart` gives up the `Hero`, `Arrive` takes it at a cell or the spawn.
- **Exertion:** `Exert(On)`, sent running or on a wheel: a breath costs `Crew.Exerted`.
- **The jetpack**: `Thrust(On)`, sent as thrust changes; an awake hero thrusting pays a unit every
  `Jetpack.TicksPerUnit` ticks, before the breath. Off with the jetpack off, on a planet or out (one landing, then cleared).
- **The suit's mode** (regen-suit): `SuitMode(On)` sets `Hero.SuitOn` (a body with `Switch`); on, the regen suit gives back a
  breath, after the thin-air clamp, up to `Suit.RegenMost`, `Suit.RegenCost` power-ticks a unit (`Drain`, after any tool).
- **A canister** (food-and-the-body): `Refill` takes one (none in creative), adds `RefillOf(peer)` up to capacity,
  sets `CanisterIn` to `Crew.CanisterCooldown` (not in creative), counted down.
- **The drone** (drone-companion): `PairDrone(Core)` `Pair`s for good; `Meets` spares it `Crew.MeetingTicks`: no
  breath, meals, thrust, harm, `Succumb` (drone-meeting); `Envision` sets `Crew.VisionTicks`, if more (form-vision). `CoreLost` (a floor cell under the core gone) clears it for good, pairs all.
- **Meals** (food-and-the-body): `Hero.Meals`, up to `Diet.Meals`, ticks `Left` counted down. `Eat(Item)` adds one or,
  in its last `Diet.RefreshWithin`, refreshes it; a print holds none, hungry (hungry-respawn). A fruit eaten rolls its `Byproducts`,
  given or dropped. `RatesOf` sums meals to `Diet.Caps`: calm cuts a breath, deep units to a canister. `Purge` empties them, counting `Purges`. Thousandths
  go to `Owed`, whole units paid; hungry, a sprint costs `Diet.HungrySprint`. `Works` (Building's `Work`) adds
  `Diet.Work` and food's, hungry `Diet.HungryWork`, to `Worked`, returning whole ticks.
- **The last breath** (the-last-breath): air run dry starts `Hero.Gasp` at `Crew.LastBreath`, counted down; air back (regen too) ends it, a covered breath only with no grit or lava. Lava, shocks and blows past the air cut `Crew.LastBreathPerUnit` a unit; running, thrust nothing.
- **Passing out is death**: a spent last breath puts it out `Crew.PassOut` ticks (tool stopped), then it is made at the spawn, full, dressed, hungry; `LeaveBody` leaves all but gear at its cell. It clears the air, `CanisterIn`, meals, `Owed`, `Worked`.
- **Degrees** (temperature-gradients, body-temperature): `TemperatureAt`: `Temperature.Deep` off the grid, `Room` aired; afloat
  `Hull` in the zone, `Temperatures[0]` at the field's line, `Deep` at the grid's edge; on a planet `Terrain.TemperatureOf`, open
  sky (band 0, no `BackAt`) `Chill` colder. `Felt` under `LowOf`, `Heat` over `HighOf` (`Bare*` + helmet's + suit's, gear-roles; `Cape.Warmth`
  lower) cost no air: they move the client's body, whose death is `Succumb`. `PaceAt`, `GritAt`: a storm's
  `Pace`, `Grit` there, unaired; grit paid from the suit after the regen suit (mild-surface).
- **Lava burns**: lava at the cell or `Lava.Reach` rows under (`Burns`) costs the suit `Lava.Burn` more a breath.
- **A shock** (`Shock`) and **a blow** (`Blow`, Creatures') take units from an awake hero's air, then its
  last breath; none in creative. They and lava are hurts (hurts-are-shown): `Hero.Hurts` counts them, `Hero.Hurt` holds the last's `Harm`.
- **The cape** (the-cape-is-worn) spares `Cape.Guard`‰ of a blow, the rest in `Hero.Spared`; a blow
  lands before the breath.
- **The wanted meter** (catcher-loot): `Loot(peer, yield)` adds `Market.WorthOf` to an awake, not creative hero's
  `Wanted` up to `Need` (drawn at 0); afloat, not full, it drains `Wanted.Drain` a breath in `Wanted.Belt`. Filled,
  `Coming` counts `Wanted.ComingLeast..Most` breaths down. `SignalOf`: `Calm`, `Coming`, `Hunted`. `CallOff` empties it (out,
  departing, a hunt's end); `Track` (dev).
- **Creative mode** (`Items`): an awake hero's air fills each tick.
- **Order:** after `Atmosphere`, `Sky`, in simulation.

## API

- **Queries** (`ICrewQueries`): `Spawn`; `ByPod(cell)`; `CapacityOf`, `PartsOf(peer)`; `RefillOf`, `RatesOf`, `Hungry(peer)`; `TemperatureAt`, `PaceAt`, `LowOf`, `Felt` (frost), `HighOf`, `Heat`, `OpenSky`;
  `SuitOn`, `SuitModeProblem`, `SuccumbProblem`; `SignalOf(peer)`, `Hunted(peer)`, `Spared(peer)`;
  `HeroOf(id)` or null; `Heroes`, live, in id order. On the module: `Burns`, `GritAt`, `RefillProblem`, `EatProblem`, `PurgeProblem`, `PairProblem`, `EnvisionProblem`, `CoreLost`: the client sends none they refuse.
- **Commands:** `Enter(Cell)`, `ChooseLook(Look)`, `ChooseSkins(Ids)`, never refused; `PickColour(Colour)` —
  `crew.noHero`, `crew.noSuchColour`, `crew.colourTaken`; `Leave`, `Exert(On)` — `crew.noHero`; `Thrust(On)` —
  `crew.noHero`, `crew.noJetpack`; `Refill` — `crew.noHero` (or out), `crew.suitFull`, `crew.canisterCooling`, `items.notEnough`; `Eat(Item)` — `crew.noHero` (or out), `crew.notFood`,
  `items.notEnough`, `crew.sameFood`, `crew.mealsFull`; `Purge` — `crew.noHero` (or out), `crew.noMeals`; `PairDrone(Core)` — `crew.noHero` (or out), `crew.hasDrone`, `crew.noCore`; `Envision` — `crew.noHero` (or out); `Succumb` — `crew.noHero` (or out, creative, `Meets`); `SuitMode(On)` —
  `crew.noHero`, on `crew.noSuitMode`; `Track` — `crew.noHero`. **Ops** (`ICrewOps`) add `Loot(peer, yield)`, `CallOff(peer)`, `Works(peer)`.
- **Fuzz:** every command, refusal; `Enter` off the grid.

## Invariants

- Air holds between nothing and capacity, a last breath runs only with none, awake (`SuitsTests`, `FieldTests`).
- A seat's hero is on its surface (`World.SurfaceOf`) and no other (`SurfacesTests`).
- A hero holds at most `Diet.Meals` meals, all different, none spent; rates within `Diet.Caps` (`MealsTests`).

## Deliberately not handled

- A `Move` or `Drop` landing while out finds the slots emptied: it moves what is left, or is refused.
- The sim trusts the client's cell (grit, lava a second late) and its body: no check of a `Succumb` or slowness.
- Peer ids are saved: after a load a hero is whoever joins in its slot; heroes of peers gone stay.
- A podless spawn in vacuum passes a hero out over and over.
