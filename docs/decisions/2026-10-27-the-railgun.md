# The railgun

- Status: accepted; built. Brings what [shots pierce](2026-10-27-shots-pierce.md) left for it: the pierce of everything and the memory of what a shot passed
- Date: 2026-10-27

## Context

The nailer's nail is slow, falls fast and stops at the first thing it does not break. The player's word: a railgun crafted of an
arc shard, held in both hands as the drill is, aimed straight at the pointer, straight up too; a very fast nail, little pulled
by gravity; through everything it hits, walls and ground too, losing damage at each; a fixed charge of the battery a shot; a
charge held about 0.8 s, all or nothing. Nails now; a steel bolt later, once there is steel.

## Decision

- **A gun says how it pierces, falls, draws and charges.** `Gun` gains `Pierces` (`Broken`, the nailer's: only through what its
  hit breaks or kills; `All`: through whatever its hit takes), `Fall` (thousandths of its projectile's pull), `Energy`
  (power-ticks of the belt a shot) and `Charge` (ticks held before it fires; 0 fires on the click). Defaults keep the nailer.
- **The railgun:** 22 and the nail's 2, 24 a hit, keeping 650‰: 24, 15, 9, 5, 3, 1; 4 off a block, then 2, 1; 300 cells a
  second, 384 cells' reach; a tenth of the nail's pull, 29 a tick on the first planet; 120 a shot, fifteen a battery; held 16
  ticks, then 40 till the next. Crafted at the arc bench of an arc shard, copper, coils, a tube, a frame and circuits.
- **What its hit takes, it passes.** A creature, a wreck, a cell whose solid thing a shot may break (`IBuildingQueries
  .ShotTakes`: the surface's tools break things, the cell in the grid's zone, a breakable wall or a built piece). Bedrock and
  lava, a planet's fog and everything aboard take nothing and stop it, listed as a hit of nothing (damage 0, any gun's);
  so does a target its kept damage has run out for. `Pierced` counts every pierce and folds the damage.
- **What it passed is remembered.** `Shot.Passed` holds what it hit and flew on through that still stands, never hit again: a
  body spans cells, and a tick's last cell is asked again at the next; a built piece is remembered by its first cell
  (`ShotKey`), so a door over several cells is hit once. Saved, none written while none: no format bump.
  `ShotTarget` is saved by number in it.
- **The charge is the client's, the energy the sim's.** `Fire` is refused `items.flat` when the belt cannot pay; applied, it
  spends the charge (nothing in creative). How long it was held is the client's word, as the pace is (0034): the sim has no
  charging state, so a joiner and a save never see one.
- **The guest's copy** judges the railgun's walk as the sim does (`ShotTakes`, the hit path's and Debris's checks), starting
  with nothing passed and counting every pierce as passed.

## Deliberately not handled

- Friends neither see the coils fill nor hear the charge: a friend's aim is never sent. They see the shot and its hits.
- No partial charge, overcharge, heat, or module for it. Energy is not held back while charging: a battery moved off the belt
  mid-hold makes the release click dry. Two half-empty batteries do not pay one shot.
- Block damage is gone after three cells: a creature behind four cells of stone is out of reach.
- Own walls and built pieces on a planet are scarred by a shot through them; machines, not solid to a shot, are passed.
- A shot's heading is one of 256: at 300 cells it may land a few cells off.
- Two-handed, the barrel turns as far as straight up and straight down: a point right over or under the hero's own shoulder
  is aimed at straight, the shot passing a cell ahead of it.

## Rejected

- **A charging state in the sim:** a command a tick held, or a start and a release, for no rule the sim needs.
- **A gravity of its own for the railgun's nail:** a second nail item; the fall is how the gun throws it.
- **Walls stopping it unless it breaks them** (the nailer's rule): the player's word is that a mighty weapon goes through them.
