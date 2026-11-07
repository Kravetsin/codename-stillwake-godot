# A tool's rays pass through wrecks and hit all they cross

- Status: accepted; supersedes, in part, "one wreck a ray" of [tool-reach](2026-11-05-tool-reach.md)
- Date: 2026-11-07

## Context

After tool-reach each ray stopped at the first wreck whose pixels it crossed. A wreck lying on or behind another, inside the
weapon's fan, took no hit, while two wrecks side by side were sometimes both hit by different rays. The user expects every
object inside the weapon's zone to take the hit.

## Decision

- A ray walks on to the first cell that stops a body, as before, and strikes **every** wreck whose drawn pixels it crosses on
  the way (`ToolRays.Fan`), each touched (sparks, sound) where a ray first meets it. Each wreck takes the hit once a stroke.
- Creatures are struck along the whole walked stretch, so a wreck no longer shields one behind it. Walls still do.
- The middle ray's end, where a stroke is seen landing, is where it first meets a wreck, else where it stops.

## Consequences

- A stroke into a cluster of wrecks breaks them all down together; the sledgehammer, which bursts wrecks of 12 hit points or
  fewer at once, clears a whole cluster in its fan.

### Deliberately not handled

- No shielding by wrecks: a hero cannot hide a creature behind debris. Walls are what stop a hit.
