# 0103 — A ram costs the suit air

- Status: accepted; slice 1 (a struck creature rams back, cornered ones too, the hit, the knock, the tells) built
- Date: 2026-10-22
- Design: `designs/hero-damage.md` (the session's scratchpad); its review outcome overrides its top, and the user's word
  after playing the first build overrides the review's trigger (below)

## Context

Creatures (0102) only run from the hero. The user wants them to answer back. The hero has no health of its own: what hurts
it today — the cold past the field, lava, a ruin's resonator, a cradle struck — takes its suit's air (`ICrewOps.Shock`), and
an empty suit passes it out (0061). This is the first time a creature hurts a hero, and bites of hostile creatures on
planets to come will follow the same path.

The first build rammed only when a fleeing creature was cornered. The user played it: "it's far too hard to find a case
where an animal attacks; I had to wall everything in. It should almost always attack after taking damage" — their idea
from the start: a fleeing creature turns back and rams.

## Decision

- **Damage is suit air.** A ram calls the existing `ICrewOps.Shock(peer, units)`: no hit points, no armour, no guard in
  the sim. Creative mode takes nothing; a hero out takes nothing; a ram may take the last unit and pass the hero out, its
  body left where it fell, as the cold would. Crew and lava are untouched.
- **Rams are species data** (`Content.Fauna.Species.Ram`, `RamChance`): the tapir 5 units, nine times in ten; the loper and
  the sixleg 3, seven in ten (the sixleg has the loper's numbers); the fluff never. Shared: `RamBackReach` 12 cells,
  `RamFleeLeast`..`RamFleeMost` 12..20 ticks, `RamReach` 6, `WindUp` 8 ticks, `Charge` 700 a tick (14 cells a second, past a
  sprint's 13.2), `RamPast` 3 cells, `ChargeTicks` 40 at most.
- **Ram back after a hit, the main way in.** Struck by a hero within `RamBackReach` (by its reported cell, box distance), a
  rammer draws `RamChance`; drawn, it flees as ever (`Mode.Retreat`) for a drawn 0.6 to 1 s, then turns back on the nearest
  hero. A retreat that meets a wall or a drop it cannot leap turns at once. Both draws happen as its mode changes, from the
  world's Rng.
- **Cornered, the second way in.** Not drawn at the hit (or struck from farther), it flees (`Mode.Flee`); a flight that must
  turn at a wall or drop it cannot leap, with the nearest hero within `RamReach` on the side it would turn to, draws
  `RamChance` again.
- **One ram a hit.** Either way it winds up (`Mode.WindUp`, 8 ticks standing still, counted once it stands), then charges
  (`Mode.Charge`) at the nearest hero as the wind-up ends, along the ground, till it is `RamPast` past it, a wall or a drop
  stops it (turned, never leaped) or `ChargeTicks` run out; a hero farther than a charge runs is let be. However the charge
  ends, hit or miss, it escapes (`Mode.Escape`): a whole flight that never rams, till it calms or is struck again. A strike
  in a wind-up or a charge knocks it and draws anew, as any strike: the counter. Jumping over the charge is the dodge. New
  mode numbers are never in an old save, so the save format stays.
- **The victim's client is the word on contact** (as on reach, 0034): its hero's body box touching a charging creature's box
  and a margin sends `HitBy(Creature)`. Refused `fauna.gone` and `fauna.notAttacking` (not charging). Applied, the suit pays
  the species' `Ram` and the creature recoils, hopping back the way it came, escaping: one hit per charge with no extra
  state, and a second hero it touched on that charge is refused and pays nothing.
- **The client's own guard**, not the sim's: after its own hit, a client sends no other for 20 ticks (`Hero.RamGuard`).
- **Felt at once, a round trip before the sim:** the hero is thrown the way the creature charged, by the ram's size
  (`Hero.KnockAcross` / `KnockUp` and their `Per`), off any ladder, and reels 24 controller steps (`Hero.Reel`): the keys
  masked and `HeroInput.Reeling` set, so the air's control does not eat the knock and a held Up takes no ladder. It flinches
  (`Poses.Flinching`, 0090's style: thrown back from the blow, arms up, over 460 ms), its suit's air bursts out toward the
  blow (`AirSwirl.Leak`), a thud through the suit; the gauge's existing flash on a drop of 5 % shows the units go. A friend
  flinches, puffs and thuds where it is when a creature that was charging leaves its charge by it.
- **The tell:** winding up, the creature faces the hero however far, crouches low and wide, nose down, ears pinned,
  trembling, and scrapes dust back with its hind feet; it snorts as it begins (a canister's hiss, low: a placeholder).
  Charging it throws dust behind it.

## Why these choices

- Suit air is already the hero's life: a second health bar would be a new resource to show and refill for one source.
- Hunting is now a fight: strike, watch it bolt, turn and crouch, then step aside, jump or strike first. A ram on almost
  every hit was the user's call after the cornered-only build showed rams too rarely to learn from.
- One ram a hit keeps it a duel, not a swarm of charges; the escape after it gives the hunter a window to follow.
- `RamBackReach` is checked at the hit, not at the turn: a 0.6 to 1 s flight at 14 cells a second carries a creature some
  9 to 15 cells, past the 10 to 12 first proposed, so a reach measured at the turn would leave most out of it. The charge
  runs to the hero and past it instead of a fixed length, so it reaches from there.
- Contact on the victim's client matches how strikes work and costs no sim geometry; between friends there is no cheating
  to guard against (principle 6).

## Deliberately not handled

- **Stale aim:** the charge goes where the hero was reported (up to a second and a round trip behind) as the wind-up ended.
- **Co-op, a second victim:** a friend the charge also touched is knocked on its own screen, but its `HitBy` is refused:
  it pays nothing.
- **The drill held on a rammer** draws anew at each quick hit and restarts its retreat: it turns back once drilling stops.
- **A stunned species never rams back** (none has a stun yet).
- **No fall damage, no knock-down tumble, no camera shake, no amber edge, no scrape sound** in this slice; the sounds are
  placeholders until the audition.
- **Fauna never stands in lava bands**, so a ram never knocks a hero into lava from its own ground.
