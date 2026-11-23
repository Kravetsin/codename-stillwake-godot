# The nailer fires a burst, and nails come ten at a time

- Status: accepted
- Date: 2026-11-23

## Context

With the breakers ([companion-fights](2026-11-22-companion-fights.md)) the game has packs to fight, and nothing but the axe and
its throw to fight them with. The nailer, a gun from almost the start, fired 6 every 3 s, 2 a second, the least of any
weapon (the axe 4, the spear 5, the sledgehammer 6.7): a pack of six took 18 s of shooting. The railgun comes late, on the
belt's charge.

## Decision

- **A gun for a pack.** The nailer fires a nail every 0.3 s while held (hold-to-fire was there), its nail's 2 alone a hit:
  6.7 a second, as the sledgehammer's; a breaker at two nails, a sentry at fifteen. Its pierce, range, speed and kick stay.
- **The kick stays** at 60 a shot: afloat, a burst pushes the hero back from what it fires at, which plays as backing away
  from a pack.
- **Nails in handfuls** (the user's): a craft may make several of its item a unit (`Craft.Makes`, 1 by default). Nails are
  ten for ten scrap in a second at the workbench, a scrap each where they were two; the recipe shows the 10 on its icon.
  The user's reasoning: their cost will not make the nailer the best weapon, as its damage is cut.
- **The weapons' roles**, as the user and the lead put them: the axe and its throw up close and free, the nailer to thin a
  pack as it comes for nails, the railgun for a line or the armoured.

## Deliberately not handled

- **A nail kills nothing in one** any more, so its pierce rarely carries on (2, then 1): a burst, not a line.
- **No spread or heat:** the burst goes where the pointer is; the ammunition is the only cost.
- **The sentry's and the warden's nails** went from five and four to fifteen and twelve: the nailer is no duelling gun now.
