# landing (client): the drop pod's seats and flights

What the launch platform and the drop pod show beyond the grid's own drawing (ADRs `2026-10-25-the-drop-pod`,
`2026-10-26-the-descent`, `2026-10-27-the-descent-pictures`). The sim's `sim/Landing` decides: the client reads `Seats`, `SeatOf`, `FlightOf` and
`LaunchProblem`, the crew feature sends `Board` and `Alight` from E, and the pod's key sends `Launch`. The platform's lamps and
socket are the power feature's (`PlatformLamps`).

## Files

| File | Purpose | Godot |
| --- | --- | --- |
| `PodSeats.cs` | each drop pod with a hero in it: its skirt's lamps lit and the glint on its glass, over the pod and the hero | yes |
| `LaunchKey.cs` | a pod's key under its slot: Descend aboard, Return off the ship, dim with the sim's refusal in its tooltip | yes |
| `FlightOverlay.cs` | over the rider's screen while it flies, kept across a surface switch: the flight, the dip to black, the white | yes |
| `FlightScreen.cs` | the rider's flight at the world's scale, no planet: star lines rushing past; up, out of a blue sky first | yes |
| `PodFlights.cs` | a pod moving in the world: the deck sliding and the fall aboard; on a planet the touchdown and the lift-off | yes |
| `PodSounds.cs` | a moving pod heard by everyone on its surface: its engine, its clamps letting go, its touchdown's thump | yes |
| `logic/FlightTimeline.cs` | when each picture of a flight plays and how far along: seconds from the flight's ticks, the curves | no |
| `logic/Arrivals.cs` | the pods just come down on the surface seen and for how long, held back from the grid till they stand | no |
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
  middle: no planet, only speed. Down: star lines alone, ever faster, to the white; up: a planet's blue sky and clouds first, to
  black, the lines rushing down. Drawn in one `_Draw` from the flight's seconds.
- **On a planet** a pod just come down (`Arrivals`: seated now and not there last frame, or the rider's own the frame its flight
  ends; off the ship only, nothing in a world just loaded) is held out of the grid, the porthole and the lamps for
  `FlightTimeline.Release` and drawn lowered from above on its retro flame into its dust, with a bounce (`PodFlights`); time here is
  frame time, a frame counted at most `Arrivals.MostDelta`, so the planet's freeze does not eat it. Leaving the pod or flying
  off lets it go at once. A return lifts off on its flame out of a cloud of dust before the rider's screen cuts to the climb.
- The white rises over a flight's last second, is full on its last tick (the frame frozen while a planet opens) and falls
  over half a second once the hero is on the surface it flew to; a flight ended any other way clears it at once.
- **Heard** (`PodSounds`, `FlightOverlay`): a moving pod is placed on the World bus, so everyone on its surface hears it, a
  guest as the host, as far as `Sound.BreakReach`: its engine (`PodThrust`, the jetpack's loop lower and louder) through the
  drop's kick, the touchdown's burn and the lift-off; as its flame reaches the ground (`FlightTimeline.Blast`) the engine swells
  `PodBlastDb` and the blasted grit roars (`PodBlast`), briefly; its clamps (`PodRelease`) as it drops or lifts off; the thump
  and its legs' clunk as it touches down, the engine cut, softer at its bounce (`PodTouchdown`). Docking aboard on a return
  (`Arrivals.DockedSince`, not drawn): a short burst blasting the deck, a thump, the clamps closing. The rider, on the Suit bus:
  the engine from inside from its firing and the hull's rumble (`PodRumble`) from the cut, louder with the speed, and a thump
  (`PodFlash`) on the flight's last tick, under the white. In a pod a hero hears as in air (`HeroView.InPod`), vacuum outside or
  not. A sound due at a moment plays only if it passes while seen (`FlightTimeline.Crossed`): none late after a load.

## Deliberately not handled

- What the seated hero wears or holds: the figure behind the glass is its suit alone.
- The docking aboard: the white falls over the pod already standing at home. The hero behind the porthole while its pod moves.
- Sounds of their own: the pod borrows the jetpack's thrust, a drone's clunk, a hard landing and lava's bed, retuned.
- A touchdown in a world loaded or joined during it: the pod shows standing.
