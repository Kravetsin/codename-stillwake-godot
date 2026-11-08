# Lobbed glob

- Status: accepted; supersedes the spit and its looks' layering in [lurker-attacks](2026-11-08-lurker-attacks.md)
- Date: 2026-11-08

## Context

Played, the capback's spit read as a hitscan: the puddle lay under the hero as the glob's first frame left the mouth, since the
sim laid it the tick it struck the arc. The arc also went through the sentry's bolt path: the hero's client reported being
crossed (`Bolted`), so a glob that hurts no one knocked the hero back and buzzed the stun gun's hit. And the gas drew behind the
capback, where its picture showed it in front.

## Decision

- **The glob is lobbed.** A `Shoot` with `Leaves` makes an arc that lingers its `Lingers` ticks (12, 0.6 s) as its flight, its
  end dropped to the floor under the aim; its puddle is laid where it lands as its lingering ends. A hero that moves off during
  the flight is not stung. The arc is kept with `Lingers` and `Leaves` (added saved fields: no format step).
- **A lob strikes no one:** `Bolted` on it is refused (`creatures.gone`), so no knock and no stun gun's hit; it hurts only by
  its puddle.
- **The client flies it by the sim's clock**, the share of its lingering gone, along `GlobFlight`'s lob, and hears it leave the
  mouth (the hiss, higher and softer) and land (the splat).
- **Gas over the bodies:** the gas and the flying glob draw at `Layers.Gas`, over the creatures and the heroes in it; the
  puddle stays on the floor at the creatures' layer.

## Consequences

- A glob aimed at a hero standing still always lands on it; only moving dodges it.
- A puddle laid in a lob's last tick is a tick old when first seen, as every patch laid mid-tick is.
