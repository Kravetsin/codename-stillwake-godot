# Crew

Heroes inside the sim: where each one is, as its client says, and how it breathes — room air in a cell
holding it, the suit anywhere else, passing out when the suit runs dry. Ported from
`packages/sim/src/modules/crew`; its rules hold here unchanged.

- **Depends on:** `Atmosphere` (`IAtmosphereOps`: its `Breathe` operation decides whether a cell's
  room has air and takes it).
- **State** (`CrewState`, members internal): `Spawn` — the map's first `S`, or cell 0 on a map
  without one; `Clock` — ticks run, for the breath beat; `Heroes` — by peer id, in id order: cell,
  suit, ticks left passed out, suit colour (`Hero`, public with internal setters). All saved.
- **Files:** `Crew.cs` — the module, its commands, `ICrewQueries` and the rules below; `State.cs` —
  its slice and `Hero`; `Fuzz.cs` — its fuzz generator.

## Rules

- A hero is named by the peer that plays it: the sender the host stamps on its commands (`From`).
- `Enter(Cell)` puts the sender's hero at `Cell`; the first one brings it in with a full suit. The
  client sends it whenever the sim has no such hero, places it in another room than its body is in,
  or on the other side of the air's edge, comparing both cells every frame (a door renames rooms, so
  ids are never remembered); the sim resolves the room from the cell at every breath. Later ones
  never refill the suit. Passed out, a hero's client sends no other command.
- Every `Crew.BreathEvery` ticks, in hero id order, each conscious hero draws through
  `IAtmosphereOps.Breathe` one unit for the breath and up to `Crew.Refill` more for what its suit
  lacks. Anything taken pays the breath and the rest tops up the suit; nothing taken (a cell with no
  air) takes the breath from the suit. Room air is taken as `Atmosphere` says: paid by its tanks, or
  from the far edge of the room's air, so a room without supply runs out under its heroes.
- **Suit colours** tell heroes apart: `Crew.Colours` of them. A hero coming in wears the first colour
  no hero wears, or with all worn, the least worn (the first on a tie). `PickColour(Colour)` puts the
  sender's hero into a colour no other hero wears.
- `Leave` takes the sender's hero out, its colour freed: the host sends it on behalf of a peer it
  heard leave (`Host.Disconnect`'s farewell, which the game passes).
- A suit at zero passes the hero out for `Crew.PassOut` ticks; then it wakes at the spawn with a full
  suit. The client moves its hero there when the passing out ends.
- The breaths run in the simulation phase, after `Atmosphere`'s (crew is registered right after it),
  so a breath reads this tick's air.

## API

- **Queries** (`ICrewQueries`): `Spawn`; `HeroOf(id)` (TypeScript's `hero`; a method cannot share
  the `Hero` class's name) — the `Hero` with `Id`, `Cell`, `Suit`, `Out`, `Colour`, or null;
  `Heroes` — all of them, in id order, live and read-only.
- **Commands:** `Enter(Cell)`, with no refusals: a cell off the grid or in a wall is simply no air;
  `PickColour(Colour)`, refused with `crew.noHero`, `crew.noSuchColour` or `crew.colourTaken`;
  `Leave`, refused with `crew.noHero`.
- **Fuzz:** a `Leave` one time in 200; else a `PickColour` one time in five, of `Crew.Colours + 1`
  colours, so taken and missing ones are refused; else an `Enter` within four cells of the spawn,
  or one time in four anywhere from 50 cells before the grid to 1050 past its start.

## Invariants

- A suit holds between nothing and `Crew.Suit`.
- Heroes are in id order, one per peer.
- While no more heroes are aboard than there are colours, no two wear the same.

## Differences from TypeScript

- `PickColour`'s colour is an `int`: TypeScript also refused a fractional one (`1.5`) as
  `crew.noSuchColour`, which cannot be sent here. Its test sends `-1` instead.
- A hero's id is `Id`, TypeScript's `hero`: a member cannot share its class's name.

## Deliberately not handled

- The sim trusts the client's cell: heroes move on the client, and friends trust each other.
- Peer ids last a session but are saved: after a load a hero belongs to whoever connects in its slot,
  until lasting player ids arrive with joining (P1-41). Heroes of peers who left before a save stay
  in it, with their colours.
- A spawn in vacuum makes a hero pass out again and again.
