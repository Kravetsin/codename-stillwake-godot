# 0093 — A planet is heard

- Status: accepted
- Date: 2026-10-21

## Context

On the first planet everything sounded like the ship: boots on its hollow metal deck on grass and stone alike, the ground
silent under the axe, a tree felled without a sound, the lava quiet. An audition page offered CC0 candidates for every sound
the planet lacks, and the user picked one set for each, or none.

## Decision

- **Client only.** Every sound is chosen from what the sim already says is there: the wall's material and shape, the
  growths standing (`Building.Plants`), its cracks, whether a hero burns (`Crew.Burns`). Nothing in `sim/` or `net/` changes.
  The choices are plain C# (`features/sound/GroundSounds.cs`, tested); the cues, their mix and the tables of what sounds
  like what are data (`Content/Sound.cs`).
- **Boots by what they stand on** (`Sound.Footing`): the cell under the middle of the feet, else under either edge, in the
  feet's row or the one under it; kept from the last ground while in the air. Anything built or of hull is the deck, as
  before. Soil with moss drawn on it (open sky above, the drawing's rule, `Grass.Grows`) is grass; moss growing in a cave's
  rock is the grass step 4 dB quieter; other soil is soil; stone, bedrock, lava's crust and the homes' plaster are stone; the
  ruins' alloy its own. A jump off the ground pushes off with its step. Every hero's boots alike, a friend's where it is.
- **Landings by the fall** (0090): under `HeroMotion.Landing.Heavy` the soft landing, from it the heavy one, from 6 dB
  quieter at their own pitch at a fall of 250 to full volume at 0.88 of it from 900, the audition's hop, jump and long fall.
  On the deck as before; on alloy its step, louder and lower, as the deck's.
- **The axe's hit as its blade strikes** (this hero's on Suit, a friend's on World, its swing played as sent): by the natural
  wall there, else by the growth the hit takes. A growth any hit fells has no hit, its fall says it; nor does the felling
  hit of one whose fall sounds as its hits (a bush), or it would play twice.
- **Breaks and falls as the sim makes them, whoever made them:** a cracked wall gone (`DigView`) by its material; a growth
  gone (`FloraView`, `CaveView`, `HomesView`) by its kind. What lies in the rock and goes with its stone (moss, mica, a
  copper vein, the heat's cracks, a fossil, rubble) has no sound of its own; a geode or an ice vein adds its crystals' snap, a
  spire's shrine and lamp their metal and glass, a fallen beam its metal, to the stone's or alloy's break.
- **The drill in soil** strikes grains of the axe's own soil hit, cut as its pickaxe strike was; on anything else its
  strike as before. The user chose no sound of its own.
- **Lava:** its synthesised rumble, a loop that closes on itself, at the open lava nearest the listener, a station's reach;
  its bubbles at random moments over it; a hero touching it hisses at each breath its suit burns. **Caves:** a stalactite's
  drip heard where it splashes, at most one every 0.5 to 2.2 s.
- **The mix:** each file kept at the audition's level and played at the offset that matched the audition's reference to the
  game: the steps by the deck's step (8 dB under the file), the landings by the deck's landing (6 under), the hits, breaks and
  falls by the axe's swing (1 over), which puts a soil hit as loud as a wreck's chop and a stone's break as a wreck's.

## Consequences

- 75 new files and one loop, every one in `game/audio/CREDITS.md`.
- A new growth kind fails a test until it is given its sound or listed as unheard (`Sound.Unheard`).

## Deliberately not handled

- **Ladders:** no rung or slide sound for now, as the user chose.
- **Cave air and surface wind:** none picked; caves are quiet but for their drips.
- **A friend's drill** stays silent, as before; a friend's axe hits are heard only as its swings are played here, so a hit
  sent while its swing was not shown is missed.
- **A drill on a growth** strikes its pickaxe, whatever the growth.
- **Rugs underfoot** sound as the floor under them.
- **The deck's landing** keeps its one loudness whatever the fall.
