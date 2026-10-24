# Crew

## Files

| File | Purpose |
| --- | --- |
| `Crew.cs` | Heroes inside the sim: where each one is, as its client says, and how it breathes — room air in a cell holding it … |
| `Fuzz.cs` | The module's `Fuzz` override: the commands the desync suite draws. |
| `State.cs` | The saved slice and its records: `Hero`, `CrewState`. |

Heroes inside the sim: where each one is, as its client says, and how it breathes — the lifepod's air
beside it, room air in a cell holding it, the suit anywhere else, passing out when the suit runs dry,
made again at the pod. Ported from `packages/sim/src/modules/crew`; the lifepod came with
[0028](../../docs/decisions/0028-the-lifepod-and-crafting-by-machine-only.md).

- **Depends on:** `Atmosphere` (`IAtmosphereOps`: its `Breathe` operation decides whether a cell's
  room has air and takes it); `Grid` (`IGridQueries.Width`, `Height` and `Zone`: where a cell is, for the pod's reach and the field;
  `WallAt`, lava under a hero); `Items`
  (`IItemsQueries.Equipped`, `ShortOf` and `Creative`, whether a hero wears a jetpack, carries a canister and needs air;
  `IItemsOps.Take`, the canister breathed; `IItemsOps.StopRunning`, an electric tool stopped as its hero passes out
  or leaves, 0055; `IItemsOps.LeaveBody`, what a hero carried left where it died, 0061); `Players` (`IPlayersQueries.Worn`
  and `IPlayersOps`: suit colours are the world's, 0076).
- **State** (`CrewState`, members internal): `Spawn` — the hatch of the map's first lifepod
  (`Content.Crew.PodHatch` from its top-left), else the map's first `S`, or cell 0 on a map with
  neither; `Pod` — that lifepod's top-left cell, null without one (it is `Fixed`: it never moves); `Clock` — ticks run, for the breath beat; `Heroes` — by peer id, in id order: cell,
  suit, ticks left passed out, whether it exerts itself, whether it thrusts and its ticks of
  thrust toward the next unit (`Hero`, public with internal setters). All saved.
- **Files:** `Crew.cs` — the module, its commands, `ICrewQueries` and the rules below; `State.cs` —
  its slice and `Hero`; `Fuzz.cs` — its fuzz generator.

## Rules

- A hero is named by the peer that plays it: the sender the host stamps on its commands (`From`).
- `Enter(Cell)` puts the sender's hero at `Cell`; the first one brings it in with a full suit. The
  client sends it whenever the sim has no such hero, places it in another room than its body is in,
  or on the other side of the air's edge, comparing both cells every frame (a door renames rooms, so
  ids are never remembered); the sim resolves the room from the cell at every breath. Later ones
  never refill the suit. The client also sends it as the hero's **report** (0050): whenever the sim
  holds it in another cell than its body's and `Crew.ReportEvery` ticks have gone since the last
  `Enter` it sent, never while passed out. So `Hero.Cell` is the reported cell, up to a second and a
  round trip behind the body: what the breath reads, and what anything placed by where heroes are
  reads. Passed out, a hero's client sends no other command.
- Every `Crew.BreathEvery` ticks, in hero id order, each conscious hero breathes. Within
  `Crew.PodReach` cells of the lifepod's footprint, either way (`ByPod`), it first takes up to
  `Crew.PodRefill` units of the pod's own air, from no store, for the breath and what its suit lacks;
  it never gives the room any. Then it draws through `IAtmosphereOps.Breathe` what is left of one
  unit for the breath and up to `Crew.Refill` more for what its suit lacks, nothing when the pod paid
  for both. Anything taken pays the breath and the rest tops up the suit; what falls short of the
  breath (a cell with no air, away from the pod) the suit pays. Room air is taken as `Atmosphere` says: paid by its tanks, or
  from the far edge of the room's air, so a room without supply runs out under its heroes.
