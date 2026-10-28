# Looks are chosen and printed

- Status: accepted; built
- Date: 2026-10-28
- Builds on [one-head](2026-10-28-one-head.md) and [suits-are-worn](2026-10-27-suits-are-worn.md); supersedes
  [0003](0003-character-rig.md)'s "Appearance is sent once on join"

## Context

Under the suit is a person (one-head): one head for everyone, told apart by skin, hair, hair colour, face and beard. Each
player picks theirs. The lifepod makes heroes (suits-are-worn), so a hero's look should come from the pod, not change
on a live hero, and stay the same on every peer.

## Decision

- **A look is five indices**, `Look(Skin, Hair, HairColour, Face, Beard)` in `Players` (beard 0 none; look 0 every index
  0). The sim never checks them: a payload's shape, not a game rule. The client draws each by a non-negative modulo of its
  palette (`HeroLooks.Wrap`), so a stale or altered client's index still draws.
- **Chosen, then printed.** `ChooseLook(Look)` (a Crew command, as `PickColour` is, never refused, with a hero or none)
  sets the peer's next look; every print — an `Enter` making a hero where its peer had none, or waking — wears it
  (`Players.Print`). A live hero keeps its look till then; moving between surfaces is no print. `PlayersState.Looks` keeps
  `(Peer, Worn, Next)` in peer order, added empty, so an older save loads in look 0 with no format bump.
- **A peer that left keeps its entry**: it comes back in its last choice, and its bodies show the look it wore.
- **Kept on this machine.** The client keeps the look in its settings and sends `ChooseLook` once a join, before the
  `Enter` that prints its hero. On a first run with none kept, the look editor opens with the world and that `Enter`
  waits till it closes. A dev run plays look 0 and keeps nothing.
- **The look editor** opens from the game menu at any time: the hero bare in the player's colour, turning, and a picker
  each for the five. A line says the pod prints the look next time.

## Deliberately not handled

- **Bodies show the peer's look now**, not the one they died in: a body from before a reprint shows the new face.
- **After a load**, a saved hero keeps its worn look; the kept look shows from its next print.
- **Indices out of the palette** are saved and hashed as sent, drawn wrapped.
- **No body shapes, no colours beyond the player's.**

## Consequences

`IPlayersQueries.LookOf(peer)` feeds every figure, body and seat. Tests: `LooksTests` (each rule, an older save, an
invariant that a worn look changes only on a print, proven to bite), `ReportTests` (the look before the first `Enter`).
Frames: `-- --looks`.
