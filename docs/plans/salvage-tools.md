# Salvage tools: the grinder, the spear, the plasma cutter and the sledgehammer

Four hand tools beside the fire axe and the drill, so the player picks a tool for a wreck and a way of playing. Settled with the user
on 2026-11-04 after a design review and a plan review; nothing has been run. Rests on
[0054](../decisions/0054-asteroids-and-the-drill.md) (all damage works alike; a wreck's hardness is only its hit points),
[0098](../decisions/0098-the-tool-bench-modules-in-tools.md) (a module rides on a tool's stack and `ToolStats.Of` folds it) and
[0040](../decisions/0040-a-melee-weapon-is-held-by-its-handle.md) (a tool held by its handle, its edge tested for contact).

## What the player meets

1. At the workbench, a sixth row: the grinder, the spear, the sledgehammer and the plasma cutter. The first three open once their parts
   are obtained, as every craft does; the cutter once its blueprint is bought on the black market.
2. **The sledgehammer:** a heavy blow every 3 s, landing 0.8 s into the swing, bursts a tank cluster (7 hit points), a soil clod (8), a
   cable rack (9) or a cargo container (11) at once; a hull section (15) takes two.
3. **The spear:** thrust at the pointer, reaching farther than the axe, a little more damage a second. The arc module doubles it, for
   no power.
4. **The grinder:** held on a wreck, it bites faster than the drill, on the belt's batteries, but only from close up (2.5 cells against
   the drill's 6.5: the price of its speed); on a wall it barely scratches. With the arc, twice that for twice the draw.
5. **The plasma cutter:** little damage a hit, on batteries, about the axe's pace on a wreck; a wreck it cuts drops half again its loot.
   One the drill bored most of and the cutter finished gets the bonus only as often as the cutter's share of it.
6. **Going wrong:** the cutter aimed at a wall, a plant or a relic's cradle does nothing to it and gives no refusal; it draws while it
   runs, as the drill does. Its card says it cuts wrecks only. A flat belt stops the grinder and the cutter as it stops the drill
   (`items.flat`).

## Numbers

All in `Content.Weapons`, one row each. Damage is a hit's hit points off a wreck or a creature, Block a hit's damage to a wall, plant
or built piece; Pace is ticks between hits (20 ticks a second); Arc in steps of a turn (`Headings`, 64 a quarter); Range in thousandths
of a cell; Draw power a tick while it runs (0055).

| Tool | Damage | Block | Pace | Arc / rays | Range | Draw | DPS | Block / s |
|---|---|---|---|---|---|---|---|---|
| fire axe (as is) | 2 | 4 | 10 | 64 / 9 | 4500 | – | 4 | 8 |
| drill (as is) | 1 | 4 | 2 | 8 / 3 | 6500 | 3 | 10 | 40 |
| `grinder` | 3 | 1 | 4 | 8 / 3 | 2500 | 4 | 15, 30 with the arc (draw 8) | 5 |
| `spear` | 3 | 2 | 12 | 8 / 3 | 6000 | – | 5, 10 with the arc | 3.3 |
| `plasmaCutter` | 1 | 0 | 4 | 8 / 3 | 3000 | 2 | 5 | 0 |
| `sledgehammer` | 12 | 24 | 60 | 32 / 5 | 4000 | – | 4 | 8 |

Recipes, all of existing parts, their craft ticks on the drill's scale (240): grinder a motor, 2 plates, 2 gears, a coil, 2 circuits;
spear 2 tubes, 2 plates, 2 wire; sledgehammer 4 plates, a tube, a plank; cutter a canister, 2 coils, 2 circuits, 2 tubes, a plate.
The sledgehammer's 0.8 s to its blow is its held look's wind-up (client), not content: content has its pace.

## Decisions

**Hardness stays hit points.** The sledgehammer's blow is 12, so every wreck of 12 or fewer breaks at once; "unarmoured" is no new
concept. A content test pins the one-blow list, so a hit-point change that moves a kind in or out is seen.

**A module in a tool with no draw:** the arc's entry for the spear is `Trade(Damage, +1000)` alone; its price is the module itself, and
the tool bench shows it drawing nothing. The grinder joins the drill's entry. The arc bench's vise takes both through `Modules.Hosted`.
The sledgehammer and the cutter take no module.

**A block hit of 0 is no hit.** `Digging.ApplyHew` returns on `hit <= 0` before the cradle's shock, the cracks and the plants, as the
shots' `Digging.Hit` already does. Today a 0 would still crack a wall at 0, fell a plant of no strength and shock the hero. The axe
and drill, at 4, are untouched by it.

**The cutter's bonus is by share.** `Piece.Cut`, `[JsonInclude]` as `Piece.Damage` is, 0 by default: the hit points the cutter's hits
took off the piece. `Apply(HewWrecks)` adds them when its tool salvages, capped so that `Cut ≤ Damage`. The roll lives at the break in
the shared `Hit`, so a shot or an axe that finishes a cut wreck still rolls by its `Cut`: after the ordinary `LootOf` draw and only
when `Cut > 0`, `Rng.Below(hp) < Cut` decides (always won where `Cut ≥ hp`, as after a content change lowering hit points); won, every
line of the drop is multiplied by 3/2, rounded down, so a single item stays single. `Record` notes the drop as it falls, bonus
included. A world no cutter touched draws exactly what it draws today. A weapon salvages when its `Effects` hold
`HitEffect("salvage", …)`, its card's chip: one source for the card and the sim.

**Unlocks:** the grinder, the spear and the sledgehammer have no research line: their parts open them. The cutter's blueprint is a
`Research` block on the black market tab, in `order4` beside the salvage drone, added as that one was (2026-11-03), with no save step.
All four go into `Market.Unsold`, as every tool is.

**The client, by motion:** a weapon with a `Draw` runs as the drill does (`HeldLook.IsDrill` becomes an electric weapon's test,
`DrillPace` its pace): the grinder and the cutter. The axe and the sledgehammer swing (`AxeSwing`, its length from the weapon's pace,
its wind-up from the look). The spear thrusts: a new motion beside `AxeSwing`, its contact the same edge test at full extension, one
hit a thrust, none if it is cut short. Others see it by `Handwork.Thrust`, added last to the enum.

**No save-format bump:** `Cut` is an added field meaning "none yet"; new items and research blocks never needed one.

## Steps

1. **Content.** The four rows, items, crafts, the workbench's sixth row (the panel grows by rows, `Crafts.Places`; a frame checks it
   fits), the arc entries, the cutter's research block, `Unsold`, names and the chip's words in `En`/`Ru`. Content tests: one per row
   of the table, the one-blow list.
2. **Building.** The `hit <= 0` guard; scenarios: the cutter leaves a wall, a plant and a cradle alone, and shocks nobody.
3. **Debris.** `Cut` and the bonus. Scenarios: a wreck cut whole always gets it; one drilled then finished by the cutter gets it by
   share (seed pinned); one cut then finished by a shot rolls too; a half-cut wreck saved, loaded and broken rolls alike; the arc spear
   deals 6 and is never refused `items.flat`; the arc grinder deals 6 at draw 8. Invariant: for a live piece `Cut ≤ Damage < hp`; with
   no cutter hit the draws and the loot equal today's. Prove it bites by dropping the cap once.
4. **Art, picked.** The user's picks (pages 2026-11-04: grinder, plasma cutter, spear, spear thrust, sledgehammer) drawn by
   `tools/anime-art` (`zfittings` beside the axe and drill), icons, held looks; each look whole, no stand-in.
5. **Client motions.** The electric run for the grinder and the cutter, the paced swing for the sledgehammer, the thrust for the spear;
   frames read from a recording of each against a wreck.
6. **Sound.** A pick page of CC0 candidates per tool, then cut, levelled and credited (`game/audio/CREDITS.md`).
7. **Docs.** An ADR for the share bonus, the 0 guard and modules without draw; the Debris, Building and crew READMEs; a concept line;
   this plan deleted.

## Deliberately not handled

- The cutter on a wall draws and does nothing: no refusal, no saved charge; its card says wrecks only.
- A swing or thrust whose tool is put away, dropped or lost before its contact sends nothing, or is refused on its tick; that blow is lost.
- Two blows on one wreck on one tick: the second finds it gone and is lost.
- Two cutters on one wreck: their cut sums, still capped.
- A half-cut far wreck that leaves the world loses its share, as any half-broken wreck does.
- A modded client ignoring a tool's pace (friends, 0034).
- The bonus on creatures, grabbed containers or net catches; modules in the sledgehammer or the cutter; Thermal in the spear.
