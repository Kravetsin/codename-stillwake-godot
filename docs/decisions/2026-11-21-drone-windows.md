# The drone's windows open in four ember corners; before the pairing only the gear and the stations open

- Status: accepted; supersedes in part [drone-frame](2026-11-21-drone-frame.md) (the tablets' fade and buzzing frame, the
  machine windows as the drone's, everything opening before the pairing), [drone-threads](2026-11-21-drone-threads.md) (the
  windows' two rows of ember squares) and [drone-companion](2026-11-21-drone-companion.md) (only the bag waiting for the pairing)
- Date: 2026-11-21

## Context

The drone's windows opened by its sparks running round the edge and landing as two rows of ember squares; the screen showed
only after. The user wanted a window readable at once, with the drone still visibly the one projecting it. The tablets
(build, board, market, journal) were also usable from the first second, though the drone that projects them comes later.

## Decision

- **Drone windows** are the bag and the tablets: plan mode with the build menu, the research board, the market and the
  journal. Each hands its place on the screen and a `CornerClock` to `DroneWindows` every frame; `DroneFrame` lays the corners
  round each one shown, however many are open at once.
- **The corners** are the pick «Уголки» (`tools/pick-kit/pages/drone-windows.js`, candidate `corners`, db doc
  `picks/droneWindows`), over «Луч», «Всплытие», «Росчерк» and «Линза»: four ember brackets just outside the window's corners,
  each reached by a spark shot from the eye, the next a little later. The screen is readable in a tenth of a second and whole
  by 0.13 s; shut, the screen goes first and the corners fly home. Reopened midway, the corners go on from where they are.
  Its numbers are `DroneInk`'s, exported from `tools/anime-art/subjects/drone.js`.
- **The flurry**: while a drone window is out, the drone flies before the hero's waist and a few of its sparks dart about it in
  a small oval (`DroneFlurry`), risen out of the core and sunk back.
- **Station windows are the station's own**: a machine's, a crate's, the fabricator's and the ship's computer, E on them. They
  open whether or not the drone is paired, keep the beam and pens of «Перо» (`MachineHolo`) and get no corners or flurry.
- **Before the pairing** (`Players.HasDrone`) only the gear and a station's window open (`DroneWindows.Opens`, one rule for
  every window): Tab shows the suit's slots alone; the bag, B's plan mode, the board, the market and the journal do not open, and
  the edge's row of keys is hidden (`EdgeRow.Shown`). Once paired, all open as before.
- **F10** (dev, `game/dev/DroneThreadsKey.cs`) shows threads from the eye to each corner, a variant the page also drew.

## Deliberately not handled

- No sound for a window opening or closing.
- A friend's corners are never seen: a window is this player's screen; their drone shows only that it projects (`Seen.Projects`).
- The corners do not redden with an alarm.
- An unpaired hero gets no hint of what pairing will open: the wreck's core is the first thing the hero meets.
