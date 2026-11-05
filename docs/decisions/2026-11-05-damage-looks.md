# Damage and work drawn in ink

- Status: accepted
- Date: 2026-11-05

## Context

The cracks on wrecks and dug blocks, and the axe's swipe of light, were left from the pixel art (0034, 0077, 0075), out of
place in the ink and watercolour the game is now drawn in. A running tool's stream and the plasma cutter's jet had a placeholder
look (tool-contact). The user picked a new look for each of the five on a page of candidates drawn in code at the game's scale.

## Decision

All five ship as picked, drawn by one Godot-free ink (`render/logic/Ink.cs`): a pen's stroke that swells and thins as a nib does,
a ragged blob, a band, as triangles with their edges faded over a screen pixel, in one draw call (`InkCanvas`). The page's
seeded randomness is ported bit for bit, so a look laid out by a seed is the one the page showed.

- **A wreck's cracks, "penDent"** (`WreckCracks`): a dent where each blow lands and six wandering pen lines out of it, each over a
  thin lit line, all growing over the next 0.45 of its hit points lost after that blow. A blow lands under a drill's bit, else on
  the side facing the nearest hero. One within 3 px of an earlier one deepens it; a wreck shows at most 8. Drawn over the picture
  as its child, lit as it, clipped to its pixels (`ClipChildren`), redrawn only when its damage or the zoom changes.
- **A dug block's cracks, "fault"** (`BlockFault`): one ragged split across the whole cell, a hairline, then thicker with a
  branch, then a second branch and the split opened into a dark gap with a lit lower lip; in stone's or scrap's ink, each cell
  laid out by its own seed and kept within it.
- **A swing's trail, "flicks"** (`SwingTrail`): three pale ink speed lines after the blade, the outer longest, fading after the
  downstroke. Built from where the edge really was over the last 2.2 downstrokes' time, frame by frame, turned round the
  shoulder between frames, never from an angle: it stays on the blade aimed any way, straight up too.
- **A running tool on a wreck, "sparks"** (`ToolSparks`): thin streaks thrown 150 a second, white-hot cooling to orange and red,
  back out of the face along the tool's way; the grinder's a tight fan down off its wheel; a white bead at the point. On
  anything else the stream stays the hit look it touches (tool-contact).
- **The cutter's jet, "beam"** (`CutterJet`): a flat violet sheath round a white heart, its width flickering, thinning to a point
  at nothing; where it cuts, a small splash, white in a violet ring. It leaves the copper nozzle and runs through the three coils,
  as on the page, never out of the last one; pressed right on a wreck, it cuts at the last coil.
- **On a wreck, where its picture begins.** The sim strikes a wreck by its box, wider than its picture; the client works the way from
  the hand on past the box's edge to the picture's first solid pixel (`Aiming.FirstSolid`), so sparks, the jet's end and a drill's
  blow land on the wreck, not in the empty corner of its box.

The pixel crack shader goes, and with it the debris' instance uniforms (`Lit` has one shader again).

## Consequences

- Blows landed on a wreck while it is out of view are not seen: its cracks out of the blows seen grow on. One first seen
  damaged, after a load or out of view, is struck in its middle.
- A wreck's picture with cracks is drawn through a clip group: a little more GPU per cracked wreck in view.
- Deliberately not handled: a crack clamped, not clipped, at its cell's edge (it overruns by half a pixel at most); sparks off
  the ruins' alloy, which keeps its own hit look.