- **Thin air** (0076): on a surface whose kind has thin air (`Surfaces.Kind.ThinAir`, the test strip's 5), what the air
  falls short of the breath — outside, or in a sealed room holding no air — the suit pays only on one breath in that
  many (the breath whose count since the surface opened, `Clock / BreathEvery`, divides by it), and then in full,
  exertion included; on the others it pays nothing. Air taken from a room or the pod works as aboard.
- **Suit colours** tell heroes apart: `Crew.Colours` of them, kept by the world's `Players` (0076), so two heroes on
  two surfaces never wear one. A hero coming in wears the first colour no hero wears, or with all worn, the least worn
  (the first on a tie) (`Players.Dress`), and a peer's first hero is given the kit, the screwdriver and the stripper (`IItemsOps.Kit`, 0107, 0108). `PickColour(Colour)` puts the sender's hero into a colour no other hero
  wears, on any surface.
- `Leave` takes the sender's hero out, its colour freed: the host sends it on behalf of a peer it
  heard leave (`Host.Disconnect`'s farewell, which the game passes). Whatever electric tool it ran stops
  (`StopRunning`): its inventory stays for its seat, and its belt would drain with no one at it. Off the ship, the
  seat goes aboard with its inventory (`ISurfaces.Send`): its player comes back at the pod (0076).
- **Surfaces** (0076): crew carries a hero from surface to surface (`ICarries`): `Depart` gives up its `Hero` record,
  suit, breath, exertion and thrust as they are, and `Arrive` takes it at the cell given or the spawn. A hero coming
  round off the ship wakes aboard: full suit, sent to the ship's spawn, its body left where it fell.
- `Exert(On)` says the sender's hero runs, or has stopped (its client no longer sends it for a swim, 0047); its client sends it when
  that changes, judging by its speed. Exerting, a hero breathes `Crew.Exerted` units a breath rather
  than one, from room air or the suit alike: stamina is the air it costs. What the air falls short
  of the breath, the suit pays. Passing out ends the exertion.
- **The jetpack** ([0047](../../docs/decisions/0047-equipment-the-jetpack-and-the-slower-swim.md)):
  `Thrust(On)` says the sender's hero thrusts with its jetpack, or has stopped; its client sends it while
  its own thrust differs from the sim's. Every tick, before the breath, a conscious hero thrusting with a
  jetpack worn (`Equipped`) counts a tick of thrust (`ThrustTicks`); every `Jetpack.TicksPerUnit` of them
  its suit pays a unit, never below nothing, the ticks short of a unit kept between bursts so taps add
  up. One no longer wearing a jetpack stops thrusting, as does one on a surface where nothing floats (a planet,
  0076: the jetpack, an air-thrust suit for space, lifts nothing under gravity and vents nothing); passing out stops
  it too, and a thrust landing on a hero out is cleared the same tick. The flight itself is the client's.
- **Electric tools** ([0055](../../docs/decisions/0055-an-electric-tool-draws-while-it-runs.md)): a run belongs to
  `Items`, which owns charge; crew only stops it (`StopRunning`) as a hero passes out and every tick it stays out, so
  a `Run` landing on a hero out is stopped on its first tick. `Items` draws before crew in a tick, so that tick is
  paid: a draw of 3 power-ticks, accepted.
- **A canister breathed:** `Refill` takes one canister from the sender (through `Take`, so from its last
  stack, and none in creative mode) and adds `Crew.CanisterRefill` units to its suit, up to `Crew.Suit`:
  an absolute amount, so a bigger suit later still gets a canister's worth.
- A suit at zero passes the hero out for `Crew.PassOut` ticks; then it is made again at the spawn,
  the pod's hatch, with a full suit.
- **Passing out is death** ([0061](../../docs/decisions/0061-death-leaves-a-body.md)): the tick it passes out, crew
  calls `IItemsOps.LeaveBody(peer, cell)` with the hero's cell, the last one its client reported, or the spawn when
  that cell is off the grid (a hero `Enter`ed there, which nothing validates). `Items` leaves there everything the
  hero carried but its gear, a body for anyone to pick up; the hero wakes with its gear. A future cause of death
  (an enemy draining the suit) lands here too. The client moves its hero there when the passing out ends. A
  hero's first `Enter` is where its client placed it, the spawn.
- **The cold past the field** ([0050](../../docs/decisions/0050-the-edge-is-made-of-oxygen.md)): the field is
  the grid's build zone grown by `Field.Margin` cells every way. `EdgeCost(cell)` is 0 inside it; past
  it, with `dx` and `dy` the cells beyond it and each axis's span the cells from the field to the grid's
  edge on that side, `depth = min(1000, max(dx·1000/spanX, dy·1000/spanY))` (thousandths, rounded down;
  a box, no root) and the cost is `ceil(Field.EdgeCost · depth² / 10⁶)`: 1 just past the line, 12 half
  way, 45 at the edge; a cell off the grid costs `Field.EdgeCost`. At every breath, after the breath
  and the refill, the suit alone pays it for the reported cell, never below nothing; a suit emptied
  so (or by the breath) passes the hero out as ever. It adds to thrust, which the suit pays apart. On a
  map that sets no zone the field reaches past the grid, so no cell on it costs anything. A planet's grid sets none (0076):
  its whole grid is its field.
- **Lava burns** ([0087](../../docs/decisions/0087-the-planets-core-is-lava.md)): `Burns(cell)`, public on the module, is
  whether a wall of lava (`Content.Lava.Material`) is at the reported cell or up to `Lava.Reach` (4) rows under it — where
  the feet of a hero standing on it are, its middle 2625 thousandths over them. At every breath the suit alone pays
  `Lava.Burn` (15) more for it, with the cold: with the breath itself a full suit (45) is gone on the third breath, two
  to three seconds after the sim hears of the hero there, and the hero passes out, dies and wakes at the pod as from any
  empty suit. It reads the reported cell, so a report up to a second late (0050) burns a second late.
- **A shock** ([0091](../../docs/decisions/0091-homes-of-the-arc-makers.md)): `ICrewOps.Shock(peer, units)`, the one operation,
  takes units from a conscious hero's suit at once, the suit alone, and passes it out as an empty suit does; nothing in creative
  mode. Building calls it for a ruin's resonator zapping a hero beside it, at the breath's tick, before crew breathes.
- **Creative mode** (`Items`' dev panel switch): every tick a conscious hero's suit is full, whatever
  breath, exertion, thrust or cold would take, so it never passes out; it takes nothing from the pod or
  a room's air either. A hero already out when creative mode comes on wakes as ever.
- The breaths run in the simulation phase, after `Atmosphere`'s (crew is registered right after it),
  so a breath reads this tick's air.

## API

- **Queries** (`ICrewQueries`): `Spawn`; `ByPod(cell)` — whether a hero there draws the pod's air; `EdgeCost(cell)` — what the
  cold takes from a suit a breath there (the breath and the client's frost read it); `HeroOf(id)` (TypeScript's `hero`; a method cannot share
  the `Hero` class's name) — the `Hero` with `Id`, `Cell`, `Suit`, `Out`, or null (its colour is `Players.ColourOf`);
  `Heroes` — all of them, in id order, live and read-only. On the module: `Burns(cell)` — whether lava burns a hero reported there.
- **Commands:** `Enter(Cell)`, with no refusals: a cell off the grid or in a wall is simply no air;
  `PickColour(Colour)`, refused with `crew.noHero`, `crew.noSuchColour` or `crew.colourTaken`;
  `Leave`, refused with `crew.noHero`; `Exert(On)`, refused with `crew.noHero`; `Thrust(On)`, refused
  with `crew.noHero`, or `crew.noJetpack` when on with no jetpack worn; `Refill`, refused with
  `crew.noHero`, `crew.suitFull` (the suit already full) or `items.notEnough` (no canister carried).
  `RefillProblem(peer)`, public on the module, is that refusal ahead: the client sends no `Refill` the sim
  would refuse.
- **Fuzz:** a `Leave` one time in 200; else an `Exert`, on or off, one time in six; else a `Thrust`, on or
  off, one time in eight, mostly from heroes wearing no jetpack, so `crew.noJetpack` is drawn; else a
  `Refill` one time in twelve, refused with a full suit or no canister; else a `PickColour` one time in five, of `Crew.Colours + 1`
  colours, so taken and missing ones are refused; else an `Enter` within four cells of the spawn,
  or one time in four anywhere on the grid or up to 50 cells off either end of it.

## Invariants

- A suit holds between nothing and `Crew.Suit`. Thrust and canisters included (`CrewTests`, FsCheck). With heroes
  anywhere on the new world, the field's line and off the grid, cold included (`FieldTests`).
- `EdgeCost` is 0 inside the field, never less farther out, never more than `Field.EdgeCost`
  (`FieldTests`, FsCheck).
- Heroes are in id order, one per peer.
- While no more heroes are in the world than there are colours, no two wear the same, on whichever surfaces.
- A seat's hero is on its surface (`World.SurfaceOf`) and on no other (`SurfacesTests`, FsCheck).

## Differences from TypeScript

- `PickColour`'s colour is an `int`: TypeScript also refused a fractional one (`1.5`) as
  `crew.noSuchColour`, which cannot be sent here. Its test sends `-1` instead.
- A hero's id is `Id`, TypeScript's `hero`: a member cannot share its class's name.

## Deliberately not handled

- A `Move` or `Drop` sent just before passing out that lands in the 5 s out finds the hero's slots already emptied
  by its death: it moves what is left there, gear, or is refused (`items.empty`); a pick-up landing then is taken.
  Accepted: the client sends nothing while out, and one in flight is a round trip at most.
- The body falls at the reported cell, up to a second and a round trip behind the hero; its client moves its
  figure there as it passes out, so it lies where the body will be.
- The sim trusts the client's flight: where a jetpack takes the hero, and that it thrusts only in space,
  as for every movement (0002). It pays the oxygen whatever the hero does with it.
- The sim trusts the client's cell: heroes move on the client, and friends trust each other. The
  cold is paid for the reported cell, up to a second and a round trip old: a hero diving out and back
  between two reports pays nothing, one coming back in pays a breath or so more than its body's cell
  would.
- Peer ids last a session but are saved: after a load a hero belongs to whoever connects in its slot,
  until lasting player ids arrive with joining (P1-41). Heroes of peers who left before a save stay
  in it, with their colours.
- A spawn in vacuum on a map without a pod makes a hero pass out again and again; by the pod a suit
  fills even in vacuum.
- The pod's air is endless: only for the suit, only by the pod, slowly. Aired rooms, tanks and
  generators stay the way to live; the pod is the way not to die. Heroes by the pod with full suits
  draw nothing from the room.
