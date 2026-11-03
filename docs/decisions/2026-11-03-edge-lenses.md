# The tablet edge's keys are frosted lenses, and the journal has one

- Status: accepted
- Date: 2026-11-03
- Changes the glyphs of [tablet-edge](2026-11-02-tablet-edge.md)'s keys and [the-regen-suit](2026-11-03-the-regen-suit.md)'s; drops its canister key, R stays; adds a key for the Handbook (0066)

## Context

The edge's keys for air, build and the suit's mode were rectangles of the old pixels beside the research key's painted lens, and the game is no longer
drawn in pixels (pixels-unsupported). The Handbook opened only by J, and players did not find it. A canister is breathed by R, a right click on one held,
or from the inventory, so its key on the edge was a fourth way to do it.

## Decision

- **Every key is a frosted lens, as the research key's.** The user picked it from four treatments on a page drawing every key in every state at the
  game's size: lenses, an ink line on paper, an engraved plate and enamel. Its motif is a dark shade in the glass, a little soft as through ground glass.
- **The motifs, as picked:** lines of wind for air, three courses of bricks for build, the `vest` for the suit's mode (a suit with a round lamp port on its
  chest) and the open book for the journal (two pages from the gutter, two lines on each). They are drawn as curves, not a grid of pixels.
- **States:** under the pointer the glass is brighter and cooler; held, the shade sinks a pixel and the glass dims; on, it is lit sky from within, and
  the suit's port and the journal's lines let the light through; refused, the glass greys and the shade stays readable. Amber stays the research key's.
- **The journal's key** opens and puts away the Handbook as J does, lit while it is out. The edge is air, build, research, suit, journal: still five.
- **The canister has no key**: R, the right click and the inventory breathe one.
- **The pictures** are `key_<air|build|suit|journal>_<state>`, exported by `tools/anime-art/subjects/edgekeys.js`; `EdgeKey` draws the one for its state.

## Deliberately not handled

- **A watercolour finish:** none, as on the research lens; at 22 pixels it would not show. Only the glass's grain and two scratches, placed per key.
- **The window's scale:** the keys are drawn at their size in screen pixels, as the other wrist keys are.
- **A key per overlay:** the pipes and cables overlays still come with their build tabs, as tablet-edge has it.
