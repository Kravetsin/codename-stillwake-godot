# Build hints: a screen of key caps that changes with the hand

- Status: accepted
- Date: 2026-11-05

## Context

In the 2026-11-04 playtest nobody understood the build menu, though its hint was on screen: one 11 px grey line under the
cards listing every input at once, cut by an ellipsis. Nothing said that the menu only plans and a screwdriver builds
([0107](0107-left-is-front-right-is-back.md)), and the pipette was still called Q after it had moved to G.

## Decision

- **Lines by the hand's state** (`Hints.LinesOf`, Godot-free, tested a state each): at most four, one action each, its
  press drawn, then one to three words.

  | State | Lines |
  | --- | --- |
  | Plan out, nothing in hand | LMB pick a card · RMB-drag take down · MMB back walls or front (aboard, two layers) · B close |
  | A card in hand | LMB place · LMB-drag fill an area (walls, back walls) or lay a row (pieces), none for a machine · RMB-drag take down · B close |
  | Wiring | LMB-drag pull a cable · Ctrl + LMB-drag move a peg · RMB cut · B close |
  | Piping, pipe | LMB-drag lay a pipe · RMB take away · B close |
  | Piping, valve | LMB valve on a joint · RMB take off · B close |
  | Typing in the search | Enter done · Esc clear |

  With any plan standing, the last line (in place of B) is how to build it: the builder's hotbar digit, its icon, LMB held
  with a filling ring, "build the plans". The power driver is named before the screwdriver; with neither carried the
  screwdriver's icon is struck red, "no screwdriver". The line flashes once as it appears.
- **The pointer's line** (`Hints.PointerOf`), left below the pointer, clear of the cost tip on its right: over a plan of
  the focused layer RMB cancel; over something built whose card is known G same card. Not over the menu, not with a card
  hovered.
- **The look, the user's picks** on [Подсказки стройки](https://claude.ai/artifact/QcRmxLNBBAkZQmhNDdo81m):
  - **Place: the wing's column** (`HintWing`), a narrow phosphor screen in a metal frame bolted under the layers' keys
    on the tablet's right, down to the screen's foot; long lines wrap, indented (`HintFlow`). Picked over a strip in the
    tablet's foot, a line by the pointer alone and a hologram over the frame.
  - **Mouse: the buttons alone**, two halves and the wheel, the pressed part lit; a drag's arrow pulses after it, a hold's
    ring fills. Picked over a whole mouse and a cap with a tiny mouse in it.
  - **Keys as lit caps** with the bound key dark on them (`Binds.NameOf`, so rebinding changes them).
  - Signs drawn by `tools/anime-art/subjects/signs.js` (`sign_mouse*`, `sign_drag`, `sign_hold*`, `sign_cap`) and drawn
    through `game/shared/KeyCaps.cs`.
- **The layer keys carry the middle button's sign** in their corner, as the mode keys carry Z X C.
- The flat `hint.build`, `hint.cable`, `hint.pipe`, `hint.valve` lines and `Tools.HintOf` are gone; "Q" is G everywhere.

## Consequences

- `KeyHint`, `HintFlow` and `KeyCaps` live in `game/shared` for the next hints: the crew's use prompt ("[Screwdriver,
  hold LMB] Build" still speaks in words over the hotbar), the handbook's hint, the fabricator's mouse keys.
- `-- --testbed --hints` plays every state in turn for a recording.

## Deliberately not handled

- Hints outside the plan: each moves to `KeyCaps` in its own change.
- Counting what a player has learned to hide lines: it would be saved state, and would hide them from those who need them.
- Esc stays one press that puts the plan away; RMB during a left drag stays ignored; nodes still light only while dragging
  a cable. No player sequence asked for any of them.
- Gamepad glyphs; the mouse buttons and Ctrl are not rebindable, so their signs are fixed.
