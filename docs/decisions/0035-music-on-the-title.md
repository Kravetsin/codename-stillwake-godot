# 0035 — Music on the title

- Status: accepted; "nothing plays in a world yet" superseded by [0041](0041-what-is-heard-in-space.md)
- Date: 2026-10-16

## Context

The title was silent. Synthesised drafts were tried first (`Drift`, `Wreck`); the team then made the
title's track itself with Suno. Suno's free plan leaves a song Suno's, for non-commercial use only, and
the repository is public; ADR 0033 allowed CC0 sounds alone.

## Decision

- **A Music bus** beside UI, Ambient and Effects, the settings' fifth volume.
- **The title's track** plays while the title is shown: faded in over 2.5 s, out over 2.5 s on entering
  a world, looping whole (it fades in and out on its own). `SoundView` owns its player, reading whether
  the title is up; nothing plays in a world yet.
- **The team's own music is allowed beside CC0 sounds,** credited in `game/audio/CREDITS.md`. A track
  made on Suno's free plan is marked `Suno Basic, non-commercial: replace before sale`, a licence the
  credits test accepts by name, so every such file stays listed: before the game is sold, or used where
  it earns money (a monetised devlog), it is made again on a paid plan or replaced.

## Consequences

- The title's track is a placeholder until the team owns it outright.
- Music in the world, by place or by moment, is a later decision.
