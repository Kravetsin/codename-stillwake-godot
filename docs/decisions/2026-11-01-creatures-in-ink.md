# Creatures in ink

- Status: accepted; under `--anime-art` until the switch ([the plan](../plans/anime-art.md)).
- Follows [ink and watercolour](2026-11-01-ink-and-watercolour.md). Sits beside the index maps of [0102](0102-creatures-live-in-the-sim.md), which still draw pixels.

The user picked on [a page](https://claude.ai/artifact/R8hBM1Y5RTrtscJNBL6zCu), one design for each creature: the loper gaunt and wary
(Б), the tapir plated and heavy (В), the sixleg alien, read through its ears and marks (Г). The fourth candidate, the old silhouette redrawn,
was rejected.

**The fluff is gone from the game.** It had no pick, and the user would rather have none than a pixel one beside these: no species, no art, no
spawns, so the caves stand empty until their kinds come. A world that had some loses them on loading (format 42). The way a kind spawns in a
band stays, for those kinds; its hopping gait, which only the fluff used, went with it.

**The rig stays; only the pictures change.** A plan feared 16 times the pixels painted each frame, but that holds only for the sentry and the
warden (`PosedSprite`). These three are sprites of parts, turned by the GPU, so a part 4 times larger costs memory and no frame time.

**A part is a finished picture, not an index map.** We meant to keep colour computed, writing ramp, tone and water into the channels, and did not:
a boundary between two zones cannot be antialiased in one ramp's index, and the picked colours are in no palette. So an individual differs by:

- its **size**, as before;
- a **tint**, its picture multiplied at the sprite (`CreatureVariety.InkTint`), shared by all and costing nothing; its ink stays dark. The whites of the eye keep theirs;
- its **markings**, the part `marks` split into pieces that do not touch, each worn or not, as before. Only these make copies. What defines the
  design, the loper's mane, lies in `body`, so that no loper loses it.

**A leg long enough stands on its feet.** The fixed bob made long legs float, so a design with `Pose.LegLen` drops its body by what its swing would lift
the lowest foot, walking and in a gallop's gathered moment.

**Data is generated** with the pictures: `tools/anime-art/subjects/creatures.js` writes `Content/logic/CreatureInk.cs`, one design per look it stands in for.

## Deliberately not here

- **Another planet's colours.** Every planet in the game uses one palette. When one wants its own, the materials are data in the drawing and a design is exported again.
- **The tint in the burst.** A killed creature crumbles into its picture's reduced pixels in plain colours: gone in a moment.
- **The hit box.** The sim's is unchanged; the loper's head now reaches past it. Felt in play or not: unchecked.
- **The sixleg's glowing tail.** The picked Г has none, so it gives no light. The tapir's pod was not on the page either and was added, for it lights the night.
- **The sentry and the warden:** their own decision.
