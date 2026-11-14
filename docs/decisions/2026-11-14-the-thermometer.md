# The thermometer: a frost star and the degrees, a row under the charge

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

- **Place:** a row of its own under the charge, over the meals: the star in the signs' column, the number where the rows'
  pieces start (`Vitals.ColdY`, 82 block units). The air and the charge rows went up 23 and 24 units for it (`AirY` 19, `ChargeY` 56),
  so the block is that much taller. The user moved it there from beside the meals after trying it in the game.
- **Sign:** a six-armed frost star of pale ice light, a small fork on each arm (`sign_cold`, `tools/anime-art/subjects/signs.js`),
  as large as the oxygen sign, never tinted; the number at 14, larger than the pieces are tall.
- **Number:** the degrees at the hero's body, a true minus, a plus above zero (`Thermometers.Reading`: −50°, 0°, +20°), in the
  mono face. A new reading is glided to, eased out, through the whole degrees between (`Thermometers.Glide`, some 1.4 s
  for 60°): the user found a jump from +20° straight to −40° on stepping into space odd. The sim stays in whole degrees; only
  the drawing glides, and the frost-blue follows the number shown. Calm pale inside the limit. Past it, frost-blue, from
  light to deep as the frost deepens (`Thermometers.Frost`, felt over 30 eased as the screen's frost), a faint glow behind.
- **Past the limit, on the sign:** a cold light behind it, and rime creeping in from its edge, more and longer strokes the deeper
  the frost, laid over the sign.
- **Hover:** the reading, and «Выдерживает до −40°», the hero's own limit; red while the cold is felt.
- **Hidden** with no hero or passed out. Read four times a second, at the body's cell as the frost reads it.
- **Heat:** +60° shows the number only, with no heat colour, until heat (T-3) gives it one.

## Consequences

- While calm the number does not say how far the limit is; the hover does, and the frost-blue says when it is passed.
- The edge of the field is not degrees: its frost shows on the screen, and the thermometer stays calm there.
- A dev room, `--thermometer`, holds the checks: the planet's day and night (F7), its bands' caves (F6), a cape to wear.
