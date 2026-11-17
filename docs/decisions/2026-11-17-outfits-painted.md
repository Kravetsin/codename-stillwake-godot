# The four outfits are painted raw, worn hard; the hips closer

- Status: accepted; amends [whole-limbs](2026-11-16-whole-limbs.md)'s outfit pictures and the rig's `HipApart`
- Date: 2026-11-17

## Context

Beside the detailed suit and helmet skins ([fancy-skins](2026-11-16-fancy-skins.md)), the four outfits a hero wears with no
suit (`Look.Outfit`) looked plain. Asking GPT for new clothes failed three times on the pick page: wrappings were taken
literally, a torso came out seen from the front, the four of a row differed only in colour, and every leg came out half as
wide again with a shoe too big: it read the pieces as a body and dressed it. What worked was handing it the four outfits
as they are, in their own colours, and asking only for more detail. The user picked the hardest-worn of three takes,
«как у раба». In play the back leg's top corner then showed past the torso's ragged hem.

## Decision

- **Painted raw.** `hero_outfit_<n>_torso`, `_arm`, `_leg` and their `_skin` are GPT's detail pass over the old pictures,
  worn hard: torn hems, crude patches, grime. Each keeps its cut, colours, marks and shoes. The code that drew them is
  gone from `tools/anime-art/subjects/hero.js`, so an export cannot write the old ones back.
- **Skin by a key colour.** Bare skin is painted bright green on the sheet and cut out into the `_skin` picture, painted
  in the skin colour `LookTint` scales from; the clothes lie over it as before.
- **Fitted to the old outline.** Each row of a piece is stretched so its painted span lies on the old picture's span in
  that row: the generator paints limbs wider than the rig they bend on, even when told not to.
- **The hips closer.** `Rig.HipApart` is 0.07 of the body's width, not 0.14: the legs stand under the torso, and no thigh's
  top corner shows past its hem. Every pose, step and test reads the constant, so none changed but the look.
- **`--dress-sheet`** shows every outfit worn bare, each on another skin.

## Deliberately not handled

- The four keep their order and their gloved one (`HeroLooks.OutfitGloved`); no new outfits.
- The old pictures' front-facing touches (a collar, the «417» number) stay, as the user picked them.
- Two legs standing still now overlap more than before, suits too: accepted, it reads cleaner.
