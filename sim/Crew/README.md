# Crew

## Files

| File | Purpose |
| --- | --- |
| `Crew.cs` | The module, its commands, `ICrewQueries`, `ICrewOps`. |
| `Fuzz.cs` | The commands the desync suite draws. |
| `State.cs` | The saved slice and its records: `Hero`, `CrewState`. |

Heroes inside the sim: where each is, as its client says, and how it breathes: the lifepod's air beside it
(0028), room air in a cell holding it, its own air elsewhere, then its last breath;
passing out, made again at the pod.

- **Depends on:** `Atmosphere` (`Breathe`, `AiredAt`); `Grid` (`Width`, `Height`, `Zone`, `WallAt`, `BackAt`); `Items` (`Equipped`, `WornIn`,
  `ShortOf`, `Creative`; ops `Take`, `Drain`, `StopRunning`, `LeaveBody`, `Kit`); `Players` (`Worn`, `IPlayersOps`); `Knowledge` (`Granted`); `Sky` (`Now`).
- **State** (`CrewState`, all saved): `Spawn` — the first lifepod's hatch (`Content.Crew.PodHatch`), else the
  first `S`, else cell 0; `Pod` — that pod's top-left, or null (`Fixed`); `Clock`; `Heroes` — by peer id, in id
  order (`Hero`, each field told on it).

## Rules

- A hero is named by its peer, the `From` the host stamps.
- **Air** (2026-10-27-suits-are-worn): `Hero.Air`, below "the suit"; capacity `CapacityOf`: the worn helmet's `Tank`,
  or `Crew.Lungs`. Each tick, first, a conscious hero's air is cut to it. Every hero made starts full to it.
- **Enter and report** (0050): `Enter(Cell)` puts the sender's hero at `Cell`, making it (full; bare on a peer's first,
  its inventory empty) when there is none; later ones never refill it. Sent when the sim lacks the hero or puts it in
  another room or air side (doors rename rooms), and every `Crew.ReportEvery` ticks while the
  cells differ, every `Crew.ReportThreatened` while a creature threatens it (2026-10-26-creatures-are-composed).
  `Hero.Cell` lags up to a second and a round trip. Passed out, a client sends no command.
- **The breath** (0011, 0016, 0028): every `Crew.BreathEvery` ticks, in id order, each conscious hero breathes.
  Within `Crew.PodReach` of the pod (`ByPod`) it first takes up to `Crew.PodRefill` of the pod's own air, from no
  store. Then `Breathe` draws the rest of one unit plus up to `Crew.Refill` for the suit. Air taken pays the
  breath, the rest tops the suit; the suit pays any shortfall.
- **Thin air** (0076): on a kind with `ThinAir` n, the suit pays the shortfall only every n-th
  breath (`Clock / BreathEvery` divisible by n), in full, exertion too.
- **Colours and looks** (0076, looks-are-printed), kept by `Players`: a hero coming in wears the first unworn of
  `Crew.Colours`, else the least worn, first on a tie; a peer's first gets the kit (`IItemsOps.Kit`, 0107).
  `PickColour` puts on a free colour. `ChooseLook` sets the look a print wears: an `Enter` making a hero, a wake.
- **Leave** (0055, 0076): the host sends it for a peer that left (`Host.Disconnect`); the hero goes, its colour
  freed, its tool stopped, its inventory kept for its seat. Off the ship the seat goes aboard (`ISurfaces.Send`).
- **Surfaces** (0076, `ICarries`): `Depart` gives up the `Hero`, `Arrive` takes it at the cell given or
  the spawn. A hero waking off the ship wakes aboard, its body left behind.
- **Exertion:** `Exert(On)`, sent on a change of running by speed (not for a swim, 0047); in a wheel, of a move key held. Then a
  breath costs `Crew.Exerted` units, room air or suit alike. Passing out ends it.
- **The jetpack** (0047): `Thrust(On)` is sent while the client's thrust differs from the sim's. Each tick before
  the breath a conscious hero thrusting with a jetpack counts a tick; every `Jetpack.TicksPerUnit` its suit pays a
  unit, down to nothing. Thrust stops with the jetpack off, on a planet (0076) or out, one
  landing on a hero out cleared; the flight is the client's.
- **Electric tools** (0055): `Items` owns runs; crew stops one (`StopRunning`) while a hero is out.
- **The suit's mode** (regen-suit): `SuitMode(On)` sets `Hero.SuitOn`, working in a body with `Switch`. The regen
  suit on, after the thin-air clamp, gives back what a breath took, up to `Suit.RegenMost`, for `Suit.RegenCost`
  power-ticks a unit (`Drain`, after any tool); unpaid, nothing.
