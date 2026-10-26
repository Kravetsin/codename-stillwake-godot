# Crew

## Files

| File | Purpose |
| --- | --- |
| `Crew.cs` | The module, its commands, `ICrewQueries`, `ICrewOps` and the rules below. |
| `Fuzz.cs` | The module's `Fuzz` override: the commands the desync suite draws. |
| `State.cs` | The saved slice and its records: `Hero`, `CrewState`. |

Heroes inside the sim: where each is, as its client says, and how it breathes: the lifepod's air beside it
([0028](../../docs/decisions/0028-the-lifepod-and-crafting-by-machine-only.md)), room air in a cell holding it, the
suit elsewhere; passing out when the suit runs dry, made again at the pod.

- **Depends on:** `Atmosphere` (`Breathe`); `Grid` (`Width`, `Height`, `Zone`, `WallAt`); `Items` (`Equipped`,
  `ShortOf`, `Creative`; ops `Take`, `StopRunning`, `LeaveBody`, `Kit`); `Players` (`Worn`, `IPlayersOps`).
- **State** (`CrewState`, all saved): `Spawn` — the first lifepod's hatch (`Content.Crew.PodHatch`), else the
  first `S`, else cell 0; `Pod` — that pod's top-left, or null (`Fixed`); `Clock`; `Heroes` — by peer id, in id
  order (`Hero`): cell, suit, ticks out, exerting, thrusting, thrust ticks toward the next unit.

## Rules

- A hero is named by its peer: the sender the host stamps on its commands (`From`) (no ADR).
- **Enter and report** (0050): `Enter(Cell)` puts the sender's hero at `Cell`; the first brings it in with a full
  suit, later ones never refill it. The client sends it when the sim lacks its hero or puts it in another room
  or across the air's edge than its body (compared by cell: doors rename rooms), and as a report every
  `Crew.ReportEvery` ticks while the cells differ. `Hero.Cell` thus lags up to a second and a round trip; the
  breath and anything placed by heroes read it. Passed out, a client sends no command.
- **The breath** (0011, 0016, 0028): every `Crew.BreathEvery` ticks, in id order, each conscious hero breathes.
  Within `Crew.PodReach` of the pod (`ByPod`) it first takes up to `Crew.PodRefill` of the pod's own air, from no
  store. Then `Breathe` draws the rest of one unit plus up to `Crew.Refill` for the suit. Air taken pays the
  breath, the rest tops the suit; the suit pays any shortfall. A room without supply runs out under its heroes.
- **Thin air** (0076): on a kind with `Surfaces.Kind.ThinAir` n, the suit pays the shortfall only every n-th
  breath (`Clock / BreathEvery` divisible by n), in full, exertion included. Room or pod air works as aboard.
- **Suit colours** (0076): `Crew.Colours`, kept by `Players`, so two heroes on two surfaces never wear one. A
  hero coming in wears the first unworn, or the least worn, first on a tie (`Players.Dress`); a peer's first
  hero gets the kit, the screwdriver (`IItemsOps.Kit`, 0107). `PickColour(Colour)` puts on a
  colour no other hero wears.
- **Leave** (0055, 0076): the host sends it for a peer that left (`Host.Disconnect`); the hero goes, its colour
  freed, its tool stopped, its inventory kept for its seat. Off the ship the seat goes aboard (`ISurfaces.Send`).
- **Surfaces** (0076, `ICarries`): `Depart` gives up the `Hero` as it is, `Arrive` takes it at the cell given or
  the spawn. A hero coming round off the ship wakes aboard: full suit, ship's spawn, body left behind.
- **Exertion** (no ADR): `Exert(On)`, sent on a change of running by speed (not for a swim, 0047). Exerting, a
  breath costs `Crew.Exerted` units, room air or suit alike. Passing out ends it.
- **The jetpack** ([0047](../../docs/decisions/0047-equipment-the-jetpack-and-the-slower-swim.md)): `Thrust(On)`
  is sent while the client's thrust differs from the sim's. Each tick, before the breath, a conscious hero
  thrusting with a jetpack `Equipped` counts a tick; every `Jetpack.TicksPerUnit` its suit pays a unit, never
  below nothing, the remainder kept. Thrust stops when the jetpack is off, on a planet (nothing floats, 0076)
  or on passing out, a thrust landing on a hero out cleared that tick. The flight is the client's.
