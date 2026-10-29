# Kills crumble into pixels

- Status: accepted; built
- Date: 2026-10-29
- Supersedes the burst of [kill-feel](2026-10-29-kill-feel.md)

## Context

A kill burst its creature into its pixels at once, 100 ms after it was held white. The user, having played it: rather the
creature freezes and crumbles into its pixels over a second or two.

## Decision

- **Every killed creature stands as its own pixels and crumbles from the top down** (`PixelBursts.Crumble`): after the
  white hold it is drawn no more, its pixels hanging where it was drawn, each breaking off after `PixelDust.WaitOf` its
  row: the top at once, the bottom `KillLooks.CrumbleS` (1.5 s) later, each up to `Ragged` later besides, for a ragged edge.
  Breaking off, a pixel flares lighter for `RimS` and falls slowly a little out from the middle and away from the striker,
  heaping as before; afloat it drifts so. A hanging pixel neither ages nor moves; its time to fade counts from its break.
- **Its pixels are sampled from the world's side** (`PixelBursts.Paint`): every world pixel a posed picture covers takes the
  colour under its middle, so a creature drawn larger or turned stands without gaps. All of them, up to `BodyPixels`.
- The body's speed is no longer carried, and the tufts are gone: they burst; the hit and the puff stay. The tick keeps its
  cracks, sparks and flash, then crumbles alike. Loot sprays out as the crumble begins, as before.

## Deliberately not handled

- A creature killed in a leap crumbles where it hung: hanging pixels do not fall as a body.
- Its glow, lights and eye go dark as it turns to pixels; a turned part snaps to whole world pixels.
- Falling pixels pass through the hanging ones.

## Consequences

`KillLooks.CrumbleS`, `Ragged`, `RimS`, `RimWhite`, `BodyPixels` in the client's content; `PopPixels` gone, `MostPixels`
16000. Client only: the sim and its saves are untouched.
