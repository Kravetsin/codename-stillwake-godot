# 0059 — Machines heard at work, and the suit heard over space

- Status: accepted
- Date: 2026-10-18
- Supersedes in part: of [0054](0054-asteroids-and-the-drill.md), the drill's strikes stopping the very frame
  its bit leaves a piece and the drill silent while it runs at nothing; of
  [0057](0057-stations-one-model-for-every-crafting-machine.md), the Former and the electric smelter silent;
  of [0033](0033-first-sound.md) and [0053](0053-charge-belongs-to-the-item.md), each station's sound carrying as
  far as its own cue said; of [0042](0042-what-is-heard-in-space.md), the Suit bus as loud in space as aboard
  and the bed of space 8 dB under the title's track
- Audition: "Machine Sound Audition" (the user's picks, 2026-10-18)

## Context

The Former pressed, the smelter glowed and the drill spun without a sound; the drill's strikes cut in and
out at full level as its bit came and went, and broke into bursts along a piece's edge. Out in space the
jetpack could not be heard at all: measured in Godot, its hiss (700 Hz to 4.8 kHz) came through the Suit
bus's muffle (a 900 Hz low-pass at resonance 0.5, four stages: about −3 dB at 400 Hz) 28 dB down, at
−53 LUFS against the bed's −28; a canister breathed fell 42 dB; the chop, the harpoon and the drill's
strikes sat 9 to 13 dB under the bed.

## Decision

- **Machines sound where they stand, while they work** (`CraftingModule.Works`, what their pictures show): the Former's press strikes each time its head
  comes down in its pictures (the third of four, every 0.44 s), counted from the same steps the pictures are,
  so a slow frame still strikes once; over it its motor, a synthesised flywheel loop, spins up over 0.6 s
  from half its pitch, runs down over 0.8 s and dips 3% at each stroke. The smelter hums, risen over 0.3 s
  and fallen over 0.6 s, and its melt pops at random moments, two a second on average.
- **The drill's motor is heard whenever it spins**, aboard or at nothing, as grains of a real drill running
  free, nine a second at speed, their rate and pitch following the motor's speed, spun up and down with the
  bit's picture (`HeldLook.SpinUpMs`). With its bit on a piece it sags 8% and quietens 3 dB under the
  strikes, which play on as before. The bit counts as on a piece for 0.1 s after it last was, so grazing an
  edge strikes on rather than in bursts; letting go stops the strikes at once and the motor runs down.
- **Repeats, not long loops** (the user's rule): every recorded sound here is a one-shot played again with
  its pitch varied; the steady parts are synthesised loops, rendered offline from the audition's parameters
  (deterministic seeds, two periods rendered and the second kept, −23 dBFS RMS). One motor model serves the
  Former and the drill (`MotorSound`).
- **Every station sounds alike: it tells that it works, no more.** One profile (`Sound.StationReach`) for
  every sound a station makes at work — the Former's stroke and motor, the smelter's hum and melt, a furnace's
  or burner generator's fire, a charging station's hum: the World bus, full within 2 cells, gone at 10, fading
  as the square of the way left. Their volumes are set so each station at work peaks at about −32 LUFS
  momentary (its loudest 400 ms, measured through Godot's mixer within 2 cells), a quiet background: under a
  step (−29), a door (−23) and every tool. Measured, momentary / short-term (3 s) LUFS:

  | Station | Before, 2 cells | Before, 6 cells | After, 2 cells | After, 6 cells |
  | --- | --- | --- | --- | --- |
  | Former (stroke + motor) | −20.4 / −22.3 | −23.7 / −25.7 | −32.6 / −34.1 | −44.7 / −46.2 |
  | Electric smelter (hum + melt) | −32.0 / −34.2 | −40.0 / −43.0 | −32.0 / −34.1 | −44.0 / −46.3 |
  | Furnace, burner generator (fire) | −28.9 / −30.3 | −35.9 / −37.3 | −31.9 / −33.3 | −44.0 / −45.3 |
  | Charging station (hum) | −30.0 / −30.2 | −49.1 / −49.4 | −32.0 / −32.2 | −44.0 / −44.3 |

  The Former's stroke is judged by its momentary loudness, not its average: it plays only 3 dB over its motor
  (the audition had 12) and 13 dB under the audition's level, so the press is heard working, not pounding.
  The smelter keeps the audition's balance (its hum 4 dB under its melt); the drill's motor, a tool, keeps
  the audition's 6 dB under its strikes. The circuit assembler and the analyzer stay silent.
- **The vacuum is never left:** everything the hero touches out in space stays dulled through the suit,
  the jetpack too. Its hiss, mostly above what the muffle lets through, is played 10 dB louder at the source
  (+3 dB), so dulled it is heard about 6 dB under the bed of space: −53 → −37 LUFS against the bed's
  −28 → −31. Floating, the Suit bus rises 6 dB as it dulls, and the bed plays 3 dB lower, so what the hero
  touches stands out; the glove's thump, which the rise made the loudest thing out there, plays 6 dB lower
  to stay where it was (−26 LUFS momentary). A chop in space −36 → −30.

## Consequences

- Others' drills stay silent, as before; a machine taken down mid-work stops at once, without running down.
- A vent's hum (about −30 LUFS within 3 cells) is a little louder than any station: a room with both hears the
  vent first. The vents are the ship breathing, not a station; left as they were.
- A canister breathed is a hiss too, and in space it is still all but lost in the muffle (−62 LUFS): it is
  breathed mostly aboard, where it plays clear; left for its own audition.
- The jetpack played that loud would be the loudest thing aboard, but it never thrusts aboard.