- **Electric tools** ([0055](../../docs/decisions/0055-an-electric-tool-draws-while-it-runs.md)): `Items` owns
  runs; crew stops one (`StopRunning`) as a hero passes out and every tick it stays out. `Items` draws first.
- **A canister** (no ADR): `Refill` takes one (`Take`; none in creative) and adds `Crew.CanisterRefill` to the
  suit, up to `Crew.Suit`: an absolute amount.
- **Passing out is death** ([0061](../../docs/decisions/0061-death-leaves-a-body.md)): a suit at zero passes the
  hero out for `Crew.PassOut` ticks, then it is made again at the spawn, suit full. That tick crew calls
  `LeaveBody(peer, cell)` at the reported cell, or the spawn if off the grid; `Items` leaves all but gear there.
  The client moves its hero there when it wakes.
- **The cold past the field** ([0050](../../docs/decisions/0050-the-edge-is-made-of-oxygen.md)): the field is the
  build zone grown by `Field.Margin`. `EdgeCost(cell)` is 0 inside; past it, with `dx`, `dy` the cells beyond
  and each axis's span from field to grid edge, `depth = min(1000, max(dx·1000/spanX, dy·1000/spanY))` (rounded
  down), cost `ceil(Field.EdgeCost · depth² / 10⁶)`; off the grid `Field.EdgeCost`. Each breath, after breath
  and refill, the suit alone pays it for the reported cell, never below nothing. No zone (a planet, 0076): no cold.
- **Lava burns** ([0087](../../docs/decisions/0087-the-planets-core-is-lava.md)): `Burns(cell)`: lava
  (`Content.Lava.Material`) at the reported cell or up to `Lava.Reach` rows under it. Each breath the suit alone
  pays `Lava.Burn` more, with the cold.
- **A shock** (0091, 0103): `ICrewOps.Shock(peer, units)` takes units from a conscious hero's suit at once,
  passing it out as an empty suit does; nothing in creative. `Building` (ruins) and `Creatures` (a ram) call it.
- **Creative mode** (no ADR, `Items`' switch): each tick a conscious hero's suit is full; it takes no pod or
  room air. A hero already out wakes as ever.
- **Order** (no ADR): breaths run in the simulation phase after `Atmosphere`'s, so they read this tick's air.

## API

- **Queries** (`ICrewQueries`): `Spawn`; `ByPod(cell)`; `EdgeCost(cell)` (the client's frost reads it);
  `HeroOf(id)` — `Hero` (`Id`, `Cell`, `Suit`, `Out`) or null; `Heroes` — live, read-only, in id order. On the
  module: `Burns(cell)`; `RefillProblem(peer)`, so the client sends no `Refill` the sim would refuse.
- **Commands:** `Enter(Cell)`, never refused (off the grid or in a wall is no air); `PickColour(Colour)` —
  `crew.noHero`, `crew.noSuchColour`, `crew.colourTaken`; `Leave`, `Exert(On)` — `crew.noHero`; `Thrust(On)` —
  `crew.noHero`, `crew.noJetpack`; `Refill` — `crew.noHero`, `crew.suitFull`, `items.notEnough`.
- **Fuzz:** every command and refusal; `Enter` near the spawn, anywhere, or off the grid.

## Invariants

- A suit holds between nothing and `Crew.Suit`, thrust, canisters and cold included (`CrewTests`, `FieldTests`).
- `EdgeCost` is 0 inside the field, never less farther out, never over `Field.EdgeCost` (`FieldTests`).
- Heroes are in id order, one per peer.
- While heroes are no more than colours, no two wear one, on any surfaces.
- A seat's hero is on its surface (`World.SurfaceOf`) and no other (`SurfacesTests`).

## Deliberately not handled

- A `Move` or `Drop` in flight landing while out finds the slots emptied: it moves what is left or is refused.
- The body falls at the reported cell; the client moves its figure there as it passes out.
- The sim trusts the client's flight and cell (0002), so cold and lava are paid up to a second late: a hero
  diving out and back between reports pays nothing.
- Peer ids last a session but are saved: after a load a hero belongs to whoever connects in its slot. Heroes of
  peers who left before a save stay, with their colours.
- A spawn in vacuum on a map without a pod passes a hero out again and again.
- The pod's air is endless: only for the suit, only by the pod, slowly, never to the room. Heroes by it with
  full suits draw nothing from the room.
