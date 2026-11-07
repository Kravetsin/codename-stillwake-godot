# Cave hostiles

- Status: accepted; slice 1 (the capback, the geode snail) built, the threadling, embermoth and driftbell wait for their bodies
- Date: 2026-11-07

## Context

The first planet's caves had nothing living in them: a hero went down to the lava with no threat on the way. The user's page
«Жители пещер» drew seven peaceful cave creatures; the user (2026-11-07) asked to take them and turn some hostile. The design,
its later slices and what it leaves out are in [`docs/plans/cave-hostiles.md`](../plans/cave-hostiles.md).

## Decision

- **Lurkers, not hunters.** A cave hostile holds its ground: a walker on the sentry's `Guard`, standing still (no crawl),
  noticing only an awake hero in line with it — the line walked as a shot's (`Rays.Walk`), so it never springs into rock — and
  striking one within its `Strike`, else waiting. Struck, it answers at once. No chase: walker steering cannot path through
  caves, and walking away stays safe till paths come.
- **Two kinds in slice 1**, the two the `Walker` body serves today: the **capback** (Шляпоспин), a mimic among the garden's
  glowcaps that pounces (4 units of suit air), and the **geode snail** (Жеодник) of the amber deep, rolling a lunge (6 units).
  Danger grows with depth: the cold band stays calm.
- **A steady lunge.** `Lunge.Steady`: no blow cuts its crouch or its roll (`MoveRule.Interruptible(Move)`), so a held drill
  cannot pin the snail in its wind-up. Every lunge now aims only at an awake hero.
- **Spawning by the hero's band.** A herd round a hero is drawn only from the habitats of that hero's depth band, the open
  ground's counting as band 0: weights alone cannot keep grazers out of the caves and lurkers off the surface.
- **A faction, `beasts`,** hunting the crew: what `Threatens` reads, and the later cave kinds' too.
- No new module, command or save field, so no format bump; the two species' brains and moves are pinned by
  `CreatureContractTests` like the rest.
- **Drawn in ink as picked** on the cave page: the capback «Кочка» (hunkered, its moss and caps glowing teal, violet as it
  winds up), the snail «Жернов» (a shell that rolls, amber veins and mouth). Their own rig (`LurkerRig`), not a walker's: they
  hold still, then act, so poses read better than a gait. Their sounds are the grazers' placeholders.

## Consequences

- Ranged weapons kill lurkers from beyond their reach unanswered; a hero on a ledge above a snail is missed.
- Capbacks spawn on any garden floor, not only among glowcaps.
- The threadling (a ceiling body, the icicler's too), the embermoth and driftbell (a flyer body) come in later slices; the
  glimmite waits for the lamp in the sim, the quillmole for the lava worm's burrowing.
