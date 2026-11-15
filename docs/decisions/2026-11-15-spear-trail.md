# The plain spear trails haze outside its thrust

- Status: accepted; amends [blade-trail](2026-11-10-blade-trail.md) and [spear-effects](2026-11-14-spear-effects.md) for the spear
- Date: 2026-11-15

## Context

blade-trail left every aimed tool without a trail: in a thrust the spear's edge moves along itself, and a line of haze there
reads as nothing. But it turned the trail off for the spear everywhere. Running, jumping or turning, the spear's edge moves
sideways as an axe's does, and it left nothing, while every swung blade did. The user asked why the spear had no haze of its
own on a run, only on its hit.

## Decision

- **The plain spear trails as a swung blade does** (`BladeTrail`, through `ReachAir`), whenever it is held and not thrusting.
- **In its thrust it leaves no trail:** the lens and eddies of spear-effects are the thrust's.
- **The arc spear leaves none at all:** its torn bolts are its effect in motion.
- A trail is only drawn. The spear still bites only in its thrust (blade-sparks): a spear carried on a run throws nothing.

## Not handled

- No test draws the figure: `Figure` is Godot code. The change is one condition, seen in the game.
