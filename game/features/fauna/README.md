# fauna (client)

The creatures ([0102](../../../docs/decisions/0102-creatures-live-in-the-sim.md)). Drawn only: the sim (`sim/Fauna`) spawns,
moves, knocks and kills them and drops what they give. Striking one is the hero's hands (`features/crew`): the axe's blade and
the drill's bit are tested against each creature's body as against a wreck's box, wherever the hero stands
(`Aiming.StruckCreatures`, `Aiming.TouchedCreatures`, `Aiming.StrikesOf`), and send `Strike`.

- **Reads:** `FaunaModule.Creatures`; `Client.InFlight` — this player's own strikes, seen the frame they are sent; `Ui.Heroes` —
  whom a creature watches and flinches from; `Ui.Bits` — a friend's running drill by a creature struck; the grid, for the
  ground under a creature's feet; `Session.Progress`.
- **Only what the camera sees:** a body only for a creature within the camera's view and 4 cells round it, from a pool that grows
  to the most ever on screen and never frees. A creature gone from the world while drawn was killed: nothing despawns in a
  hero's view.
- **Drawn between ticks:** each body kept at the tick before and the last, drawn `Progress` of the way between them, a tick
  behind the sim, so a knock, a step up or down a cell and a landing glide. On the ground its feet are put on the ground under
  each (a slope's diagonal, the step below an overhang, at most 0.6 of a cell down), the body between the two and tilted to
  them up to 0.4 rad, eased over 70 ms: the sim stands it on the top of a whole cell. Facing the way it moves; still, toward a
  hero within 9 cells, else the way it last moved.
- **The body** (`CreatureBody`, a placeholder till the user picks art): a soft round body of its species' size on two stubby
  legs, a belly, two ears, a tail tuft and one big eye whose pupil follows the hero near it, its fur a shade its own by id; lit
  as the world is (`Lit`), with `Layers.Creatures`, under the heroes. Its legs swing by the ground covered (a stride every
  9 px), so fleeing they go faster; tucked in the air; it breathes and blinks standing.
- **A blow** (`CreatureMotion`, plain C#, tested): this player's own strike reacts the frame it is sent, a round trip before the
  sim's knock; a friend's as the sim's `Damage` rises (an own strike's damage within 0.7 s is not shown twice). It flashes
  white (an unlit copy of the silhouette over it, 150 ms), squashes wide and flat and springs back past round (380 ms), flinches,
  its top thrown the way it is knocked (320 ms), its eye wide and ears pinned (900 ms), and is held where it was struck a
  moment, shaking (the blow's stop, 70 ms, with the figure's own `Bit`). A drill's hit is its share of the axe's
  (`Impacts.ShareOf`): flashes and stops less, ten a second. Fleeing it leans forward and stretches long, ears back.
- **Seen and heard by what it is** (0093, 0094): `HitLooks.Creatures` names each species' `Impact` (the loper's `Flesh`: white
  slash streaks, tufts of fur, a warm puff, a white glint and shock line, no blood), thrown from its side facing the hero, its
  share for a drill; `Sound.Creatures` its hit, a body thump under it and its death (placeholders from the files here: the cap's
  squelch lower, a soft landing's thud higher, a heavy landing's), this player's own on Suit, a friend's on World. An own axe
  hit nudges the view as a wreck's does.
- **Death:** its hit thrown whole, fur and soft puffs every way, a white glint and a shock ring, and its body swelling white and
  fading over 170 ms; what it gives falls from its top row as the sim drops it (`features/items`). Heard where it fell.
- **A surface left or a world loaded:** every body put away at once, no puff.

## Dev

`-- --planet --fauna` lets six lopers out either side of the hero (`dev/FaunaDemo`, the sim's `Release`) and logs every strike
and every creature gone; without `--manual` `dev/FaunaHands` hunts them as a player's keys and mouse would, the axe first, the
drill after the first kill (or 16 s), the nearest within 6 rows and 45 columns of where it began, in sight. Record with
`--write-movie … --fixed-fps 20 --quit-after 900 -- --planet --fauna --zoom 2`.

## Deliberately not handled

- **Creatures overlap** one another and the heroes, as the sim lets them: a struck herd bunches into one lump.
- **A friend's strike shows a round trip late,** as the sim's damage rises, and its tool is guessed: a running drill's bit by the
  creature, else the axe.
- **No outline or prompt** for a creature under the blade: the hit is the answer.
- **One look and one set of sounds per species;** no calls, no pain cries: sounds go through the user's audition.
