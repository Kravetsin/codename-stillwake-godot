# The quilted helmet and traced art

- Status: accepted
- Date: 2026-11-14

## Context

Between the basic helmet's 45 air and the mafia's heavy bell's 100 ([heavy-helmet](2026-11-03-the-heavy-helmet.md)) there was
nothing a player could make. The user wanted a middle helmet sewn at the workbench. For its look the user tried a new way: a
GPT-made reference, traced by script into shapes, finished by our own engine; and picked that over our own drawings.

## Decision

- **The quilted helmet** (`Helmet.Quilted`, `quiltedHelmet`, «Стёганый шлем»): head slot, `Tank` 70, gear, worn as any helmet. Not
  sold (`Market.Unsold`); its worth is worked out from its craft (543).
- **Sewn at the workbench, no power**, of the basic helmet, 3 cloth, 2 plates and 2 canisters, in 120 ticks; in the bench's gear row
  beside the helmet lamp, jetpack and cape, the drill moved to the tools' row to keep four a row. No blueprint: the board's rule opens
  it once its parts are known, so with the ruins' cloth, after a first landing. **These numbers are placeholders** the user retunes.
- **Its look, as picked** (`trace_detailed`): a padded olive canvas hood of rolls, strapped to a welder's steel plate, its glass dark.
  The one flaw seen, a dark block at the visor frame's right edge, was a second piece of glass under the frame: a palette material
  may now be `single`, its smaller pieces given to their neighbours.
- **On the hero** as the heavy helmet: `hero_quiltedHelmet_shell` and `_visor` (76 × 72, on the `helmet` joint), the same traced
  shapes scaled to the head (15.6 px tall, front edge at 13: its last roll a collar, as the bell's breastplate), the glass the visor;
  the lamp on its crown (`HeadLampArt.OnOf`, 7.2, −3.4), the scanner's crest on the crown's upper hull (`ScannerInk.OnQuilted`). The
  head and every face are covered, the glass showing the face behind it at the visor's strength. Its card, `items/quiltedHelmet`, is
  the trace's own fit at 16 logical pixels.
- **Traced art** (tools/anime-art, Traced subjects): an AI-made reference is traced for its **form** only; the line, the paint and
  the water stay ours. `trace.js` cuts the reference by its own ink strokes into parts, names each by the palette's nearest
  material, traces them to beziers and writes `traced/<name>.json`, committed, so `export.js` draws from it and never reads the
  reference; `traced.js` places and paints the shapes with the engine's pen, palette and finish. Re-tracing is a separate command.
- **AI disclosure.** The reference (`tools/anime-art/refs/quilted.png`) is AI-made, and so the shapes of a shipped picture come from
  AI output: the game's Steam AI disclosure must say so.

## Consequences

- No lit eyes: neither the basic nor the heavy helmet has a `_lit` picture; only the catchers' do.
- The glass is a welder's narrow window at the front: it shows the far eye's corner and the brow, not the whole face as the bell's port.
  A bigger window would mean redrawing the picked form.
- The hood's crown is lumpy: the scanner rests on its hull, bridging the dips between rolls.
- A traced subject's palette (`refs/<name>.palette.json`) holds both what reads the reference (`ref`) and our paint (`mat`): paint is
  retuned without tracing again.
- The traced shapes are many (26 ops) against about 20 calls for a hand-placed icon; the picked look is kept whole rather than
  simplified.
