# Pipes in ink

- Status: accepted.
- Follows [ink and watercolour](2026-11-01-ink-and-watercolour.md); changes how [0110](0110-the-plan-has-a-focused-layer.md) draws a span.

On [a page](https://claude.ai/artifact/Emx3DSirSYMZj2eRiMi11y) the user took «Клёпаная сталь» of five candidates, asking for 4 px in place of its 5: bare grey steel, a riveted seam, a collar every 24 px, a rust patch at one rivet, bolted flanges.

**A span repeats a picture.** `pipe_run`, a sheet of 24 by 8 px, is tiled along a span at any angle, where the pixels' pipe was a colour per row. `TubeRun` draws a span from whichever end turns the sheet's lit edge to the world's light, or half the pipes would be lit from below. `-- --pixel-art` keeps the rows, till the pixels go.

**The art moves to the ports; the ports stay.** They are sim data: moving them would break saves and every laid span. The stubs drawn before stood off them, the cistern's 5 px above, and three machines had none, so a pipe met nothing. Now `fitting` in `machines.js` puts a bolted plate on each port's middle and a neck out to the picture's edge. `PipeArtTests` holds each picture to `Machines.Ports`.

## Deliberately not here

- **A pipe drawn over its machine.** Pipes lie behind machines. A span leaving a port across its own machine runs behind the body and shows from the far side: the plate says where it goes in.
- **The pipes overlay.** The plan's coloured lines and sockets speak by colour, not by material.
- **A pipe's picture per fluid.** Air and water look alike until the overlay is open.
