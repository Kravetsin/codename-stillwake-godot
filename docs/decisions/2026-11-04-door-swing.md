# A door takes no other toggle while it moves

- Status: accepted
- Date: 2026-11-04
- Amends [0071](0071-a-door-command-says-the-state-it-wants.md): a door asked to change while it moves is refused

## Context

A `SetDoor` applied on its tick every time. On playtests several players clicked one door at once and it jumped with each
click, so none of them could open or shut it; griefers kept a door from opening by shutting it as soon as someone opened it.

## Decision

- **A doorway moves for `Doors.SwingTicks`** (24 ticks, 1.2 s: the door's open and shut sounds) after a `SetDoor` that changed
  it. Meanwhile a `SetDoor` that would change it again is refused `grid.doorBusy`, whoever sends it. A `SetDoor` asking for how
  it already is still refuses nothing and does not restart the swing: two peers opening it at once leave it open (0071).
- The swing is saved in the grid's slice (`Swings`, counted down a tick at a time; written only while a door moves) and kept by
  the doorway's first cell. `AfterToggles` skips what the swing would refuse, so a client's own second click shows nothing.
- E on a moving door carries the reason (`DoorProblem`), as the prompt shows any refused press.
- The panel slides for 0.54 s (`DoorSlides.Seconds`, 0.2 s in 0071), arriving as the stop rings in its sound; the rest of the
  swing is the sound ringing out as the door settles.
- Doors still push heroes out as they shut (0089's `ShutOut`): a hero standing in the doorway, by intent or away from the
  keyboard, cannot hold it open and let the air out. The swing is the only guard against griefing.

## Deliberately not handled

- Feet, air and light follow the sim, not the panel (0071): for 0.54 s a hero may pass a panel still drawn part way across.
- A launch platform's `SetDoorway` from Landing has no swing: power opens it, not a click.
- A doorway rebuilt on the same first cell within its swing is still moving: harmless, as taking one apart and building another
  takes longer than the swing.
- A client's prediction reads the swing at its own tick: a press sent just as a swing ends may show refused and then apply, or
  the other way, for a round trip.
