# The cape is worn

- Status: accepted; built
- Date: 2026-10-27
- Builds on [2026-10-27-suits-are-worn](2026-10-27-suits-are-worn.md); for the cape, an exception to
  [0057](0057-stations-one-model-for-every-crafting-machine.md)'s pod list only in that it is made at the Former instead

## Context

The cape began as a drawing (a Verlet cloth over and behind the figure, `game/features/crew/logic/Cape.cs`), switched on
only by `-- --cape`, which the user approved as it moves. The user wants it as equipment: worn in a slot of its own,
softening enemies' blows a little and the cold a little, as a share of all the cold.

## Decision

- **An item in its own slot.** The item `cape` (`Content.Cape`), worn in a new equipment slot of kind `cape`, appended
  last after the suits' `suit` and `lamp`, so it is worn beside the jetpack, which keeps the back. On and off by `Move`.
- **Made of cloth at the Former:** three `cloth`, 60 ticks. Cloth comes only from planets' ruins; it needs analysis by being
  a cost, so the craft opens once a piece is analysed. No cape before the first landing, and the Former needs power.
- **A share, exact over time.** Worn, it spares `Cape.Guard` (200) thousandths of an enemy's blow, a creature's ram or
  pounce (`HitBy`) or its bolt (`Bolted`), and `Cape.Warmth` (200) thousandths of the cold: the edge's past the field and
  the bare cold with no body. What it spares is rounded down; the rest is kept in `Hero.Spared` (0–999 thousandths,
  saved, one remainder for the cold and the blows), so a cape never spares a unit before it has earned it and the share
  is exact over time: bare in vacuum, five breaths cost 9 rather than 10. Without a cape nothing is softened and the
  remainder is left alone.
- **Blows are not shocks.** Creatures call `ICrewOps.Blow`, which softens and then pays as `Shock` does; ruins' zaps and
  the cradle's shock keep calling `Shock`, which no cape softens. Lava's burn is heat: unsoftened. The breath itself is
  paid in full.
- **The constants live on `Content.Cape`**, not on `ItemSpec`: the cape is the only item with them. They become item fields
  when a second item softens anything.
- **The client** draws the cape (`Figure.WearsCape`) on any hero exactly while that hero wears one, read from the items
  as the helmet lamp is; the frost reads `ICrewQueries.Warmth` and draws the edge's cold that much lighter, its floor for
  the bare cold unchanged. The cape's card shows both shares.
- **Saves:** format 36. Every peer's inventory gains the empty cape slot. The 34 → 35 step uses format 35's own
  inventory size, no longer `Inventory.Total`, which grows with every slot added.

## Consequences

- Within a tick a blow (commands phase) takes the remainder before the breath (simulation phase).
- Tests: `CapeTests` covers each rule and an FsCheck invariant (what each step charges is what the cape leaves of it,
  the remainder within a unit), proven to bite by rounding up once; the ram, the bolt, the edge, lava, research and the
  format-35 save each beside their own.

## Deliberately not handled

- **No other item softens anything** yet: armoured helmets and warm bodies get `ItemSpec` fields when one is designed.
- **The remainder is not cleared** on taking the cape off or dying: under a unit, so the next cold or blow may spare one
  unit more. It goes with the hero on `Leave`.
- **One remainder for cold and blows:** the sum is exact; a blow may finish a unit the cold began.
- **A first ram of 3 or bolt of 4 costs in full:** rounding against the hero, evened out over the next.
- **The frost draws the average:** a fifth lighter, without the remainder's steps.
- **A save from before cloth was a cost** has no cloth analysed: the player analyses a piece.
- **The cloth is not in the sim:** every peer moves its own drawing, as before.
