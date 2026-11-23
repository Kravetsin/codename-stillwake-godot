# unseen (client): a derelict's inside dark till seen

A vessel world's vessel is seen from the side as a whole hull, its rooms ink-dark till a hero has seen them
([hidden-rooms](../../../docs/decisions/2026-11-17-hidden-rooms.md)). What is seen is worked out here, in the client only, from
the grid and where the heroes stand; the sim knows nothing of it.

## Files

| File | Holds |
| --- | --- |
| `UnseenView.cs` | the dark drawn: one texel a cell over the vessel's frame, unlit, over all in it; what the pointer may name |
| `logic/Sight.cs` | the rule: what is seen, what is dark, how fast the dark goes |

## Rules

- **Only on a vessel world** (`VesselKind`), over its frame: every cell with a back wall, found once as the surface loads.
- **Seen** is every open cell (a back wall, nothing that seals) joined to where a hero's body stands through open cells; walls,
  shut doors and shut hatches stop it. A door or hatch opened beside a hero shows the space past it; a hero walking in shows it
  too. Every hero on the surface sees, so friends share what they open.
- **Seen stays seen**, a door shut again or not. A cell newly dark goes dark at once; one seen fades over `Sight.FadeMs`
  (300 ms). The first look, as the hero arrives, shows the airlock and what open doors join it at once, and on a wreck every
  room a breach opened to space, as the space round it (vessel-wrecks); sealed rooms stay dark till seen.
- **Dark** is unseen space, and every sealed cell with the dark on both its sides, across or up and down, within two cells
  (a hull wall's thickness): a shut door or a deck's floor between unseen rooms, the floor under such a door. The hull,
  with space past it, and a wall beside a seen room stay drawn.
- **Over everything inside** (`Layers.Unseen`, with the effects under it), plain ink, unshaded: neither the ship's dark nor a
  lamp behind it shows through.
- **The pointer names nothing** of a machine in a dark cell (`Hands.Unseen` → `MachineTips.Hidden`).
- Costs: a look floods the vessel's inside only when the grid changed or a hero stands in an unseen cell; the texture is
  written only while a cell's dark changes.

## Deliberately not handled

- Nothing is saved: a vessel world never outlives a load (vessel-worlds). A friend arriving later sees only from then on.
- The game's light still falls on the hull's inner face from a lamp in an unseen room, a faint glow at its edge.
- The air, pipe and power overlays draw over the dark.
