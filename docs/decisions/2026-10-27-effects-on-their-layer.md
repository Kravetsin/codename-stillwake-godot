# Effects on their layer

- Status: accepted; built
- Date: 2026-10-27
- Supersedes the "over everything" of [0075](0075-effects-are-light-not-shapes.md)

## Context

Every spark, ember, arc, ring and trail went into one set of pools drawn at `Layers.Effects`, over the whole world: a
furnace's embers over the hero before it, the arc drill's coil arcs over the hand that holds it, a smelter's arcs over a
hero walking past. The player's word: every effect on the same layer as the object that makes it; and we choose, per
effect, where one should still show over the heroes.

## Decision

- **A stage per layer.** A stage is its own particles, flares, arcs and hit thrower, with today's caps, drawn by its
  own effects view at its layer's z (`Stages.Of(Layers.Fire)`, keyed by z, so names sharing a z share one). The world's
  views sit in one node kept last among the world's views (`StagesView`), so at a shared z a stage draws over that
  layer's objects. The shared pools are gone; every emitter names its stage.
- **The maker's layer by default:** the furnace's on `Fire`; machines, lamps, the bench, the pod on `Machines`; doors on
  `Doors`; flora, caves, homes, spires and relics on `Flora` (with lava); creatures on `Creatures`; drones on `Drones`;
  the jetpack and the harpoon's flight on `Tethers`; a figure's dust, landing and glove sparks, and the air spun round a
  breathing hero, on its hero's layer; building's on `PlanHands`.
- **Over the heroes, by choice**, at `Layers.Effects`: hits and what a hit breaks, fells or kills; a blow on a hero (a
  ram, the sentry's arc, a relic's or resonator's zap, the suit's air knocked out); venting air; pick-ups flying to the
  HUD. The player should always see these.
- **What a hero holds** draws over its torso and the tool and under its front arm: a figure makes a held stage on
  first use, its view a part of the figure just under the front arm, drawn in the world's pixels however the figure
  turns (`EffectsView.InWorld`), let go of as the figure leaves the tree. The drill's light and arcs and the harpoon's
  muzzle glint and gas go there, every hero's alike.
- **Light is merged:** `LightView` takes every stage's flashes, then arcs, then particles, under its one cap, as it did
  across one pool. An empty stage draws nothing.

## Deliberately not handled

- **A particle keeps its stage for life**: an ember rising past a hero's helmet stays behind it.
- **No splits inside one object** but the figure's: a helix's back half, a creature's far legs. The screwdriver's tip
  sparks stay on `PlanHands`: building's view cannot reach the figure without one feature reaching into another.
- **Caps are per stage**: a dozen busy stages could hold many times the old 1600 particles; in play a few are busy.
- **Effects under `Shimmer`** are in the screen's copy and wobble with the heat, as their machines do.
