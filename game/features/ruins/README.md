# Ruins (client)

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

## Deliberately not handled

- Pieces' parts animate as the page drew them; a hit piece only shakes, it shows no cracks (as plants, 0080).
- The page's wide halo round every glowing pixel is the light itself here, and the bloom of a pulse.
- The study's mound is whole stone cells, stepped, where the page cut it to slopes: the caves' growth stands on whole walls only.
