# The wheels and the edge keys are single ember threads

- Status: accepted; supersedes in part [hologram-keys](2026-11-05-hologram-keys.md) (the keys' green phosphor motifs and the suit's
  key) and [light-drone](2026-11-21-light-drone.md) (the F wheel's look); the threads in ember superseded by
  [drone-colour](2026-11-22-drone-colour.md)
- Date: 2026-11-21

## Context

Once the drone spoke only in embers ([drone-threads](2026-11-21-drone-threads.md)), the F and H wheels and the edge's keys
were the last green phosphor screens on the HUD, a second language beside it.

## Decision

- **One thread an icon**: the pick «Одна нить» (`tools/pick-kit/pages/thread-icons.js`, candidate `oneThread`, db doc
  `picks/threadIcons`), over «Тлеющий контур», «Пунктир углей», «Орбита» and «Жаркий штрих», ported whole. The F wheel's lamp and
  scan, the H wheel's yes, no and wave, and the edge's air, build, research, market and journal keys are each one continuous
  ember thread, with faint bridges between its parts (`ThreadIcon`), drawn in `DroneInk`'s ember.
- **States**: smouldering at rest; a spark running it under the pointer; walking it while on; standing while held; a dashed
  thread while the form is not known yet; a pulse while something waits. A spark kindles at the start of its run and dies out at
  its end, never popping.
- **The wheels** come in over 0.12 s; let go, the picked side flares 0.45 s as the rest goes in 0.2 s (`WheelFade`).
- **The polylines** are exported from `tools/anime-art/subjects/drone.js` into `IconInk`.
- **The suit's key leaves the edge.** The row is air, build, research, market, journal (`EdgeRow.Keys`); V still switches the
  suit's mode. The `holo_*` masks and `edgekeys.js` are deleted.
- **«Планшет» is «Изучения»**: the research key's name and its binding.

## Deliberately not handled

- One hue for every icon; the page's hue switch was for viewing only and is not saved.
- No icon for a form the wheel does not show (the alarms): the board shows it.
