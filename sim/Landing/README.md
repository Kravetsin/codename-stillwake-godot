# Landing

## Files

| File | Purpose |
| --- | --- |
| `Landing.cs` | The launch platform opened and shut with power; the drop pod boarded and left; its landers to come. |

The launch platform (ADR `2026-10-24-the-launch-platform`) and the drop pod (ADR `2026-10-25-the-drop-pod`), and later the
landers that stand on the platform and drop through it to a planet. For now a platform's run opens and shuts by hand,
gated by power, and a hero sits in a pod and gets out again: nothing launches.

- **Depends on:** `Grid` (`IGridQueries`: the platform cells and the pods; `IGridOps.SetDoorway`: opening a run), `Power`
  (`IPowerQueries.Feeds`: whether a run has power enough), `Crew` (`ICrewQueries.HeroOf`: whether a seated hero is here and
  awake).
- **State** (`LandingState`, saved): `Seats` — `Seat(Pod, Peer)` by pod id, each pod one hero, each hero one pod. New to save
  format 32, its slice made empty by the migration step; the seats added later with an initializer, none.

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
- **The drop pod** (`dropPod`, a machine that `Seats` and `Lands`): Grid lets it stand only on a platform, shut or open, the
  row under it all a run's (`grid.noPlatform`). `Board(pod)` seats the sender's hero in it; `Alight` takes it out. Its one
  slot takes biofuel only (`MachineSpec.Takes`, Items' `items.notTaken`), put in by hand: drones never feed it.
- A seat holds while its pod stands and its hero is on the surface and awake: a pod taken apart, a peer gone (`Leave`) or a
  hero passed out frees it in the derivation phase.
- Where a seated hero is, its client says (0002): it holds the hero at the pod's floor, in its middle, and draws it behind
  the porthole. The sim keeps no position of its own.

## API

- **Queries:** `Problem(cell, open)` — why `SetPlatform` would be refused, what E shows; `Powered(cell)` — its run's lamp;
  `RunOf(grid, cell)` (static) — its run, shut and open alike, what the client's lamps and E's outline find;
  `BoardProblem(peer, pod)`; `SeatOf(peer)`, `RiderOf(pod)`, `Seats`.
- **Commands:** `SetPlatform(cell, open)` — refused `landing.noPlatform` with no platform at `cell`, `landing.unpowered` when
  opening a run without power enough. `Board(pod)` — `landing.noPod` (no machine that `Seats`), `landing.seated` (the
  sender sits in a pod), `landing.taken` (another hero in it). `Alight` — `landing.notSeated`.
- **Fuzz:** one turn in 8: a third of them a board (a pod mostly, any id otherwise) or a leave, the rest a set of a platform
  cell mostly, of any cell (off the grid too) otherwise, open or shut at random.

## Invariants

- A run is all shut or all open after any `SetPlatform`; a shut run never opens without power enough.
- A pod has at most one rider and a hero at most one pod; every seat's pod stands and its hero is awake after a tick.

## Deliberately not handled

- A run open when its power fails stays open: shutting it is the player's, as a door's is.
- A piece added to a run's left moves its plug: a cable left on the old end no longer feeds it, and is plugged again by
  hand. Building a run is rare and the lamp shows it red.
- No slide as it opens: the client shows it open or shut, its door sounds playing.
- Boarding asks no reach: the client sends it only for a pod within reach, and peers are trusted (0002).
- A pod over a platform taken apart stays where it stands; a platform opened under it holds it. Nothing falls yet.
- The fuel is not burnt: it waits for the launch.
