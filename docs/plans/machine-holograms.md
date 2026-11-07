# Plan: machine windows as station-coloured holograms

Client only. Every machine window in the one frame (0058, `Shared.MachineFrame`) stops being the wrist's metal module
(0074) and becomes a hologram in its station's colour, with a beam from the machine in the world to the window. The
look is the user's pick D «Цвет станции»; its numbers are below, and they go into the ADR.

## Rules (confirmed with the user)

- Every window in `MachineFrame` becomes a hologram: crates, furnaces, generators, smelters, the charging station, the
  trade stations, the queued stations (those whose window is `FabricatorPanel`'s: the Former, the assembler, the
  workbench, the arc bench, the lifepod) and every other machine that opens one.
- Colour by kind of station, from the spec's static traits, never from whether it has power now:
  `spec.Is(MachineTrait.Fire)` → warm; otherwise `spec.Powered` (it makes, draws or stores power) → cold; otherwise
  neutral (crates, trade stations, workbench, arc bench, lifepod, drop pod).
- The window stays where it is, same size, same slots; icons, slot boxes and their tints, counts, digits, bars and
  marks are not tinted. The ground, the edge, the name, the section headings and dim words, and the beam take it.
- Touch point: the point of the window's rectangle edge nearest the centre of the machine's box. Opening: the beam
  reaches from the machine to the touch point (120 ms = `HoloBeamS` 0.12 s), then the window grows from that point to
  its whole rectangle (150 ms, `HoloGrowS`), its contents' alpha rising with the growth. Closing (the window shut by
  Tab, Esc, E or reach): the contents go at once, as today, the ground shrinks into the touch point (90 ms,
  `HoloShrinkS`) and the beam pulls back (60 ms, `HoloRetractS`).
- Reopening the same machine while it closes carries on from where it is; opening any other machine makes the old
  hologram go at once and the new one play from the start.
- Input is live from the window's first frame (slots can be clicked at alpha 0: accepted); while it closes there is
  no window, so no input, as today.
- The beam is seen only by the hero who opened the window. It stays attached at both ends while the hero walks; the
  window closes as today when the machine leaves reach (`Aiming.CrateInReach`), and the beam pulls back.
- The wrist's cuff, forearm display and equipment unit keep their metal. The display always keeps its own bottom edge
  (`WristPieces.Display`): nothing docks under it any more.

## The look (pick D)

| Tone    | Light (ground wash, edge, beam) | Ground (under contents) | Name      | Headings  | Dim words |
| ------- | ------------------------------- | ----------------------- | --------- | --------- | --------- |
| Warm    | rgb(255,172,96)                 | rgba(22,13,8,0.87)      | `#ffe2c2` | `#cf9f74` | `#8d6c52` |
| Cold    | rgb(178,208,255)                | rgba(8,13,24,0.87)      | `#e4ecff` | `#a2b4d8` | `#6c7b9b` |
| Neutral | rgb(204,210,218)                | rgba(14,16,20,0.87)     | `#e6e9ee` | `#a9b0ba` | `#727a85` |

The ground carries a faint wash of the light (11% at the edge nearest the machine, fading to none halfway across); a
1 px edge in the light at 50% with a 6 px glow at 45%; the edge nearest the machine at 95%. The beam is a soft ribbon in
the light with a slight shimmer, wider at the machine, ending in a small emitter glow on it. Reference drawing:
`drawD` and `ribbonBeam` in the pick page's source (kept with this plan's PR description).

## Steps

1. **Logic** (`game/shared/logic/`, Godot-free: plain tuples or records of floats, no `Rect2`; unit tests in `game.tests`):
   - `HoloMotion`: given open/closed, the elapsed seconds and the machine id, the beam's and the window's progress
     0..1, by the rules above (carry on for the same id, restart for another). Tests: the opening order; closing drops
     contents at once, shrinks, then retracts; the same machine reopened mid-close carries on; another restarts.
   - `HoloShape`: the touch point; the emitter (the point of the machine box nearest the touch point, inset by one cell,
     or by half the box where it is under two cells); the grown rectangle at a progress; `ToneOf(MachineSpec)`; and
     `BoxOf(cells, width)` in cells, the one copy that `AimOutline` and `BuildingView` then call (each scales it by its
     cell). Tests: beside, at a corner, over; growth from the touch point; every spec that opens a window gets its tone
     by the rule. FsCheck invariant: the touch point lies on the window's edge and the emitter inside the box, for any
     rectangles; shown to bite once.
