# Machine windows are station holograms

- Status: accepted; supersedes, of [0074](0074-the-inventory-is-a-wrist-computer.md), the open machine's module,
  "yours or a crate's" by its screen and the Fabricator opening at once, and of
  [0058](0058-one-place-for-every-machine-window.md), the frame's look (its layout stands)
- Date: 2026-11-07

## Context

An open machine's window was the wrist computer's module docked under the bag (0074): the same metal as the hero's
own screens, its screen only a little greyer and greener. Nothing tied it to the machine in the world, and a furnace's
window read like a crate's. A pick page showed five looks over real frames; the user picked D, «Цвет станции»: a
hologram in the station's colour, a beam from the machine to the window.

## Decision

- **Every machine window is a hologram**: every frame (`Shared.MachineFrame`) the items feature and the Fabricator's
  panel open, so crates, furnaces, generators, smelters, the charging station, the trade stations, the queued stations
  and every other machine with one. It stays where it was, its size and slots unchanged.
- **Colour by kind of station**, from the spec's static traits, never from whether it has power now
  (`HoloShape.ToneOf`): a fire (`MachineTrait.Fire`) warm; else one that makes, draws or stores power (`Powered`)
  cold; else neutral (crates, trade stations, the workbench, the arc bench, the lifepod, the drop pod).

  | Tone    | Light (ground wash, edge, beam) | Ground (under contents) | Name      | Headings  | Dim words |
  | ------- | ------------------------------- | ----------------------- | --------- | --------- | --------- |
  | Warm    | rgb(255,172,96)                 | rgba(22,13,8,0.87)      | `#ffe2c2` | `#cf9f74` | `#8d6c52` |
  | Cold    | rgb(178,208,255)                | rgba(8,13,24,0.87)      | `#e4ecff` | `#a2b4d8` | `#6c7b9b` |
  | Neutral | rgb(204,210,218)                | rgba(14,16,20,0.87)     | `#e6e9ee` | `#a9b0ba` | `#727a85` |

- **What takes the tone**: the ground, its edge, the name, the section headings and dim words, and the beam. Icons,
  slot boxes and their tints, counts, digits, bars and marks do not.
- **The ground** (`Shared.MachineHolo`) carries a faint wash of the light, 11% at the edge nearest the machine fading
  to none halfway across; a 1 px edge in the light at 50% with a 6 px glow at 45%; the edge nearest the machine at 95%.
- **The beam** leaves the machine's footprint a cell in from its side nearest the window (from its middle along a side
  under two cells) and touches the point of the window's edge nearest the machine's middle: a soft ribbon in the light,
  three bands widening toward the window as the pick drew it, a slow shimmer along it, a small breathing glow where it
  leaves. It stays attached at both ends while the hero walks; only the hero who opened the window sees it.
- **Opening**: the beam reaches the touch point in 120 ms, then the window grows from it over 150 ms, first along the
  touched edge and then across, its contents' alpha rising with it. **Closing** (Tab, Esc, E or out of reach): the
  contents go at once, the ground shrinks into the touch point in 90 ms, the beam pulls back in 60 ms (`HoloMotion`,
  numbers in `HoloLook`). The same machine opened again while it closes goes on from where it is; another machine
  makes the old hologram go at once. Input is live from the window's first frame.
- **Under every panel, over the world**: the hologram is a node, not a control, first under its owner's panels; it
  takes no clicks and covers nothing. Each owner (the items panel, the Fabricator's) has its own; a machine the other
  owner shows makes it go at once, and a load or resync too.
- **The wrist keeps its metal** (cuff, display, equipment unit); the display always keeps its own bottom edge. The dock
  piece, the docked display and their art are gone.

## Deliberately not handled

- Other players seeing the beam or a station's contents; holograms over machines in the world.
- Redrawing the wrist: a later pick.
- The flash when an item lands in a slot, scanlines, the computer's window (not a `MachineFrame`).
- A machine behind its own window (zoomed in): the beam lies over its ground, under its contents.
- The beam draws over the hero, fog and frost: it is the suit's display, not the world.
- A stack let go over the 6–10 px strip where the dock's metal was is now thrown into the world, not kept.
