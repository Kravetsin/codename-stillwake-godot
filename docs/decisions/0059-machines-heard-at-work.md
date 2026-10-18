# 0059 — Machines heard at work, and the suit heard over space

- Status: accepted
- Date: 2026-10-18
- Supersedes in part: of [0054](0054-asteroids-and-the-drill.md), the drill's strikes stopping the very frame
  its bit leaves a piece and the drill silent while it runs at nothing; of
  [0057](0057-stations-one-model-for-every-crafting-machine.md), the Former and the electric smelter silent;
  of [0042](0042-what-is-heard-in-space.md), the jetpack and a breathed canister on the Suit bus, the Suit
  bus as loud in space as aboard, and the bed of space 8 dB under the title's track
- Audition: "Machine Sound Audition" (the user's picks, 2026-10-18)

## Context

The Former pressed, the smelter glowed and the drill spun without a sound; the drill's strikes cut in and
out at full level as its bit came and went, and broke into bursts along a piece's edge. Out in space the
jetpack could not be heard at all: measured in Godot, its hiss (700 Hz to 4.8 kHz) came through the Suit
bus's muffle (a 900 Hz low-pass at resonance 0.5, four stages: about −3 dB at 400 Hz) 28 dB down, at
−53 LUFS against the bed's −28; a canister breathed fell 42 dB; the chop, the harpoon and the drill's
strikes sat 9 to 13 dB under the bed.

## Decision

- **Machines sound where they stand, while they work** (`CraftingModule.Works`, what their pictures show), on
  the World bus, heard by reach as every machine is (0033): the Former's press strikes each time its head
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
- **Levels as auditioned:** the audition's 0 dB plays at −2 dB in the game, set by the one sound it shared
  with the game, the drill's strike. So the Former's stroke is at −2, its motor and the smelter's hum 12 dB
  under, the melt 8 dB under and the drill's motor 6 dB under its strikes.
- **The suit's own air is heard in the helmet:** the jetpack's thrust and a canister breathed are the suit's
  air moving, as the alarm is in its air (0042), so they go on the Helmet bus, never muffled. Floating, the
  Suit bus rises 6 dB as it dulls, so what the hero touches stands out, and the bed of space plays 3 dB
  lower. Measured as played: the thrust in space −53 → −25 LUFS, a breath −68 → −26 (momentary), a chop
  −36 → −30, against the bed's −28 → −31 LUFS.

## Consequences

- Others' drills stay silent, as before; a machine taken down mid-work stops at once, without running down.
- The hums sit about 5 dB under a vent's; a vent beside a smelter covers much of its hum, not its pops.
- The jetpack and a breath sound the same aboard and out; the jetpack only ever thrusts out.
