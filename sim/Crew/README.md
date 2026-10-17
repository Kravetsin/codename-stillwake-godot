# Crew

Heroes inside the sim: where each one is, as its client says, and how it breathes — the lifepod's air
beside it, room air in a cell holding it, the suit anywhere else, passing out when the suit runs dry,
made again at the pod. Ported from `packages/sim/src/modules/crew`; the lifepod came with
[0028](../../docs/decisions/0028-the-lifepod-and-crafting-by-machine-only.md).

- **Depends on:** `Atmosphere` (`IAtmosphereOps`: its `Breathe` operation decides whether a cell's
  room has air and takes it); `Grid` (`IGridQueries.Width`: where a cell is, for the pod's reach); `Items`
  (`IItemsQueries.Equipped` and `ShortOf`, whether a hero wears a jetpack and carries a canister;
  `IItemsOps.Take`, the canister breathed).
- **State** (`CrewState`, members internal): `Spawn` — the hatch of the map's first lifepod
  (`Content.Crew.PodHatch` from its top-left), else the map's first `S`, or cell 0 on a map with
  neither; `Pod` — that lifepod's top-left cell, null without one (it is `Fixed`: it never moves); `Clock` — ticks run, for the breath beat; `Heroes` — by peer id, in id order: cell,
  suit, ticks left passed out, suit colour, whether it exerts itself, whether it thrusts and its ticks of
  thrust toward the next unit (`Hero`, public with internal setters). All saved.
- **Files:** `Crew.cs` — the module, its commands, `ICrewQueries` and the rules below; `State.cs` —
  its slice and `Hero`; `Fuzz.cs` — its fuzz generator.

## Rules

- A hero is named by the peer that plays it: the sender the host stamps on its commands (`From`).
- `Enter(Cell)` puts the sender's hero at `Cell`; the first one brings it in with a full suit. The
  client sends it whenever the sim has no such hero, places it in another room than its body is in,
  or on the other side of the air's edge, comparing both cells every frame (a door renames rooms, so
  ids are never remembered); the sim resolves the room from the cell at every breath. Later ones
  never refill the suit. Passed out, a hero's client sends no other command.
- Every `Crew.BreathEvery` ticks, in hero id order, each conscious hero breathes. Within
  `Crew.PodReach` cells of the lifepod's footprint, either way (`ByPod`), it first takes up to
  `Crew.PodRefill` units of the pod's own air, from no store, for the breath and what its suit lacks;
  it never gives the room any. Then it draws through `IAtmosphereOps.Breathe` what is left of one
  unit for the breath and up to `Crew.Refill` more for what its suit lacks, nothing when the pod paid
  for both. Anything taken pays the breath and the rest tops up the suit; what falls short of the
  breath (a cell with no air, away from the pod) the suit pays. Room air is taken as `Atmosphere` says: paid by its tanks, or
  from the far edge of the room's air, so a room without supply runs out under its heroes.
- **Suit colours** tell heroes apart: `Crew.Colours` of them. A hero coming in wears the first colour
  no hero wears, or with all worn, the least worn (the first on a tie). `PickColour(Colour)` puts the
  sender's hero into a colour no other hero wears.
- `Leave` takes the sender's hero out, its colour freed: the host sends it on behalf of a peer it
  heard leave (`Host.Disconnect`'s farewell, which the game passes).
- `Exert(On)` says the sender's hero runs, or has stopped (its client no longer sends it for a swim, 0047); its client sends it when
  that changes, judging by its speed. Exerting, a hero breathes `Crew.Exerted` units a breath rather
  than one, from room air or the suit alike: stamina is the air it costs. What the air falls short
  of the breath, the suit pays. Passing out ends the exertion.
- **The jetpack** ([0047](../../docs/decisions/0047-equipment-the-jetpack-and-the-slower-swim.md)):
  `Thrust(On)` says the sender's hero thrusts with its jetpack, or has stopped; its client sends it while
  its own thrust differs from the sim's. Every tick, before the breath, a conscious hero thrusting with a
  jetpack worn (`Equipped`) counts a tick of thrust (`ThrustTicks`); every `Jetpack.TicksPerUnit` of them
  its suit pays a unit, never below nothing, the ticks short of a unit kept between bursts so taps add
  up. One no longer wearing a jetpack stops thrusting; passing out stops it too, and a thrust landing on a
  hero out is cleared the same tick. The flight itself is the client's.
- **A canister breathed:** `Refill` takes one canister from the sender (through `Take`, so from its last
  stack, and none in creative mode) and adds `Crew.CanisterRefill` units to its suit, up to `Crew.Suit`:
  an absolute amount, so a bigger suit later still gets a canister's worth.
- A suit at zero passes the hero out for `Crew.PassOut` ticks; then it is made again at the spawn,
  the pod's hatch, with a full suit. The client moves its hero there when the passing out ends. A
  hero's first `Enter` is where its client placed it, the spawn.
- The breaths run in the simulation phase, after `Atmosphere`'s (crew is registered right after it),
  so a breath reads this tick's air.

## API

- **Queries** (`ICrewQueries`): `Spawn`; `ByPod(cell)` — whether a hero there draws the pod's air; `HeroOf(id)` (TypeScript's `hero`; a method cannot share
  the `Hero` class's name) — the `Hero` with `Id`, `Cell`, `Suit`, `Out`, `Colour`, or null;
  `Heroes` — all of them, in id order, live and read-only.
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
  or one time in four anywhere from 50 cells before the grid to 1050 past its start.

## Invariants

- A suit holds between nothing and `Crew.Suit`. Thrust and canisters included (`CrewTests`, FsCheck).
- Heroes are in id order, one per peer.
- While no more heroes are aboard than there are colours, no two wear the same.

## Differences from TypeScript

- `PickColour`'s colour is an `int`: TypeScript also refused a fractional one (`1.5`) as
  `crew.noSuchColour`, which cannot be sent here. Its test sends `-1` instead.
- A hero's id is `Id`, TypeScript's `hero`: a member cannot share its class's name.

## Deliberately not handled

- The sim trusts the client's flight: where a jetpack takes the hero, and that it thrusts only in space,
  as for every movement (0002). It pays the oxygen whatever the hero does with it.
- The sim trusts the client's cell: heroes move on the client, and friends trust each other.
- Peer ids last a session but are saved: after a load a hero belongs to whoever connects in its slot,
  until lasting player ids arrive with joining (P1-41). Heroes of peers who left before a save stay
  in it, with their colours.
- A spawn in vacuum on a map without a pod makes a hero pass out again and again; by the pod a suit
  fills even in vacuum.
- The pod's air is endless: only for the suit, only by the pod, slowly. Aired rooms, tanks and
  generators stay the way to live; the pod is the way not to die. Heroes by the pod with full suits
  draw nothing from the room.
