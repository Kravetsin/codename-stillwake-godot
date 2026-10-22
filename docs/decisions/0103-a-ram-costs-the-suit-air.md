# 0103 — A ram costs the suit air

- Status: accepted; slice 1 (cornered creatures ram, the hit, the knock, the tells) built
- Date: 2026-10-22
- Design: `designs/hero-damage.md` (the session's scratchpad); its review outcome overrides its top

## Context

Creatures (0102) only run from the hero. The user wants them to answer back: a cornered animal turns on the hunter. The
hero has no health of its own: what hurts it today — the cold past the field, lava, a ruin's resonator, a cradle struck —
takes its suit's air (`ICrewOps.Shock`), and an empty suit passes it out (0061). This is the first time a creature hurts a
hero, and bites of hostile creatures on planets to come will follow the same path.

## Decision

- **Damage is suit air.** A ram calls the existing `ICrewOps.Shock(peer, units)`: no hit points, no armour, no guard in
  the sim. Creative mode takes nothing; a hero out takes nothing; a ram may take the last unit and pass the hero out, its
  body left where it fell, as the cold would. Crew and lava are untouched.
- **Rams are species data** (`Content.Fauna.Species.Ram`, `RamChance`): the tapir 5 units, two times in five; the loper and
  the sixleg 3, about one time in seven (the sixleg has the loper's numbers); the fluff never. Shared: `RamReach` 6 cells,
  `WindUp` 8 ticks, `ChargeTicks` 10, `Charge` 700 a tick (14 cells a second, past a sprint's 13.2).
- **One trigger, in the sim.** A fleeing walker that must turn at a wall or a drop it cannot leap, with the nearest hero
  (box distance, by reported cell) within `RamReach` on the side it would turn to, draws `RamChance` from the world's Rng —
  a draw only at that possible change of mode. On a hit it winds up (`Mode.WindUp`, standing still), then charges
  (`Mode.Charge`) at the nearest hero as the wind-up ends, along the ground; a wall or a drop ends the charge, turned, in
  flight, never leaped; with no hero, or one level with it, it flees instead. A strike during either knocks it and it flees,
  as any strike does. New mode numbers are never in an old save, so the save format stays.
- **The victim's client is the word on contact** (as on reach, 0034): its hero's body box touching a charging creature's box
  and a margin sends `HitBy(Creature)`. Refused `fauna.gone` and `fauna.notAttacking` (not charging). Applied, the suit pays
  the species' `Ram` and the creature recoils, hopping back the way it came, fleeing: one hit per charge with no extra
  state, and a second hero it touched on that charge is refused and pays nothing.
- **The client's own guard**, not the sim's: after its own hit, a client sends no other for 20 ticks (`Hero.RamGuard`).
- **Felt at once, a round trip before the sim:** the hero is thrown the way the creature charged, by the ram's size
  (`Hero.KnockAcross` / `KnockUp` and their `Per`), off any ladder, and reels 24 controller steps (`Hero.Reel`): the keys
  masked and `HeroInput.Reeling` set, so the air's control does not eat the knock and a held Up takes no ladder. It flinches
  (`Poses.Flinching`, 0090's style: thrown back from the blow, arms up, over 460 ms), its suit's air bursts out toward the
  blow (`AirSwirl.Leak`), a thud through the suit; the gauge's existing flash on a drop of 5 % shows the units go. A friend
  flinches, puffs and thuds where it is when a creature that was charging leaves its charge by it.
- **The tell:** winding up, the creature crouches low and wide, nose down, ears pinned, trembling, and scrapes dust back
  with its hind feet; it snorts as it begins (a canister's hiss, low: a placeholder). Charging it throws dust behind it.

## Why these choices

- Suit air is already the hero's life: a second health bar would be a new resource to show and refill for one source.
- A single sim trigger (cornered) keeps rams rare and readable: the hunter learns that a beast backed against a cliff
  fights back, and can step off, jump or strike first. A ram on every hit would make the axe a trade.
- Contact on the victim's client matches how strikes work and costs no sim geometry; between friends there is no cheating
  to guard against (principle 6).

## Deliberately not handled

- **Stale aim:** the charge goes where the hero was reported (up to a second and a round trip behind) as the wind-up ended.
- **Co-op, a second victim:** a friend the charge also touched is knocked on its own screen, but its `HitBy` is refused:
  it pays nothing.
- **A creature cornered again after its recoil** may roll again; the knock usually throws the hero out of its reach.
- **No fall damage, no knock-down tumble, no camera shake, no amber edge, no scrape sound** in this slice; the sounds are
  placeholders until the audition.
- **The drill held on a cornered creature** cancels every wind-up with its quick hits: it never gets its charge off.
- **Fauna never stands in lava bands**, so a ram never knocks a hero into lava from its own ground.
