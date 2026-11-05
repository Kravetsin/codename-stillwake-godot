# The tablet's top edge along the foot, with the keys on it

- Status: accepted; where the keys sit superseded by [hologram-keys](2026-11-05-hologram-keys.md)
- Date: 2026-11-02
- Changes the place of [key-lens](2026-11-02-key-lens.md)'s key, and [tablet-key](2026-10-31-tablet-key.md)'s press, which now also puts the tablet away; replaces the air overlay's chip of 0030 and the words at the build menu's foot

## Context

The foot of the screen said "O air" and "[B] Build [T] Tablet" in small boxes, and the tablet's lens sat apart at the top beside the gauges. The user asked for the top of a tablet showing at the
foot, with three keys on it that say what to press when the pointer is on them. What is always on screen should speak without words, and the tablets already slide up from there.

## Decision

- **The rim is a tablet's real art** across the window, 12 pixels in from either side, with a little of its dark screen showing: 34 pixels at the keys, 38 at the stylus. The user picked it from five
  candidates on a page drawing each on a real frame: the whole rim, a short length of it, its corners alone, a thin sliver with the keys behind it, and a tilted wedge.
- **Three keys sit on a saddle of the same metal in the middle:** air (O), build (B) and tablet (T), the last the lens of [key-lens](2026-11-02-key-lens.md), with its light. Air and build are lit sky while on. Their glyphs are drawn in code, as the other wrist keys'.
- **Every tablet rises out of the rim** (`TabletPeek.Pose`): it starts as the rim, whatever its own size, and narrows or widens to its rest along its slide, what it shows staying at its own size in its middle. While one is up, the rim is gone.
- **The keys ride on the top of whichever tablet is up,** so they stay in reach, drawn over the tablets on a layer of their own. The tablet's key now puts the tablet away as well, as T does.
- **A key says its name and hotkey above it** under the pointer, or below it with no room above (`TipPlace.Over`): beside the pointer, at the screen's foot, it would cover the next key. There is no text at rest.
- **The plan taken out by its key puts away the tablet and the Handbook,** as B's press does for the tablet from the keyboard.
- **The version moved** left of the stylus and above the metal, which would otherwise cover it.
- **A click on the rim's metal does not reach the world,** and the crew's markers keep out of the rim, for it is on the screen all the time.

## Deliberately not handled

- **Rebound keys:** the hotkeys are fixed, so a tip names them as text.
- **The 22 pixels of the world under the metal:** the camera shows them no other way. They are the price of the whole rim, which the user chose over the shorter ones.
- **A sound for a key:** none. A new sound is its own pick.
- **What is not a tablet:** the inventory, the fabricator and the computer's window do not rise from the rim. They are not tablets, and lie above it.
- **The wings of the build tablet** come out from behind it as it narrows, with no motion of their own.
