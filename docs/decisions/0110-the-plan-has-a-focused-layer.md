# 0110 — The open plan has a focused layer; the hand tools have card menus

- Status: accepted; supersedes 0108's rule for the right button's layer and its back-wall fade, 0017's refusal to take a
  cabled machine apart, and 0109's Ctrl+click valve
  and its drawing of spans
- Date: 2026-10-23
- Design: agreed by both developers in chat; no design file

## Context

With 0108 the right button removed in the selected card's layer, a front card's choosing between machines and structure by
the first thing under the drag's start; with no card it did nothing, and the right button held only faded the walls round
the pointer (a bug in play: a press meant to remove). A back wall behind a lifepod could not be seen or aimed at. The
stripper and the wrench hid choices behind Ctrl, and a span was drawn as the pipe art repeated a cell at a time, so its
seams read as separate pieces the right button does not take.

## Decision

**1. A focused layer** (`Focus`, `PlanHand.Focus`), one of two: the front, everything that cannot stack in a cell (walls,
machines, doors, hatches, ladders, floors and the other pieces), and the back walls.

- **The layers are strictly apart.** The menu shows the focused layer's cards alone, the search too: the back walls' in the
  back, every other in the front. A card picked sets the focus to its own layer (`Tools.FocusOf`). A press of the middle
  button with the plan open toggles it (`PlanHand.ToggleFocus`, `Tools.Toggled`), as does a click on a layer's button
  (`PlanHand.FocusOn`): the card in hand is kept for the layer left, and the one kept for the layer reached comes back to
  hand, so the card in hand is always of the focused layer, one a layer; the plan put away forgets both. The left button
  places only a card of the focused layer (`Tools.ActOf`). The wheel keeps its old work: the hotbar's slots
  over the world, the cards scrolled over the tablet. Nothing else takes the middle button in play; the dev camera's pan
  keeps it only with the plan away, as the plan takes the press first.
- The right button, by click or rectangle, with a card or none, marks or cancels in the focused layer only
  (`Tools.RemovalIn`, `Drag.Judge`): everything of that layer in the rectangle, each piece or machine once; begun on a plan
  of that layer, its plans are cancelled instead. **The old rule is gone**: what lies under the drag's start no longer picks
  the layer or the kind.
- The focused layer draws whole, the other at 35% everywhere in view (`Tools.AlphasOf`, the grid's tile layers and the
  machines' views), replacing the fade round the pointer while a back wall's card aimed. That fade stays for the axe and the
  drill digging back walls with the plan away.
- On the tablet's right edge under its stylus, two stacked keys with no words (`LayerKey`): wrist keys as the sort keys
  are (`WristKey`, the crate header's square size), sticking out of the metal as the bag's sort key does from the display's,
  the focused one backlit (the user: the painted buttons first tried read as recoloured, not as hardware): the front a solid bright block before a faint grid, the back a dim hatched block behind a dotted
  outline, drawn in code.
- No grid is drawn while the plan is open: the ghost and the pointer's outline already show where a card snaps.
- **Away from the ship** (`PlanHand.Aboard`, `PlanHand.Shown`) nothing fades, and the axe's and the drill's fade round the
  pointer is off as well (`PlanHand.SeesThrough`); the right button still digs back walls there. Seeing through the ground
  would show what it hides. The plan's layers still turn and keep apart wherever the plan works.

**2. The hand tools' menus.** Holding the stripper or the wrench opens the plan's tablet with that tool's cards alone
(`Tools.CardsOf`): the cable; the pipe and the valve. Each shows how many of its item the player carries and its cost. The
card picked (`PlanHand.ToolCard`) decides the left button: the pipe lays spans and the right takes the nearest away; the
valve makes a valve on the joint pressed and the right takes it off. The tool put away closes the menu. The stripper keeps
Ctrl+drag for carrying a peg or plug: one card, and the press already tells a peg from a node. Away from the ship neither
tool opens a menu or lays and cuts anything, and the sim refuses `power.notAboard` (`Lay`, `Move`) and `pipes.notAboard`
(`LayPipe`, making a valve).

**3. Pipes drawn whole.** A span is one straight tube at 4 px, at any angle, coloured a row at a time from the pipe art's
run, without its rivets; a flange only at joints, a valve's wheel on its joint. Hovered with the pipe card, the whole span
the right button would take is lit.

**4. A machine comes apart with its cables plugged in.** `building.cabled` is gone. As `building` takes a machine apart
it calls `IPowerOps.Unplug`: every cable ending on it stays, its end a free peg at the cell the plug was in, as a pipe
keeps a joint where a port was. A powered machine built is `Plug`ged: every cable ending on a peg at its plug's cell plugs
into it, so one built again where it stood is wired again. It costs a pass over the cables at each, nothing per tick.

## Rejected

- **A focus kept with the plan away:** nothing works by it then; the weapons keep 0107's left-front, right-back.
- **The wheel turning three layers (objects, walls, back walls):** the wheel is the hotbar's, and walls and the other
  pieces never share a cell, so one front layer with the middle button's toggle is enough.
- **A card kept in hand when the focus leaves its layer:** the left placed in a layer the right did not take (a bug in
  play); one card a layer, brought back with it, instead.
- **A peg card for the stripper:** a second card for what Ctrl+drag already does.

## Accepted limitations

- Pipes, cables, plans and lights do not fade with a layer out of focus.
- A rectangle in the front's focus marks walls, floors and the machines on them alike.
- A machine taken apart other than by `building` (a meteor, a dev clear) leaves its cables at its top-left, in the air;
  a peg left at a plug may stand up to a machine's size farther than a cable spans, and keeps working.
