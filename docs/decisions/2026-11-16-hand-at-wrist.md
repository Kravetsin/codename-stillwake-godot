# The hand is its own picture, hung at the wrist

- Status: accepted
- Date: 2026-11-16

## Context

Each raw GPT suit painted its glove inside the arm picture, so the finger count changed from run to run (four or five) and
each suit's hand differed. Every new suit brought the artefact back. Gestures (gestures-for-words) also need hand shapes the arm
picture cannot have: an open palm, a pointing finger, a thumb.

## Decision

- **The boot's way, at the wrist.** `Rig.Wrist` is how far down the forearm the wrist is, one constant for every arm. The
  arm's sprite is cut there by a region (`Figure.CutAtTheWrist`, beside `CutAtTheAnkle`); the raw PNGs are not edited, so
  their old gloves below the line are never drawn. The hand is a sprite on its own wrist node on the forearm's node, never
  bent, and listed in `Figure._parts`, so far shading, the skin tint and the crumbling body need nothing new.
- **Two hands for every body.** `hero_hand_glove` is worn by every suit, catcher and the Трюмный outfit. `hero_hand_skin`
  is worn by the other bare outfits and tinted by `LookTint`. Which an outfit wears is `HeroLooks.OutfitGloved`. A 4 px
  glove that differs per suit is hardly seen; only work.
- **The near hand is seen from outside, the far one from its palm.** Each is painted on its own strip:
  `hero_hand_far_glove` and `hero_hand_far_skin`. The picked look was painted by GPT on one sheet and traced, both sides.
- **A piece of wrist** above each hand runs up under the cuff, so no cut shows under a cuff that ends a little high or
  while the wrist turns.
- **A strip of frames** (`Hframes`): 0 grips as the old hand did; 1 open, 2 point, 3 thumb are the gestures'
  (`GestureMotions.*Frame`). Only a gesture turns the wrist node.
- **New suits** are painted down to a hem on the wrist line (the `suits.js` sheet note); below it is never shown.

## Considered

- **Repainting each suit's glove**: the same artefact returns with every new suit.
- **A glove per suit**: unseen at this scale. A suit that later wants its own draws `<body>_hand_glove`, with no new
  machinery.

## Not handled

- **Hand poses outside gestures** (a grip round a haft, open on a ladder): no feature asks for one; frame 0 serves.
- **Turning the hand with a swing or the screwdriver**: today's motion stays. The wrist node makes it a one-line change.
- **A per-body wrist line**: every arm we have, the gauntlet included, cuffs above one line.
- Catchers and the Трюмный lose their own gloves' colour. The raw PNGs keep dead glove pixels below the line.
