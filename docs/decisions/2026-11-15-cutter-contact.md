# The cutter's slag blows out the far side, and struck ground sparks off the trail

- Status: accepted; extends [drill-contact](2026-11-14-drill-contact.md) to the plasma cutter and
  [blade-sparks](2026-11-15-blade-sparks.md) to the ground and the hull; supersedes where the cutter's sparks start in
  [damage-looks](2026-11-05-damage-looks.md); where its sparks start, superseded by [spark-rule](2026-11-15-spark-rule.md)
- Date: 2026-11-15

## Context

After the grinder, the drill and struck wrecks, two hits still threw from one point: the plasma cutter, its sparks fanned
back toward the tool from where its jet met a wreck, with a white bead there; and a struck tool on rock, a wall or the hull,
from the face toward the hero. The user asked for both on the shared rule: sparks come off what is seen working, where it
meets matter.

## Decision

- **The cutter's slag** (`ToolContact.Through`): the jet, run on past where it touches a wreck along its way, is walked
  every `SlagStep` px through the wreck's drawn pixels to the first point past it. The slag flies from there on along the
  jet, spread `SlagSpread`: out of the far side, as if cut through («шлак насквозь», of the two looks discussed, the one the
  rule gives). No bead there. A wreck the jet does not come out of within `SlagDepth` px throws as before, back toward the
  tool with its bead.
- **Struck ground and hull:** a struck tool's hit on a cell throws from where the blade's last swept stretch lies in cells a
  struck tool would hit, as on a wreck (`BladeBites`, up to `BladeBites.Throws` points, a share each), along the swing. A
  swing into the face glances off it: the way is mirrored by the face. A blade that did not reach the cell throws from its
  face, as before. A friend's hit uses its figure's trail as drawn here.

## Deliberately not handled

- A cut wreck thicker than `SlagDepth` keeps the old look; a derelict's hull is that thick only at its middle.
- The jet is not drawn through the wreck: only its slag comes out.
- A cutter's hit on a wall or rock is unchanged: it is seen at work there all along (tool-contact).