2. **`MachineFrame`** becomes a plain `PanelContainer` with the margins `WristScreen` gave it: 8 px top and bottom,
   `Slots.ScreenPadding` left and right (`StyleBoxEmpty`), so every slot keeps its place. It takes a tone (name,
   headings and dim words in it) and keeps `Lit(power)`, reimplemented on itself: the contents' alpha by the bag's
   `TabletMotion`, as `InventoryPanel.Power` calls it today. The hologram sets the frame's own `Modulate` alpha for
   the growth, so the two multiply. Remove `WristPieces.Dock`, `WristPieces.DisplayDocked`, their art
   (`wrist_dock.png`, `wrist_display_docked.png` and their imports), `WristArt.MachineScreen`; grep for other setters of
   `WristScreen.Piece` and remove the setter if none. `InventoryPanel` keeps `WristPieces.Display`. `Covers()` takes
   the frame's own rectangle. Compare slot centres with `main` in a frame; a shift of at most 1 px from the display's
   edge is accepted.
3. **`Shared.MachineHolo`** (`game/shared/`): a `Node2D`, `MouseFilter` irrelevant (not a `Control`), so it takes no
   clicks and is not in `Covers()`. It sits as the first child of the window's parent (the items panel's `_shown`, the
   Fabricator panel's own parent), so the ground and the beam draw under the bag, the hotbar, the window's contents
   and the stack in hand, and over the world. It redraws every frame while it shows (a deliberate exception to
   "panels read on a timer": the beam follows the moving hero and camera; it reads no sim state per frame, only the
   two rectangles). Its owner hands it the machine id, the window's global rectangle and the machine's box in world
   pixels, once the window has been laid out (its size above 0; until then nothing is drawn), or null once closed. It
   keeps the last window rectangle (where the ground shrinks) and the last box (where the beam retracts, so a machine
   destroyed while open still retracts to where it stood), never the freed frame. Each frame it maps the box to the
   screen with the viewport's canvas transform and draws the grown ground as a `StyleBoxFlat` (ground colour, 1 px
   edge, shadow as the glow) plus the wash, the beam as one tapered line (a `Line2D` with a width curve and gradient,
   or `DrawPolyline`), and the emitter with `ScreenGlow`. No world light. It hides when the retract ends. On a fresh
   registry (a load or resync) it snaps shut.
4. **Owners.** `InventoryPanel.Machine` gives its frame the tone of the open machine's spec and its holo the rectangles
   from the grid's footprint; `FabricatorPanel` re-reads the tone in `Describe` and hands its holo the rectangles after
   `FabricatorPanel.Dock` has placed it. Each owner has its own holo; when `OpenCrate` moves to a machine the other
   owner shows, the first's holo goes at once.
5. **Docs.** ADR `docs/decisions/2026-11-07-machine-holograms.md` (records pick D and the table above; supersedes
   0074's module bullet, its "yours or a crate's" bullet and its note that the Fabricator opens at once, and 0058's
   look; 0058's layout stands); `tools/adr-index.ps1`; the items and crafting READMEs' panel sections and Files tables;
   the map's UI line. This plan is deleted in the same PR once done.
6. **Check.** `tools/test.ps1 "" game.tests` while working, the full suite once. Frames by the lead:
   `tools/snap.ps1 -Movie 1 -- --testbed --open=furnace|former|crate|charger|lifepod`; then a dev room for the user.

## Deliberately not handled

- Other players seeing the beam or a station's contents; holograms over machines in the world.
- Redrawing the wrist (cuff, display, equipment unit): a later pick.
- The flash when an item lands in a slot, scanlines, the computer's window (not a `MachineFrame`).
- A machine behind its own window (zoomed in): the beam hides under the window.
- The beam draws over the hero, fog and frost: it is the suit's display, not the world.
- A stack let go over the 6–10 px strip where the dock's metal was is now thrown into the world, not kept.
