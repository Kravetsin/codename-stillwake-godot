# Crusher heard

- Status: accepted
- Date: 2026-11-08

## Context

Machines are heard where they stand while they work ([0059](0059-machines-heard-at-work.md)), but only the Former and the
electric smelter had sounds; the crusher, its rollers turning in their dust on screen, was silent. Played, it was missed.

## Decision

- **The crusher rumbles while it works**, a loop risen over 0.4 s and fallen over 0.8 s, as the smelter's hum: a quarry's
  crusher grinding rock into gravel, recorded (CC0), picked on its pick page over a synthesised rumble, a rock tumbler, a
  millstone and a concrete mixer.
- **No crunches over it.** The page offered stones crushed at random moments over the loop, as the smelter's melt pops; the user
  picked the loop alone.
- Its level is set as the other stations' (`Sound.StationReach`): the cue at −12 dB over a −23.2 LUFS file, about as loud as
  the charging station's hum (−10 dB over −25.3 LUFS).

## Consequences

- `StationSounds` hears a third kind; a station with a loop and no one-shots needs no shots player.
- The circuit assembler and the analyzer stay silent, as 0059 left them.
