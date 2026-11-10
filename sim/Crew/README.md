# Crew

## Files

| File | Purpose |
| --- | --- |
| `Crew.cs` | The module, its commands, `ICrewQueries`, `ICrewOps`. |
| `Fuzz.cs` | The commands the desync suite draws. |
| `Harm.cs` | What hurt a hero: `Harm`. |
| `Signal.cs` | What a hero's tracker shows: `Signal`. |
| `State.cs` | The saved slice and its records: `Hero`, `CrewState`. |

Heroes in the sim: where each is, as its client says, and how it breathes: the pod's air by it (0028),
a room's, its own, then its last breath; passing out, made again at the pod.

- **Depends on:** `Atmosphere` (`Breathe`, `AiredAt`); `Grid` (`Width`, `Height`, `Zone`, `WallAt`, `BackAt`); `Items` (`Equipped`, `WornIn`,
  `ShortOf`, `Creative`; ops `Take`, `Drain`, `StopRunning`, `LeaveBody`, `Kit`); `Players` (`Worn`, `IPlayersOps`); `Knowledge` (`Granted`); `Sky` (`Now`).
- **State** (`CrewState`, all saved): `Spawn` — the first lifepod's hatch (`Content.Crew.PodHatch`), else the
  first `S`, else cell 0; `Pod` — its top-left or null (`Fixed`); `Clock`; `Heroes` — by peer id, in id
  order (`Hero`, fields told on it).

## Rules

- A hero is named by its peer, the `From` the host stamps.
- **Air** (2026-10-27-suits-are-worn): `Hero.Air`, below "the suit"; capacity `CapacityOf`: the worn helmet's `Tank`,
  or `Crew.Lungs`. Each tick, first, a conscious hero's air is cut to it. Every hero made starts full to it.
- **Enter and report** (0050): `Enter(Cell)` puts the sender's hero at `Cell`, making it full (bare, empty on a peer's
  first) if none. Sent on a new room or air side, every `Crew.ReportEvery` ticks while the cells differ,
  `Crew.ReportThreatened` while threatened (creatures-are-composed). Passed out, a client sends none.
- **The breath** (0011, 0016, 0028): every `Crew.BreathEvery` ticks, in id order, each conscious hero breathes: by the
  pod (`ByPod`) up to `Crew.PodRefill` of its endless air, then `Breathe` the rest of a unit and up to `Crew.Refill` for
  the suit; the suit pays any shortfall.
- **Thin air** (0076): with `ThinAir` n the suit pays the shortfall every n-th breath, in full, exertion too.
- **Colours and looks** (0076, looks-are-printed), kept by `Players`: a hero coming in wears the first unworn of
  `Crew.Colours`, else the least worn, first at a tie; a peer's first gets the kit (`IItemsOps.Kit`, 0107).
  `PickColour` puts on a free colour. `ChooseLook` sets the look a print wears: an `Enter` making a hero, a wake.
- **Leave** (0055, 0076): sent by the host for a peer gone; the hero goes, its colour freed, its tool stopped, its
  inventory kept for its seat, sent aboard.
- **Surfaces** (0076, `ICarries`): `Depart` gives up the `Hero`, `Arrive` takes it at a cell or the spawn. One waking
  off the ship wakes aboard.
- **Exertion:** `Exert(On)`, sent as running by speed changes (no swim, 0047), in a wheel as a move key is held. Then a
  breath costs `Crew.Exerted` units, room air or suit alike. Passing out ends it.
- **The jetpack** (0047): `Thrust(On)` is sent while the client's thrust differs from the sim's. Each tick before
  the breath an awake hero thrusting counts one; every `Jetpack.TicksPerUnit` its suit pays a unit. Thrust stops with the jetpack off, on a planet (0076) or out
  (one landing then cleared); the flight is the client's.
- **The suit's mode** (regen-suit): `SuitMode(On)` sets `Hero.SuitOn`, working in a body with `Switch`. The regen
  suit on, after the thin-air clamp, gives back what a breath took, up to `Suit.RegenMost`, for `Suit.RegenCost`
  power-ticks a unit (`Drain`, after any tool).
- **A canister** (heavy-helmet): `Refill` takes one (`Take`; none in creative) and adds `CanisterRefill` to the
  air, up to capacity: `Crew.CanisterRefill`, `Crew.CanisterUp` more per canister upgrade.
- **The last breath** (2026-11-05-the-last-breath): air run dry starts `Hero.Gasp` at `Crew.LastBreath`, a tick off
  each tick; air back or a breath air made (the regen suit) ends it. Lava, shocks and blows past the air cut
  `Crew.LastBreathPerUnit` a unit; cold, running, thrust nothing.
