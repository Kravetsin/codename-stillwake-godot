# The drone of threads: a coal core and sparks, no plates and no holograms

- Status: accepted; supersedes in part [light-drone](2026-11-21-light-drone.md) (its look «Пепел в латах», the plates, a
  gesture's hologram), [drone-frame](2026-11-21-drone-frame.md) (the motes' cloud, the net), [drone-meeting](2026-11-21-drone-meeting.md)
  (the plates arriving) and [steady-scanner](2026-11-07-steady-scanner.md) (the readout's digits); the spyglass superseded by
  [scan-form](2026-11-21-scan-form.md), the windows' ember squares by [drone-windows](2026-11-21-drone-windows.md),
  all of it in ember by [drone-colour](2026-11-22-drone-colour.md)
- Date: 2026-11-21

## Context

The drone wore four scrap plates over a cloud of soot and spoke in a second language: cyan holograms for gestures, green
phosphor digits over a scanned wreck, a mote cloud while it projected a window. The user wanted it to speak with its own
body, and the drone's thoughts (warnings as pictures by it) needed one shape language to add to.

## Decision

The look is the pick «Нити» (`tools/pick-kit/pages/drone-embers.js`, candidate `threads`; the scan gauge `dots`), over
five all-ember bodies (swarm, grid, threads, tiles, haze) and an earlier page of five hologram looks
(`tools/pick-kit/pages/drone-thoughts.js`). It ships whole, exported by `tools/anime-art/subjects/drone.js` into
`game/shared/logic/DroneInk.cs`.

- **Body** (`logic/DroneSwarm.cs`, drawn by `LightDrone.cs`): a coal core with an ember eye; three faint orbit threads,
  sparks with tails running round them. No plates.
- **Shapes are data**: polylines in the drone's frame, where they stand, their share of sparks. The lamp's mirror, the
  scan's spyglass and the gesture's fan are the first; sparks fly to a shape along straight threads from where each is and
  flow along it. A window's frame outranks a gesture, a gesture the mode. The thoughts add glyphs to the same table.
- **Gestures**: the sparks' fan above the drone, turned toward a pointed target. No hologram (`DroneHolo` deleted).
- **Windows** stay screens inside. Sparks run from the corner nearest the drone round the edge and land as two rows of
  ember squares that shimmer; the screen shows after. One clock runs up open and down closed, so a reopen mid-flight never
  jumps. The motes' cloud and the net are gone.
- **The scan gauge**: a row of 26 ember dots over the wreck, dot j lit `clamp(left / (max / 26) − j, 0, 1)`; sparks fly
  from the spyglass's mouth to them; on a hit the lost dots burst off as falling embers; the row reddens with the drone's
  alarm. No digits (`Phosphor`'s glyphs deleted, its box geometry kept).
- **The meeting**: back from the vision, the three threads kindle one after another where the plates used to arrive; the
  meeting's length is unchanged.

## Consequences

- Client only: nothing enters the sim, nothing new is sent; a friend's drone is the same body fed by what is already
  replicated.
- The modes' colours stay on the F wheel; the eye is ember in every mode.

## Deliberately not handled

- Wave, yes and no share the upright fan; separate signs come with the thoughts.
- The frame's squares do not redden at an alarm; the item's icon still shows the plates.
- Holding a plan no longer shows the blueprint grid over the drone.
