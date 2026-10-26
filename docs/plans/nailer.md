# Plan: Shots, the nailer and the sentry's bolt

## Why

Nothing in the game fights at range: the axe reaches 4.5 cells, the drill 6.5, and the sentry can only pounce. The
creatures ADR (`2026-10-26-creatures-are-composed`) put shots in the Creatures module, to come with their first user;
this plan brings two at once, the hero's nailer («гвоздомёт») and the sentry's electric bolt, on one mechanic the turret
and bosses will reuse. The sim flies every shot, so every peer sees every shot, as in Terraria.

**Order and PRs.** N-1 to N-3 are each their own branch and PR, merged before the next starts. N-1 is the sim with the
items' names (the nailer and nails can be crafted, and fired only from the dev panel); N-2 makes the nailer playable;
N-3 arms the sentry. N-1 touches Creatures, Debris and Building; N-3 the sentry's rules and look: tell the other
developer before either.

**Words.**
- A *shot* is a projectile in flight, the sim's; a *nail* is the item and its shot; a *bolt* the sentry's shot.
- A *projectile* is a shot's row of content (`Content.Shots.Projectile`); a *surface kind* is `Surfaces.Kind`, never
  shortened to "kind" here.
- *Open space* is a surface kind that `Floats` (the ship's), where shots fly straight; *on a planet* is one with
  `Gravity`. Shots never feel the ship's gravity aboard: aboard is open space to a shot.
- The *muzzle* is where the nailer's barrel ends as the shooter's client draws it, rounded to thousandths of a cell.
- A *blow* is a hit on a creature or a wreck that the sentry answers (Debris' `Piece.Struck`, Creatures' `Hit`).
- Times are in ticks (20 a second), lengths in cells, positions and speeds in thousandths of a cell (`CreaturesModule.Unit`)
  and thousandths of a cell a tick; distances to heroes by box distance, as the module's. Divisions round toward zero
  unless written otherwise.

**Decided with the user** (2026-10-26):
- The nailer is single-shot, a shot every 3 s; a shot spends a nail, a miss too; out of nails a dry click.
- Nails: 2 scrap a nail at the Fabricator, 1 s a nail there, stack 100. The nailer: 1 tube, 2 plates, 2 wire, 15 s
  there (the axe's), one-handed, its art a placeholder of ours till a designer pass. Both known from the start.
- A nail takes 6 off a creature or a wreck and 1 off a block, breaking blocks where tools break things (planets), not
  aboard. No third damage number.
- The nail falls under the surface's gravity times its weight, about 30 cells at 45° on the first planet, other planets
  following; in open space it flies straight and is gone about two screens out.
- The sentry: 30 hit points (five nails), notices at 40, pounces within 20 (its pounce faster, 40 cells a second, just
  as far), and beyond that shoots a bolt: its eye glows 0.6 s, the bolt flies straight at 30 cells a second up to 60
  cells, stopped by walls, 4 units of a suit, then 2 s before its next move. A blow from beyond the pounce's reach is
  answered by a bolt.

## Tasks

### N-1 — Shots in the sim, the nail first ✅

**Built, but:** the nail's `Reach` is 288 cells (half the farthest zoom's 192-cell screen and a whole one past it); the
next id is kept as `Fired`; empty `Shots` save nothing, so the format's fixture stands; no `--fire` dev mode (the testbed's
supply crate holds the nailer and 100 nails, N-2 fires them); the fuzz's new draws moved one desync test's seed 9 to 10.
The nailer and nails have placeholder icons. ADR: `2026-10-26-shots-are-the-sims`.