- **A canister** (heavy-helmet): `Refill` takes one (`Take`; none in creative) and adds `CanisterRefill` to the
  air, up to capacity: `Crew.CanisterRefill`, `Crew.CanisterUp` more per canister upgrade.
- **The last breath** (2026-11-05-the-last-breath): air run dry starts `Hero.Gasp` at `Crew.LastBreath`, a tick off
  each tick; air back or a breath air made (the regen suit) ends it. Lava, shocks and blows past the air cut
  `Crew.LastBreathPerUnit` a unit; cold, running, thrust nothing.
- **Passing out is death** (0061): a spent last breath passes the hero out for `Crew.PassOut` ticks, then it is made
  at the spawn, full, in what it wore. That tick `LeaveBody(peer, cell)` leaves all but gear at
  the reported cell (the spawn off the grid). The client moves its hero to the spawn as it wakes.
- **The cold past the field** (0050), the build zone grown by `Field.Margin`: `EdgeCost(cell)` is 0
  inside, past it the square of the depth by the deeper axis toward `Field.EdgeCost` at the grid's edge,
  `Field.OffGrid` off it (rounded up). Each breath after the regen suit the suit alone pays it for the reported
  cell, down to nothing; no zone (a planet, 0076), no cold. With no body, where `AiredAt` is false, it pays
  `Crew.BareCold` more, on thin air's beat; at night with no `BackAt`, `Chill` more on its own beat (night-chills).
- **Lava burns** (0087): `Burns(cell)`: lava at the reported cell or up to `Lava.Reach` rows under it; each breath the suit
  alone pays `Lava.Burn` more.
- **A shock** (0091): `ICrewOps.Shock(peer, units)` takes units from a conscious hero's air at once, the rest from
  its last breath; nothing in creative. **A blow** (0103): `Blow`, from `Creatures` (a ram, a bolt), the same.
- **The cape** (2026-10-27-the-cape-is-worn) spares `Cape.Guard`‰ of a blow, `Cape.Warmth`‰ of the cold (not the
  breath or lava), rounded down, the rest in `Hero.Spared` for either; a blow lands before the breath.
- **Creative mode** (`Items`): a conscious hero's air is full each tick, taking no pod or room air; one out wakes as ever.
- **Order:** breaths run in the simulation phase after `Atmosphere`'s and `Sky`'s, reading this tick's air.

## API

- **Queries** (`ICrewQueries`): `Spawn`; `ByPod(cell)`; `EdgeCost(cell)`; `CapacityOf(peer)`; `CanisterRefill`; `Cold`, `Warmth` (frost);
  `SuitOn`, `SuitModeProblem`;
  `HeroOf(id)` — `Hero` (`Id`, `Cell`, `Air`, `Gasp`, `Out`) or null; `Heroes` — live, read-only, in id order. On the
  module: `Burns(cell)`; `RefillProblem(peer)`, so the client sends no `Refill` the sim would refuse.
- **Commands:** `Enter(Cell)`, `ChooseLook(Look)`, never refused; `PickColour(Colour)` —
  `crew.noHero`, `crew.noSuchColour`, `crew.colourTaken`; `Leave`, `Exert(On)` — `crew.noHero`; `Thrust(On)` —
  `crew.noHero`, `crew.noJetpack`; `Refill` — `crew.noHero`, `crew.suitFull`, `items.notEnough`; `SuitMode(On)` —
  `crew.noHero`, on `crew.noSuitMode`.
- **Fuzz:** every command and refusal; `Enter` near the spawn, anywhere, or off the grid.

## Invariants

- Air holds between nothing and capacity, a last breath runs only with none, awake (`SuitsTests`, `FieldTests`).
- `EdgeCost` is 0 inside the field, never less farther out, never over `Field.EdgeCost` (`FieldTests`).
- While heroes are no more than colours, no two wear one, on any surfaces.
- A seat's hero is on its surface (`World.SurfaceOf`) and no other (`SurfacesTests`).

## Deliberately not handled

- A `Move` or `Drop` landing while out finds the slots emptied: it moves what is left, or is refused.
- The sim trusts the client's cell (0002): cold, chill and lava are paid a second late.
- Peer ids last a session but are saved: after a load a hero belongs to whoever connects in its slot. Heroes of
  peers gone before a save stay, colours and all.
- A spawn in vacuum on a map without a pod passes a hero out over again.
- The pod's air is endless: only for heroes, only by it, slowly, never to the room; full ones draw no room air.
