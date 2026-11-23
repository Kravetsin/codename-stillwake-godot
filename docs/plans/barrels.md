# Plan: fuel drums, and every burst hurts everything

## Goal
A fuel drum drifts among the space debris. Broken, it bursts: a threat near it, a weapon against a pack, and, reeled into
a cluster of debris with the harpoon, a way to salvage several pieces at once (the user's idea). With it every burst
becomes one mechanic: it hurts every body in its reach and sight, heroes, the one who set it off among them, and creatures.

## Rules (confirmed with the user, 2026-11-23)
- A drum is a wreckage kind, `fuelDrum`: 4 hp, every weapon breaks it, the harpoon reels it, loot one scrap chunk.
- **Broken, it bursts with no fuse**, at the cell of its middle when it broke (clamped to the grid), by whoever broke it:
  that peer's own-ship tenth applies, kills count as that peer's as a shot's do; a creature's shot or burst breaks it as
  `FloorStack.Nobody`. Broken by a hew command (axe, tools: commands phase) it bursts that tick; broken by a shot or a burst
  (Creatures' simulation) it bursts the next tick. So a chain ripples a tick a link.
- **Every burst hurts every body** (the user: «Взрывы наносят урон всему, с чем взаимодействуют»; «от собственного заряда
  игрок тоже должен получать урон без исключений»): every awake hero and every live creature within its reach and in its
  sight takes the blast's `Body`, the one who set it off included, the catcher hurt by its own shell too; the blasting
  charge too, on planets as well. Today a burst hurts no creature and a hero's burst no hero: both rules go.
- A body hit directly by a shot is not hurt again by that shot's burst ("a charge hurts once at most" stands).
- The one damage rule stands: a player's burst on their own ship a tenth, everything else full (`Building.Burst` already).

## Numbers (`sim/Content`)
- `Blast` gains `Body`, the only damage a burst does to a body: the charge 10, the shell 3, the missile 6 (each its
  projectile's `Damage` today), `Blasting.Drum = new(Power: 300, Reach: 6, WreckDamage: 60, Body: 8)`: under the charge
  (500/8/120) as drums are free; check against the content while building and write the reason in its comment: 300 should
  crack a scrap hull cell without breaking it, 60 break every stream piece, 8 kill a breaker (4 hp).
- `Wreckage` gains a table `Bursts` beside `All`: `(Item, Blast)`, today only `("fuelDrum", Blasting.Drum)`; `fuelDrum` gets
  its `All` (4 hp) and `Sizes` rows, loot, item and names (En and Ru).
- Spawn weights: 4 in the ship stream's kinds; in the far field's three tiers 4, 6, 8, about the weight of `tankCluster` in
  each; none in the asteroid band or stretches.

## Design
- **Debris** owns the drum's break: when `Hit` breaks a piece listed in `Wreckage.Bursts`, its loot falls as any wreck's
  and it appends a `PendingBurst(int Cell, int Peer, string Kind)` (a record with public properties: tuples serialize as
  `{}`) to a saved `List<PendingBurst> Bursting` in its state, a new field empty by default (no format bump). `Kind` names
  the blast. It is saved, not derived, because shots break drums after that tick's drain.
  `IDebrisOps.TakeBursting()` swaps in a new empty list and returns the old one, so drums broken while it is drained wait
  for the next tick. Debris and Creatures run on every surface, so the list is always drained.
- **Creatures** owns every burst (it alone reaches walls, wrecks, heroes and creatures). `Burst(Shot s, …)` becomes
  `Burst(int peer, int cell, Blast blast, Shot? shot)`, shared by shots and drums. Its `Fly` system: clears `_ended`, `_hits`
  and the drum bursts' derived list; takes the wreck snapshot; drains `TakeBursting()` in order; then the shots as now;
  then removes dead creatures whether or not any shot flew. Inside a burst: the grid's wave; wrecks in spawn order, a piece
  already gone skipped; heroes in crew order (`Blow`, as today: distance from the cell, `Sees`); live creatures in id order,
  in reach by their body's box as wrecks are and in sight of its middle, hurt through `Hurt` with the hit a shot of that
  peer would give, no knock. The shot's direct target, if any, is skipped.
- **Client:** the drum's look is the user's pick (a pick page, while the sim is built; a plain stand-in until then); its
  burst is drawn as a shot's burst is today (`game/features/creatures/ShotsView.cs`), fed by the derived list of the tick's
  drum bursts. No shake, no rings.
- **Next users:** an exploding station appends to such a list from Building; a drum the drone hauls is a reeled piece.

## Steps
1. Content: `Body` on `Blast` and its rows; `fuelDrum` item, `Wreckage` rows (`All`, `Sizes`, `Bursts`), loot, weights, names.
2. Debris: `PendingBurst`, `Bursting`, filled by `Hit`; `TakeBursting()` on `IDebrisOps`.
3. Creatures: the shared `Burst`; `Body` hurts heroes for every burst, the setter included.
4. Creatures: `Body` hurts live creatures (catchers' shells and missiles hurt breakers and catchers).
5. Creatures: `Fly` drains the drums' list, removes the dead always.
6. Tests (`sim/Tests`), scenarios: an axed drum bursts that tick at its cell; a shot drum the next tick; a hero in reach
   hurt, one out of reach or behind a wall not; the one who broke it hurt; a breaker in reach dies; a wreck in reach takes 60
   and drops its loot; a chain a tick a link with the first one's peer; own hull a tenth when a player broke it, full when a
   catcher's shot did; a thrown charge hurts its thrower and a breaker in reach, its direct target once; a catcher's shell
   hurts a breaker; two heroes hitting one drum on one tick: one burst, the first one's; a save with `Bursting` filled loads
   and bursts once, hash as an unsaved run. Existing tests asserting "untouched" change with the rule. Invariant: each
   drum broken bursts exactly once. `Fuzz` of Debris can draw a `fuelDrum`.
7. Client: the stand-in look, the burst drawn; then the picked look.
8. Try: the user plays the plain game in space. Ship: the Creatures README's "a burst hurts no creature" line and the
   charge's guarantee, the Debris README, an ADR superseding the burst rules of blasting-charge and companion-fights.

## Deliberately not handled
- **No fuse:** an axe blow on a drum hurts the one who struck; drums are for shooting from range.
- **No falloff, no push:** flat damage in reach; nothing is knocked or moved by a burst.
- **No burst on impact:** nothing collides (0006); a reeled drum dragged into a wreck stays whole.
- **A pack around the hero** sits inside a drum's reach: blowing it up hurts the hero too.
- **Friendly fire:** a drum one player reeled and another shot hurts both if near; the shooter's own-ship tenth applies.
- **No alarm:** the drum's look is the warning; a burst unseen off screen hurts like any blow.
- Drums only in space; none on planets, in nets or in the bag; `tankCluster` unchanged.
