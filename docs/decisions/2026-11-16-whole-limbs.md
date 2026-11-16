# One picture an arm or leg

- Status: accepted
- Date: 2026-11-16
- Supersedes the two pictures meeting at a seam of [bent-limbs](2026-11-14-bent-limbs.md); its band at the joint stays

## Context

Each arm and leg was two pictures, `upperArm` over `forearm` and `thigh` over `shin`, bent as one piece at the joint and meeting
at a seam where both were full width (bent-limbs). New suits are painted raw by GPT from a sheet of the rig's parts: six parts made
six chances for artifacts, and a pattern broke at every knee and elbow where the halves met.

The whole limb was tried behind a dev key in the plain game, beside the two pictures; the user played both and took the whole limb.

## Decision

- **A limb is one picture**, `hero_<body>_arm` and `_leg` (`_lit`, `_skin` beside them), in the upper half's box and joint, the
  lower half's below it at the elbow or knee. It hangs from the lower bone's node, bent there by `BentPicture` as a helmet's and a
  creature's are: above the joint rigid with the upper bone, below it with the lower. Poses do not change.
- **Converted, never redrawn.** `tools/anime-art/limbs.js` joins each pair a subject draws, straight in the rest pose, at the old
  seam: the lower from where it is full width, the upper fading out over it to where it still is, so the rest pose is as before.
  Only the whole is written; the code-drawn subjects (`hero.js`, `catchergear.js`, `heatSuit.js`) keep drawing halves as its input.
- **A raw suit's sheet is four parts**, torso, pack, arm and leg (`traced/suit*.png`); the four painted ones were re-laid from their
  joined limbs. A new one is painted so, the arm and leg straight.
- The boot is still the leg's picture below the ankle, turning there; the tracker is strapped over the leg on the lower bone's node.
  A crumbling body breaks a limb along its two bones.
- With no second picture, the seam left the game: `BentLimb.SeamOf` and the fade across it, `SuitArt.RowsOf`.

## Deliberately not handled

- The regen suit's wave lights a whole limb at one height (`SuitGlow.UpOf`), where its halves differed by about a tenth.
- An outfit's skin and clothes were joined each on its own, not together: they differ only in the fade's rows.
- The lower half sits up to ⅛ px off its bone, rounded to the hd pixel at the join.
