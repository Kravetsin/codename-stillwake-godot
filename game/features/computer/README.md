# Computer

The ship's computer's window: a monitor opened by E at the computer, a button a program on its side, the chosen program on
its screen. Only the radar so far, and only a picture.

## Files

| File | Holds |
| --- | --- |
| `logic/Readout.cs` | A computer as its screen reads it: powered or not, its programs' buttons, what the screen shows |
| `logic/Computer.En.cs` | Its player text (`En`) |
| `ComputerPanel.cs` | The window: the monitor, the side panel's buttons, the screen's line or program |
| `RadarScreen.cs` | The radar program: the ship in the middle, rings, the sweep, a planet's curve below |
| `ComputerScreens.cs` | Each computer's screen in the world: dark, running text, or the radar's sweep; its rack's lights; its glow |

Content: `c/Programs.cs` (`ProgramSpec`: a program and the machine kind bringing it), `MachineTrait.Computer` on the
computer's row in `c/Machines.cs`. Files in `logic/` are Godot-free and tested in `game.tests/` (`ComputerReadoutTests`).

## Rules

- **E opens it** (`Aiming` opens a machine holding items or a computer): the crew sets `Ui.OpenComputer`, not
  `Ui.OpenCrate`, and closes it out of reach or passed out, as a crate. E again or Esc puts it away; opening it puts away the
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
- **The radar** draws only time: the ship held in the middle, the beam turning every 4 s, a planet's curve still below,
  the ship drifting round it too slowly to see. Nothing in the sim stands behind it yet.
- **In the world** (`ComputerScreens`): a computer with no power is dark; a powered one runs lines of text and blinks its
  rack's lights; once its radar is ready, the sweep. Drawn unlit, its glow lighting the room in the screen's colour.
- **While the window is open** the hero aims at nothing: no outline, prompt or world tip under the pointer. The one
  tooltip names a button's program, and on the radar `My ship` over the dot and `Planet` over the planet.
- The panel reads the world every quarter of a second, never every frame; the radar redraws every frame while shown.

## Main's hook

`Main` makes a `ComputerPanel` with the hands' `Ui` and the world now played; it is put away with the rest by Esc and
when the hero changes surface.

## Deliberately not handled

- **The rules live in the client** (`Readout`): nothing in the sim asks which programs a computer has, and no command
  depends on it. Move them into the sim once a program changes the world (the radar's finds, a jump).
- **Each peer's window is its own**: two players at one computer see their own choice; nothing is shared.
- **No planet behind the radar's**: it is drawn whatever the ship orbits, of one look, at no real height.
- **No text on the radar** but its tooltips: its finds, ranges and heights come with places to find.
