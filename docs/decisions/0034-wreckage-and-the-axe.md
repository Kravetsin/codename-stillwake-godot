# 0034 — Wreckage and the axe

- Status: accepted; "other heroes are not seen holding or swinging the axe" superseded by [0037](0037-others-see-the-hands.md); the hit sent at the press superseded by [0038](0038-one-swing-one-hit.md); the left button held on a wreck within reach superseded by [0039](0039-melee-by-contact.md); the hull section as the one wreck and how often wrecks come superseded by [0041](0041-wreckage-of-many-kinds-and-circuits.md)
- Date: 2026-10-16

## Context

Every piece of debris was caught with one press: nothing drifting by was worth stopping for, and
there was no tool to earn. A first piece too big for the hands, broken with a tool made early, gives
the spacewalk a job and the Fabricator a reason, without new systems for physics, towing or wear.

## Decision

- **Wreckage is debris.** A hull section (3 × 2 cells) is a kind in `Debris.Kinds` like any other: it
  spawns, drifts, is saved and fades the same. Only the hands differ: `Grab` on it is refused
  `debris.tooBig`, and the client outlines it red with that reason.
- **Hit points and damage are data.** `Content.Wreckage` gives each kind its hit points (a hull section
  15), `Content.Axe` the axe's damage (2) and the client's swing pace (`SwingTicks`, 10 ticks). A
  piece keeps the damage taken (`Piece.Damage`, saved; save format 4, its step setting 0 on every
  piece of an older save). `Hit(Piece)` adds the axe's damage; refused `debris.gone`,
  `debris.notOutside` (as `Grab`, the wreck judged by its middle cell) or `debris.notWreckage`.
- **Loot as floating stacks.** The hit that takes the last hit points removes the wreck; its loot
  (`Loot`, drawn with the world's Rng as a container's) falls on the cell under its middle as floor
  stacks, which in space drift back past the ship and are picked up by swimming through them. They are
  dropped by nobody (`FloorStack.Nobody`), so the hero who broke it sweeps them up at once. No new
  small pieces, no spread, no physics.
- **The tool is the client's word.** The sim does not ask whether the sender holds an axe, nor how
  fast it swings (principle 6, friends): the client sends `Hit` only with the fire axe chosen on the
  hotbar, the hand free, the left button held on a wreck within reach, once every `SwingTicks`
  (superseded by 0039: held or clicked anywhere, a hit for each wreck the blade touches; 0040: where its drawn edge sweeps).
  The chosen hotbar item reaches the crew through `Ui.Wielded`; the screwdriver's tool slot (0030) is
  left alone. A small piece is still caught by hand, axe or not.
- **Tools never break.** The fire axe (6 scrap, at the lifepod, known from the start) has no
  durability and no better versions yet.
- **One crack filter for all debris.** The lit pictures' shader draws pixel-art cracks on any picture's
  own pixels from two instance uniforms, the share of hit points lost and the piece's id: dark lines
  and a lit edge from a widening dent, more and longer as the share grows, before the light falls on
  them. No crack art per picture; a picture never set costs a branch.
- **Feedback from the world.** A rise in a piece's damage jolts it, throws sparks and plays a chop
  where it is; a wreck gone with damage before its path's end bursts. Every peer sees and hears the
  same from its own world, so friends see cracks and hear hits; only the striker's figure swings. The
  chop is on the Suit bus, as wrecks are chopped only out in space, where the World bus is silent.

## Consequences

- About one spawn in thirteen is a wreck, one every two minutes or so; eight hits, four seconds of
  one hero's swinging; two heroes break it twice as fast.
- A wreck half over the hull cannot be hit while its middle is over the ship.
- A hit sent just after the axe was put away still lands, and creative mode chops without one.
- Own hits show when their tick runs, not at once (0014 left aside): the swing hides the round trip
  (superseded by 0038: the hit goes as the blade meets the wreck, the jolt a round trip after).
- Other heroes are not seen holding or swinging the axe.
