# 0047 — Equipment, the jetpack and the slower swim

- Status: accepted; where the block sits superseded by [0058](0058-one-place-for-every-machine-window.md) (the screen's right edge); its recipe by [0068](0068-the-harpoon-before-the-jetpack.md)
- Supersedes: of [0007](0007-spacewalks-and-airlocks.md), the tether for every hero in space (one wearing
  a jetpack has none); of the hero's controller, Shift swimming fast in space
- Date: 2026-10-17

## Context

Out in space the hero swam at 4.8 cells a second, twice that with Shift, on a 12-cell tether: space was
crossed for free, and nothing was worth making to cross it. The first thing worth making is an oxygen
jetpack, and it needs somewhere to be worn: a hand holds one thing (0022), and a pack is not held. Later
come batteries for a drill, worn on a belt, and something on the head. The suit's oxygen already is the
hero's stamina (`Exert`): what a jetpack burns.

## Decision

- **Equipment slots by the body.** An inventory is its 36 own slots and then 5 of equipment, each of a
  kind (`Inventory.Equipment`): head, back, three on the belt. An item worn names its kind in content
  (`ItemSpec.Equip`, `"back"` for the jetpack); an equipment slot holds only items of its kind. A move that
  would leave anything else in one, put or swapped in, is refused with `items.notEquipment`, so `Move`,
  `Drop`, `AfterMoves` and the client's slots work on equipment unchanged.
- **Giving, taking and picking up never touch equipment.** `Give`, `Take`, `ShortOf`, `FullFor`, `Totals`,
  a pick-up and a move onto the inventory as a whole use the own slots only: what is worn is never paid
  with, and nothing falls into a belt by itself.
- **The block on screen** sits right of the inventory's grid while it is open (moved to the screen's right edge by 0058), laid out as the body (the
  mockup's "right, by the body"): head over back over belt, each row under its name, no silhouettes in an
  empty slot. While a stack is carried, the slots that would wear it are lit green and the rest dimmed.
- **Quick equip, Terraria's:** with the inventory open, Shift and the left button on a worn kind of item of the
  hero's own puts it on the first free slot of its kind, or swaps it with the first worn there; on what is worn,
  takes it off onto the first free own slot, the hotbar first. It wins over sending to an open crate, but a
  battery still goes into an open charging station. One `Move` each, client only; the sim refuses what no longer
  fits.
- **The jetpack:** `jetpack`, a stack of one, made at the lifepod of a frame, two tubes, a canister and two
  wire in 120 ticks, opened by analysis as every craft is (0027). Its art is a placeholder (two canisters
  strapped side by side; on the hero, a canister on the back) until it is picked.
- **No tether with a jetpack worn.** A hero with one on its back ties none and flies free; the grid's edge
  stops it as a wall does. Without one, the tether stays as it was.
- **Thrust on the suit's oxygen.** Shift held in space, with a jetpack worn and air in the suit, thrusts
  toward the movement keys, or with none held toward the pointer: `Jetpack.Accel` (8 thousandths of a cell
  a step) up to `Jetpack.Top` (250, 15 cells a second, in about half a second); without thrust the speed
  falls by `Jetpack.Drag` (4) a step toward what the keys swim, some seven cells of coast. The flight is the
  client's, as all movement is (0002). **The sim owns the oxygen:** a command, `Thrust(On)`, refused
  `crew.noHero`, and `crew.noJetpack` when on with none worn; while a conscious hero thrusts with a jetpack
  on its back, every `Jetpack.TicksPerUnit` (10) ticks of thrust take a unit from its suit, the ticks short
  of a unit kept (`Hero.ThrustTicks`) so taps add up. Passing out, or taking the jetpack off, stops the
  thrust. The client sends `Thrust` whenever its own differs from the sim's, as it does `Exert`: off on
  Shift let go, aboard, the jetpack taken off or the suit empty.
- **The plume** is a puff of grey air from the pack's nozzle while a hero thrusts, the harpoon's puff
  (`Effect.Puff`, `PuffInSpace`), drawn for every hero from the sim's `Thrusting`; this hero's own from its
  own thrust at once (0014), blown against its thrust, others' against the way they move.
- **The swim is slow:** `Hero.Swim` 30 (about 1.8 cells a second), and Shift in space only thrusts. Aboard,
  walking and running are as they were. Swimming no longer exerts: the slow swim is free, flight is paid in
  thrust.
- **Canisters refill the suit, by an absolute amount.** A right click on a canister in the hero's own
  inventory (none picked), or with one chosen on the hotbar over the world, sends `Refill`: one canister
  for `Crew.CanisterRefill` units (20 of the suit's 45), up to full. Refused `crew.suitFull` with a full
  suit, `items.notEnough` with no canister (never in creative mode, where none is taken), `crew.noHero`.
  An absolute amount, not a share, so a bigger suit later does not cheapen a canister. The client asks the
  sim first (`RefillProblem`) and sends nothing it would refuse, and none while one is in flight; the
  hiss (`Sound.Breathe`, a placeholder cut from the room's hiss) plays as it is sent. The canister's card
  says "RMB breathe"; the jetpack's "Shift thrust in space".
- **Format 9.** Its step pads every inventory with the five equipment slots, empty, and gives every
  hero no thrust.

## Rejected

- **Equipment as a slice of its own** (a list per hero beside the inventory): `Move`, `Drop`, the fold of
  moves in flight and every slot on screen would need a second kind of place. Slots at the end, typed by
  position, reuse all of them.
- **Thrust in the sim's hands, or the oxygen on the client:** the hero moves on the client (0002); the
  suit is the sim's. One command a burst keeps both where they are, as `Exert` does.
- **A refill by a share of the suit:** it would shrink a canister's worth as the suit grows.
- **Silhouettes in empty equipment slots** (the first mockup): the lit and dimmed slots while dragging
  say the same, and only when it matters.

## Consequences

- A hero with a jetpack can fly anywhere on the grid, to its edge; nothing ties it back to the ship. The
  suit is the leash: some 22 seconds of thrust from full, less the breaths.
- A jetpack taken off far out leaves the hero swimming slowly with no tether (none is tied in open space):
  it swims home at 1.8 cells a second, or drinks a canister on the way.
- A right click on a canister in the inventory no longer picks half of it; in a crate it still does, and
  with a stack picked it still puts one down.
- Breathing a canister takes from the last stack of canisters, as every payment does, not necessarily
  the one clicked.
- Others see the jetpack on a friend's back from the sim's inventory, and its plume from the sim's
  `Thrusting`, a few ticks after the friend's own screen does.
- No sound for thrust yet; the hiss of a canister is a placeholder for the user's audition.
