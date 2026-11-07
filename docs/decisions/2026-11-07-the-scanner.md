# The scanner reads a wreck's hit points

- Status: accepted
- Date: 2026-11-07

## Context

Players hitting a big asteroid gave up after about ten seconds, thinking no damage got through
([cracks-to-end](2026-11-07-cracks-to-end.md)). The cracks now show that it weakens; the user asked as well for a device
worn on the helmet, in the lamp's slot, that shows a wreck's hit points as numbers where the pointer is. Big wrecks are
reached only with the jetpack, so it is sold early on the black market.

## Decision

- **An item, `scanner`**, worn in the helmet's mount (`Helmet.Mount`) as the lamp is: the one mount takes one, so wearing
  the scanner means no lamp's light.
- **Sold on the black market's second orders row** (`order2`, a block of 3 at 3 times its worth, 1500 credits), crafted at
  the workbench once bought: a plate, a coil, two wires and a circuit, 60 ticks, all found in the first wrecks: no glass,
  which comes only from asteroids and ruins, the very things a player skips at first.
- **A readout, client only** (`features/scanner`): while this player wears it and the pointer is over a wreck (in the box the
  hands aim by, `WreckAim`), a hologram over the middle of the wreck's top edge shows `left / max` hit points, the sim's
  `HpOf` less its `Damage`, following the wreck as it drifts. A pile in the hold is read the same way ([pile-hit-points](2026-11-07-pile-hit-points.md)). Nothing over a small piece, empty space, or while a menu,
  computer or the tree is open. Only the wearer sees it.
- **Its look**, the user's picks of five each on a page: the **crest**, a swept fin along the helmet's crown hugging each
  helmet's dome, its front edge dark glass with three green dots running down it, one lit at a time
  (`tools/anime-art/subjects/scanner.js`); the readout **phosphor**, the edge keys' green on dark glass with their scanlines,
  grain and a rolling line, the value flaring white as it falls, 3 by 5 digits in world pixels.

## Consequences

- `Aiming`'s wreck box moved to `game/shared/logic/WreckAim.cs`, shared by the hands and the scanner.
- The readout (`Readout`) is a point, a value left and a maximum, so creatures can be read the same way once they are.

### Deliberately not handled

- Creatures: no readout over them yet; the readout takes any point, left and maximum when they are added.
- Other players' scanners show nothing to this player; a friend's readout is theirs.
- Hits in flight are not foreseen: the number drops on the tick the hit applies, a frame or two after the blow.
