# Personal windows are the drone's holograms

- Status: accepted; supersedes in part [0083](0083-the-handbook-is-a-tablet.md) and
  [tablet-edge](2026-11-02-tablet-edge.md) (the rugged tablet, its rim along the foot), [0074](0074-the-inventory-is-a-wrist-computer.md)
  (the bag on the forearm, once paired) and [machine-holograms](2026-11-07-machine-holograms.md) (how a window opens); the motes and the net superseded by [drone-threads](2026-11-21-drone-threads.md)
- Date: 2026-11-21

## Context

The build menu, the Handbook, the board and the market sat on a rugged tablet the hero carried from the first second, and
the bag on the forearm. Once the hero had a drone ([drone-companion](2026-11-21-drone-companion.md)), every personal
window should come from it.

## Decision

- **Every tablet window is a hologram** (`shared/Tablet`): the build menu's, the Handbook's, the board's and the market's,
  and the game menu's. Each is the drone's blue glass with a machine window's buzzing frame (`BuzzFrame`, `HoloFrame`), with
  no bezel, stylus or glare. It fades in where it rests, above the keys (`TabletPeek.Floor`). The build menu's wings are
  small holograms beside it.
- **The keys are pinned at the foot** (`TabletEdge`), where the rim lay, whatever window is open. The rim is gone. The game
  menu still hides them while it shows.
- **The paired bag is the drone's hologram window** (`HoloWindow`): framed as a machine window, the trash under its right
  edge, the sort and stash keys at its right. It opens and closes at once. Unpaired, there is no bag window.
- **The drone projects before the hero.** While the paired bag is open or the plan is out, the drone turns its mode off,
  flies before the hero's waist and projects motes over itself (`DroneMotes`, `SheetHoloLook`): a drifting cloud for the
  bag, a blueprint's grid for plans. Each plan placed is pointed at by the near hand (`PlanPoint`) and reached by a mote
  trailing a ribbon. Friends see it: the hero's packet carries `Seen.Projects`. Without a drone the plan is the paper
  sheet in hand.
- **Machine windows** open as the pick «Перо»: a beam to the near edge, two pens tracing the frame, the ground fading in
  (`HoloPen`). Once open, the frame buzzes and two beams run to the near corners. Hologram windows trail the hero by a few
  pixels (`HoloSway`, the pick «Отставание»). The bag, the hotbar and the equipment never move.
- **The soot frame was tried and cut.** The pick «Угли», with «Сеть по краю», had the drone's soot run round an open
  window's outline and kindle into a glowing net (`SootFrame`, `SootRun`, `SootClaims`, `SootFade`, `DroneFrame`). In play
  the user preferred windows that open at once. Of it only the crackle stays in the game, on the drone's own arcs
  (`SootRun` with `window: false`, `NetLook`); `DroneFrame` is not mounted.

## Deliberately not handled

- Before pairing, the build menu, the Handbook, the board and the market still open; only the bag waits for the drone.
- No sound, no drone habits while projecting.
