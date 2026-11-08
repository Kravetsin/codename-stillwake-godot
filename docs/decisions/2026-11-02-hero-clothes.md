# One body, four sets of clothes

- Status: accepted.
- Supersedes [the player's colour is a mark](2026-11-01-player-colour-mark.md) on where the mark sits; follows [ink and watercolour](2026-11-01-ink-and-watercolour.md).

The hero is a scavenger who was a slave aboard a ship. The old drawing did not say so: a big head, sausage limbs, anime eyes, a thumb lying on top of the fist, a bright yellow suit. On [a page](https://claude.ai/artifact/FPyv4HhatgsXPeNs5pgCAW) of four candidates the user wanted them all, with «Выцветший»'s suit for everyone.

**Clothes, not body types.** `Look.Outfit` picks among «Каторжник», «Списанный», «Трюмный» and «Выцветший», a new row in the look editor; a peer that never chose wears «Каторжник», whose stencilled number tells the past best. A body type would ask every suit to fit every body. Clothes and the suit are drawn in the same boxes on the same joints and replace each other whole, so nothing is fitted to anything, and each set may be a little thinner or heavier inside its box.

**One head.** All four share a skull, 9.3 px tall of the box's 12, so any face and hair go with any clothes and under the helmet. The heavy jaw and the hollow cheeks of two candidates, and «Трюмный»'s head carried forward, are given up for it.

**Skin is its own picture.** `hero_outfit_<n>_<part>_skin` lies under the clothes and alone takes the look's skin. The colour test it replaces (`LookTint.IsSkin`) took rags and waxed cloth for skin.

**The mark is a tracker on the near shin**, «Браслет слежения» of three: owned, like a house-arrest bracelet. A strap lit as the hero is and a light of its own, 5.2 × 1.4 px, unlit and tinted by the player's colour; it replaces the helmet's, chest's and pack's. Separate sprites, so no clothes or suit draw it.

## Deliberately not here

- **The mark as far as the stripe carried.** The 6.5 × 1.3 px stripe told players apart at a far zoom; this one is smaller and seen only from one leg. The markers layer still tells them apart from afar.
- **The ground's tall grass.** Grass over 5 px hides it on a planet. Accepted.
- **`-- --pixel-art`.** The clothes have no pixel pictures: pixels are no longer supported.
