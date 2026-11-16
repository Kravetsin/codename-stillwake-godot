# The crew shuttle stands on a launch platform

- Status: accepted; built (only built: boarding, fuel and flight are to come)
- Date: 2026-11-15

## Context

The crew flies to the vessels the radar finds rather than the ship jumping there
([vessel-worlds](2026-11-09-vessel-worlds.md)), so it needs a craft of its own: one that visibly takes four, burns biofuel
put in by hand as the drop pod does, and leaves the ship and comes back. Its first slice is a machine the player builds,
picked in two rounds on pick pages: where it stands, then how it looks.

## Decision

- **It stands on a launch platform**, the user's pick of three (a platform as the drop pod's, a side hangar with a gate in
  the wall, a docking clamp outside): it reuses the platform, its shaft, its power and the pod's launch, and a hangar gate
  or a clamp would each be a new mechanic. It is the `Lands` trait the drop pod has, so the grid's one rule covers it: the
  whole row under its 24 columns a run of platform pieces, open or shut (`grid.noPlatform` otherwise).
- **Machine `crewShuttle`, 24 cells by 10**, on the ship tab beside the drop pod, of frames, plates, glass, tubes, three
  nozzles, two processors, a display, twelve copper and six magnetite (added 2026-11-15, the user's word). The nozzles
  are a planet's, as the engine's are: it opens after a landing, as the radar that finds where it flies does.
- **Its look is «Ледокол»** (H of the second round; the first round's four were all turned down): a heavy prow of worn red
  plates over a grey ice belt, a stack of three stubby thrusters at the stern, two keel blocks on the platform. Its glass is
  four frosted slits nobody shows through, the user's rule: the crew is never seen through it; when it is boarded the slits
  will light warm one by one. Drawn by `tools/anime-art/subjects/shuttle.js`, its body only for now.
- **The testbed** shows it in a hangar off the airlock's outer door, a row above the ship's roof (the shuttle is 10 cells
  tall), with a lamp of its own, a door to space and no air, as the airlock: machines outside the hull have no light but a
  lamp's, so in the open it stood in the dark.

## Deliberately not handled

- **Boarding, the fuel slot, the launch and the flight to a vessel** are the next slices: no seat (`landing.noPod`), no slot.
  (Boarding and the flight: [shuttle-flight](2026-11-16-shuttle-flight.md); fuel waits for fuel-liquid.)
- **A run longer than the shuttle** takes it anywhere along it; nothing centres it.
- **Its lit states** (lamps, slits, flame) are drawn on the pick page, not exported yet: nothing shows them.
