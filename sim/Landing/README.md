# Landing

## Files

| File | Purpose |
| --- | --- |
| `Landing.cs` | The launch platform opened and shut with power; the drop pod and its landers to come. |

The launch platform (ADR `2026-10-24-the-launch-platform`), and later the drop pod and its landers that stand on it and drop
through it to a planet. For now it opens and shuts a platform's run by hand, gated by power.

- **Depends on:** `Grid` (`IGridQueries`: the platform cells; `IGridOps.SetDoorway`: opening a run), `Power`
  (`IPowerQueries.Feeds`: whether a run has power enough).
- **State:** none yet (`LandingState` is empty): a platform's state is its structure codes, the grid's. New to save format 32,
  its slice made empty by the migration step.

## Rules

- A platform piece (`Structure.PlatformClosed`, `_`; open `PlatformOpen`, `~`) is a doorway of its own group
  (`StructureGroup.Platform`): the pieces side by side along a row are one doorway (`Join.Run`), so a run opens and shuts
  whole, from any of its cells.
- `SetPlatform(cell, open)` sets the run at `cell` as asked, whole; already as asked, it changes nothing. Opening needs the
  run to have power enough: its plug fed (`Feeds`: its network makes or stores power and covers its demand). Every cable
  to a run plugs in at its left end, opposite its lamp (`StructureSpec.Plug`, `PowerNodes.NodeAt`), laid where it may on
  it. Shutting needs none, so a run left open on a network gone dark can still be shut.
- Open, the deck is a hole (no trait): no room's edge runs through it, so space under a hull's floor comes in and its room
  turns to space, air and gravity gone, as with a floor torn open. Heroes pass it both ways. A stack on the deck stays,
  afloat before its back wall (Items drifts only what lies out past the hull). An open door, an `Opening`, leaves its room
  a room that vents; a platform is a hole on purpose, the pod's way out.
- `SetDoor` refuses a platform (`grid.noDoor`): only this module opens one.

## API

- **Queries:** `Problem(cell, open)` — why `SetPlatform` would be refused, what E shows; `Powered(cell)` — its run's lamp;
  `RunOf(grid, cell)` (static) — its run, shut and open alike, what the client's lamps and E's outline find.
- **Commands:** `SetPlatform(cell, open)` — refused `landing.noPlatform` with no platform at `cell`, `landing.unpowered` when
  opening a run without power enough.
- **Fuzz:** one turn in 8, a set of a platform cell mostly, of any cell (off the grid too) otherwise, open or shut at random.

## Invariants

- A run is all shut or all open after any `SetPlatform`; a shut run never opens without power enough.

## Deliberately not handled

- A run open when its power fails stays open: shutting it is the player's, as a door's is.
- A piece added to a run's left moves its plug: a cable left on the old end no longer feeds it, and is plugged again by
  hand. Building a run is rare and the lamp shows it red.
- No slide as it opens: the client shows it open or shut, its door sounds playing.
