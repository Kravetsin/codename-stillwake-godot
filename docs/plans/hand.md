# Design: the hand as its own picture

Reviewed (design-author, cut by design-reviewer); awaiting the user's yes.

## Problem
Each raw GPT suit paints its glove inside the arm picture, so the finger count changes from run to run (four or five); on
2026-11-16 every glove was repainted by hand. Each new suit brings the artefact back, and the suits' hands differ.

## Player experience
1. In any raw suit the arm ends at its cuff and the same five-finger glove hangs below it.
2. Swinging, drilling, climbing, holding an axe in both hands: the hand moves exactly as today, fixed to the forearm.
3. Bare, the outfit's own hand (bare skin, rags, the rubber gauntlet) in the player's skin tone. Catchers keep their coloured gloves.

## Approach (client art only; no sim change)

**The boot's way, again.** The leg's picture is cut at the ankle at runtime (`CutAtTheAnkle`) and the boot hangs on its own node
there. The hand does the same at the wrist:
- `Rig.Wrist`: how far down the forearm the wrist is, one constant for every arm (≈ 4.7 px below the elbow, the code-drawn cuffs'
  lower edge, set on the dress sheet). `limbs.js`/`hero.js` read the same number; `WholeLimbsTests` ties them.
- The arm's sprite is cut there by a region (`CutAtTheWrist`, beside the ankle's); `BentPicture` already draws a region-cut picture.
  The PNGs of the existing raw arms are **not** edited: the old glove below the line is simply never drawn.
- The hand is a small picture of its own (`Joints["hand"]`, its wrist at the top-middle), a sprite on a wrist node on the forearm's
  node. It does not bend (the elbow's band is far above). Its top runs ~1 px above the line, under the arm: the cuff covers it.
- In `Figure._parts`, so the far arm's `FarShade`, the skin tint and the crumbling body need nothing new.

**Which glove: one default, overridden by a body's own.** `PictureOf("hand")` is `<body>_hand` if that picture exists, else
`hand`, the shared glove (`hero_hand`). No table, no json entry:
- the seven raw suits wear `hero_hand`;
- catchers draw their coloured `glove()` as `hero_catcher<X>_hand`;
- bare outfits draw theirs as `hero_outfit_N_hand` (+ `_hand_skin` under it, tinted by `LookTint` as every `*_skin` is): the bare
  hand, the rag wrap over its back, the Трюмный gauntlet's glove (its cuff stays on the forearm, above the line).

**The shared glove** is picked by the user on an Artifact page: the gloves cut from the seven repainted raw arms and the code-drawn
`glove()`, each at game scale on an arm. The pick is exported once as `hero_hand`.

**New suits.** The GPT sheet note (`suits.js` header, art README): the arm painted down to a hem on the wrist line; below it is
never shown.

## Migration order
1. `Rig.Wrist`, `CutAtTheWrist`, the hand node and `PictureOf("hand")` in `Figure`.
2. `hero.js`/`catchergear.js`: each code-drawn body draws its hand as its own part; its forearm stops at the line.
3. The glove pick page; the pick exported as `hero_hand`.
4. Suit cards re-cut (`--suit-cards`); every pose checked on the dress sheet (`--dress-sheet`). A raw suit whose cuff ends above the
   line shows a flat strip of old glove: only then is its sleeve repainted down to the line.

## Out of scope
- **Hand poses** (a fist, a grip, open on a ladder): no feature asks for one; a held item's grip covers today's closed glove.
- **Turning the hand with a swing's wrist or the screwdriver's turn**: today's motion stays (animation changes start from the
  current). The wrist node makes it a one-line change for the first feature that wants it.
- **A second shared glove** (a heavy gauntlet for heat or diver): a body that ships one draws `<body>_hand`; no machinery now.
- **A per-body wrist line**: every arm we have, the gauntlet included, cuffs above one line.
- Tinting a glove per suit or in the player's colour; the regen suit's light wave on the hand.
- Rig changes: `FistOf`, `FistBack`, `HeldAt`, `HaftGrip`, `OffHand` stay; the glove sits where the old one sat.

## Trade-offs accepted
- Raw suits share one glove: the goal.
- The raw PNGs keep their old gloves below the line as dead pixels (a few KB): no repaint, no risk to the paint above.
- Bare outfits: the forearm's skin now lies over the hand's back, where the hand lay over it; accepted if the sheet is clean.

## Tests
- game.tests: `Rig.Wrist` equals the art tools' number; every `hero_*_arm` body that is not a raw suit has its `_hand`; a
  `_hand_skin` exactly where there is an `_arm_skin`. Proven to bite by removing one.
- No sim tests: no sim state changes. The dress sheet and suit cards are the visual check: every hand identical per glove, no seam.

## Cost
`Figure.cs` (~60 lines), `Rig.cs`, `hero.js`, `catchergear.js`, `limbs.js`; ~9 new small PNGs. Two more sprites per figure.
