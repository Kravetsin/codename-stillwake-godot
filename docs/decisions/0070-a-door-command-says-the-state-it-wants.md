# 0070 — A door command says the state it wants

- Status: accepted
- Date: 2026-10-19

## Context

Doors and valves were moved by `ToggleDoor(cell)` and `ToggleValve(cell)`: flip whatever the cell holds when
the command's tick runs. In co-op two players often reach a shut door together. Both press E before either
sees the other's press come back, both send a toggle, the host stamps them in turn, and the first opens the
door while the second shuts it again: both wanted it open, and it is shut. A player's view lags the world
by a round trip and the playout reserve (370 ms median under `?lag=100&jitter=60`, 0014), so the window is
wide.

A proposed fix locked a door for the 200 ms of an opening animation. That narrows the window without
closing it: the window is the network's, not the animation's, and it often passes 200 ms. It would also
refuse a player who really means to shut a door just opened.

## Decision

- **Commands say the state their sender saw it should take:** `SetDoor(Cell, Open)` and
  `SetValve(Cell, Open)` replace the toggles. The client builds a press from the door or valve as it shows
  it, its own sets in flight made (0014): a shut door's press opens, an open one's shuts.
- **Already as asked is no change:** the command is accepted and does nothing; the grid's version stays, so
  rooms, air and drawings are not redone. Not a refusal: the player got what they wanted, and there is
  nothing to tell them. A doorway part open (a door cell built beside an open one) is set whole.
- **The slide is only the look:** the client slides a doorway's panel, one across its cells, over 0.2 s (`features/grid`'s
  `DoorSlides`), but the sim moved it at once; nothing waits on the slide or is locked by it.
- Commands carry the new names on the wire (`grid.setDoor`, `grid.setValve`). Saves are untouched: a door
  is still one structure code.

## Deliberately not handled

- **Two players who want opposite things at once:** one opens, the other shuts, and the later command wins.
  That is a real disagreement between friends; no lock would settle it.
- **A press built on a stale view:** a player who saw a door shut that a friend already opened sends an open,
  which does nothing. Pressing again, now seeing it open, shuts it.
- **The slide does not hold the door:** feet, air and light follow the sim, not the panel, so for 0.2 s a
  hero may walk through a panel still drawn part shut, or be stopped by one drawn part open.
