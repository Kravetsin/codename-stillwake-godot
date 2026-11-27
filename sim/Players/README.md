# Players

## Files

| File | Purpose |
| --- | --- |
| `Players.cs` | What is the player's rather than a surface's (0076): the suit colour each hero wears, so two heroes on two surfaces … |

What is the player's rather than a surface's ([0076](../../docs/decisions/0076-a-world-is-made-of-surfaces.md)): the
suit colour each hero wears, so two heroes on two surfaces never meet in one colour, and the look each player chose
([looks-are-printed](../../docs/decisions/2026-10-28-looks-are-printed.md)) and the skins it wears
([hero-skins](../../docs/decisions/2026-11-16-hero-skins.md)), and whether it paired with its light drone ([drone-companion](../../docs/decisions/2026-11-21-drone-companion.md)). A global: one per world, made by the ship's
registry and shared by every surface's, saved first in the ship's `slices`. Records only: no commands, no systems.

- **Depends on:** nothing.
- **State** (`PlayersState`, members internal): `Colours` — `Worn(Peer, Colour)` for every peer with a hero on any
  surface, in peer order. `Looks` — `PeerLook(Peer, Worn, Next)` for every peer that chose a look, with a hero or not,
  in peer order; added empty, so an older save loads with none. `Skins` — `PeerSkins(Peer, Ids)` for every peer that
  chose skins, with a hero or not, in peer order; saved only while any, so an older save loads with none. `Drones` —
  the peers paired with their drone, sorted. `Cores` — `Core(Peer, Charge)` for every paired peer whose core ever
  filled, in peer order; saved only while any, `Charge` clamped to `Abilities.Full` on load.
- **Files:** `Players.cs` — the module, its state, query and operation interfaces.

## Rules

- `Crew` dresses a hero coming in (`Dress`): the first of `Crew.Colours` nobody wears, or with all worn the least worn,
  the first on a tie; a peer wearing one already keeps it. `PickColour` puts on another (`Wear`), crew checking that
  no other peer wears it; a hero leaving frees its colour (`Undress`). Moving between surfaces changes nothing here.
- A `Look` is six indices (skin, hair, hair colour, face, beard, 0 none, clothes), never checked: the client draws each by a
  non-negative modulo of its palette. `ChooseLook` sets a peer's `Next` (`Choose`); every print — a hero made where
  its peer had none, or waking — wears it (`Print`). A peer that left keeps its entry, so it comes back in its last
  choice and its bodies show the look it wore.
- Skins are ids only the client knows (its catalogue `Skins`), never checked. `ChooseSkins` replaces a peer's whole set at
  once (`Skin`), keeping a copy of the command's list, with a hero or none yet: no print, no refusal. A peer that left
  keeps its set, as with looks.
- `Crew` pairs a peer as its `PairDrone` lands, or every hero still unpaired once the core is lost (`Pair`): for good, on every surface, through death and leaving. Pairing
  twice throws: crew refuses it first. `Items` opens the bag rows of a paired peer alone.
- The drone's core is its stamina ([drone-abilities](../../docs/decisions/2026-11-27-drone-abilities.md)): `Charge(peer, by)`
  adds or spends, held to `0..Abilities.Full`, for a paired peer only. `Crew` fills it each tick by `Abilities.Regen`
  less its light's `DrainOf(Shining)` (awake, not meeting), by `PerWorth` a worth of `Loot` (creative too), and spends it on
  `Cast`. It lasts through death and leaving.

## API

- **Queries** (`IPlayersQueries`): `ColourOf(peer)` — null while it has no hero; `Worn` — live, in peer order;
  `LookOf(peer)` — the look worn, `Look.Zero` for a peer that never chose; `SkinsOf(peer)` — the set
  chosen last, empty for a peer that never chose; `HasDrone(peer)`; `ChargeOf(peer)` — 0 for none.
- **Operations** (`IPlayersOps`): `Dress(peer)`, `Wear(peer, colour)`, `Undress(peer)`, `Choose(peer, look)`,
  `Print(peer)`, `Skin(peer, ids)`, `Pair(peer)`, `Charge(peer, by)`.

## Invariants

- One entry per peer in each list, in peer order; while no more heroes are in the world than there are colours, no
  two wear one.
- A worn look changes only on a print, to the look chosen last (`LooksTests`); skins change only on a choice
  (`SkinsTests`). A pairing is never undone (`LightDroneTests`). A core holds within `0..Full` (`DroneCoreTests`).
