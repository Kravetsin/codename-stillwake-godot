# A flat press blinks the energy tab red

- Status: accepted; adds to [energy-ring](2026-11-06-energy-ring.md)'s energy tab
- Date: 2026-11-07

## Context

At a playtest a player pressed the drill again and again and could not tell why it did nothing: the belt held no battery.
The tab showed it, dim, but nothing pointed there. The user asked for the charge to blink red, brightly, at such a press.

## Decision

- A press of the left button with an electric tool aimed (the drill, the grinder, the cutter), the power driver over the
  world, or the railgun clicking dry, while the belt cannot pay a tick of its draw or wears no battery, raises
  `Ui.PressFlat`, once a press; a grab of debris is no press of the tool.
- The bolt alone blinks red three times over two thirds of a second, hard on and nearly off, throwing red light round
  it past the tab's edge with a hot core; the first blink jolts it a pixel. At full brightness, a tab dim for no battery
  too. A press again starts it over. `FlatBlink`. A first cut washed the whole tab red and lit its rim: the user found the
  frame blinking ugly and asked for the bolt alone, glowing.
- Asked for by the user as it is, so no pick page.

## Deliberately not handled

- Holding the button down blinks once, at the press: the blink answers a press, not a hold.
- A drill fitted with the arc draws twice; a belt that pays the plain drill's tick but not the fitted one's blinks no red,
  as `Motor` asks by the content draw.
