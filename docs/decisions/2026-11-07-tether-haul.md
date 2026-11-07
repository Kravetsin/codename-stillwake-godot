# Hauling in along the tether

- Status: accepted
- Date: 2026-11-07

## Context

A hero out in space without a jetpack gets back only by swimming, 3.6 cells a second: up to 7 s from the tether's 24-cell
end. The tether held the hero out (0007) but never helped it home. The jetpack (0047) already crosses space; this is the way
back for one who has none.

## Decision

- **Shift held**, afloat on a tether with no jetpack worn, hauls the hero in at `Hero.Haul`, 12 cells a second, keys aside:
  slower than a tool's pull (the harpoon's 20, the grappling gun's 30), a pull by hand. From the tether's end to the door in
  2 s. Letting go stops it, as letting go of the keys stops a swim. It costs nothing: the suit's air stays the clock.
- **Shift is the space key**: a jetpack worn, it thrusts as before; with none, it hauls. Its label: "Sprint, jetpack, haul in".
- **Only once Shift was let go since the tether was tied** (`Hero.HaulArmed`): a hero that runs out of a door with Shift held
  is not hauled straight back in. Set at the end of `Step`, after the tether, so no move's own `Hero` loses it.
- **The haul goes the way the hero went** (`Hero.Wraps`). Played straight at the anchor, a hero that swam out of a door and up
  over it was pressed onto the roof's lip, its body too wide to pass the door's corner on the diagonal. Each step on a tether,
  a bend of the line the body now passes straight from the bend before it (or the anchor) is let go, and where the body cannot
  pass straight to the last bend, the line bends where its middle was a step before. A haul makes no bend: along a line that
  just grazes the hull, it would send the hero back to the bend it had just let go. The haul pulls toward the last bend, then on.
  "Passes" is the body's box looked at each half cell along the way. At most 32 bends.
- **It comes in at speed**, as off the harpoon's line: aboard, the tether comes off and the feet brake it in about a cell.
- **What wins**: the grapple's pull; a jetpack; rammed, passed out or seated (no Shift reaches the step). A gun's kick is
  taken by the hands on the line. The tool in hand still works.
- **Hand over hand** («Перехват», picked by the user from three drawn by the game's own figure on a published page, over a rowing
  stroke of both hands and a head-first pull): afloat the arms by turns reach out along the way the body goes and draw back
  past the chest, a grip each `Poses.Haul.GripMs`; the legs trail, the body leans in as a fast swim does. The tether is still drawn
  from the air pack.
- **Nothing on the wire or in the sim** (0002): the haul is the controller's, and others see the hero move as it is sent. Its
  figure (`Poses.Hauling`: on a tether, faster than a swim) every peer reads from what it has.

## Consequences

- A door shut between the hero and its anchor, or a wall built across the way back, stops a haul flush against it; a swim
  frees it. Wound round the hull past 32 bends, a haul may stop at the hull too.
- The tether is still drawn straight to its anchor, through the hull's corners, and kept within its length of the anchor in a
  straight line: the bends are only the haul's way.
- A kick or a harpoon's pull toward the ship on a tether is drawn as a haul while it lasts.
- Only the label tells a player that Shift hauls.
- Dev: `--haul` starts the testbed's hero in the airlock, no jetpack, creative.
