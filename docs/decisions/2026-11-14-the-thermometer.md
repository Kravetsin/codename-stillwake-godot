# The thermometer: a frost star and the degrees, right of the meals

- Status: accepted
- Date: 2026-11-14
- Extends: [hud-vitals](2026-11-13-hud-vitals.md)'s block; built as step T-2 of
  [temperature-in-degrees](2026-11-14-temperature-in-degrees.md)

## Context

The cold is counted in degrees: a place has a temperature, a hero withstands down to a limit, and the degrees felt past that
limit cost air and draw the screen's frost. The plan asked for the reading in a corner, against the hero's limit, in the vitals'
"light and water" look. On a pick page of four candidates drawn at the game's scale through a day, a night, the caves and a
flight out, the user picked B, "number only", on 2026-11-14 over:

- A, a scale row above the air with a limit mark and a bead;
- C, a piece as long as the margin to the limit;
- D, an upright column left of the signs.

## Decision

- **Place:** right of the three meal slots, on their row: the sign centred at (124, 99) block units, the number from 134
  (`Vitals.ColdX`, `ColdY`, `ColdText`). It scales with the block.
- **Sign:** a six-armed frost star of pale ice light, a small fork on each arm (`sign_cold`, `tools/anime-art/subjects/signs.js`),
  at the oxygen sign's size, never tinted.
- **Number:** the degrees at the hero's body, a true minus, a plus above zero (`Thermometers.Reading`: −50°, 0°, +20°), in the
  mono face. Calm pale inside the limit. Past it, frost-blue, from light to deep as the frost deepens (`Thermometers.Frost`,
  felt over 30 eased as the screen's frost), with a faint glow behind it.
- **Past the limit, on the sign:** a cold light behind it, and rime creeping in from its edge, more and longer strokes the deeper
  the frost, laid over the sign.
- **Hover:** the reading, and «Выдерживает до −40°», the hero's own limit; red while the cold is felt.
- **Hidden** with no hero or passed out. Read four times a second, at the body's cell as the frost reads it.
- **Heat:** +60° shows the number only, with no heat colour, until heat (T-3) gives it one.

## Consequences

- While calm the number does not say how far the limit is; the hover does, and the frost-blue says when it is passed.
- The edge of the field is not degrees: its frost shows on the screen, and the thermometer stays calm there.
- A dev room, `--thermometer`, holds the checks: the planet's day and night (F7), its bands' caves (F6), a cape to wear.
