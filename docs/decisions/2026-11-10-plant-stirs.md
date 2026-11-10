# Plants stirred by bodies

- Status: accepted
- Date: 2026-11-10

## Context

A hero walks, runs and fells trees on a planet through plants that never moved: grass stood upright under boots, a bush
ignored a runner, a felled acacia vanished in a puff of leaves. The player spends most of the game digging and breaking
things, so these small answers are seen all the time. The user asked for grass that parts as in RDR2, leaves shaken off
by a runner, and a felled tree that falls over.

## Decision

1. **Stirs, one shared piece.** `render/logic/Stirs.cs` holds the grass pushed by bodies: points along the ground a cell
   apart, each keyed by its column and its ground's row, springing toward what pushes it this frame (`Feel.SpringHz`,
   `Feel.Damping`). A body pushes the points on the row at its feet, out to about three `Feel.Reach`: parted away from
   its feet (`Part`, none right under them) and leaned the way it goes (`Lean`, whole at a run). Standing, the grass stays
   parted; left behind, it springs back past upright and settles. Only points pushed or still moving are kept, at most
   `Stirs.Most` (128), the stillest dropped first. Fed now by every hero on the surface (`SurfaceContext.Heroes`); next by
   creatures walking and a swing's arc, which push the same way. Nothing in the client recorded where bodies pass, and a
   sprite's shake (`FloraView.Shake`) cannot reach tiles or a chunk's batch. A spring a point rather than a list of
   ringing wakes: holding parted while standing and overshooting once let go fall out of it with no special case.
2. **The bend is a vertex shader.** `Lit` makes a bending material (`Lit.SetBending`), its own shader: the lit fragment with
   a `vertex()` that moves each vertex sideways by the points near it: by its height over a point's ground (none at or
   under it, whole a blade's 16 px up, none past two) and across, blended between the two points either side. A quad's
   top leans whole, both its top corners by its middle (`VERTEX_ID` 0 and 3), each clump a little stiffer or softer by a
   hash, its tips dropping along their arc; columns spring at rates up to a fifth apart (`Stirs.Uneven`). The first try
   moved each corner by its own column, which stretched a tile's blades and read as jelly. A tile's bottom sits on the
   soil, so roots stay put. Heroes, machines and walls keep the plain shader and pay nothing. The additive glow of a
   cave's floor moss bends by the same snippet (`Bend.Glow`).
3. **What bends:** the grass edge (`GridView`'s `_tops` and `_topsFront`), the surface's tufts (`FloraLooks.Stir.Bends`),
   and a cave's floor moss (`CaveView`'s `Front` and `FrontShine`, which hold only that).
4. **Bushes and trees sway, and shed.** A bush or an acacia sways about its foot (`Sway`, a spring in how far its top
   leans, drawn as the picture's skew; its glow a child, so it moves with it): a body faster than `BrushSpeed` meeting its
   box (found by `BoxesByLeft`, never a walk over every plant) kicks it its way, once each `BrushEvery`. A body going
   through a bush drags its leaves behind it: `Swing.Trail` a second at a run, fewer slower, from where its body is in the
   bush, thrown at about its speed, so the air slows them behind it. A tree passed only sways: leaves shaken off a crown
   by a runner looked wrong to the user. An axe's hit that leaves it standing keeps
   its short shake, kicks it away from the hero and throws `Swing.Hit` leaves up. A plant has `Leafage` leaves to lose,
   growing back `Regrow` a second (`Leafage`), so going back and forth thins the trail. A bush is light and quick, a tree
   heavy and slow (`FloraLooks.Bush`, `Tree`). The first try shook a brushed bush as a hit does, which the user found too
   sharp; the second shook leaves off a bush at a touch, which read as thrown rather than dragged.
5. **A felled acacia falls.** Its picture is kept, pivoted at its foot, and tips away from the nearest hero
   (`FloraLooks.FellerReach`; right with none): slowly, then fast, as a rod falls (`Topple`), to `Topple.Lie` from upright,
   rebounding once. Leaves fly from its crown as it starts and where it lands; it lies `LieSeconds`, then fades. Other
   kinds fall away as before.
6. **Wind, a look only.** On a surface with plants, a gust now and then (`Gusts`, `FloraLooks.Breeze`: calm 25–70 s,
   gusts 5–9 s, one way, rising, fluttering and dying, its strength rolling along the ground) leans the grass's and tufts'
   tips (`Bend`'s `wind`; the top corners of a quad, `VERTEX_ID` 0 and 3; never a cave's moss), holds the bushes and trees in
   view over and blows leaves across the view, off their crowns and through the air upwind. Each peer blows its own. F9
   in `--planet` blows one now.
   The leaves (`render/logic/Leaves.cs`): each a small drawn picture of its plant's (`FloraLooks.LeafPictures`), falling at
   its look's pace, swinging side to side as a pendulum, turning and flipping over to show its back (its width the cosine
   of its tumble), taken by the wind; it lands on the first solid cell under it, looked up only when it enters a new column,
   lies `Leaves.Lie` and fades; at most `Leaves.Most`, the oldest go first. `LeafLayer` draws them in four multimeshes a
   picture (face and back), lit, behind the heroes and before them. They were glims, which drew triangles.
   The user picked from four looks drawn by `tools/anime-art` `leaves`, each with its own flight (`FloraLooks.Flights`): a
   leaflet that swings like a pendulum, a watercolour blot that flutters, a dry curled leaf that spins, a sprig that tumbles.
   All four are kept for plants to come: the acacia sheds blots, the thorn bush sprigs, which look like its twigs.
7. **Trees stand thick.** The first planet grows over twice the acacias (`Content.Flora`: on level ground one column
   either side, ten apart, chance 370): a walk passes woods. More planks lie about; a tree still takes three seconds of
   the axe.
8. **Feels are data.** `FloraLooks.Feels` holds three candidates of `Feel(Reach, Part, Lean, SpringHz, Damping)`, soft,
   springy and heavy; F8 cycles them in a dev room (`--planet`), its name shown a moment. The user picks one in play.

Client only: nothing enters the sim. Each peer stirs the plants from the heroes as it sees them.

## Deliberately not handled

- A falling tree meets no ground: it tips to the same angle on a slope or by a wall and may sink into it for the seconds
  it lies there, as sprites overlap today. Trees stand on open ground, and a slope steep enough to show it is rare.
- Grass is not flattened under a landed trunk; it is hidden under it.
- A steady wind, seasons; trampled grass that stays down. No sound of wind yet: a sound is the user's pick.
- Peers see different gusts; nothing depends on them.
- Bells, rocks, glowcaps and stalagmites do not bend; rocks fall away as before, a felled bush throws up its leaves. A sway is a skew of one
  picture, its crown and trunk leaning as one.
- Ceiling threads and roots, which a head could brush in a tunnel near the surface.
- Explosions push out from a point, which a sideways stir cannot say; they wait for their own design.
- A tuft is one quad, so it leans whole rather than curving; grass tiles are 8 px wide, so the parting moves by 8 px
  columns. A bent blade is lit where it would stand. At these sizes in ink it reads as a bend.
- A leaf meets only the ground under it: it drifts through a wall sideways, and one lying on a cell dug away hangs
  there till it fades. Leaves are blown about in the open, where walls are few, and lie three seconds.
- Four heroes running in one view may stir more than `Stirs.Most` points; the stillest go first, a little early.