- **Content** (`sim/Content/Shots.cs`):
  - `Projectile(Id, Speed, Weight, Reach, Damage, BlockDamage, Pace, HitsBodies)`: `Weight` a whole multiplier of
    `Content.Creatures.Gravity` (60), 0 for none; `Reach` in cells; `Pace` what the hit's knock is reckoned by, as a
    melee weapon's (`Hit.Pace`); `HitsBodies` whether it stops at creatures and wrecks, or only at solid cells.
  - The nail: `Speed 3000` (60 cells a second), `Weight 7` (420, which the first planet's `Fall` makes 294: about 30.6
    cells at 45°), `Reach 240` (about two screens at the default zoom; measured once from the camera and written down),
    `Damage 6`, `BlockDamage 1`, `Pace 10` (the axe's: a full knock), `HitsBodies true`.
  - `Gun(Item, Ammo, Projectile, Pace)`: the nailer, `Gun("nailer", "nail", Nail, Pace 60)`; `Pace` is the client's
    (0034), as the harpoon's cooldown.
  - Items `nailer` (1, `Gear`, `Tool`) and `nail` (100); crafts `nailer` (tube 1, plate 2, wire 2; `Ticks 60`) and
    `nail` (scrap 2; `Ticks 4`: 1 s at the Fabricator, whose `Speed` is 200, a fifth of full), both in its rows. The
    nailer is not a `Weapons` row: nothing melees with it.
  - `ContentTests`: every projectile's `Speed` and `Reach` above 0, `Damage` at least 0.
- **i18n** (here, since the crafts show): `item.nailer`, `item.nail` and their `.about`; `creatures.noGun`.
- **State** (`CreaturesState`): `Shots`, a list in id order of `Shot { Id, Projectile, By, X, Y, VX, VY, Left }`
  (`Projectile` its row's id; `By` a `Ref.Hero` for a nail, `Ref.None` for a bolt; `Left` ticks of flight left), and
  `NextShot`. Both start empty and 0: no format bump (`added-save-fields-need-no-bump`). What a hit takes is read from
  the row.
- **`Fire(Tool, X, Y, Heading)`**: the muzzle in thousandths of a cell, the aim a `Headings` step. Refused
  `creatures.noGun` (no `Gun` for `Tool`, or the sender carries none: `ToolStack(peer, gun.Item)` — carried, not wielded,
  as the sim knows no wielded slot), `creatures.blocked` (the muzzle off the grid), `items.notEnough` (`ShortOf` the
  ammo; never in creative). Applied: one nail through `IItemsOps.Take` (none in creative), a shot at the muzzle,
  velocity `Rays.Direction(Heading)` × `Speed` / `Unit`, `Left` = `Reach × Unit / Speed` rounded down. Why a command:
  `Strike`, `Hew` and `HewWrecks` report a hit the client saw; none starts a flight or spends on a miss.
- **The flight step**, public and static in Core (`Shots.Step`), integer only, so the client predicts by the same
  code: given a shot, the surface kind's gravity and a test for what stops it, `Left` drops by one, `VY` gains the
  gravity (`Kind.Fall(Content.Creatures.Gravity × Weight)`, none where the surface kind `Floats`, since `Fall` returns
  its argument there), and the walk runs from the old point to the new by a vector overload of `Rays.Cast`. The walk
  only finds: the first cell it enters (the muzzle's own included, on the first step) where something stops the shot,
  and the point there. At an exact corner it tests the x neighbour first. Nothing is changed during the walk.
- **The system**, in the simulation phase after the creatures move (and after Debris' `Drift`, registered before):
  shots in id order, one made this tick included. Each steps; in the cell found, a solid cell first (the walker's
  solid: wall, floor, closed door, the grid's edge, the fog past a planet's zone), then, for a `HitsBodies` shot, the
  lowest-id live creature whose box the cell overlaps, then the lowest-id wreck the axe could hit there (a `Wreckage`
  kind, its middle outside, as `HewWrecks`' filter). Heroes never. Once the walk returns, the hit is applied and the
  shot ends; `Left` at 0 ends it; off the grid ends it. Ended shots leave the list and are listed in `Ended` for the
  tick (derived, `[JsonIgnore]`, cleared each tick): where each ended and what it met, for the client's puff.
- **What a nail's hit does:**
  - a creature: `Hurt(damage, new Hit(By, Away, Pace))`, knocked along the shot's way: `Away` is `VX`'s sign, and for a
    shot with `VX` 0 the side from the shooter's hero to the creature, right when level; `Retaliate` measures the
    shooter's distance as now;
  - a wreck: `IDebrisOps.Hit(peer, piece, damage)`, the body of `Apply(HewWrecks)` extracted (damage, `Struck`,
    breaking, its loot and `Found`) and called by both;
  - a cell where the surface kind's `ToolsBreak`: `IBuildingOps.Hit(peer, cell, damage)`, Building's first interface,
    the one-cell path `Hew` runs (a wall at its material's strength, a built piece at its build's), after the same
    filter `Hew` applies: bedrock, lava, `Fixed` ruin pieces, what stands `Behind` another, back walls and a relic's
    cradle take nothing, shock no one, and stop the nail. Aboard, the nail stops and nothing breaks. Creatures takes
    Building as a dependency: Building is registered first and depends on nothing of Creatures (`Registry.cs`).
  - Yield: to the shooter as `Hew`'s, or, with no hero of `By` on this surface (gone aboard, left), on the floor at
    the cell, anyone's (`FloorStack.Nobody`).
- **Blows from the simulation phase and the sentry.** Guard's answered stamp today is `Debris.Clock + 1` from a hit,
  because `Strike` lands in the commands phase before `Drift` advances the clock. A shot lands after `Drift`, so its
  stamp is `Debris.Clock`: the module passes "the debris clock once this tick has drifted" to `Hurt`, `Clock + 1` from
  a command and `Clock` from the shot system. A wreck hit by a shot is stamped `Debris.Clock` as any blow.
- **Queries:** `Shots` and `Ended` (read-only), for the client.
- **Dev:** `--fire <heading>`, a nail from the hero's hand (`dev/logic/DevModes.cs`), and the dev panel's give for both
  items.
- **ADR** `YYYY-MM-DD-shots-are-the-sims`: shots live in Creatures and the sim flies them; the flight step shared with
  the client; the hit order; what a hit takes from content; blows stamped by the phase they land in.
- **Done when** scenario tests cover:
  - `Fire`: a nail spent; `creatures.noGun` (unknown tool, and none carried), `creatures.blocked`, `items.notEnough`;
    creative spends none.
  - Flight: on the first planet a nail at 45° lands 30–31 cells out on flat ground; in open space it flies straight
    and ends at its reach (80 ticks); fired with the muzzle in a wall it ends there at once; at an exact corner one
    thing is hit.
  - Hits: a loper takes 6 and runs the shot's way, not retaliating from 30 cells; a nail straight down knocks it to
    the side away from the shooter; a planet wall takes 1, a wall aboard none, bedrock none; a wreck takes 6 and is
    `Struck`; a scrap piece is passed; in one cell a wall wins over a creature, the lower id between two creatures; a
    hero is passed; the shooter gone aboard before the hit, its yield falls on the floor.
  - The sentry: a nail on it, then an axe on its wreck the next tick, and the second blow is answered too.
  - Save and load mid-flight plays on alike.
  - Invariants (FsCheck): every shot on the grid; `Left` never past its projectile's; ids only grow; no shot ends past
    a solid cell it crossed (proven to bite once). `Creatures/Fuzz.cs` draws `Fire`.
  - Creatures', Debris' and Building's READMEs say what changed; the map gains Building's interface.

### N-2 — The nailer in hand

- **The shot:** held, a click fires when the gun is ready (`Gun.Pace`, kept on the hero as the harpoon's cooldown, so a
  slot swap does not dodge it): the client sends `Fire` with the muzzle and heading as integers, and at once flies its
  own nail from those same numbers by `Shots.Step`, with this client's grid and creatures, to where the step ends it.
  It never draws the sim's shot of its own hero; friends draw the sim's. With no nails (`ShortOf`), a dry click and
  nothing sent. How many are left is the nail stack's own count in the inventory and hotbar, as for any item: nothing
  new is shown.
- **Drawn:** `game/features/creatures/ShotsView.cs` draws the sim's shots, interpolated between ticks; a nail a short
  grey streak along its way; a puff and a chip where one ends, from `Ended` (the shooter's own from its prediction's
  end). A line in `SurfaceViews.cs`.
- **The nailer:** one-handed, a placeholder sprite of ours (a short barrel, a magazine of nails), the hand's pose as
  the harpoon's; its reload shown on the gun (a nail sliding in over the 3 s).
- **Sound:** the shot (a pneumatic thunk) and the dry click, at the gun; the hit, where it ends; friends' as the
  harpoon's (0065), at `Sound.ToolReach`.
- **Done when** frames show: a nail arcing on the first planet and chipping a wall; a nail straight in space; a loper
  hit and running; two windows (`--host`/`--join`) each seeing the other's nail. A unit test: the client's predicted
  flight from the sent numbers ends where the sim's does on the same world. The crew README says how the nailer fires.

### N-3 — The sentry's bolt

- **Content** (`Creatures.Sentry`): `Hp 30`; `Guard(Notice 40, Strike 20, AlertTicks 20)`; `Pounce(WindUp 16, Speed
  2000, Ticks 10, Damage 6)`, about 20 cells, so `Strike` meets its run and no hero is left between the two; a second
  move `Shoot(WindUp 12, Recover 40, Projectile Bolt)`; the bolt `Projectile("bolt", Speed 1500, Weight 0, Reach 60,
  Damage 4, BlockDamage 0, Pace 0, HitsBodies false)`.
- **`Shoot`**, a move on the existing clock, not interruptible, as the pounce: wind-up (the eye glowing), then at the
  step to `Active` the bolt leaves its middle toward its target's reported cell as it is then (its grudge, else the
  nearest awake hero within `Notice`; none, and the move ends with no bolt), velocity by integer division on the
  longer axis as the pounce's; `Active` then runs `Recover` ticks; `Hits` is false (the bolt hits, not the body). The
  sentry stays clamped; the move over, `Guard` has it again.
- **`Guard`** picks the move: a hero within the pounce's run, the pounce; beyond, within `Notice`, the bolt. A blow
  answered (`Target`): the same choice by the striker's distance. A bolt tests solid cells only.
- **`Bolted(Shot)`**, the hit hero's client's (the ADR: hits on a hero are that hero's): pays the bolt's `Damage` from
  the suit and ends the shot. Refused `creatures.gone`: no such shot in flight, or not a bolt. One bolt hurts one
  hero. A bolt whose sentry died still flies and hurts. The client sends it when the bolt drawn here touches its hero.
- **Drawn:** the bolt a small bright arc of light (an `ILightSource`, cyan-white), crackling; the eye's wind-up for the
  bolt a flicker distinct from the pounce's crouch; sparks where a bolt ends (`Ended`); the hero's hurt as a pounce's.
- **Done when** scenario tests cover: five nails kill it; it notices at 40; a hero at 30 cells gets a bolt 12 ticks
  after the alert's end, stopped by a wall or ended at 60 cells; within 20 it pounces; struck from 50 it shoots, from
  15 pounces; the target gone during the wind-up, no bolt; `Bolted` costs 4 once and ends the bolt; a second
  `Bolted` on it, or one after it ended, refused; a dead sentry's bolt still hits. `Creatures/Fuzz.cs` draws `Bolted`.
  Frames (`--sentry`) show the eye's flicker, a bolt in flight and its hit. The concept's catching and dangers lines
  and the Creatures README say what changed.

## Not in this plan

Picking nails back up; recoil; pinning; an ammo slot or readout; modules on the nailer; the sentry's circuit drop; the
harpoon's rod as a shot; nails hurting heroes; bolts hurting creatures, wrecks or blocks; nails cutting plants; lag
compensation; leading a moving target; gravity for shots aboard; a fire rate checked by the sim.

## Deliberately not handled

- **A drawn nail with no shot behind it:** a `Fire` refused as it applies (the last nail crafted away that tick, the
  gun dropped) leaves the shooter's drawn nail flying with nothing in the sim. Rare; nothing is hit.
- **The shooter's nail and the sim's may part:** its own prediction runs on what this client sees, so it may end a
  cell off, or pass a creature, before the sim's hit lands. Cosmetic; what it hit is the sim's.
- **After a load or a resync** a nail in flight keeps its saved `By`; the player now in that slot sees it as a
  friend's, drawn from the sim.
- **The muzzle is the client's:** the sim checks only that it is on the grid, not that it is at the hero's hand.
  Friends are trusted (0002).
- **A sentry struck from past 60 cells** fires a bolt that falls short; past `ThreatReach` 96 it aims at a stale cell.
- **Bolts on friends' screens:** a bolt that hit a hero flies on a moment there; a hero's client that saw a hit after
  the sim's bolt ended is refused, and that hero takes nothing.
- **Saves with sentries** (far-field derelicts): one already at 30 damage or more dies at its next hit; a pounce in the
  air finishes its 17 ticks.
- **Aboard, nails fly straight** and break nothing; rooms are short.
