# The salvage tools

- Status: accepted; the grinder's draw of 4 and the cutter's of 2 amended to 2 and 1 by [lighter-tools](2026-11-06-lighter-tools.md)
- Date: 2026-11-04

## Context

Wrecks were broken with two tools, the axe and the drill. The user wanted more of them, some only stronger, some doing something
of their own. Of the brainstorm the user kept four: an angle grinder better than the drill on wrecks and nearly useless on blocks; a
spear a little better than the axe that takes tech modules ("nothing burns in space", so no lance); a plasma cutter from the black
market that gets more loot out of a wreck; and a sledgehammer, a blow every three seconds that breaks light, unarmoured wrecks at
once. A demolition charge waits for a material to make it of (sulfur, perhaps); a bow grinder waits for the salvage drones' turret.

## Decision

- **Four weapons** (`Weapons`): the grinder 3 a hit every 4 ticks (15/s), 1 on blocks, 2.5 cells, drawing 4; the spear 3 every 12
  ticks (5/s), 6 cells, no power; the cutter 1 every 4 (5/s), 0 on blocks, 3 cells, drawing 2; the sledgehammer 12 every 60 (4/s),
  6 on blocks, a fan of 32 over 5 rays. All are made at the workbench, a sixth row; the cutter's blueprint is a block on the black
  market's `order4`. None is sold (`Market.Unsold`).
- **Hardness stays hit points** ([0054](0054-asteroids-and-the-drill.md)): the sledgehammer's 12 breaks every wreck of 12 or fewer at once,
  pinned by a content test, so "unarmoured" is no new concept. Its 6 on blocks stays under the weakest ground (stone, 12) and plant
  (a bush, 8): a wall still cracks first.
- **A module in a tool that draws nothing.** The arc's entry for the spear is `Trade(Damage, +1000)` alone, paid by the module itself;
  the grinder joins the drill's entry, twice the damage for twice the draw ([0100](0100-tech-branches.md)).
- **A block hit of 0 is no hit.** `Digging.ApplyHew` returns on it before the cradle's shock, the cracks and the plants, as the
  shots' `Digging.Hit` already did: the cutter aimed at a wall, a plant or a relic's cradle does nothing and is refused nothing.
- **The cutter's bonus is by share.** A piece keeps `Cut`, the hit points a salvaging weapon's hits took (`Weapons.Salvages`, its
  `salvage` effect, which is also its card's chip). Whatever breaks a piece with some cut, a shot or an axe too, draws
  `Rng.Below(hp) < Cut`; won, each line of its drop is half again, rounded down (`Weapons.CutBonus`). So drilling most of a wreck and
  finishing it with the cutter wins the bonus only as often as the cutter's share. A piece never cut draws exactly what it drew
  before, and `Cut` is unwritten at 0: no save-format bump.
- **In the hand**: the grinder and the cutter run as the drill does, on the belt, hitting at their pace. The axe, the sledgehammer and
  the spear are struck with by one clock (`AxeSwing`, its `Stroke` per tool): the sledgehammer's swing is the axe's slowed so the blow
  lands 0.8 s in, then rests out its three seconds; the spear's is the picked jab, aimed as a gun is, the elbow cocked and
  straightened, the hit at 215 ms of its 0.6 s. Others follow it as they follow the axe, by what the hero wields: no new message.

## Consequences

- The grinder outpaces the drill on wrecks threefold, and the arc doubles that to 30/s: the user's call, for weapons of thousands of
  damage are planned. On walls it is the worst tool there is.
- The cutter's loot is a gamble per wreck, never a sure half; a whole cut is sure.
- **Looks, as picked**: the grinder a long ochre motor tube with a cast head and a half-guarded wheel; the cutter its jet run through
  three copper coils on a rail, lit violet; the spear a hull plate's leaf in a flattened tube bound in copper; the sledgehammer five
  bolted plates on a plank.
- **Sounds, as picked** (`Sound`): the grinder a running motor looped on the drill's spin, its wheel biting metal as it comes on
  something and every 0.6 s while it stays; the cutter one recording's jet, struck as it starts, looped, cut off as it stops; the
  spear a bamboo whoosh and a knife on a beam; the sledgehammer a slow low whoosh and a big metal hit, heard with the break too as
  it breaks a wreck. The grinder's and the cutter's hits on a wreck play no chop, their own sound is it; the cutter at a wall,
  dealing nothing, sends and plays nothing.
