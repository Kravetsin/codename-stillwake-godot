# Ruins (client)

## Files

| File | Purpose |
| --- | --- |
| `HomesView.cs` | The arc makers' homes, seen (0091): each piece the sim keeps standing (`BuildingModule.Plants` of a kind in … |
| `RelicSparks.cs` | A relic sparking (0096), the relic page's live emitter a step at a time: small arcs on twos from its cracks to its … |
| `SpiresView.cs` | The spires alight round their relics (0096): each spire whose cradle the sim keeps standing … |

The arc makers' homes, seen (ADR 0091). The pieces are Building's growths (`sim/Building/Flora.cs`, `Content.Ruins.Pieces`):
stamped into the ground with it, felled by hits or with what holds them. This feature only draws them (`HomesView`).

- **Reads:** `BuildingModule.Plants` of a piece's kind, `PlantAt` (what a piece stands on, a chair's table), `Cracks` (hits),
  `Zaps(cell)` (whom the resonator zaps: the rule is the sim's); where each hero's body is, from `Main`.
- **Pictures** (`art/homes/{kind}_{variant}`, `_glow`, `_dark`): exported from the concept page's own drawing code by
  `tools/homes-art/export.js` (the page's `decor.js`, unchanged but for its require), which also writes `Content/HomesArt.cs` —
  each piece's box in its picture, its lights and its live emitters — the plaster's tiles, the cloth's and the arc lamp's
  icons and the two placeholder sounds. Change the drawing code and export again; never edit what it writes. Two variants: the
  chair turned (it faces its table or desk), the banner in indigo (by its cell).
- **Draws** one atlas, two canvas items for every piece in view, at `Layers.Flora`: their pictures lit as the world is, behind
  ones (a back wall's, a rug) first; their glows added unlit. A floor's piece stands on the floor in the middle of its columns,
  or on the piece under it (a table's top a pixel in); a ceiling's hangs from it; a back wall's sits in the middle of its cells;
  the fallen beam lies on the floor under its walls, its high end over the room (`HomesLooks.BoxAt`, tested).
- **Mood:** what never goes out always burns; every lamp that may go out (the ceiling lamp, the sconce, the floor lamp, the
  lantern, the window, the kettle, the cradle's mobile, the chest's lock, the hologram) is lit two in three, by a hash of its
  cell (`HomesLooks.Lit`). `--homes lit` and `--homes dark` light all or none of them, the page's two moods.
- **Light:** each lit piece's lights where the page puts them, in their colour, the makers' arcs a fifth wider and three tenths
  stronger (the page's scenes), keep 0.72 a cell, 4 px wide; a pulse swells and sinks them, the broken lamp's sputter switches
  them; at most `HomesLooks.MostLights` (40), the nearest the view's middle, to the light feature (`LightView.Homes`).
- **Live** (the page's emitters at its 24 ticks a second, only in view): arcs (`Bolts`, cyan), pulses with a soft bloom, the
  broken lamp's bursts, the mobile's four stars, the hologram's scanline and its jump, the desk's last pixel, the garland's
  twinkles, the live plant's spores, the hearth's embers, motes in the window's light and the cobweb's; banners, the tapestry,
  the curtain and the herbs sway from their top, whole pixels, a slow breath each in its own phase (`HomesLooks.SwayAt`).
- **Hits:** a deeper crack shakes a piece and knocks chips off it; felled, a puff of dust and chips, a lamp's sparks. The music
  box plays its tune (`HomesLooks.Tune`) on every hit and as it breaks: the placeholder tine (`Sound.MusicBox`) at eight pitches.
- **The zap:** while the sim says a hero's cell is zapped, arcs leap from the resonator's sphere to its body, crackling
  (`Sound.Zap`, a placeholder).
- **Cost** (`--ground=home-hearth --homes lit --measure`, RTX 4060 Ti, 1280 × 720, zoom 2.4): 842 fps against 799 with
  `--no-homes` (noise), the view 0.012 ms a frame, 11 draws and 8 lights more.

## The relic (0096)

- **Reads:** `BuildingModule.Relics` (each relic grown and whether it is taken, from the felled cells: the rule is the sim's), its
  `Cracks` (a hit on a cradle), `PlantAt` (a spire's lens still in its tip), `IGridQueries.StructureAt` (the rock round the roots).
- **Pictures:** the cradle (`homes/shardCradle_0`, its `_dark` the empty cradle a taken one leaves), the niche
  (`homes/shardNiche_0`), the lamp a chip feeds (`homes/ceilLamp_0_fed`), the lens (`caves/spireLamp_0` dark, `ruins/spireLens_lit`
  and `_glow` alight) and the shard's icon (`items/arcShard`), all exported by `tools/homes-art/export.js` from the relic page's
  drawing code (`relic.js`, its design 1), with where their arcs play (`Content/RelicArt.cs`). The cradle and the niche are pieces:
  this view draws them as any.
- **A spire** (`SpiresView`), found by its cradle: while it stands the shard sparks (`RelicSparks`, the page's live emitter at 24
  steps a second): small arcs on twos from its cracks to its collar, legs and plates or into the air, now and then a reach to the
  chamber's walls, the ledge over it and the slab by it, sparks dripping; every 2.6–5.5 s a big branchy discharge into the
  chamber's metal, a flash of its violet-white re-lighting the room, sparks raining, the zap heard. Its light breathes with the
  hum; a light every 26 px up the light well's column lights the ledges; the lens burns in the tip and throws a pillar of light
  144 cells up the sky, a thin white core in it, motes rising in the beam; its roots glow in the rock round the chamber, where the
  rock still stands (V1's fulgurites, drawn again only when the grid or the light changes). A hit on the cradle arcs into the
  nearest hero as the sim shocks whoever struck.
- **Taken:** watched, the cradle sparks three seconds more, its arcs thinning out, a last discharge into the slab at
  2.5 s, then the whole spire goes dark over 0.4 s: pillar, shaft, lens, roots. Found taken (a load, a friend joining later), it is
  dark from the start. The empty cradle is drawn where it stood.
- **A home's heart:** in the hearth home the chip's niche sparks as the cradle does, smaller, its big discharge into the cable by
  it; its copper cable runs to the hearth and up and along the ceiling to the lamp over the table, arcs crawling along it. Only
  what it feeds burns (`HomesLooks.Fed`, `MoodOf`): the hearth, its arcs violet, and the lamp, violet-white; every other lamp of the
  home that may go out is dead. Taken, those two go dark and the cable is blackened.
- **Sound** (placeholders till the user's audition): the zap (`Sound.Zap`, the resonator's) on every discharge and every hit;
  the hum (`Sound.RelicHum`, the charging station's synthesised hum half again as high) round a burning cradle, fading as it
  sparks out.
- **Cost** (`--ground=spire-in --zoom 2.4 --measure`, RTX 4060 Ti, 1280 × 720): 792 fps, 1.26 ms a frame, against 935 fps and
  1.07 ms with `--no-homes`: 21 draws, some 30 lights (arcs, sparks, the shaft) and 0.06 ms of effects more.
- **Dev:** `--ground=spire-climb --autopilot` climbs from the chamber up the ledges, five held jumps, and out through the right plug, dug for it;
  `--take-at SECONDS` at a spire or a home strikes its nearest relic till it is taken.

## Deliberately not handled

- Pieces' parts animate as the page drew them; a hit piece only shakes, it shows no cracks (as plants, 0080).
- The page's wide halo round every glowing pixel is the light itself here, and the bloom of a pulse.
- The study's mound is whole stone cells, stepped, where the page cut it to slopes: the caves' growth stands on whole walls only.
