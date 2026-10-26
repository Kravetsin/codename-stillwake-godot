# landing (client): the drop pod's seats and flights

What the launch platform and the drop pod show beyond the grid's own drawing (ADRs `2026-10-25-the-drop-pod`,
`2026-10-26-the-descent`). The sim's `sim/Landing` decides: the client reads `Seats`, `SeatOf`, `FlightOf` and
`LaunchProblem`, the crew feature sends `Board` and `Alight` from E, and the pod's key sends `Launch`. The platform's lamps and
socket are the power feature's (`PlatformLamps`).

## Files

| File | Purpose | Godot |
| --- | --- | --- |
| `PodSeats.cs` | each drop pod with a hero in it: the hero behind its porthole and its skirt's lamps lit, over the pod | yes |
| `LaunchKey.cs` | a pod's key under its slot: Descend aboard, Return off the ship, dim with the sim's refusal in its tooltip | yes |
| `FlightOverlay.cs` | the white over the rider's screen at a flight's end, over the HUD, kept across a surface switch | yes |
| `logic/Whiteout.cs` | how white the screen is, from the own flight's ticks left and a surface switch made while white | no |
| `logic/Landing.En.cs` | Its player text (`En`): the landing refusals and the key's two words | no |

## Rules

- A seated hero is drawn behind its pod's porthole from the chest up, the same picture for every peer; its own body is
  hidden (`HeroView`), held at the pod's floor (`Seating`), so the pod stands in front of nothing. The seats are compared every
  frame, so the porthole empties the frame the body shows.
- E on a pod boards it and opens its one slot under the inventory, as a crate's; seated, E leaves it and shuts the slot
  (`crew`). A pod the sim would refuse is neither boarded nor opened; the prompt says why.
- The pod's frame carries `LaunchKey` under its slot (`InventoryPanel.Launch`, set by `Main`), read on a timer: a press sends
  `Launch` only while the sim would take it. The panel closes on a launch and on a surface switch; E, E brings it back.
- While its pod flies the hero is hidden for every peer and held where the pod stood on the surface it left (`Flight.Floor`);
  its hands take nothing, as passed out.
- The white rises over a flight's last second, is full on its last tick (the frame frozen while a planet opens) and falls
  over half a second once the hero is on the surface it flew to; a flight ended any other way clears it at once.

## Deliberately not handled

- The suit's colour behind the glass: one picture for every hero.
- The fall, the streaks and glow of a flight, the touchdown and the docking: slice 2. Peers aboard see the pod vanish.
