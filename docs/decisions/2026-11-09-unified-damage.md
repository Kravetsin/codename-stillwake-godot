# One rule for damage

- Status: accepted; built. Supersedes in part [blasting-charge](2026-11-08-blasting-charge.md) (a crater to dust, ore to
  the thrower), [shots-pierce](2026-10-27-shots-pierce.md) (a share per pierce on cells; bodies keep it),
  [round-blasts](2026-11-09-round-blasts.md) (a wall's cost by its armour, the charge's numbers) and
  [meteor-wave](2026-10-29-meteor-wave.md) (armour, Hazards' own wave)
- Date: 2026-11-09

## Context

A pickaxe, a shot, the blasting charge and a meteor each decided what they broke by their own rule: tools by `Strength`,
meteors and the charge by a separate `Armour` through a wave each module copied, shots by a share per pierce. A player who
learned one could not predict another: a charge did nothing to a scrap wall, a meteor tore 7–17 cells of hull or none,
machines never broke. The user agreed a direction (one damage that pierces with its surplus, cracks as residue, the ship
breakable at a reduced share, the charge mining); the coming meteor shower and a station that overheats and blows up are
its next users.

## Decision

1. **A hit has damage.** It breaks a cell whose remaining strength (strength less its crack) it covers, passing on the
   surplus times the hitter's carry in percent; short of that, it adds to the crack. Strengths and block damage are in
   tenths (`World.SaveFormat` 61, its step drops saved cracks): soil 40, stone 120, alloy 600, scrap 20, iron 50.
2. **Shots** carry their surplus cell to cell (`Gun.Carries`: railgun 65 at block damage 140, so it breaks two soil and
   cracks a third; the nailer 0) and stop on a cell they only crack. Bodies keep `PierceKeep`.
3. **Tools** carry one cell behind (`Weapon.Carries`: pickaxe 70 at 75), along the line from the hand (`Hew`) or the hero's
   reported middle (`HewCells`) through the broken cell, each cell its largest carry once, never carried again.
4. **A burst** (`IBuildingOps.Burst(peer, cell, power, reach, Spoils)`, `sim/Building/Bursts.cs`) is the same hit in every
   direction through the shared `Wave`: a step costs `Damage.Step` (2, 7/5 aslant), natural ground its remaining strength,
   a pile its `Hp`; built things (walls not natural, pieces, machines) `ceil(left × 1000 / BuiltTakes)` with `BuiltTakes`
   37, about a 27th, once a piece or machine. Bedrock, lava, the fog and the lifepod stop it. What it pays for breaks, in
   cell order; what it reaches unpaid cracks by what was left there (built: its share, rounded down), a piece at its head.
   `Wave` reports those cells (`unpaid`), once each, with the most left on the cheapest way.
5. **What breaks goes**: to the hitter for a tool or shot, as before; for the charge (`Spoils.Drop`) at its cell for anyone,
   crushed where `Material.Crushed` says (stone: 1 gravel); for a meteor (`Spoils.Lost`) nowhere, and the back wall behind
   each cell entered breaks where what is left pays its cost. A debris net lays its cost down either way.
6. **Machines break** at `MachineSpec.HitPoints` (a pile's `Hp`, else `Damage.MachineHp` 20, a scrap wall's; none for the
   lifepod): `Items.Spill` lays its slots and modules, `Crafting.Scrap` refunds its queue, its cables stay on a peg, then its
   cost drops or is lost.
7. **Cracks heal** `Builds.CrackHeals` 1200 ticks (a minute) after their last hit; no repair.
8. **The charge**: `Power` 500, `Reach` 8: stone opens about 4 out, soil the whole circle, alloy cracks; one charge cracks a
   scrap wall and a second breaks it. A relic a burst takes from its cradle shocks the thrower once, wherever they stand.
9. **Meteors** burst at the wall they strike with power × `PerPower` 220 within `Reach` 4. A hull is two cells thick, 1084 in
   scrap: power 2 cracks it, 3–4 hole its outer cell, 5 and up breach it (5 % before landing, 20 % after), and a second on a
   fresh crack breaches too. An aired, sealed room behind a struck wall flashes the breach alarm's red, weak, once.
   `Material.Armour` and the wall card's meteor lines are gone. `MeteorHit(cell)` breaks the one wall with just its cost.

Integers throughout: carries and the built share round down, entering a built cell rounds up.

## Accepted findings

- One `BuiltTakes` cannot make scrap (20) and iron (50) both fall to the second charge: tuned for scrap, an iron hull takes
  4–5 charges and 1352 to a meteor, which only the strongest pays.
- Alloy is natural ground (600), so the strongest meteors break it where built iron only cracks.
- Two meteors on one patch within a minute are rare at one each 4–6 minutes: until the shower event, breaches come mostly
  from the strongest alone; `--meteors=N` shows the second strike.
- A burst runs through open space cheaply, so a breaching meteor strips the hull's face a few cells up and down.
- The carry alone never breaks fresh soil: in stone, straight on, it gives every fourth cell at one blow.
- Charges from the side open the spire's hall and leave the relic standing; it falls when the floor under it breaks.
- `HewCells` aims its carry from a body's middle up to `Crew.ReportEvery` ticks old: a hero digging on the run may carry a
  step off their aim.
- A machine whose floor a burst breaks stands on.

## Deliberately not handled

- Bursts do not harm heroes or creatures; damage to creatures and wrecks is unchanged.
- Tools and the charge never take back walls; tools aboard the ship still break piles only, the plan's marks take walls apart.
- No repair of cracks, no faster healing, no anti-griefing: a guest's charges crack the host's hull.
- No meteor shower event and no overheating yet: overheating will be a `Burst` at the machine's cell.
