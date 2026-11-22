# Breakers to mow down, and the weapon thrown on the drone's thread

- Status: accepted
- Date: 2026-11-22

## Context

Both developers found the game short of action: sentries are rare single duels, and the drone, the stronger of the pair,
took no part in a fight. The user wanted simple dummies that are fun to mow down, not enemies worked out like the
catcher, and the drone as part of the weapons.

## Decision

- **Breakers** («разборщики»), the dead fleet's service machines still on their last order: take apart whatever drifts,
  the hero included. They sleep in nests on the far field's wrecks; one struck or neared lets out a pack that rings a
  hero out in space and pounces at it, one at a time. A blow cuts a pounce and throws the breaker along itself; a second
  axe hit kills it. They follow nobody into a room.
- **A mechanic, not a creature:** nests, the `Swarm` drive on the flyer's body, a knock along the blow for any flyer
  afloat. No system names the species.
- **The companion's ability is on the right button, by the item in hand** (`Content.Abilities`): the first is the throw.
  The axe, the sledgehammer and the spear each have a row of their own; a tool that mines or runs has none.
- **The throw has weight:** it leaves with the hero's speed, slows to a stop at its range or at what stops it, and is
  hauled back up to speed on a thread. It goes through every one in its path. The axe strikes on both ways; the spear
  flies point first. No cooldown, yet a throw never out-hits the same weapon's swings. It walks the grid as every ray does
  (`Rays.Walk`): what stops a swing stops a throw. A hero moved across the world (a fog's seam) ends it uncaught.
- **The drone keeps its place and its mode:** only its thread catches and hauls. A lamp or the scanner together with a
  throw is worth more than a mode for throwing.
- **The catch, as the user picked it in play:** the stroke followed through, the hero faces the weapon with the arm held
  out and the hand open, and the grip turns into that fist, a friend's too.
- **The client's word, as swings are** (0002): the flight sends the commands a swing sends, through the one lookup
  (`ToolRays`), and adds no sim state.

## Rejected, played or discussed

- The drone flying out to fetch the weapon: it flitted about in view.
- An arm mode for the drone: it would take the lamp and the scanner away while fighting.
- The flight drawn to the shoulder: the head or the point arrived in the body and the grip jumped into the fist.

## Accepted limitations

- The breakers' look and sounds are stand-ins, the sentry's made small, till their pick page.
- A pack may wait outside an airlock up to its linger, or wake unseen by a wreck struck far off.
- Breakers ignore each other, wrecks as obstacles, and bursts; none live aboard vessels yet.
- One joining mid-flight sees no throw; a friend's far hit on ground is silent; no weapon is lost or stuck.
- While its weapon flies, a hero faces that, not the pointer.
- Each folded breaker is a whole rig, not a baked picture: five nests at most were counted in one view of the far field.
- An older build does not open a save with breakers in it (saves break at release).

## Later, agreed but not built

A sentry's arc teaches the drone its shot: the first hit shows a question mark, a later arc is caught, drawn into the
core and sent back; from it a shot from the palm that is fun, not a bare wait.
