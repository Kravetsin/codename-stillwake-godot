# What a hero gains, counted steady over its head

- Status: accepted; supersedes the pick-up popups of [0075](0075-effects-are-light-not-shapes.md)
- Date: 2026-11-06

## Context

Playtest 2026-11-06: what a hero digs on a planet is not seen, and the "Scrap (12)" line over its head jitters as it
walks. Digging, felling and taking a machine apart put items straight into the inventory with nothing on screen. The line
was placed every frame at the hero's feet and shook with them.

## Decision

- **One tally for every gain.** A chip per item over the hero's head: its icon and "+n", no words. The same item coming
  again while its chip shows adds to it with a bump and a flash of pale sky. A chip holds 1.6 s after its last gain and
  fades in 0.5 s. A body bag keeps one chip, in words, there being no one picture of all it held.
- **The steady head tally**, picked by the user over a ledger by the hotbar and chips left where a cell broke (the
  broken-cell look was liked, but would get in the way while digging). The column hangs on an anchor that follows the head
  through critically damped springs: stiff across (ω 20, within about 1.5 cells of a walk), soft up and down (ω 9), where
  the head bobs with each stride. It trails a walking hero and never shakes.
- **Every gain flies home** as before (`PickUpFlights`): floor stacks from where they lay, catches from the piece, digs,
  fellings and refunds from their cell.
- **The sim notes the gains.** `GiveOrDrop` notes what went into the inventory, per item at its cell, in
  `ItemsModule.Gains` (the last 32). It is never saved and empties on a load, so peers' differ: it is a member of the
  module alone, not of `IItemsQueries`, and nothing in the sim reads it. A debris grab passes `noted: false`, and a net's
  collect notes nothing: `OwnCatches` shows both from where each piece was. The client keeps a mark per surface's module
  (`OwnGains`), each surface counting from 1.
- Not an inventory diff: it would count crate moves as gains and could not say where one came from. Not a prediction in
  the client: a dig's yield is game rules (material, bag room, felling).

## Consequences

- `PickUpPopups` is gone; `HeadTally` (the chips and the anchor, Godot-free) and `GainTally` (drawn on the HUD layer)
  replace it.
- Payments coming back (`GiveStock`: pipes, cables, valves, crafts), crafting output, crate moves and the starting kit are
  not gains and show nothing.

## Not done

- Others' gains: their heroes show nothing; the wall breaking says enough.
- A burst of more than 32 gains between two frames loses its oldest: four heroes digging cannot reach it.
- A gain in the frame a peer arrives on a surface it has not seen yet is not shown: the mark starts at what the journal holds.
