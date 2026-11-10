# Plan: plants stirred by bodies

The design is [plant-stirs](../decisions/2026-11-10-plant-stirs.md). Client only. Deleted once the user has played it and
picked a feel; the pick goes into the ADR.

1. `game/render/logic/Stirs.cs`: live stirs and wakes, the cap, ages; `Stirs.Uniforms` packs them as `vec4`s
   (x, feet y, speed or swing, age; age < 0 live). Tests in `game.tests`.
2. `game/features/flora/logic/Topple.cs` (the fall: angle, speed, one rebound, landed) and `logic/Brushes.cs` (whether a
   runner sheds a plant now). Tests.
3. `game/Content/logic/FloraLooks.cs`: `Feel`, `Feels`, `Feeling`; `ScatterSpeed`, `ScatterEvery`, `BrushSpeed`, `Bush`, `Tree`, `Breeze`,
   `FellerReach`, `FallLeaves`, `LandLeaves`, `LieSeconds`, `FadeSeconds`; `Look.Bends`, `Look.Topples`.
4. `game/render/Lit.cs`: the bending shader and `SetBending`; `game/render/Bend.cs`: the snippet, the bending glow
   material, `Bend.Push(stirs)` uploading to every bending material.
5. `SurfaceContext.Heroes` (feet and speed of every hero here) in `Main.cs`/`SurfaceViews.cs`; `FloraView` feeds `Stirs`
   each frame and pushes them; sheds; topples acacias. `GridView` makes `_tops`, `_topsFront` bending; `CaveView` its
   `Front`, `FrontShine`.
6. F8 in `--planet` cycles `FloraLooks.Feels`, its name shown a moment (`game/dev`).
7. Flora README, map lines, ADR index; full tests; build, import, launch `--planet` for the user.
8. After the first play (2026-11-10): a brushed bush sways (`Sway`) instead of shaking; more leaves, and a shower on each axe
   hit; wind gusts (`Gusts`, F9); acacias over twice as thick on the first planet.
