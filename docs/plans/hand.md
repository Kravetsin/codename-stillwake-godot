# Design: the hand as its own picture

Reviewed (design-author, cut by design-reviewer); two hands and the pick page agreed with the user 2026-11-16.

## Problem
Each raw GPT suit paints its glove inside the arm picture, so the finger count changes from run to run (four or five); on
2026-11-16 every glove was repainted by hand. Each new suit brings the artefact back, and the suits' hands differ.

## Player experience
1. In any suit the arm ends at its cuff and the same five-finger glove hangs below it.
2. Swinging, drilling, climbing, holding an axe in both hands: the hand moves exactly as today, fixed to the forearm.
3. Bare, the same bare hand in the player's skin tone (the Трюмный outfit keeps a glove under its gauntlet).

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

**Two hands, for every body** (the user, 2026-11-16: a glove differing per suit is hardly seen at 4 px, and only work):
- `hero_hand_glove`, worn by every suit, catcher and the Трюмный outfit (its gauntlet's cuff stays on its forearm);
- `hero_hand_bare`, worn by the other bare outfits, a skin picture tinted by `LookTint` as every `*_skin` is. The Списанный's
  rag wrap stays on its forearm, above the line.
- Which a body wears: a suit or catcher gloved; an outfit by a `Gloved` flag beside `HeroLooks.Outfits`.
- Each picture has **a piece of wrist** above the hand, 1.5–2 px, running up under the cuff: no cut shows, even under a cuff that
  ends a little high, or later when the wrist turns.
- Each is **a strip of frames** (`Hframes`): frame 0 the hand as it hangs and grips today; the others are emotes' hand shapes
  (open palm, pointing…, docs/plans/emotes.md), drawn once for both hands. The wrist node turns for them; today it never does.

**The look is picked by the user** on an Artifact page: the hand drawn in code (`hero.js`'s `hand`/`glove`), cut from the raw
suits' repainted gloves, and painted fresh by GPT on one sheet; each bare (in several skin tones) and gloved, on the arms of several
suits at game scale, with an open palm and a point beside the hanging hand to show how each takes a pose.

**New suits.** The GPT sheet note (`suits.js` header, art README): the arm painted down to a hem on the wrist line; below it is
never shown.

## Migration order
1. `Rig.Wrist`, `CutAtTheWrist`, the wrist node and the hand's picture (glove or bare) in `Figure`.
2. `hero.js`/`catchergear.js`: no code-drawn forearm draws a hand; each stops at the line.
3. The pick page; the pick exported as `hero_hand_glove` and `hero_hand_bare`.
4. Suit cards re-cut (`--suit-cards`); every pose checked on the dress sheet (`--dress-sheet`). A raw suit whose cuff ends above the
   line shows a flat strip of old glove: only then is its sleeve repainted down to the line.

## Out of scope
- **Hand poses outside emotes** (a grip round a haft, open on a ladder): no feature asks for one; frame 0 serves them as today.
- **Turning the hand with a swing's wrist or the screwdriver's turn**: today's motion stays (animation changes start from the
  current). The wrist node makes it a one-line change for the first feature that wants it.
- **A glove per suit**: two hands for all; a suit that later wants its own draws `<body>_hand_glove`, no machinery now.
- **A per-body wrist line**: every arm we have, the gauntlet included, cuffs above one line.
- Tinting a glove per suit or in the player's colour; the regen suit's light wave on the hand.
- Rig changes: `FistOf`, `FistBack`, `HeldAt`, `HaftGrip`, `OffHand` stay; the glove sits where the old one sat.

## Trade-offs accepted
- Every body shares one of two hands; catchers and the Трюмный lose their own gloves' colour.
- The raw PNGs keep their old gloves below the line as dead pixels (a few KB): no repaint, no risk to the paint above.
- Bare outfits: the forearm's skin now lies over the hand's back, where the hand lay over it; accepted if the sheet is clean.

## Tests
- game.tests: `Rig.Wrist` equals the art tools' number; both hands exist with their frames. Proven to bite by breaking one.
- No sim tests: no sim state changes. The dress sheet and suit cards are the visual check: every hand identical per glove, no seam.

## Cost
`Figure.cs` (~60 lines), `Rig.cs`, `hero.js`, `catchergear.js`, `limbs.js`; 2 new small PNGs. Two more sprites per figure.
