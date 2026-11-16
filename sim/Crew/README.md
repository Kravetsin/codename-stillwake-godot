# Crew

## Files

| File | Purpose |
| --- | --- |
| `Crew.cs` | The module, commands, queries. |
| `Fuzz.cs` | Commands drawn. |
| `Harm.cs` | `Harm`, what hurt. |
| `Signal.cs` | `Signal`, a tracker's. |
| `State.cs` | `Hero`, `CrewState`. |

Heroes: where each is (as its client says), how it breathes, passing out.

- **Depends on:** `Atmosphere` (`Breathe`, `AiredAt`); `Grid` (`Width`, `Height`, `Zone`, `WallAt`, `BackAt`); `Items` (`Equipped`, `WornIn`,
  `ShortOf`, `Creative`; ops `Take`, `Drain`, `StopRunning`, `LeaveBody`, `Kit`); `Players` (`Worn`, `IPlayersOps`); `Sky` (`Now`).
- **State** (`CrewState`, all saved): `Spawn` — the first lifepod's hatch (`Content.Crew.PodHatch`), else the
  first `S`, else cell 0; `Pod` — its top-left or null (`Fixed`); `Clock`; `Heroes` — by peer id, in id
  order (fields on `Hero`).

## Rules

- A hero is named by its peer, the `From` the host stamps.
- **Air** (air-segments): `Hero.Air`, "the suit", in parts (`PartsOf`): lungs, the helmet's `Tank`, food's lungs;
  spent last first, a part gone taking its fill (`Shape`). Recut first each tick, awake.
- **Enter and report**: `Enter(Cell)` puts the sender's hero at `Cell`, making it full (bare, empty on a peer's
  first) if none. Sent on a new room or air side, every `Crew.ReportEvery` ticks while the cells differ,
  `Crew.ReportThreatened` while threatened. Passed out, a client sends none.
- **The breath**: every `Crew.BreathEvery` ticks, in id order, each conscious hero breathes: by the
  pod (`ByPod`) up to `Crew.PodRefill` of its endless air, then `Breathe` the rest of a unit and `FillOf(capacity)` for
  the suit, which pays any shortfall.
- **Thin air** (0076): with `ThinAir` n the suit pays the shortfall every n-th breath, in full, exertion too; off the
  beat `Owed` moves only where air covers the breath.
- **Colours, looks, skins** (looks-are-printed, hero-skins), kept by `Players`: a hero coming in wears the first unworn of
  `Crew.Colours`, else the least worn; a peer's first gets the kit (`IItemsOps.Kit`). `PickColour` takes a free
  colour; `ChooseLook` sets the look the next print wears; `ChooseSkins` the skins, worn at once.
- **Leave** (host, a peer gone): the hero goes, colour freed, tool stopped, inventory kept.
- **Surfaces** (`ICarries`): `Depart` gives up the `Hero`, `Arrive` takes it at a cell or the spawn. One waking
  off the ship wakes aboard.
- **Exertion:** `Exert(On)`, sent running or on a wheel: a breath costs `Crew.Exerted`.
- **The jetpack**: `Thrust(On)`, sent as the client's thrust changes; an awake hero thrusting pays a unit every
  `Jetpack.TicksPerUnit` ticks, before the breath. Off with the jetpack off, on a planet or out (one landing, then cleared).
- **The suit's mode** (regen-suit): `SuitMode(On)` sets `Hero.SuitOn` (a body with `Switch`); on, the regen suit gives back a
  breath, after the thin-air clamp, up to `Suit.RegenMost`, `Suit.RegenCost` power-ticks a unit (`Drain`, after any tool).
- **A canister** (food-and-the-body): `Refill` takes one (none in creative), adds `RefillOf(peer)` up to capacity,
  sets `CanisterIn` to `Crew.CanisterCooldown` ticks (not in creative), counted down.
