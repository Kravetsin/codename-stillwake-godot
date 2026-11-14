# Held tools ship their picked reference's own paint

- Status: accepted; supersedes how the tools of [fire axe](2026-11-14-fire-axe-redrawn.md), [drill](2026-11-14-drill-redrawn.md),
  [pickaxe and harpoon](2026-11-14-pickaxe-and-harpoon-traced.md), [railgun](2026-11-14-railgun-traced.md) and
  [grappling gun and nailer](2026-11-14-grapple-and-nailer-traced.md) are drawn; their picks stand
- Date: 2026-11-14

## Context

Held tools were redrawn from picked AI references by tracing: the reference cut into parts by its ink, each part repainted in a
palette material, our line and water over it. Each took a palette tuned by hand, mends for what the trace lost (the grapple's drum,
the harpoon's spring window, the railgun's lamp), and code to draw those back.

A tool is small in the hand, and on its card it is shown up to five world pixels a pixel
([tool-card-pictures](2026-11-14-tool-card-pictures.md)). Put side by side at those sizes, the reference itself, cut out and scaled
by the same fit, read as well as the trace, with finer paint; the user: "так даже лучше выглядит". Without a line round it, it
went faint in the hand: the reference's own outline is thin and vanishes when scaled down ten times.

## Decision

- A held tool's palette says `"raw": true`. `trace.js` stops at the silhouette (background cut from the edge, `holes` left open,
  `mend` with the background still cutting out what code draws) and writes the reference's pixels inside it, `traced/<name>.png`,
  and its box. The box is the trace's own, so every `HeldLook` point measured on a trace stays where it was.
- `Traced.raw(name, s, at)` places it as `placed` places a trace. The wash draws it as it is: our ink laid round its silhouette as
  wide as a shape's outer pen, and no water over it. What code draws over it gets the water as before.
- Code draws only what moves, lights up or leaves the tool: the drill's bit, the railgun's coils charged, the harpoon and the nail
  loaded, the grapnel's shank and eye in flight. The grapple's drum, the harpoon's spring window and the railgun's lamp are the
  reference's now.
- The next tools (grinder, plasma cutter, spear, sledgehammer, blast charge) go the same way: references, a pick, a raw palette.

## Consequences

- A picked tool reaches the game in minutes: a palette of a few lines, its fit and its grip.
- The tools' paint is GPT's, not ours: their colour and wear follow the reference. On a tool that small the pick is the style
  check; the outline keeps the silhouette in our ink.
- The Steam AI disclosure covers them, as it did the traced art.
- A reference's background must stay flat: the cut needs it. Codex's image tool cannot give a transparent background.

## Deliberate omissions

- Worn gear, helmets and suits stay traced: they sit on the hero beside drawn parts, where a second style would show. Not
  revisited until a raw piece is tried on the body.
- Moving parts cut from the reference beyond the grapnel (a disc, a drum) wait for a tool that needs one: the grinder.
