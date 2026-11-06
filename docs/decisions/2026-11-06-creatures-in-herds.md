# Wild creatures come in small herds of one kind

- Status: accepted; supersedes the one-at-a-time spawning and the share of ten of [0102](0102-creatures-live-in-the-sim.md)
- Date: 2026-11-06

## Context

Playtest 2026-11-06: too many creatures on the surface, and the kinds walk mixed together; the user asked for small groups of
one kind. Measured on the grown first planet (seeds 1 and 2, a hero walking five minutes, sampled every 30 s): 10–14 creatures
within about 220 cells of the hero, all three kinds at once, two thirds of them nearest to another kind.

## Decision

- **A spawn is a herd** (`CreaturesModule.Spawn`): on the 2 s beat, round each hero whose keep box has room for `HerdLeast`,
  four draws always taken: the side, the columns out (`RingFrom`..`RingTo`), the habitat by weight, the size `HerdLeast`..
  `HerdMost` (2..4). The habitat is the herd's one species, so its place (sky, a cave's band) decides it as before.
- **Laid outward**: the first member just past the view, each next `Spacing` (6) columns further out, each at the first place
  down its column. `RingTo` came down from 152 to 136 so the farthest member stays in the keep box.
- **Whole or not at all**: a member off the grid, with no place or in a hero's view; no room for all of it under `PerKeep`
  (now 6, about two herds) or `Max`; or any creature within `Apart` (32 cells) of the herd: none comes, and the next beat
  draws again.
- **Kept together without state**: a grazer starting a wander still draws its way, but when the nearest other of its kind is
  more than `Herd` (16) columns off, it walks toward it (`GrazeDrive.Kin`, the creatures in id order, the first at a tie).
  No herd id, no saved field: no format bump.
- Measured after (seeds 1–3): 4–7 creatures within 220 cells, almost always next to their own kind.

## Rejected

- **A herd every 12 s** (the first design): on rough ground most tries miss a place for one member, and the surface sat empty
  for minutes. `PerKeep` and `Apart` keep the numbers down; the beat stays 2 s.
- **Dropping members that find no place**: one rule, whole or nothing, is simpler; the next beat tries again.

## Deliberately not handled

- **No herd alarm**: a struck member flees alone, the rest graze on.
- **Herds of one kind may merge** into one bigger herd: still one kind.
- **A member left below a drop** stays apart; one despawned by `Linger` at the keep box's edge frays its herd.
- **Size per habitat** and cave herds wait for a habitat that needs them.
