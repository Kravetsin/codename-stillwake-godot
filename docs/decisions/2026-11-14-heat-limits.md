# Heat: the other side of the degrees

- Status: accepted; built: the rule, the heat suit and the looks, picked on a page of candidates
- Date: 2026-11-14
- Extends [temperature-in-degrees](2026-11-14-temperature-in-degrees.md): a body's limit both ways

## Context

The first planet's ember band reads +60° and cost nothing: the deep felt like the garden, nothing asked for a heat suit, no card
said what a body does against heat. The cold already runs on degrees past a limit; the plan made heat its other side. The user
set the numbers by what each thing is made for, not by round numbers, on 2026-11-14.

## Decision

- **A hero's heat limit** (`HighOf`): its body's `Item.High`, or `Temperature.BareHigh` with none. The cape lowers both
  limits by `Cape.Warmth` (5°): warm against the cold, worse in the heat (the user's pick over a cape that ignores heat).
- **The limits:** none 0° to +30°, a body's comfort; the basic and the regen suit −40° to +50°, made for space and the
  surface, space's coldest and the ember band's top; the heat suit −40° to +60°, the ember band's hottest.
- **The heat suit** (`Suit.Heat`, `heatSuit`, «Жаростойкий скафандр»): a body, made at the workbench of 4 mica, 4 alloy scrap,
  2 plates and 2 tubes in 240 ticks, so after a landing. No blueprint and never sold: the board's rule
  ([one-to-understand](2026-11-12-one-to-understand.md)) opens it once one of each part was obtained beside a workbench. It is the
  one body met first that is free of heat in the whole ember band; wearing it means not wearing the regen suit.
- **Heat felt** (`Heat`): degrees over `HighOf`. The breath charges `Felt + Heat` at `Bite` on the cold's schedule, and a
  covered breath ends the last breath only with neither felt; the regen suit gives none of it back. Every pair has
  `Low < High`, so the two are never felt at once, and an aired room (+20°) is inside every pair, caped or not.
- **On screen** (the user's picks): past the high the screen's edges **shimmer**, the world's rows pushed sideways by a rising
  wave, faintly warmed, and **sparks** drift up from the bottom and sides, both growing with the heat felt over 30 (`Haze`,
  `Report.Heat`, eased as the frost). The **thermometer**'s frost star gives way to a **flame** over the first 1.5° past the high
  (`Thermometers.Swap`), its number turns ember (`ffc46e` to `ff5a26`, deeper the further) and a shimmer rises off it; its hover
  gives both limits, the crossed one red. The **flame** (`sign_heat`, the ink sign's glyph in ember) is also the cards' heat
  sign, before «Выдерживает жар до +50°».
- **Its look, as picked** ("aluminised foundry"): quilted rolls of dull silver cloth, dark cuffs and a leather belt, heavy boots
  and gauntlets, a small dark pack. Traced from an AI-made sheet of its six pieces (`tools/anime-art/refs/heatSuit.png`), each
  piece apart so each is traced alone (`pieces` in the palette, `traced/heatSuit.<piece>.json`) and stretched over its part's box
  on the basic suit's joints (`subjects/heatSuit.js`). Its card is the jacket as worn: the torso, the near arm and the pack. The
  game's Steam AI disclosure must say so, as for the traced helmets. Seen worn, the user had it reshaped (2026-11-14): the torso
  straight, with no waist and no flare under the belt like a skirt; the sleeves fuller; the pack cut short of its strap's loop;
  the shin taken in under the tracker's strap so it wraps the leg. `Traced.straighten` moves a piece's outline row by row to
  its box's sides, keeping the quilting's bumps.
- Content only: no save field, no format bump.

## Consequences

- The limbs keep the rig's two pieces a limb: the joints show at the elbow and the knee, the dark cuffs of both pieces meeting.
  One-piece limbs bent at the joint are tried apart, for every suit at once.
- With the gradients, the amber deep is free for everyone suited; a basic suit feels heat from a fifth into the ember band
  and 10° from its middle down: a basic helmet lasts 112 s there, not 225. With the cape, 15°: 90 s.
- A hero with no body feels heat from +30°, low in the amber deep.
- The regen suit in the ember band feels the heat as the basic one does, but gives back the thin air's share while powered.
- The heat suit only ties the regen suit in the ember band (225 s on a basic helmet), the regen suit paying power for it; the heat
  suit is met first, needs no power and costs no credits.
- A suited hero's haze in the deepest ember is moderate, never full: it costs twice the breath there, not more.

## Deliberately not handled

- Heat from lava, furnaces, machines or the galley; rooms heated or cooled; lava's burn is still `Lava.Burn`, spared by no suit.
- Temperature for machines, plants, creatures or liquids; a suit with both regen and heat; coolant consumables.
- Hotter planets: they need only their own `Temperatures` row.
