# debris (client)

The debris drifting past the ship (`apps/game/src/features/debris/`, G-08). Drawn only: the sim
(`sim/Debris`) spawns, moves and hands out what a catch gives. Catching is the hero's hands, aimed as
everything else E works is (`features/crew`, `Aiming.AimAt`), which send `Grab`; chopping wreckage
with the fire axe is the hands too, which send `Hit` (0034).

- **Reads:** `DebrisModule.Pieces`; `Client.InFlight` — its own grabs (`Shared.Predicted.Grabbing`) and hits (`OwnHits`);
  `Ui.AimedPiece` — the piece the crew's hands would catch or chop; `Ui.Heroes` — who most likely
  struck a piece; `Session.Progress`.
- **Drawn** behind the back walls (`Layers.Debris`): it never collides with the hull (0006). Each piece
  wears one of its kind's own pictures (`art/debris`, `Content/DebrisLooks`: 12 of scrap, 12 of hull, 8 crates, 4 pods, one picture of each wreck: a hull section, a machine block 48 × 32, a cargo container and a shield slab 64 × 32, a cable rack and a tank cluster 32 × 32 (`DebrisLooks.Wrecks`, the box they are aimed at and struck by)), the one its id picks, so every peer sees it alike and pieces in a row differ; a pixel of art a pixel of the world; a kind with none is its item's picture (`ItemIcons`), 0.8 of a cell; lit by the stars and the lamps, between ticks
  at `X − Speed × Progress`, or reeled by a harpoon its share of the way to its reel's point
  (`Shared.PieceDrawn`, where the crew's rod sticks to it too), so it glides, tumbling a turn every 4 s each its own way, faded in over the
  first 8 cells of its path and out over the last (`Fading.FadeOf`, 0009); a wreck only rocks, 0.08 rad
  either way every 7 s, so its picture stays where the aim tests it.
- **Damage (0034):** one filter for every piece, in the lit pictures' shader (`render/Cracks.cs`, taken
  into `render/Lit.cs`): set per picture by instance uniforms, the share of hit points lost and the
  piece's id, it draws pixel-art cracks on the picture's own pixels — dark as the outline, a lit pixel
  under each — from a dent that widens, more and longer as the share grows, before the light falls on
  them. A rise in `Damage` jolts the piece 3 px away from the nearest hero and turns it a little,
  settling over 220 ms, throws sparks from the struck side and, if the hit was this player's own, plays `Sound.Chop` there (on the Suit
  bus, dulled while floating: wrecks are chopped in space, where only what the hero touches is heard, 0042). A wreck gone with hits in it before
  the end of its path was broken (`Fading.Broken`, counting the ticks run since it was last seen, so one
  drifting off its end only fades, a stall between frames or not): a burst of dust and sparks and
  `Sound.Break` if its own hit broke it, louder and heard farther than a chop, on the Suit bus too. It takes the last chop's place:
  the last hit removes the wreck on its tick, so its damage never rises in view and no chop plays for it. Every peer sees the
  same, from the world; only the striker hears it. Own is a `Hit` of this peer's seen in flight one frame and gone the next (`OwnHits`), read after the hero sends this frame's hits, so none runs unseen. The stars behind it are
  `render/Stars.cs`: a filtered tile placed once the camera has followed the hero, so they glide by
  fractions of a pixel a frame as the TypeScript tile did, never holding still and jumping a pixel.
- **Aimed:** the piece the crew sets in `Ui.AimedPiece` — out in space, within reach (with the axe, the
  first wreck its blade would touch, 0039), the hand free —
  is outlined along its own picture as it tumbles, a screen pixel wide whatever the zoom
  (`render/Outline.cs`), green, or red while the sim would refuse the grab, fading with the piece; the
  crew's prompt says "[E] Catch"; on a wreck, red with "Too big", or with the axe "[Hold LMB] Chop".
- **Own grabs at once (0014):** a piece this peer has a grab in flight for is gone from its view and
  passed over by the aim, so E cannot send it twice.
- **Recording:** `-- --autopilot --debris` starts in the wreck with pieces already out
  (`DebrisDemo.Seed`, put in through the save) and a cargo crate drifting to where the wreck's
  autopilot leaves the hero outside the airlock; there the pointer goes to it and E catches it,
  planks landing in the hotbar. With `--host` the demo is hosted, so a guest
  sees the catch.
- **Recording:** `-- --harpoon` (`WreckDemo`): a harpoon in the third slot, a cargo crate and a hull
  section out of reach; 3 chooses it, the crate is shot, reeled in with a second click and caught with E,
  then, the gun ready again, the section is shot and reeled in to the gun (0044).
- **Recording:** `-- --grapple --lit` (`WreckDemo.Hall`): a lit hall aboard, a harpoon in the first slot;
  it is fired into the ceiling, the hero pulled up and hanging, D lets go; then the same into the far wall.
- **Recording:** `-- --wreck` (`WreckDemo`): the hero floating in space, a fire axe in its second
  slot, a hull section drifting up slowly and three more chopped a third, a half and four fifths of
  the way; the pointer on it bare-handed (red), 2 chooses the axe, the left button held till it breaks,
  then the hero swims through the loot. `-- --wrecks` lines up one of every kind instead, and the
  hero swims to each in turn and chops it till it breaks, sweeping up the loot on its way.

## Files

| File            | Holds                                                         | Godot |
| --------------- | ------------------------------------------------------------- | ----- |
| `Fading.cs`     | `FadeOf`, `Broken`                                            | no    |
| `OwnHits.cs`    | which pieces this peer's own hits ran on since the last frame | no    |
| `DebrisView.cs` | the layer: pieces and the outline of the one aimed at         | yes   |
| `DebrisDemo.cs` | the world and the script of `--debris`                        | yes   |
| `WreckDemo.cs`  | the world and the script of `--wreck`, `--wrecks`, `--harpoon` and `--grapple` | yes   |

`Fading.cs` and `OwnHits.cs` are compiled again into `game.tests/` (`OwnHits` tested in `HearingTests`), where `fade.test.ts` and the debris test of
`predicted.test.ts` are ported (`DebrisTests`); the debris tests of `aim.test.ts` are in `AimTests`.

## Deliberately not handled

- Other peers' grabs in flight: a piece another player is catching stays until the host's tick says
  so, as in TypeScript.
- Own hits before their tick (0014): a chop jolts and is heard when the tick it is stamped for runs,
  a round trip after the swing lands in the figure; the swing's pace hides most of it.
- Which hero struck: the jolt and sparks face the nearest, which is the striker but for two heroes
  chopping one wreck from both sides.
