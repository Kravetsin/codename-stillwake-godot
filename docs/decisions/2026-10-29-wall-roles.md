# Walls either armour or seal

- Status: accepted
- Date: 2026-10-29
- Builds on [meteor-wave](2026-10-29-meteor-wave.md); a material's meteor numbers move from `Strength` to `Armour`

## Context

Meteors read a wall's `Strength`, which was its hardness to dig on a planet: stone (12) and alloy (60) stood every meteor,
soil (4) better than scrap (2). Leaks were near alike: soil and stone about scrap's, alloy worse. A wall best at
everything, found early, would end the reason to build again. The user wants a choice: a ship well armoured or well sealed.

## Decision

- **Two numbers:** `Material.Armour` is a wall's stand against meteors (the least power that breaks it, and twice it what
  breaking it soaks of a blast; 0 for none breaks), apart from `Strength`, its hardness to dig. `Leak` stays the air lost.
- **Roles pull apart:** metal and laid stone stand up to meteors and leak at their seams and joints; whole sheets seal and
  give way. Per cell, a minute:

  | Wall | Armour | Leak | Cost |
  | --- | --- | --- | --- |
  | soil | 1 | 12 | 1 soil |
  | scrap | 2 | 3 | 2 scrap |
  | stone | 4 | 9 | 1 rock |
  | iron | 5 | 2.4 | 1 plate |
  | alloy | 7 | 2.4 | 1 alloy scrap, what digging a cell of it gives |
  | glass, mica, hide | 1 | 0.6 | 2 glass, 2 mica, a hide and a fat |

  Scrap and iron keep the leaks the start's air is tuned to. Concrete (9) joins as armour that leaks.
- **Three seals, alike:** glass (smelted on the ship), mica (mined on planets) and hide sealed with fat (from planets'
  creatures) differ only in look and where they come from: the player picks by taste. Each is known once its items are
  analysed, as every blueprint.
- **Layers pay:** air leaks only through the cells a room's air touches, and a blast is soaked by the walls it meets
  first, so armour outside and a seal inside or behind a room is the way to build. Later materials better one axis.

## Deliberately not handled

- No wall is see-through: glass stops light as any wall does (replaced by [clear-glass](2026-10-29-clear-glass.md)).
- The seals' pictures are placeholders, scrap's tinted, until the drawn ones land.