- **Meals** (food-and-the-body): `Hero.Meals`, up to `Diet.Meals`, ticks `Left` counted down. `Eat(Item)` adds one or,
  in its last `Diet.RefreshWithin`, refreshes it; a print holds none, hungry (hungry-respawn). A fruit eaten rolls its `Byproducts`,
  given or dropped. `RatesOf` sums meals to `Diet.Caps`: calm cuts a breath, deep units to a canister. `Purge` empties them, counting `Purges`. Thousandths
  go to `Owed`, whole units paid; hungry, a sprint costs `Diet.HungrySprint`. `Works` (Building's `Work`) adds
  `Diet.Work` and food's, hungry `Diet.HungryWork`, to `Worked`, returning whole ticks.
- **The last breath** (the-last-breath): air run dry starts `Hero.Gasp` at `Crew.LastBreath`, counted
  down; air back (the regen suit's too) ends it, a covered breath only with nothing `Felt`. Lava, shocks and blows
  past the air cut `Crew.LastBreathPerUnit` a unit; cold, running, thrust nothing.
- **Passing out is death**: a spent last breath passes the hero out for `Crew.PassOut` ticks (tool stopped),
  then is made at the spawn, full, dressed, hungry; `LeaveBody(peer, cell)` leaves all but gear at the reported cell. It
  clears `CanisterIn`, meals, `Owed`, `Worked`.
- **Cold and heat** (temperature-gradients, heat-limits): `TemperatureAt`: `Temperature.Space` off the grid, `Room` aired;
  afloat `Hull` in the zone, to `Temperatures[0]` at the field's line; on a planet `Terrain.TemperatureOf`, open sky (band 0, no `BackAt`)
  `Chill` colder. `PaceAt`: a storm's `Pace` there, unaired. After the regen suit, each degree `Felt` under `LowOf` or `Heat` over `HighOf`
  (the body's or `Bare*`, `Cape.Warmth` lower) costs `Bite`‰ a breath, whole by `Clock`. Then
  `EdgeCost(cell)`: 0 in the zone grown by `Field.Margin`, past it depth² to `Field.EdgeCost`; `OffGrid`.
- **Lava burns**: lava at the reported cell or `Lava.Reach` rows under it (`Burns`) costs the suit alone
  `Lava.Burn` more a breath.
- **A shock** (`Shock`) and **a blow** (`Blow`, from Creatures) take units from a
  conscious hero's air at once, the rest from its last breath; none in creative. They and lava are hurts
  (hurts-are-shown): `Hero.Hurts` counts them, `Hero.Hurt` holds the last's `Harm`.
- **The cape** (the-cape-is-worn) spares `Cape.Guard`‰ of a blow, the rest in `Hero.Spared`; a blow
  lands before the breath.
- **The wanted meter** (catcher-loot): `Loot(peer, yield)` adds `Market.WorthOf` to an awake, not creative hero's
  `Wanted` up to `Need` (drawn at 0); afloat, not full, it drains `Wanted.Drain` a breath in `Wanted.Belt`. Filled,
  `Coming` counts `Wanted.ComingLeast..Most` breaths down. `SignalOf`: `Calm`, `Coming`, `Hunted` (the catcher
  comes). `CallOff` empties it (passing out, departing, a hunt's end); `Track` (dev).
- **Creative mode** (`Items`): an awake hero's air fills each tick.
- **Order:** after `Atmosphere` and `Sky`, in simulation.

## API

- **Queries** (`ICrewQueries`): `Spawn`; `ByPod(cell)`; `EdgeCost(cell)`; `CapacityOf`, `PartsOf(peer)`; `RefillOf`, `RatesOf`, `Hungry(peer)`; `TemperatureAt`, `PaceAt`, `LowOf`, `Felt` (frost), `HighOf`, `Heat`;
  `SuitOn`, `SuitModeProblem`; `SignalOf(peer)`, `Hunted(peer)`;
  `HeroOf(id)` or null; `Heroes`, live, in id order. On the
  module: `Burns(cell)`, `RefillProblem(peer)`, `EatProblem(peer, food)`, `PurgeProblem`: the client sends none they refuse.
- **Commands:** `Enter(Cell)`, `ChooseLook(Look)`, `ChooseSkins(Ids)`, never refused; `PickColour(Colour)` —
  `crew.noHero`, `crew.noSuchColour`, `crew.colourTaken`; `Leave`, `Exert(On)` — `crew.noHero`; `Thrust(On)` —
  `crew.noHero`, `crew.noJetpack`; `Refill` — `crew.noHero` (or out), `crew.suitFull`, `crew.canisterCooling`, `items.notEnough`; `Eat(Item)` — `crew.noHero` (or out), `crew.notFood`,
  `items.notEnough`, `crew.sameFood`, `crew.mealsFull`; `Purge` — `crew.noHero` (or out), `crew.noMeals`; `SuitMode(On)` —
  `crew.noHero`, on `crew.noSuitMode`; `Track` — `crew.noHero`. **Ops** (`ICrewOps`) add `Loot(peer, yield)`, `CallOff(peer)`, `Works(peer)`.
- **Fuzz:** every command and refusal; `Enter` anywhere, off the grid too.

## Invariants

- Air holds between nothing and capacity, a last breath runs only with none, awake (`SuitsTests`, `FieldTests`).
- `EdgeCost` is 0 inside the field, never less farther out, never over `Field.EdgeCost` (`FieldTests`).
- A seat's hero is on its surface (`World.SurfaceOf`) and no other (`SurfacesTests`).
- A hero holds at most `Diet.Meals` meals, all different, none spent; rates within `Diet.Caps` (`MealsTests`).

## Deliberately not handled

- A `Move` or `Drop` landing while out finds the slots emptied: it moves what is left, or is refused.
- The sim trusts the client's cell: cold and lava are paid a second late.
- Peer ids are saved: after a load a hero is whoever connects in its slot's; heroes of peers gone stay.
- A podless spawn in vacuum passes a hero out over and over.
- The pod's air is endless: for heroes by it alone, never the room's; full ones draw no room air.
