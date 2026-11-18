# The heavy bell sits as the helmet skins do, its shoulder piece gone

- Status: accepted; amends [helmets-fit-the-suit](2026-11-14-helmets-fit-the-suit.md)'s fit of the heavy helmet
- Date: 2026-11-18

## Context

The heavy helmet's dome stood on a shoulder piece of its own, a grey skirt with a brass band drawn over the near arm. It met
none of the suits well and stood out on every one, while the basic helmet and its skins sit on a rubber seal alone.

## Decision

- **No shoulder piece.** `hero_heavyHelmet_collar` is empty, as the skins' collars are; nothing of the bell lies over the
  shoulders or the arm.
- **The skins' fit.** The bell turns whole on its own rubber gasket as the skins do on theirs (`BodyFit.Shell`): its own pivot,
  band and fade are gone. Its shell and gasket pictures are unchanged.

## Deliberately not handled

- Two other takes were tried in play and rejected: the bell on the basic helmet's rubber ring, its dark strip cut off, left a
  hole behind; its own strip stretched level onto the shoulders read worse than the first.
- The hose at the back still ends on the bell, not in the tank.