- **Passing out is death** (0061): a spent last breath passes the hero out for `Crew.PassOut` ticks (its tool stopped),
  then it is made at the spawn, full, dressed; `LeaveBody(peer, cell)` leaves all but gear at the reported cell.
- **The cold past the field** (0050, the zone grown by `Field.Margin`): `EdgeCost(cell)` 0 inside or in a vessel's box,
  past it the deeper axis's depth squared toward `Field.EdgeCost` at the edge, `Field.OffGrid` off it (rounded up). Each breath after the regen suit the suit pays it for the reported
  cell; no zone (a planet, 0076), no cold. With no body, where `AiredAt` is false, it pays
  `Crew.BareCold` more, on thin air's beat; at night with no `BackAt`, `Chill` more on its own beat (night-chills).
- **Lava burns** (0087): `Burns(cell)`: lava at the reported cell or up to `Lava.Reach` rows under it; each breath the suit
  alone pays `Lava.Burn` more.
- **A shock** (0091): `ICrewOps.Shock(peer, units, harm)` takes units from a conscious hero's air at once, the rest from
  its last breath; nothing in creative. **A blow** (0103): `Blow`, from `Creatures` (a ram, a bolt), the same. These and
  lava are hurts (hurts-are-shown): counted in `Hero.Hurts`, the last's `Harm` in `Hero.Hurt`.
- **The cape** (2026-10-27-the-cape-is-worn) spares `Cape.Guard`‰ of a blow, `Cape.Warmth`‰ of the cold (not the
  breath or lava), rounded down, the rest in `Hero.Spared` for either; a blow lands before the breath.
- **The wanted meter** (catcher-loot): `Loot(peer, yield)` adds its `Market.WorthOf` to an awake, not creative hero's `Wanted`,
  up to `Need`, drawn when 0. Afloat, not full, it loses `Wanted.Drain` a breath in `Wanted.Belt` (catchers-space). Filled, `Coming` draws `Wanted.ComingLeast..Most` breaths, counted
  down each breath. `SignalOf`: `Calm`, `Coming` (the alarm), `Hunted` (full, `Coming` 0: Creatures sends its catcher). `CallOff`
  empties it: passing out, departing, a hunt's end. `Track(Short)` (dev) sets it that short.
- **Creative mode** (`Items`): an awake hero's air fills each tick, drawing no pod or room air; one out wakes as ever.
- **Order:** breaths run in the simulation phase after `Atmosphere`'s and `Sky`'s, reading this tick's air.

## API

- **Queries** (`ICrewQueries`): `Spawn`; `ByPod(cell)`; `EdgeCost(cell)`; `CapacityOf(peer)`; `CanisterRefill`; `Cold`, `Warmth` (frost);
  `SuitOn`, `SuitModeProblem`; `SignalOf(peer)`, `Hunted(peer)`;
  `HeroOf(id)` — `Hero` (`Id`, `Cell`, `Air`, `Gasp`, `Out`) or null; `Heroes` — live, read-only, in id order. On the
  module: `Burns(cell)`; `RefillProblem(peer)`, so the client sends no `Refill` the sim would refuse.
- **Commands:** `Enter(Cell)`, `ChooseLook(Look)`, never refused; `PickColour(Colour)` —
  `crew.noHero`, `crew.noSuchColour`, `crew.colourTaken`; `Leave`, `Exert(On)` — `crew.noHero`; `Thrust(On)` —
  `crew.noHero`, `crew.noJetpack`; `Refill` — `crew.noHero`, `crew.suitFull`, `items.notEnough`; `SuitMode(On)` —
  `crew.noHero`, on `crew.noSuitMode`; `Track` — `crew.noHero`. **Ops** (`ICrewOps`) add `Loot(peer, yield)`, `CallOff(peer)`.
- **Fuzz:** every command and refusal; `Enter` near the spawn, anywhere, or off the grid.

## Invariants

- Air holds between nothing and capacity, a last breath runs only with none, awake (`SuitsTests`, `FieldTests`).
- `EdgeCost` is 0 inside the field or a vessel's box, never less farther out, never over `Field.EdgeCost` (`FieldTests`).
- While heroes are no more than colours, no two wear one, on any surfaces.
- A seat's hero is on its surface (`World.SurfaceOf`) and no other (`SurfacesTests`).

## Deliberately not handled

- A `Move` or `Drop` landing while out finds the slots emptied: it moves what is left, or is refused.
- The sim trusts the client's cell (0002): cold, chill and lava are paid a second late.
- Peer ids last a session but are saved: after a load a hero belongs to whoever connects in its slot. Heroes of peers gone before a save stay.
- A spawn in vacuum with no pod passes a hero out over again.
- The pod's air is endless: only for heroes, only by it, slowly, never to the room; full ones draw no room air.
