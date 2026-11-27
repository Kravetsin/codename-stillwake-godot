# The drone's core is its stamina, and its wheel has two branches

- Status: accepted
- Date: 2026-11-27

## Context

The drone had two modes on F and one action on the right button, the throw, which cost nothing. The user wanted its
forms finite, the lamp too, so that choosing between them matters, without risking a player left in the dark and
without another bar on the screen. A plan for a first gun form (a nail form fired from the drone, its charge filled
only by loot, Warframe's Incarnon rule) was built and played: the core and the wheel were liked, the nail form was not
(see below).

## Decision

- **The core is the drone's stamina** (`Players.Cores`, one `Core(Peer, Charge)` per paired peer, saved only while
  any, clamped to `Abilities.Full` on load). Each tick it fills by `Abilities.Regen` less what its light draws
  (`DrainOf`: the lamp 2, the scanner 3, so light alone never empties it), in creative too; from empty it is full in
  10 s. Loot adds `PerWorth` a worth (`Crew.Loot`, beside the catcher meter, which creative still skips). Nothing fills it
  unpaired or out, nor by itself during the meeting.
- **Light and actions reach the sim as commands.** `Shine(Form)` says what the drone shines, sent by the client when
  its shown mode changes and again while the sim has not heard it; `Cast(Ability)` spends `CostOf` and is refused
  `crew.tired` below it. The throw costs a sixth of a full core: a throw every 2 s is paid by the filling, spammed, some six
  empty it. Too tired, the right button gives the gun's dry click and nothing flies.
- **The core shows its charge on the drone itself:** the coal glows as full as it is, embers fly in from loot (not
  from its own filling), and six beads, one a throw, arc over it while it is short of full, the filling one partly
  lit, gone a moment after it fills. There is no pulse at full: the user found it irritating.
- **The F wheel has two branches,** actives (the throw) and modes (lamp, scanner); the right button inside it switches
  branch, and the chosen side is lit. A tap of F still cycles modes. Its look is a stand-in the user dislikes; their
  idea for later is the drone itself in the middle, reshaping as the pointer passes over a form.
- **Breakers always drop one scrap chunk,** so a fight fills the core.

## The combat form is not decided

The nail form played as "a nailer fired from another place": no power, the nailer's sound and speed. The user doubts
the drone should become a gun at all. Of four directions offered they liked the drone winding its threads into the
weapon in hand (Incarnon-like), but it leaves open how a player learns that it works on some weapons and not every
one. A new design starts from that doubt; the core and the actives branch are ready for it.

## Deliberately not handled

- **No upgrades yet:** a bigger core, slower drain, faster filling are the expected growth, undesigned.
- A raised throw cancelled before it leaves keeps its cost spent.
- A full core shows nothing; an empty one has no look of its own, since light alone never empties it.
- Friends' beads show as their own do, read from the same charge.
