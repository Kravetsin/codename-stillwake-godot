# Plan: the drone's threads body («Нити»)

By design-author, cut by design-reviewer, choices by the user (2026-11-21). The look is the pick «Нити»
(`tools/pick-kit/pages/drone-embers.js`, candidate `threads`, scan gauge `dots`; db doc `picks/droneEmbers`). It ships
whole: the code that drew it is the reference. The drone's thoughts follow it (`drone-thoughts.md`).

## Problem

The light drone wears four scrap plates and projects cyan holograms, while the picked look is a core and its embers
alone. The scanner shows green digits over a wreck that the pick reads as ember dots.

## Player experience

1. Idle: the coal core, an ember eye where it looks; three faint orbit threads round it, sparks with tails running round.
2. F to the lamp: sparks fly along straight threads from the core into a mirror outline behind it and keep flowing along
   it. Scan: the same, into a spyglass on the aim.
3. A gesture (H): the gesture's sign (the point, a ping's mark) is drawn by sparks on threads above the drone, as a
   thought will be. No cyan hologram is left.
4. The pointer on a wreck while scanning: sparks fly from the spyglass's mouth straight to a row of 26 ember dots over the
   wreck; the lit ones are the strength left. No digits.
5. A hit: the row shudders, the dots that went out burst off as falling embers.
6. Tab: sparks run from the core to the window's corner nearest the drone and round its edge; hundreds of small squares
   stay there shimmering; the screen inside appears after. Closing, they fly home. No cloud over the drone.
7. The meeting: after the vision the black rings shrink into the three orbits and the first sparks kindle and run round
   them one thread after another, where the plates used to arrive.
8. An alarm (a dart's flash, low air's pulse) turns the sparks, threads, dots and eye red, as the soot turns today.

## Decided (the user, 2026-11-21)

- «Нити» with the dots gauge.
- Gesture signs are thread shapes, no hologram.
- The meeting keeps its beat: threads kindle one by one.
- The scan's sparks fly straight to the dots; no run along the wreck's edge (the game has no outline as a line).

## Out of scope

- The thoughts (next plan); sounds; new modes; what is inside windows.
- A friend's open bag shows their drone drifting to their waist with no cloud: enough.

## Approach

- **Scale.** A pick px is 1/2.52 world px about the drone (it keeps its size), 1/4 over a wreck, a screen px on a window.
- **Body** (`DroneSwarm`, rewritten): 120 sparks on orbit `i % 3`, one shape at a time. Priority: a drone window out
  (`Projecting`, own or a friend's) forms none; else a gesture's sign; else the lamp's mirror or the spyglass. A shape takes
  the sparks with `hsh(3i+1)` under its share (0.8, about 96; a thought 0.72), flowing 30 px/s; the rest orbit, so nothing
  sums past 120. In and out 0.6 s, each late by `hsh(i+50)`. The pick never switches shape to shape, so a switch goes
  straight, each spark from where it is. A shape is `DroneShape(Lines, Share, Turn)`: pick px about the core, turned to the
  aim, up swaying, or a pointed line. A load or re-shown drone starts idle. Two `Triangles` calls; `drone_core` under the
  coal's cracks and one ember eye; the modes' colours stay on the F wheel.
- **Signs.** `DroneSign.KindOf` gives Point, Wave, Yes, No. The pick's one gesture shape is the fan (7 rays, an arc): Point
  turns it where it points, the others stand it up. `DroneSign` keeps `KindOf` and `Turned` (the fan's heading);
  `DroneHolo` goes (`WheelView.Hand` stays for the H wheel).
- **Frame.** `SootClaims` (which drone window owns the frame) and `SootSpots` (the squares' places from the near corner)
  stay; the bag hands its outline again. Two rows 3.4 px apart, a square each 3.4 px, at most 1,024 a row. Square j leaves
  after `along/reach · 0.45 s` and flies 0.3 s straight, every fourth trailing its thread; the screen comes at 0.8–1.0 s.
  Closing, the screen goes in 0.15 s, then the squares, far first. One clock runs up open, down closed: a reopen
  mid-flight turns squares back where they are. A square: 1.8 px dark, 1.6 px glow shimmering
  `0.14 + 0.7(½+½ sin(6τ·per − 4t + row))⁶` by a shader on `TIME`; landed ones drawn once, moved as the window sways.
- **Gauge.** 26 dots 5 world px over the read point, 1.75 apart, 0.8 wide, whole screen px; dot j lit
  `clamp(left/step − j, 0, 1)`, step `max/26`; none at `max ≤ 0`, `left` clamped. In 0.4 s from the spyglass's mouth, out
  to the drone. A hit shudders the row 0.8 s, bursting the lost dots; a new target drops bursts. `Phosphor` becomes `ScanDots`.
- **Meeting.** `DroneShown`, `DroneFade`, `Times` and `End` stay; a friend's looks as today. The plates go (`PlateStage`,
  `PlateOf`, arcs, the debris's plates). Back from the vision the 170 black drops sit on the orbits; thread o kindles at
  19.8 + o s, running round in 0.25 s as its drops go out; the fourth beat stays empty. Their seeds move to `Meeting`.
- **Friends** draw the same body from what is already sent: mode, gesture, `Projects`; their alarm red as today.
- **Deleted.** Plates, `NetLook`, the crackle, the film; `DroneMotes`, `DroneMotesLook`, Figure's `_motes`, `_holo*`,
  `_droneThere`; `SheetHoloLook` but its place before the waist; `DroneHolo`; `LightDrone.Holo` and `Window`. Tests:
  `DroneMotesLookTests`, `SheetHoloLookTests` go; `DroneSwarmTests`, `DroneSignTests`, `SootRunTests`, `SootFrameTests`,
  `PhosphorTests`, `MeetingTests` are rewritten.
- **Budget.** No worse than main at `-- --frames`, 60 fps.
- **Glossary.** spark: on the orbits or a shape's threads; square: a landed frame piece; dot: the gauge's; ember: a lost dot
  falling. "Soot" is the code's old name.

## Determinism and co-op

Client only: nothing enters the sim, nothing new is sent. `Crew.MeetingTicks` stays.

## Tests

game.tests `logic/`: shares; window > gesture > mode; a mode switch mid-gesture; no jump on a switch, the aim turning or a
reopen; a re-shown drone idle; a hit, and one mid-flight; the frame from the near corner, the screen after; the alarm; the
threads in order. Invariant: lit dots × step = left, proven to bite.

## Steps

1. `drone.js` writes `shared/logic/DroneInk.cs`: polylines, mouth, counts, shares, orbit, spark, frame and gauge numbers.
2. Body: `DroneSwarm` rewritten, `LightDrone` drawing it; the drone's plates and net out (the meeting's till step 6).
3. Signs as the fan; `DroneHolo` out.
4. Frame: squares and shader; the bag framed; net, film and cloud out.
5. Gauge: dots; `Phosphor` out.
6. Meeting: threads kindle; the last plates out.
7. Try: build, the touched tests, the plain game for the user; frames by the lead.
8. Ship after the user's yes: full suite; an ADR superseding the plates in light-drone, drone-frame, drone-meeting and
   steady-scanner; map, glossary, crew and scanner READMEs; this plan deleted.
