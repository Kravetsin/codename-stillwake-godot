# Tufts in ink

- Status: accepted; under `--anime-art` until the switch ([the plan](../plans/anime-art.md)).
- Follows [ink and watercolour](2026-11-01-ink-and-watercolour.md).

On [a page](https://claude.ai/artifact/ExyoSKmH96m7wyANLtfdph) the user took three of four candidates at once: «Осока», wet blades laid over by wind; «Сухостой», dead jointed reed, some snapped; «Грибница», a parasite's stiff stalks out of black rot. «Гарь», burnt stumps, was rejected. A tuft is any of the three, at random.

**The plant is a look, not a kind in the sim.** A tuft stays one flora kind with one rule, so nothing about hitting, loot, saves or the hash of a world changes. `FloraLooks.Drawn` picks the picture from the hash of the plant's cell, so every peer and every load shows the same plant; `Look.Ink` names what else a kind may be drawn as. Its shape and its light are still read from the logical `tuft_N`, so each drawing keeps that picture's size and has its lit tips on the same pixels. Without the flag only the pixel picture is shown.

**Per cell, not in stands.** As asked. Dead reed beside wet sedge is a mix no real meadow has; stands would be a coarser hash, should it read badly in play.

## Deliberately not here

- **A tuft's name or sound by its plant.** All three are `tuft`: only the look differs.
- **Bushes, rocks and bells.** Still pixels: their own pick.
