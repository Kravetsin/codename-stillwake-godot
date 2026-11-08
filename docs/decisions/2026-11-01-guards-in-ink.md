# The guards in ink

- Status: accepted; under `--anime-art` until the switch ([the plan](../plans/anime-art.md)).
- Follows [creatures in ink](2026-11-01-creatures-in-ink.md). Sits beside [spire guards](2026-10-31-spire-guards.md), whose pixel Istukan still draws without the flag.

The user picked on pages, one design for each: for the sentry [«Зонд»](https://claude.ai/artifact/WFxJEwYxREburoh4HbHWxv) (Б), a forgotten probe on stilts, its
knees over its back, a bare truss, torn gold foil, a nozzle bigger than the rest; for the warden [«Латник»](https://claude.ai/artifact/CPz4oXZpT3ayGwQGz6Wwva) (А), a dry plated
guard, shoulder plates over its helm, blocks for fists, asleep with its face behind them. Three others on each page were rejected, today's redrawn tick among them.

**Parts on sprites, not a fixed set of poses, and not `PosedSprite`.** At 4 times the pixels, painting a body on the CPU each frame costs 16 times as much; turned by the GPU, a part 4
times larger costs memory and no frame time. Poses as pictures could not hold what the sim asks of them: a crawl at any speed, a body turned along any flight. So
`TickRig` and `IstukanRig` port the pages' poses, plain and tested, and `InkRig` places a sprite a picture. Under the flag the pixel painter is simply not called; `PosedSprite` keeps only
its transform and the blank layers it was built with, gone at the switch with `TickArt`'s and `IstukanArt`'s painting.

**Motion stays as it was.** `TickMotion` and `IstukanMotion` read the sim, and every moment, spark, sound and burner hangs on the same anchors, so the rigs answer with `TickArt.Anchors` and
`IstukanArt.Anchors`. Their poses gained what the drawings needed: the tick how alert it stands, the Istukan's motion its clock, for its sway.

**What shines is white, coloured by the sprite.** The lamps took the pose's colour on the pixels and still do, so no picture per colour; the Istukan's seams and core keep their drawn colours. Over all, a
white copy of each plate for a blow's flash, as the other creatures have.

**Each guard of a kind looks the same.** No sizes, tints or markings: they are machines, and the old ones did not differ either.

**Data is generated** with the pictures: `tools/anime-art/subjects/tick.js` and `warden.js` write `Content/logic/TickInk.cs` and `IstukanInk.cs`.

## Deliberately not here

- **The cracks growing.** The page grew them run by run; the game gives them 0.1 s. One picture, fading in.
- **The body parting along jagged lines**, or thrown apart, as on the pages: every creature crumbles into its pixels (2026-10-29-kills-crumble).
- **The floor's cut on the Istukan.** The pixel one hid what fell under its feet; these parts stay over the ground in every key pose.
- **The tick's cutter swaying at rest**, and the page's flame: the game's own burner, sparks and light stand in.
- **The hit boxes.** The sim's are unchanged: the sentry's 3 × 2 cells cover its belly, not its stilts, and the warden stands a little over its 5 cells. Felt in play or not: unchecked.
