# landing (client): the drop pod's seats and flights

What the launch platform and the drop pod show beyond the grid's own drawing (ADRs `2026-10-25-the-drop-pod`,
`2026-10-26-the-descent`). The sim's `sim/Landing` decides: the client reads `Seats`, `SeatOf`, `FlightOf` and
`LaunchProblem`, the crew feature sends `Board` and `Alight` from E, and the pod's key sends `Launch`. The platform's lamps and
socket are the power feature's (`PlatformLamps`).

## Files

| File | Purpose | Godot |
| --- | --- | --- |
| `PodSeats.cs` | each drop pod with a hero in it: its skirt's lamps lit and the glint on its glass, over the pod and the hero | yes |
| `LaunchKey.cs` | a pod's key under its slot: Descend aboard, Return off the ship, dim with the sim's refusal in its tooltip | yes |
| `FlightOverlay.cs` | over the rider's screen while it flies, kept across a surface switch: the flight, the dip to black, the white | yes |
| `FlightScreen.cs` | the rider's flight at the world's scale, no planet: speed lines, re-entry fire and plasma, clouds ripped past | yes |
| `PodFlights.cs` | a pod in the air from this surface: the deck's halves sliding apart and shut, the pod falling through on its thruster | yes |
| `logic/FlightTimeline.cs` | when each picture of a flight plays and how far along: seconds from the flight's ticks, the curves | no |
| `logic/Whiteout.cs` | how white the screen is, from the own flight's ticks left and a surface switch made while white | no |
| `logic/Landing.En.cs` | Its player text (`En`): the landing refusals and the key's two words | no |

## Rules

- A seated hero is the crew's to draw (`SeatedHeroes`): its own figure in its suit, behind the porthole from the chest up;
  its walking body is hidden (`HeroView`), held at the pod's floor (`Seating`). This feature lays the lit lamps and the
  glass's glint over it (`dropPod_lit.png`), drawn after it (`SurfaceViews`). The seats are compared every frame, so the
  lamps go out the frame the body shows.
- E on a pod boards it and opens its one slot under the inventory, as a crate's; seated, E leaves it and shuts the slot
  (`crew`). A pod the sim would refuse is neither boarded nor opened; the prompt says why.
- The pod's frame carries `LaunchKey` under its slot (`InventoryPanel.Launch`, set by `Main`), read on a timer: a press sends
  `Launch` only while the sim would take it. The panel closes on a launch and on a surface switch; E, E brings it back.
- While its pod flies the hero is hidden for every peer and held where the pod stood on the surface it left (`Flight.Floor`);
  its hands take nothing, as passed out.
- **The flight's pictures** take their time from its ticks (`FlightTimeline`), never a clock of their own, so a save loaded or a
  guest joining mid-flight sees the right moment. Aboard, everyone sees the deck slide open, the pod drop through on a thruster's
  kick and the deck slide shut by `FallTicks` (`PodFlights`, over the grid, which draws the run open and no pod); the rider's view
  shakes through the camera's nudge.
- **The rider's flight** (`FlightScreen`) cuts in through a dip to black and fills the screen at the world's scale, the pod in the
  middle: no planet, only speed. Down: star lines, then re-entry (fire, a bow shock, plasma up both sides, shaking), then blue sky
  and clouds; up: the same without the fire, blue to black, the world rushing down. Drawn in one `_Draw` from the flight's seconds.
- The white rises over a flight's last second, is full on its last tick (the frame frozen while a planet opens) and falls
  over half a second once the hero is on the surface it flew to; a flight ended any other way clears it at once.

## Deliberately not handled

- What the seated hero wears or holds: the figure behind the glass is its suit alone.
- The fall, the streaks and glow of a flight, the touchdown and the docking: slice 2. Peers aboard see the pod vanish.
