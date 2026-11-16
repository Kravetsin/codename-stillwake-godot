# Stretches of space

- Status: accepted; its one warning for all, stretches only aboard and draws from the world's Rng superseded by
  [surface-storms](2026-11-16-surface-storms.md)
- Date: 2026-11-15

## Context

A crew that has landed and built up the ship met nothing new in space. As Terraria's events do, spells of space opened by
progress bring it back: one mechanic, each spell a row of data. Designed by `design-author`, cut by `design-reviewer`, the
plan reviewed by `/review-plan`, played by the user and tuned on what they saw.

## Decision

- **A stretch is a record** (`Content.Stretches`): what opens it (`Unlock`, any one item obtained), when it first comes and
  comes again (`First`, `Gap`), how long it lasts (`Duration`), how much colder outside (`Chill`), its own debris zone, and
  how much it thins the usual stream (`Thins`). One warning of 3 min for all. Only the ship has stretches (`Kind.Stretches`).
- **Sky owns the schedule**, in its construction phase: opened stretches in content order, the first never seen chosen first,
  else one drawn by the world's Rng; a choice made stands, so at most one is chosen, coming or running. Unlocks are per world,
  read from `Knowledge.ObtainedOf`, and never lock again; a creative spawn opens nothing. Saved: `Opened`, `Came`, `Next`,
  `NextIn`, `Active`, `Left`, `Ran`, each with a "none" initializer, so no format bump. `StretchNow` brings one in from the
  dev panel.
- **`SkyNow` says it**: `Stretch`, `Coming`, `ComingIn` and `Ran`. Its `Chill` is night's plus the stretch's; Crew applies it
  as it did night's, to open cells only.
- **Debris**: a stretch's zone is an ordinary zone with a `Stretch` id, beating only while it runs, its first piece a beat
  after the start. While a stretch runs, the stream by the hull beats `Thins` times slower, so its own pieces are not lost
  among the junk (the ice belt: 4).
- **The two stretches**: the **ice belt**, opened by the quilted or heavy helmet, 40° colder outside (−60° under the open
  sky), ice blocks of 6–10 ice; the **contraband drift**, opened by the railgun, crates of contraband worth 1200. Contraband
  fills the wanted meter as any loot does, and counts at full worth toward the mafia's reputation at every level.
- **Seen coming, without words**, all from `SkyNow`, so a peer joining mid-warning sees what the others do:
  - a **far picture** behind the far wrecks, passing right to left at its own steady speed from the warning's start, as if the
    ship sailed into it, and out past the left edge after; zoomed with the camera. Its place and pick come from the world's
    tick at the warning's start, the same for every peer and after a load. The ice belt's is a field of small ice pieces cut
    from a picture, dim and blue so it never reads as debris near; the drift's is one derelict of three;
  - the **radar's band** in the stretch's colour, sliding toward the ship, when a computer is built;
  - **frost on the hull** before the ice belt: a light rime crust along every wall facing space, proud of the edge, its inner
    edge scalloped, its corners rounded or filled, growing from the outer corners and cold spots and melting back after.
- **The looks were picked** on published pages: the ice block, the contraband crate and item, and the frost «Кромка инея».
  The far pictures are **GPT-made** (ice drusen and three derelicts), picked by the user and shipped as they are, cut and
  dimmed: the Steam page must disclose AI-generated content for them.

## Deliberately not handled

- Two stretches at once; steering round one or jumping away; unlocks per hero; a stretch reaching a hero on a planet.
- Meteor and flare overrides: no stretch sets them yet.
- The cold has no easing: it changes on the tick a stretch starts and ends.
- A crew without a computer sees the far picture and the frost only.
- Losing the helmet does not lock the belt again; contraband caught after its drift is worth as much as any.
- All numbers are placeholders for a play pass, in `sim/Content`.
