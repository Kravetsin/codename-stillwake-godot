# The basic suit ships its picked repaint, its arm and leg one picture each

- Status: accepted, a trial to judge in game; supersedes for the basic suit the omission of
  [raw-tool-pictures](2026-11-14-raw-tool-pictures.md) that worn suits stay traced
- Date: 2026-11-14

## Context

Held tools ship their picked reference's own paint ([raw-tool-pictures](2026-11-14-raw-tool-pictures.md)); worn gear stayed
traced, for a second style beside the drawn head and helmets might show. The user wanted to try a raw suit on the body to see.
The basic suit was laid out from today's pictures at 8 times their hi-res size (`refs/suit.input.png`: the torso, the arm as the
upper arm over the forearm hung at the elbow, the leg as the thigh over the shin hung at the knee) and repainted by GPT over it.
The user picked `refs/suit.png`: a quilted padded ochre suit with ribbed rubber over the elbow and the knee. Its silhouettes
match the input's to within half a world pixel at the edges, so its parts land on today's rig.

## Decision

- `subjects/suit.js` cuts the torso, the arm and the leg from the repaint (`suit.palette.json`, `"raw": true`) and draws each
  as the raw tools are drawn: its own paint and colour, our ink round the silhouette, no water. Shrunk from the repaint by the
  canvas's high-quality smoothing, then the finish's box filter; the repaint is kept, so a larger `HiRes.Scale` is a re-export.
- The arm, shoulder to glove, is one picture (`hero_arm`), bent at the elbow; the leg, hip to sole, is one (`hero_leg`), bent
  at the knee and again at the ankle by the foot's angle, in a band of its own (`BentLimb` with an `ankle`). No end of a picture
  is anywhere along a limb, so nothing shows at a bend.
- The input hung the forearm and shin a fraction of a pixel lower than the rig does (23 and 37 hi-res pixels for 22.76 and
  36.56): each picture is raised by that, so the glove and the boot are where the two-piece pictures had them; the fist, every
  grip and the feet stand as before. The shoulder and the hip, under the torso, rise as much.
- A body brings whole limbs by data: `hero_<body>_arm` and `_leg` (the basic suit's `hero_arm`, `hero_leg`) are shown in place
  of its two parts when they exist, per limb; any other body, bare clothes and the catchers stay two-piece.
- The far arm and leg are the same pictures darkened, as the far limbs are. The whole arm's top fades in as the upper arm's
  does (helmets-fit-the-suit): no round end at the shoulder. The tracker stays on the near shin, over the leg.
- The pack stays the drawn one; the basic suit's drawn torso and limbs are gone.

## Consequences

- The suit is GPT's paint, a warmer and more saturated ochre than the muted one it replaces, beside the drawn head and helmets.
  The trial is whether that sits well; if not, this is reverted whole.
- The repaint's own shoulder pad, a dark disc, shows its lower half under the shoulder's fade.
- A crumbling body breaks its whole limbs at the elbow or knee, not at the ankle.
- The suit's card (`items/suit`) is still the drawn one.

## Deliberate omissions

- The other suits, the bare clothes and the catchers' outfits stay two-piece and traced till each is repainted and picked.
- No wrist bend: no pose turns the glove apart from the forearm.
