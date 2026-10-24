# debris (client)

## Files

| File | Purpose |
| --- | --- |
| `Fading.cs` | `Broken`: whether a wreck gone from the world was broken or drifted off. |
| `OwnHits.cs` | Which pieces this peer's own hits ran on since the last frame, and with which weapon. |
| `DebrisView.cs` | The layer: pieces and the outline of the one aimed at. |
| `BreakView.cs` | A broken wreck's pieces flying apart, their torn edges glowing (0075). |
| `DebrisDemo.cs` | The world and the script of `--debris`. |
| `WreckDemo.cs` | The world and the script of `--wreck`, `--wrecks`, `--harpoon` and `--grapple`. |
| `AsteroidDemo.cs` | The world and the script of `--asteroid`. |
| `SortieDemo.cs` | The world and the script of `--sortie`. |
| `FarDemo.cs` | The big world and far field of `--fly` (`--manual` to fly it yourself), `--far=N`, `--edge=N`. |
| `Finds.cs` | The Handbook's debris: loot slots revealed item by item, ranges, chances, tally, tips. |
| `HandbookPage.cs` | The Handbook's Debris section: the kinds' rows and the chosen kind's entry. |

The debris drifting past the ship, drawn only: the sim (`sim/Debris`) spawns, moves and hands out what a catch gives.
Catching is the hero's hands, aimed as everything E works is (`features/crew`, `Aiming.AimAt`), sending `Grab`;
breaking wreckage with a weapon is the hands too, sending `Hit` (0034, 0054).

- **Reads:** `DebrisModule.Pieces` and `Found`; `Client.InFlight` (own grabs, `Shared.Predicted.Grabbing`, and hits,
  `OwnHits`); `Ui.AimedPiece`; `Ui.Heroes`; `Ui.Bits`; `Session.Progress`.

## Rules

- **Only what the camera sees** (0052): a piece gets a picture only while its middle is within `ShipCamera.View` and
  8 cells round it. Pictures come from two pools, wrecks' and small pieces', that grow to the most ever on screen and
  never free, warmed in `_Ready` (`WarmSmall`, `WarmWrecks`) with every debris texture, so a cloud drifting in makes
  none. Only wrecks take the cracking material; a small piece is `Lit` plain. A piece leaving the view goes back to its pool.
- **Drawn** behind the back walls (`Layers.Debris`): it never collides with the hull (0006). Each piece wears one of
  its kind's pictures (`Content/DebrisLooks`, `art/debris`), the one its id picks (`Content.Looks.Of(kind, id)`), so
  every peer sees it alike (0066); a wreck's is drawn of what it breaks into and is the box it is aimed at and struck
  by (`DebrisLooks.Wrecks`). A kind with none is its item's icon at 0.8 of a cell. Lit by the stars and lamps; drawn
  between ticks at `X − Speed × Progress`, or reeled by a harpoon its share of the way (`Shared.PieceDrawn`, 0044).
- **Motion** (no ADR): a small piece tumbles a turn every 4 s, each its own way; a wreck only rocks (`Rock`, `RockMs`),
  so its picture stays where the aim tests it. Full strength from the first frame: it spawns out of sight (0052).
- **Damage** (0034): one crack filter for every wreck in the lit shader (`render/Cracks.cs` in `render/Lit.cs`), set
  per picture by instance uniforms (share of hit points lost, piece id): pixel-art cracks from a dent that widens as
  the share grows, before the light falls on them.
- **A hit** (a rise in `Damage`) jolts the piece away from the nearest hero, turning it a little and settling, and
  throws `HitLooks.Of[Impact.Wreck]` by `HitBurst` where it was struck, the picture flaring warm (0075, 0094). The
  axe's lands on the side facing the nearest hero; one under a running drill's bit (within 4 px of the box) lands at
  the bit and throws that drill's share (0094).
- **Own hits** (`OwnHits`: a `Hit` of this peer's in flight one frame and gone the next, read after this frame's hits
  are sent): the view nudges (`ShipCamera.Nudge`) and `Sound.Chop` plays, but not for the drill, whose strikes the crew
  plays; on the Suit bus, dulled while floating (0042).
- **Broken** (`Fading.Broken`): a wreck gone with hits in it before the end of its path, counting ticks since last
  seen, so one drifting off its end only fades, as does one broken past its end while a hero's view keeps it there
  (0052), its loot falling all the same. It flies apart in six tumbling wedges with glowing torn edges and
  embers (`BreakView`), with sparks, dust, a cloud, glint, ring and flash, more for a derelict (0075); `Sound.Break`
  if its own hit broke it, louder and farther, on the Suit bus. It replaces the last chop: the last hit removes the
  wreck on its tick, so its damage never rises in view. Every peer sees it; only the striker hears it.
- **Aimed** (0039, 0054): the piece in `Ui.AimedPiece` (in space, within reach; the first wreck the axe's blade or the
  drill's bit touches) is outlined along its picture a screen pixel wide (`render/Outline.cs`), green, or red while the
  sim would refuse the grab. The crew's prompt says "[E] Catch", on a wreck "Too big" or with the axe "[Hold LMB] Chop".
- **Own grabs at once** (0014): a piece with a grab of this peer's in flight is gone from its view and skipped by the
  aim, so E cannot send it twice.

## The Handbook's Debris section (0066, 0083)

- `HandbookPage` on `features/handbook`'s tablet: every kind in rows by where to look (`Content/Handbook.DebrisRows`),
  in slots 16 px larger than an item slot, laid out again when the slot size changes. A found kind shows its first
  look found; one not found its first look black.
- **Tally** at the tab strip's right: looks found of all there are, with a bar; its tooltip gives kinds and looks.
- **Entry** of the chosen kind (the first found until one is clicked): the look big, a strip of every look (not found
  black, a click shows a found one big, "2/5"; none for a one-look kind); name, hit points (`Wreckage.HpOf`) or
  "Caught by hand"; loot slot by slot (`Loot.Of`): a seen item with its range, an unseen one a black silhouette, a pie
  for a roll's chance (`OneIn`). With nothing found, the entry says what to do.
- Words only in the one tooltip (`Finds`); "???" for what is not found or seen.
- Reads `DebrisModule.Found` every 250 ms while shown and builds again only when it changed.

## Recordings

`--debris` (a crate caught by E at the airlock; `--host` for a guest to see it), `--fly` and `--far=N`/`--edge=N`
(the far field, 0052), `--harpoon` and `--grapple --lit` (0044), `--asteroid` and `--sortie` (0054, 0056; `--manual`
leaves the script off), `--wreck`/`--wrecks` (chopping till it breaks), `--handbook` (`dev/HandbookDemo`).

## Tests

`Fading.cs`, `OwnHits.cs`, `Finds.cs` and `Content/Handbook.cs` are compiled into `game.tests/` (`DebrisTests`,
`HearingTests`, `AimTests`, `HandbookTests`: every kind that spawns in exactly one row).

## Deliberately not handled

- Other peers' grabs in flight: a piece another player is catching stays until the host's tick says so.
- Own hits before their tick (0014): a chop jolts and is heard a round trip after the swing; its pace hides it.
- Which hero struck: the jolt faces the nearest, wrong only for two heroes chopping one wreck from both sides.
- Off the view: a hit on a piece not drawn jolts nothing; a wreck broken there bursts unseen. Pooled pictures draw
  over or under one another in no set order.
