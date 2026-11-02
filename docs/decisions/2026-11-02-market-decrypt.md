# The black market looks hacked

- Status: accepted
- Date: 2026-11-02
- Amends [black-market](2026-10-31-black-market.md): the market gets a look of its own, and the solar panel a stage
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
- **The solar panel moves to the fourth order's row,** at its same price, the user's idea. Sold from the start it was a blueprint nothing could yet build, for its cells come from the assembler, and the fourth order asks for circuits from that same bench. It also gives a decipher something other than one more slot to open on.

Client only, with no change to the sim: buying stays `BuyBlock`. A sale at the station fills the order with the tablet away, and when the market next shows, after the tablet's
rise, the group deciphers character by character, cards in turn, in about 1.6 s. Orders seen stay one count per machine and world, kept with the board, so the lamps and the decipher
agree and nothing is sent. The hack's marks over the screen are placed by hand and are part of the look.

## Deliberately not handled

- **Scrolling:** six rows fit, and a Content test fails on a seventh or a sixth good in a row, which brings it. A window under about 720 high clips the last rows.
- **Resuming a decipher** after closing or switching tab: rows show clear, for seeing it once is enough.
- **Combining or CJK text in the character grid:** only English and Russian exist.
- **A font of our own:** the tablet's mono, Consolas on the Windows release; another machine falls back to its monospace.
- **Rescaling the marks:** at another window's size their places scale, their sizes do not.
- **Repeat purchases:** contraband does not exist yet.
- **The candidate's flashes** on a sale and a purchase, its thousands commas and its count of goods open: the head and `CreditLine` are shared with the rest of the tablet.
