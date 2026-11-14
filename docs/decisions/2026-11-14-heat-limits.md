# Heat: the other side of the degrees

- Status: accepted; the rule built (step H-1 of [docs/plans/heat.md](../plans/heat.md)), the heat suit and the looks to come
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
  surface, space's coldest and the ember band's top; the heat suit (to come) −40° to +60°, the ember band's hottest.
- **Heat felt** (`Heat`): degrees over `HighOf`. The breath charges `Felt + Heat` at `Bite` on the cold's schedule, and a
  covered breath ends the last breath only with neither felt; the regen suit gives none of it back. Every pair has
  `Low < High`, so the two are never felt at once, and an aired room (+20°) is inside every pair, caped or not.
- **On screen, for now:** the thermometer's hover gives both limits, the crossed one red; a body's card adds «Выдерживает жар
  до +50°» under a stand-in heat sign. The haze, the thermometer's heat colour and the sign come with the picks (H-2, H-3).
- Content only: no save field, no format bump.

## Consequences

- With the gradients, the amber deep is free for everyone suited; a basic suit feels heat from a fifth into the ember band
  and 10° from its middle down: a basic helmet lasts 112 s there, not 225. With the cape, 15°: 90 s.
- A hero with no body feels heat from +30°, low in the amber deep.
- The regen suit in the ember band feels the heat as the basic one does, but gives back the thin air's share while powered.
