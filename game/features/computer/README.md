# Computer

The ship's computer's window: a monitor opened by E at the computer, a button a program on its side, the chosen program on
its screen. Only the radar so far: the solar system and the ship's course in it, the vessels near, the shuttle's route.

## Files

| File | Holds |
| --- | --- |
| `logic/Readout.cs` | A computer as its screen reads it: powered or not, its programs' buttons, what the screen shows |
| `logic/Computer.En.cs` | Its player text (`En`) |
| `logic/Computer.Ru.cs` | Russian |
| `logic/RadarOrbit.cs` | Where the radar draws a contact: on the ship's orbit, the farthest 45 % of the screen from the ship; its distance in km |
| `logic/RadarBand.cs` | Where the radar draws a stretch of space coming or running: a band on the orbit sliding toward the ship |
| `ComputerPanel.cs` | The window: the monitor, the side panel's buttons, the screen's line or program |
| `logic/NavMap.cs` | Where the radar's map puts the sun, planets, ship, contacts and a callout; its zoom and course curve |
| `RadarScreen.cs` | The radar program: the system from above, zoomed to the ship's planet and its contacts, callouts, its ink kept |
| `ComputerScreens.cs` | Each computer's screen in the world: dark, running text, or the radar's sweep; its rack's lights; its glow |

Content: `c/Programs.cs` (`ProgramSpec`: a program and the machine kind bringing it), `MachineTrait.Computer` on the
computer's row in `c/Machines.cs`. Files in `logic/` are Godot-free and tested in `game.tests/` (`ComputerReadoutTests`).

## Rules

- **E opens it** (`Aiming` opens a machine holding items or a computer): the crew sets `Ui.OpenComputer`, not
  `Ui.OpenCrate`, and closes it out of reach or passed out, as a crate. E, Esc or its close puts it away, Esc an open callout first; opening it puts away the
  inventory, the plan and the Handbook (`Main`).
- **A program is brought by its module on the computer's power network** (`Readout.Of`): a `ProgramSpec.Module` machine
  whose node is on the computer's network. Ready when one gets all it draws, short otherwise; on another network or none,
  absent.
- **The side panel** shows a button for every program of `Programs.All` once any is brought, none before. A lit button is
  ready, a dim one short of power, an empty socket absent; a click opens a ready program only.
- **The screen** (`Readout.ScreenOf`): no power at all says so before all else; no program brought says `No modules found`;
  the chosen program while it is ready; else the first program short of power says so; else `Select a module`. A network
  short slows all its users alike, so the computer's screen stays lit while its antenna stands short.
- **The program shown** is `Ui.ComputerProgram`: none each time it opens, and none again once it stops being ready.
- **The window** lies over the screen as the tablet does (`TabletPeek.Margin` round the tablet's bezel), its close top right.
- **The radar** (2026-11-16-solar-system) is one map in the tablet's ink (`NavMap`): the system from above, the sun in the
  middle, each planet on its orbit, the ship's mark over `Landing.At` or along its course; the wheel, `+`/`−` or the
  breadcrumb zoom it 12 times down to the ship's planet, where the ship's orbit and places' contacts on it show (an offset
  a quarter turn each way at the radar's reach), a drag pans it. A contact leaving within 2 minutes blinks amber.
- **A click** on a planet or a contact opens its callout (`Ui.RadarContact` for a contact, the player's own), on the side
  away from the middle, tied by ink: a planet's number, type, ruins, flight, fuel and `Plot course` (landing's `PlotCourse`,
  dim with the sim's reason but `crewAway`, which sends: the call); a contact's class, size, decks, distance
  (`RadarOrbit.Km`), the shuttle's litres there and back (`PlacesModule.CostOf`), when it leaves, and `Set course` (places'
  `SetCourse`). Esc, its ✕ or empty map puts it away; a contact gone is let go on the next read.
- **The route** (`Places.Course`, every player's): a dashed line from the ship to its contact and its words on top.
- **A stretch of space** ([stretches-of-space](../../../docs/decisions/2026-11-15-stretches-of-space.md)) is a band on the
  ship's orbit in its colour (`RadarBand` across the old strip, mapped round as contacts are): ahead of the ship as its
  warning starts, over it as it comes, fading after.
- **In the world** (`ComputerScreens`): a computer with no power is dark; a powered one runs lines of text and blinks its
  rack's lights; once its radar is ready, the sweep. Drawn unlit, its glow lighting the room in the screen's colour.
- **While the window is open** the hero aims at nothing: no outline, prompt or world tip under the pointer. The one
  tooltip names a button's program, and on the radar the thing under the pointer.
- The panel reads the world every quarter of a second. The radar redraws only on a change and keeps its ink (sketched
  boxes by size, orbits as scaled unit circles): rebuilt every frame it cost 1.4 ms and 328 KB a frame.

## Main's hook

`Main` makes a `ComputerPanel` with the hands' `Ui` and the world now played; it is put away with the rest by Esc and
when the hero changes surface.

## Deliberately not handled

- **The rules live in the client** (`Readout`): nothing in the sim asks which programs a computer has, and no command
  depends on it. The course is places' own rule, so contacts come and a course is set whether or not a radar
  is built or powered elsewhere. The jump stays on the dev panel.
- **Each peer's window is its own**: two players at one computer see their own choice; nothing is shared.
- **The planets alike** on the map: one look, one type, until they differ.
- **No text on the radar** always on screen but the breadcrumb, the zoom's marks, the hint and a set route's words.
