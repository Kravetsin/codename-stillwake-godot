# The credit chip stands upright and larger, and changing numbers count with a flare

- Status: accepted
- Date: 2026-11-04
- Changes [ink-signs](2026-11-03-ink-signs.md)'s credit chip; the credits' display from [market-key](2026-11-03-market-key.md) and the energy tab count with it

## Context

The diagonal gunmetal chip did not read at all at the sizes it is drawn, in the market or on the credits' display: a grey blob before a gold
number. Numbers that change, the credits and the belt's charge, just swapped digits.

## Decision

- **The chip, upright:** a light steel card, its window burning a cold cyan band, three brass teeth below, a lanyard hole. The user picked it
  from five on [Кредиты: иконка и счётчик](https://claude.ai/artifact/96YCk3D9RCYmRmBLu6TrEY), against a bronze cash coin, a verdigris nut, a stack
  of copper coins and an enamel «Cr» tag. Drawn by the `credit` subject as before.
- **Larger:** beside a number of size *s* the coin is *s* + 7 pixels (`CreditLine.CoinOf`), 19 at 12 px where it was 16; on the credits' display
  12, the glass's whole height, where it was 9.
- **A flare as a number changes** («Счёт со вспышкой»), picked against a rolling drum, a split-flap and the market's cipher settling: the credits
  count to what they read in 0.4 s (shorter when reads come quicker), a rise lighting toward white with a halo, a fall darkening toward ember;
  the digits that changed glow on half a second after. Weaker for a change under 8 % of the number. `CountFlare` keeps the count, `FlareText`
  draws it.
- **The energy tab** flares white on every change read, either way; its number still counts with `GaugeMotion`, and a hit still flashes it.

## Deliberately not handled

- **Prices:** they do not change while shown, so they do not count; only the coin grew.
- **The sale button's icon** in a trade station's panel keeps its size: it is a button's icon, sized by the button.
