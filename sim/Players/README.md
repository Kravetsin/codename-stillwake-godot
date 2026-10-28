# Players

## Files

| File | Purpose |
| --- | --- |
| `Players.cs` | What is the player's rather than a surface's (0076): the suit colour each hero wears, so two heroes on two surfaces … |

What is the player's rather than a surface's ([0076](../../docs/decisions/0076-a-world-is-made-of-surfaces.md)): the
suit colour each hero wears, so two heroes on two surfaces never meet in one colour, and the look each player chose
([looks-are-printed](../../docs/decisions/2026-10-28-looks-are-printed.md)). A global: one per world, made by the ship's
registry and shared by every surface's, saved first in the ship's `slices`. Records only: no commands, no systems.

- **Depends on:** nothing.
- **State** (`PlayersState`, members internal): `Colours` — `Worn(Peer, Colour)` for every peer with a hero on any
  surface, in peer order. `Looks` — `PeerLook(Peer, Worn, Next)` for every peer that chose a look, with a hero or not,
  in peer order; added empty, so an older save loads with none. Saved.
- **Files:** `Players.cs` — the module, its state, query and operation interfaces.

## Rules

- `Crew` dresses a hero coming in (`Dress`): the first of `Crew.Colours` nobody wears, or with all worn the least worn,
  the first on a tie; a peer wearing one already keeps it. `PickColour` puts on another (`Wear`), crew checking that
  no other peer wears it; a hero leaving frees its colour (`Undress`). Moving between surfaces changes nothing here.
- A `Look` is five indices (skin, hair, hair colour, face, beard, 0 none), never checked: the client draws each by a
  non-negative modulo of its palette. `ChooseLook` sets a peer's `Next` (`Choose`); every print — a hero made where
  its peer had none, or waking — wears it (`Print`). A peer that left keeps its entry, so it comes back in its last
  choice and its bodies show the look it wore.

## API

- **Queries** (`IPlayersQueries`): `ColourOf(peer)` — null while it has no hero; `Worn` — live, in peer order;
  `LookOf(peer)` — the look worn, `Look.Zero` for a peer that never chose.
- **Operations** (`IPlayersOps`): `Dress(peer)`, `Wear(peer, colour)`, `Undress(peer)`, `Choose(peer, look)`,
  `Print(peer)`.

## Invariants

- One entry per peer in each list, in peer order; while no more heroes are in the world than there are colours, no
  two wear one.
- A worn look changes only on a print, to the look chosen last (`LooksTests`).
