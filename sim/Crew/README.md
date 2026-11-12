# Crew

## Files

| File | Purpose |
| --- | --- |
| `Crew.cs` | The module, its commands, `ICrewQueries`, `ICrewOps`. |
| `Fuzz.cs` | The commands the desync suite draws. |
| `Harm.cs` | What hurt a hero: `Harm`. |
| `Signal.cs` | What a hero's tracker shows: `Signal`. |
| `State.cs` | The saved slice and its records: `Hero`, `CrewState`. |

Heroes in the sim: where each is, as its client says, and how it breathes (0028); passing out, made again at the pod.

- **Depends on:** `Atmosphere` (`Breathe`, `AiredAt`); `Grid` (`Width`, `Height`, `Zone`, `WallAt`, `BackAt`); `Items` (`Equipped`, `WornIn`,
  `ShortOf`, `Creative`; ops `Take`, `Drain`, `StopRunning`, `LeaveBody`, `Kit`); `Players` (`Worn`, `IPlayersOps`); `Sky` (`Now`).
- **State** (`CrewState`, all saved): `Spawn` — the first lifepod's hatch (`Content.Crew.PodHatch`), else the
  first `S`, else cell 0; `Pod` — its top-left or null (`Fixed`); `Clock`; `Heroes` — by peer id, in id
  order (fields told on `Hero`).

## Rules

- A hero is named by its peer, the `From` the host stamps.
- **Air** (suits-are-worn): `Hero.Air`, below "the suit"; capacity `CapacityOf`: the worn helmet's `Tank`,
  or `Crew.Lungs`. Each tick, first, a conscious hero's air is cut to it. Every hero made starts full.
- **Enter and report** (0050): `Enter(Cell)` puts the sender's hero at `Cell`, making it full (bare, empty on a peer's
  first) if none. Sent on a new room or air side, every `Crew.ReportEvery` ticks while the cells differ,
  `Crew.ReportThreatened` while threatened. Passed out, a client sends none.
- **The breath** (0028): every `Crew.BreathEvery` ticks, in id order, each conscious hero breathes: by the
  pod (`ByPod`) up to `Crew.PodRefill` of its endless air, then `Breathe` the rest of a unit and up to `Crew.Refill` for
  the suit; the suit pays any shortfall.
- **Thin air** (0076): with `ThinAir` n the suit pays the shortfall every n-th breath, in full, exertion too; off the
  beat `Owed` moves only where air covers the breath.
- **Colours and looks** (0076, looks-are-printed), kept by `Players`: a hero coming in wears the first unworn of
  `Crew.Colours`, else the first least worn; a peer's first gets the kit (`IItemsOps.Kit`). `PickColour` takes a free
  colour; `ChooseLook` sets the look the next print wears.
- **Leave** (0055), by the host for a peer gone: the hero goes, colour freed, tool stopped, inventory kept.
- **Surfaces** (0076, `ICarries`): `Depart` gives up the `Hero`, `Arrive` takes it at a cell or the spawn. One waking
  off the ship wakes aboard.
- **Exertion:** `Exert(On)`, sent as the hero runs (0047) or holds a move key in a wheel: a breath costs
  `Crew.Exerted` units, room air or suit alike, till passing out.
- **The jetpack** (0047): `Thrust(On)`, sent as the client's thrust changes; an awake hero thrusting pays a unit every
  `Jetpack.TicksPerUnit` ticks, before the breath. It stops with the jetpack off, on a planet or out (one landing, then
  cleared); the client flies.
- **The suit's mode** (regen-suit): `SuitMode(On)` sets `Hero.SuitOn` (a body with `Switch`); on, the regen suit gives back what a
  breath took, after the thin-air clamp, up to `Suit.RegenMost`, `Suit.RegenCost` power-ticks a unit (`Drain`, after any tool).
- **A canister** (food-and-the-body): `Refill` takes one (none in creative), adds `RefillOf(peer)` up to capacity,
  sets `CanisterIn` to `Crew.CanisterCooldown` ticks (not in creative), counted down.
- **Meals** (food-and-the-body): `Hero.Meals`, up to `Diet.Meals`, ticks `Left` counted down. `Eat(Item)` adds one or,
  in its last `Diet.RefreshWithin`, refreshes it; a print holds paste. `RatesOf` sums them to `Diet.Caps`: calm cuts a breath, deep adds to a canister. A breath's thousandths go to `Owed`, whole
  units paid; hungry (no meal, not creative), a sprint costs `Diet.HungrySprint`.
