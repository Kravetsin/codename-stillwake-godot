# The blasting charge and the deep caches

- Status: accepted; its crater to dust and ore to the thrower superseded by [unified-damage](2026-11-09-unified-damage.md)
- Date: 2026-11-08

## Context

Playtesters found the first planet's descent dull: a thousand rows of the same rock, no tool for going down and nothing to
find between the depth bands. The user accepted a design of two reasons to dig deep: sulfur ([deep-sulfur](2026-11-08-deep-sulfur.md)) made into a blasting charge, and caches in each band's
rock holding something only sold.

## Decision

- **The charge** (`blastCharge`, stack 50): 2 sulfur and a tube at the workbench, 40 ticks; learned by the rule that grants
  (`Research.Understood`) once sulfur is had. In the hand it is a gun that fires itself (`Shots.Thrown`): its own 10 off a
  creature or wreck it strikes, a lob of weight 3 (some 18 cells at 45° on the first planet), at most 40 cells, a throw a
  second. Refusals are the gun's: `creatures.noGun`, `creatures.blocked`, `items.notEnough`.
- **The burst** (`Content.Blasting.Charge`: `Power` 63, `Reach` 8, `WreckDamage` 120): wherever its shot ends it spreads
  the meteor's wave ([meteor-wave](2026-10-29-meteor-wave.md)), moved to `sim/Core` `Wave` so both share it. A cell costs
  `Meteors.Step`, a wall `PerStrength` × its armour more: stone 10 a cell, soil 4, ruin alloy 16, 7/5 of that aslant, within
  `Reach` cells: round ([round-blasts](2026-11-09-round-blasts.md)). It starts in the open cell the shot flew from when it met something, so the wall it met is
  entered at its cost. Measured: soil opens the whole circle, stone a bowl 6 out, alloy 3 (a third smaller than first built: the user found it too big); on a floor it runs along the air first, a wide bowl.
- **It breaks natural ground only** (`Material.Breaks`, where `Rules.ToolsBreak`; the user's call: built walls stay). A built
  wall, a door, a ladder, a machine, bedrock or fog stops it whole; aboard the ship nothing breaks and the charge is spent.
  Walls reached break in cell order through `IBuildingOps.Blast`: an ore's cost (`Material.In`) and the yields of the growths a
  cell held go to the thrower (`GiveOrDrop`; with the thrower gone from the surface, to anyone); plain ground goes to dust, so a
  crater of 300 stone is a way down, not 300 rock.
- **Wrecks:** every wreck whose box lies within `Reach` of the burst's cell takes `WreckDamage` in spawn order, the thrower the
  hitter; the one struck takes its direct 10 as well.
- **Deep caches:** four rock growths, one a band (`frostCache`, `bloomCache`, `amberCache`, `emberCache`), chance 3 in its
  own band, `Apart` 200; appended after every cave growth so they take only rolls nothing took. About 16, 18, 13 and 8 a
  planet (a test pins ±30 % over 16 seeds). A cache's cells move at most a moss or a mica a few cells off, and the ore pockets
  that skip growths' cells, nothing farther (pinned too). Dug or blasted, one gives 1, 2, 4 or 7 `opal`.
- **The opal** (stack 50, worth 150, used in nothing): sold only, one item for every band (the user's call); an amber cache is
  worth a geode's six crystals. Caches ring and shatter as crystals do.
- Nothing new is saved: shots in flight already are; old planets grow caches in rock nobody dug. No format bump.
- **Why not what exists:** every tool hits a fan of a few cells by the hero, none an area from afar; the meteor's wave is the
  area break wanted (cost spread, walls soaking), so it moved to `Core` rather than a second, square radius beside it; no growth
  yields what only sells, and growths are already scattered by band. One `opal`, not four: the caches differ by look and count,
  and four items would add icons, prices and names for nothing the player does differently.
- **Cost:** a burst walks a circle of 8 once, in the tick it lands; the client remembers the walls round a charge in flight
  (`ShotsView`), a box of 21×21 a tick, and nothing per frame but the burst's look.

## Looks and sounds

The user's picks (2026-11-08), each from four candidates on its audition page:

- **The charge, "pipe":** an iron pipe packed with sulfur, screw caps, a short fuse; in flight it turns twice a second, the
  fuse sparking. Its burst: a white flash, a warm afterglow of burning sulfur, some ninety sparks curling and falling, long streaks and drifting embers (the user's
  ask after a first try: it went off too plainly in the dark), a grey ring of dust as wide as what broke, and out of each broken wall chips of
  its ground flung away from the burst, falling where there is weight (none afloat).
- **The caches, "ammonite":** a fossil shell turned to opal, half in the rock, its chambers glowing in its band's colour,
  larger deeper; the opal's icon is its shell.
- **Sounds:** the throw a short whip (`crew/charge-throw`), heard as a friend's shot is; the burst a litre of oxyhydrogen set
  off in the open (`crew/charge-burst`), heard full to 24 cells and to 120, the thrower's on the suit, so in space too.

## Deliberately not handled

- Harm to heroes from a burst, and area harm to creatures: a direct hit only. A friend standing in a crater is untouched,
  though the ground under them may go; no fall hurts.
- Back walls; a placed charge, a fuse, chains of charges; sulfur in anything but the charge.
- Rock from a crater; a ruin wall's alloy blasted is lost, the price of blasting instead of cutting.
- Caches on other planet types, detectors or a sound through rock for them; a cache that refills.
- A charge thrown point-blank, born and burst in one tick, or a burst in a tick a client skipped: its flash and ring show, no chips.
- Laid stone cannot be told from ground: a charge breaks a stone wall a player built on a planet, as a tool already does.
