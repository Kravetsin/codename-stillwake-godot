# Fancy skins: nine helmets and their suits

- Status: accepted; follows [hero-skins](2026-11-16-hero-skins.md), its first helmet skins and the suits that go with them
- Date: 2026-11-16

## Context

[hero-skins](2026-11-16-hero-skins.md) shipped body and cape skins but no helmet skin. The user asked for showy helmets in
the game's ink and watercolour, kept all nine of a GPT sheet («ахах ваще прикол, добавляй все»), then found the plain
suits dull beside them and kept all nine suits painted to match. In play, the first helmets bent their beaks, horns and
jaws as the head turned, and the basic helmet's collar ring sat over them.

## Decision

- **Nine sets, free for now.** Helmets `brassHelmet`, `beakHelmet`, `roninHelmet`, `knightHelmet`, `reliquaryHelmet`,
  `skullHelmet`, `ramHelmet`, `bathysphereHelmet`, `paradeHelmet`, each with a suit of its name (`brassSuit` … `paradeSuit`).
  They carry the placeholder app id `0`, as every skin so far: locked in a release build until GodotSteam.
- **Painted raw over the basic helmet.** Each helmet is GPT paint laid so its opening sits on the basic helmet's, shrunk
  until nothing sticks out of the 13 × 12 box: the head, glass and lamp stay where the basic helmet has them. Each suit is
  the four part pictures of a body skin, repainted inside the same outlines, so it bends at the same joints.
- **A helmet skin turns whole on a seal.** `BodyFit` gives every one `HelmetTurn.Ring` about the basic helmet's seal
  (`SealY`), `Sealed`: the shell turns as one piece, so nothing below the pivot is bent, and its own seal, the basic
  helmet's rubber ring (`_seal`), bends between it and the collar. The soft bend of the basic ball stretched a beak or a jaw.
- **No collar over them.** A helmet skin's `_collar` picture is empty: the ring is drawn under the shell as its seal, not
  across it.
- **Rows, not code.** Each helmet has its `LampMounts` and `ScannerInk` rows (set by eye: crests fooled a measure) and its
  `BodyFit` row; `SkinCatalogueTests` fails on a missing one.
- **`--dress-sheet`** shows every helmet skin over the basic suit, then over the suit of its set.

## Deliberately not handled

- No `_lit` pictures for the new suits: a regen suit under one shows no mode lights, as with the first body skins.
- Sets are only a naming match: nothing makes a player wear the two together, and any helmet goes with any suit.
- A crest or horn that would clear the box is shrunk with the whole helmet rather than drawn out of the box.
