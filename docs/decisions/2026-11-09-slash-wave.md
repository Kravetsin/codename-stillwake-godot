# Slash wave

- Status: proposed (a prototype on its own branch, over axe-combos, to be played and kept or dropped whole); supersedes in
  part [tool-reach](2026-11-05-tool-reach.md): a swing no longer strikes its whole fan at once
- Date: 2026-11-09

## Context

The fire axe strikes 36 px from the shoulder, its blade reaches about 26: the last 10 px were struck by nothing seen. Every
look of the air cut tried (the dust, and five candidates on the «Рассечение воздуха» page) showed that reach beside the
blade, ahead of it or detached. The user rejected them all as the same thing drawn differently, and proposed that the
swing send out a wave that does the damage and is gone where the reach ends; a shorter reach (only what the blade touches)
was set aside, as digging in caves would turn awkward.

## Decision

- **A swing's hit is a wave** (`SlashWave`): at the hit the hands start one from where the hero stood, its fan's heading and
  tool; each frame its fan of rays (`ToolRays.Fan`, now with a reach) is cast out to where the wave is, and what it reaches
  that it has not struck is sent (`HewCells`, `HewPieces`, strikes), each cell, wreck and creature once. It starts
  `WorkLooks.WaveFrom` (0.72) of the range out, about the blade, and runs to the range in `WaveMs` (90 ms), slowing.
- **The arm stops 50 ms (0075) only on what the wave strikes as it is born**; a later strike stops nothing.
- **Seen** (`AirCuts.Wave`): a pale crescent across the fan, born at the blade as it hits, running out as the wave does and
  fading to nothing at the range: where it is gone is how far the swing reaches. The swing's dust is gone; a thrust keeps its.
- Only the client changes: the sim takes the same commands, split over the wave's frames.

## Consequences

- A hit lands up to 90 ms after the swing's, later the further it is: wanted.
- **Not handled (a prototype):**
  - The crescent hangs from the figure's shoulder, so a hero moving drags it a pixel or two from where the wave strikes.
  - The wave's start is a share of the range, not each weapon's blade: right for the fire axe, near for the others.
  - A friend's swing shows its wave but its ground ring sounds at once, as before.
  - Catchers' blows are the sim's and send no wave; only their crescent is drawn.
