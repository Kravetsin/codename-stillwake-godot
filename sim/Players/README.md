# Players

What is the player's rather than a surface's ([0076](../../docs/decisions/0076-a-world-is-made-of-surfaces.md)): the
suit colour each hero wears, so two heroes on two surfaces never meet in one colour. A global: one per world, made by
the ship's registry and shared by every surface's, saved first in the ship's `slices`. Records only: no commands, no
systems.

- **Depends on:** nothing.
- **State** (`PlayersState`, members internal): `Colours` — `Worn(Peer, Colour)` for every peer with a hero on any
  surface, in peer order. Saved.
- **Files:** `Players.cs` — the module, its state, query and operation interfaces.

## Rules

- `Crew` dresses a hero coming in (`Dress`): the first of `Crew.Colours` nobody wears, or with all worn the least worn,
  the first on a tie; a peer wearing one already keeps it. `PickColour` puts on another (`Wear`), crew checking that
  no other peer wears it; a hero leaving frees its colour (`Undress`). Moving between surfaces changes nothing here.

## API

- **Queries** (`IPlayersQueries`): `ColourOf(peer)` — null while it has no hero; `Worn` — live, in peer order.
- **Operations** (`IPlayersOps`): `Dress(peer)`, `Wear(peer, colour)`, `Undress(peer)`.

## Invariants

- One entry per peer, in peer order; while no more heroes are in the world than there are colours, no two wear one.