- **The last breath** (the-last-breath): air run dry starts `Hero.Gasp` at `Crew.LastBreath`, counted
  down; air back (the regen suit's too) ends it. Lava, shocks and blows past the air cut
  `Crew.LastBreathPerUnit` a unit; cold, running, thrust nothing.
- **Passing out is death** (0061): a spent last breath passes the hero out for `Crew.PassOut` ticks (tool stopped),
  then is made at the spawn, full, dressed; `LeaveBody(peer, cell)` leaves all but gear at the reported cell. It
  clears `CanisterIn`, meals and `Owed`.
- **The cold past the field** (0050): `EdgeCost(cell)` 0 in the zone grown by `Field.Margin` or a vessel's box,
  past it the deeper axis's depth squared up to `Field.EdgeCost`, `Field.OffGrid` off the grid (rounded up); paid each
  breath after the regen suit, none on a planet. Bare where `AiredAt` is false, `Crew.BareCold` more on thin air's beat;
  at night with no `BackAt`, `Chill` more on its own beat (night-chills).
- **Lava burns** (0087): lava at the reported cell or `Lava.Reach` rows under it (`Burns`) costs the suit alone
  `Lava.Burn` more a breath.
- **A shock** (0091, `Shock(peer, units, harm)`) and **a blow** (0103, `Blow` from `Creatures`) take units from a
  conscious hero's air at once, the rest from its last breath; none in creative. They and lava are hurts
  (hurts-are-shown): `Hero.Hurts` counts them, `Hero.Hurt` holds the last's `Harm`.
- **The cape** (the-cape-is-worn) spares `Cape.Guard`‰ of a blow, `Cape.Warmth`‰ of the cold (not the
  breath or lava), rounded down, the rest in `Hero.Spared` for either; a blow lands before the breath.
- **The wanted meter** (catcher-loot): `Loot(peer, yield)` adds `Market.WorthOf` to an awake, not creative hero's
  `Wanted` up to `Need` (drawn at 0); afloat, not full, it drains `Wanted.Drain` a breath in `Wanted.Belt`. Filled,
  `Coming` counts `Wanted.ComingLeast..Most` breaths down. `SignalOf`: `Calm`, `Coming`, `Hunted` (the catcher
  comes). `CallOff` empties it (passing out, departing, a hunt's end); `Track(Short)` is dev's.
- **Creative mode** (`Items`): an awake hero's air fills each tick, from nothing.
- **Order:** breaths run after `Atmosphere`'s and `Sky`'s, in simulation.

## API

- **Queries** (`ICrewQueries`): `Spawn`; `ByPod(cell)`; `EdgeCost(cell)`; `CapacityOf(peer)`; `RefillOf`, `RatesOf`, `Hungry(peer)`; `Cold`, `Warmth` (frost);
  `SuitOn`, `SuitModeProblem`; `SignalOf(peer)`, `Hunted(peer)`;
  `HeroOf(id)` — `Hero` or null; `Heroes` — live, read-only, in id order. On the
  module: `Burns(cell)`, `RefillProblem(peer)`, `EatProblem(peer, food)`: the client sends nothing they refuse.
- **Commands:** `Enter(Cell)`, `ChooseLook(Look)`, never refused; `PickColour(Colour)` —
  `crew.noHero`, `crew.noSuchColour`, `crew.colourTaken`; `Leave`, `Exert(On)` — `crew.noHero`; `Thrust(On)` —
  `crew.noHero`, `crew.noJetpack`; `Refill` — `crew.noHero` (or out), `crew.suitFull`, `crew.canisterCooling`, `items.notEnough`; `Eat(Item)` — `crew.noHero` (or out), `crew.notFood`,
  `items.notEnough`, `crew.sameFood`, `crew.mealsFull`; `SuitMode(On)` —
  `crew.noHero`, on `crew.noSuitMode`; `Track` — `crew.noHero`. **Ops** (`ICrewOps`) add `Loot(peer, yield)`, `CallOff(peer)`.
- **Fuzz:** every command and refusal; `Enter` near the spawn, anywhere, or off the grid.

## Invariants

- Air holds between nothing and capacity, a last breath runs only with none, awake (`SuitsTests`, `FieldTests`).
- `EdgeCost` is 0 inside the field or a vessel's box, never less farther out, never over `Field.EdgeCost` (`FieldTests`).
- No two heroes wear a colour while there are enough, on any surface.
- A seat's hero is on its surface (`World.SurfaceOf`) and no other (`SurfacesTests`).
- A hero holds at most `Diet.Meals` meals, all different, none spent; rates within `Diet.Caps` (`MealsTests`).

## Deliberately not handled

- A `Move` or `Drop` landing while out finds the slots emptied: it moves what is left, or is refused.
- The sim trusts the client's cell (0002): cold, chill and lava are paid a second late.
- Peer ids are saved: after a load a hero is whoever connects in its slot's; heroes of peers gone stay.
- A spawn in vacuum with no pod passes a hero out over again.
- The pod's air is endless: only for heroes, only by it, slowly, never to the room; full ones draw no room air.
