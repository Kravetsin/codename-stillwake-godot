# Whole limbs: one picture an arm or leg

The hero's arm and leg are each drawn from one picture (`arm`, `leg`) bent at the elbow and knee, instead of two
(`upperArm` + `forearm`, `thigh` + `shin`). A raw GPT suit is then a sheet of four parts (torso, pack, arm, leg), not six, and
its patterns run on through the knee and elbow.

## Rules

- Poses, joint angles and `Pose.cs` do not change; client only.
- Nothing is redrawn: each existing pair is joined into one straight picture in the rest pose, by today's seam
  ([bent-limbs](../decisions/2026-11-14-bent-limbs.md)): the lower from where it is full width, the upper fading out over it.
- A try first: the new limbs behind a dev key in the plain game, the two-picture limbs beside them.

## Try (done on `claude/whole-limbs-c8161e`)

- **W-1 Converter.** `tools/anime-art/limbs.js`, run by `export.js` after every subject: `X_upperArm` + `X_forearm` → `X_arm`,
  `X_thigh` + `X_shin` → `X_leg`, `_lit` and `_skin` alike, a missing half empty. The upper's box at the top-left, the lower's
  joint at the elbow or knee to the hd pixel, the height padded to 4. One seam a limb, a port of `BentLimb.SeamOf` on the halves'
  widths, cloth over skin. A stem with a whole already is skipped, so a raw sheet with `arm` and `leg` boxes needs no code.
- **W-2 Figure.** `Figure.WholeLimbs`, on by default: the lower part (forearm, shin, the boot) wears the whole limb hung from its
  joint, drawn `Seamless` by its `BentPicture(lower: true)`; the upper wears nothing. The boot, ankle cut, tracker and draw order
  are unchanged. `Wear` dresses again when the flag changes. A crumbling body paints a whole limb's rows above the joint with the
  upper bone. `BodyFit.FadeOf` fades `arm` as `upperArm`; `SuitGlow.UpOf` has `arm` and `leg`; bands wear `<body>_arm_lit`.
- **W-3 Key.** F10 in any run (`game/dev/WholeLimbsKey.cs`, added in `Main.Dev.cs` unconditionally) switches every figure.
- **W-4 Tests.** `WholeLimbsTests`: every pair has its whole, `_lit` and `_skin` too, its size and lower rows by `Rig`'s bones;
  `BodyFitTests`, `CreatureLooksTests` name the wholes.

## Deliberately not handled

- The regen suit's wave lights a whole limb in one step (`UpOf` one height a limb), where the halves differed by 0.1.
- Skin and cloth are joined layer by layer, not half by half: they differ only in the fade rows of outfits with skin.
- The lower half sits up to ⅛ px off its bone (rounded to the hd pixel).
- The design review proposed cutting the seam port and the live switch: kept, the seam because the task names today's seam
  rule, the switch because the user compares in play as in PR 570.

## Ship (after the user's yes)

- **S-1** Delete the upper sprites and their bands, `Limb.Seam`, the flag and the key; the lower sprite is named `arm`/`leg`.
  `SeamOf`, `Edge`, `Plain` and `SuitArt.RowsOf` go if no creature uses them (`CreatureBody`, `CreatureBendTests`).
- **S-2** The code-drawn subjects (`hero.js`, `catchergear.js`, `heatSuit.js`) keep drawing halves as the converter's input only:
  halves are no longer written to `game/art/hd`. Delete the old PNGs.
- **S-3** Re-lay `traced/suit*.png` and `.json` as torso, pack, arm, leg from the joined pictures; the GPT sheet for a new suit
  has those four parts. The suit icon built from the parts (skins) takes arm and leg.
- **S-4** Rename `UpOf`, `FadeOf` cases and tests; an ADR superseding the seam part of bent-limbs; the crew README; delete this plan.
