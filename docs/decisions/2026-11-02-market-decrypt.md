# The black market looks hacked

- Status: accepted
- Date: 2026-11-02
- Amends [black-market](2026-10-31-black-market.md): the market gets a look of its own
- Supersedes [tech-rings](2026-10-30-tech-rings.md): with the tree gone, nothing is drawn as rings

## Context

The market was the tree's ring map with 20 goods: half-empty and crude, in the user's words. A first round drew it as
journals and real objects, and the user turned them down: the window opens inside the tablet, so it is a screen. A
second round drew four electronic interfaces in the real tablet: a pirate channel, a radar sweep, a hacked interface and
a plant's control panel.

## Decision

- **The user picked the hacked interface, "Decrypt".** The tablet's own cold slate, a row per group with its order on
  the left and goods cards on the right. What is not open yet is garbage in place of names, prices and pictures, so
  nothing stands empty, and it deciphers character by character when its order fills.
- **One colour does not belong to the tablet:** a poison accent, `#ff2d78`, on what can be bought and the order being
  filled.
- **The logic stays:** goods for credits, groups open by orders, orders fill only through ordinary sales.

The design is in [market-decrypt](../plans/market-decrypt.md).

## Deliberately not handled

- **Scrolling:** six rows fit, and a Content test fails on a seventh, which brings it.
