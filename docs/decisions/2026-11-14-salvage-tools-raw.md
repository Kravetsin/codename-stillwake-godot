# Cutter, spear and sledgehammer drawn raw

- Status: accepted; amends the looks of [salvage-tools](2026-11-04-salvage-tools.md); the cutter's pick superseded by
  [homemade-cutter](2026-11-14-homemade-cutter.md)
- Date: 2026-11-14

## Context

Three of the four salvage tools still had their first, code-drawn pictures. Under [raw tool
pictures](2026-11-14-raw-tool-pictures.md) each got a page of GPT references and the user picked one. The spear takes the arc
module ([0098](0098-the-tool-bench-modules-in-tools.md)), whose look was the drill's coil laid on a mount: a hand-drawn part on a GPT-painted
tool would look stuck on. The user: "можно попросить GPT сгенерить сразу с отдельным модулем дуги, как он его видит".

## Decision

- **The picks:** the plasma cutter its current look redrawn, a dark body banded in brass, three copper rings on a rail before the
  nozzle (the user leaned to a plainer one for the jet's sake; the jet is code and already runs through rings, so the rings stay);
  the spear «Телескопическое», a three-part aluminium pole with twist locks, a foam grip and a lashed blade; the sledgehammer its
  current look redrawn, five plates bolted into a block on a wooden handle. All three raw.
- **The cutter's rings light in code:** running, each ring's inner face burns violet over the raw picture
  (`hero_plasmaCutter_on`); the jet runs from the nozzle to the last ring's far side.
- **A module comes from an edit of its tool's reference.** Codex edits the picked reference, the module added as it sees it, on a
  transparent background; the palette's `over` keeps only the pixels that differ from the tool's ref within `clip`, closed and
  filled into one part, and writes it at the tool's box. The spear's arc (`hero_spear_arc`): a coil box between the locks, a cable
  to the blade, the blade's edge alight.
- **A mount may carry its own picture of a module** (`ToolMount.Own`, by the shared overlay's picture): the spear's coil mount lays
  `hero_spear_arc` at its origin in place of `module_arc_coil`, and crackles over the blade. The drill keeps the shared coil.
- **References come transparent.** Codex's image tool gives a real alpha channel when asked; a palette without `background`
  cuts by alpha. The spear's arc is the first; older refs keep their flat backgrounds.
- The sledgehammer's head sits nearer its handle's line than the old one's, so the heave aimed down swings back 0.1 rad less,
  keeping the arm within the shoulder.

## Consequences

- A module that shows on a GPT-drawn tool costs one edit and a palette of three lines per tool, not a hand drawing.
- The edit drifts a pixel or two of the 1536-px reference from the tool (about 0.07 world px): unseen.
- Each further tool that takes the arc needs its own edit, or it falls back to the shared coil at its mount.

## Deliberate omissions

- The grinder waits for references showing the whole disc: the first set lay it edge-on.
- Only one arc edit of the spear came through; no page of alternatives was made.
