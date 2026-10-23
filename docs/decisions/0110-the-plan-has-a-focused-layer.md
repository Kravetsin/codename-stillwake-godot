# 0110 — The open plan has a focused layer; the hand tools have card menus

- Status: accepted; supersedes 0108's rule for the right button's layer and its back-wall fade, and 0109's Ctrl+click valve
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

**1. A focused layer** (`Focus`, `PlanHand.Focus`): objects (machines, doors, hatches, ladders, floors), walls, back walls.

- A card picked sets the focus to its own layer (`Tools.FocusOf`). The wheel over the world turns it, down toward the back,
  round again (`Tools.Cycled`); the card stays in hand. Over the tablet the wheel still scrolls the cards. With the plan out
  the hotbar is chosen by number keys only.
- The right button, by click or rectangle, with a card or none, marks or cancels in the focused layer only
  (`Tools.RemovalIn`, `Drag.Judge`): everything of that layer in the rectangle, each piece or machine once; begun on a plan
  of that layer, its plans are cancelled instead. **The old rule is gone**: what lies under the drag's start no longer picks
  the layer or the kind, inside the objects layer too, nor does a wall's blueprint limit a cancel to walls.
- The focused layer draws whole, the other two at 35% everywhere in view (`Tools.AlphasOf`, the grid's tile layers and the
  machines' views), replacing the fade round the pointer while a back wall's card aimed. That fade stays for the axe and the
  drill digging back walls with the plan away.
- Beside the tablet, three stacked pictures with no words show the layers, the focused one lit.

**2. The hand tools' menus.** Holding the stripper or the wrench opens the plan's tablet with that tool's cards alone
(`Tools.CardsOf`): the cable; the pipe and the valve. Each shows how many of its item the player carries and its cost. The
card picked (`PlanHand.ToolCard`) decides the left button: the pipe lays spans and the right takes the nearest away; the
valve makes a valve on the joint pressed and the right takes it off. The tool put away closes the menu. The stripper keeps
Ctrl+drag for carrying a peg or plug: one card, and the press already tells a peg from a node.

**3. Pipes drawn whole.** A span is one straight tube at 4 px, at any angle, coloured a row at a time from the pipe art's
run, without its rivets; a flange only at joints, a valve's wheel on its joint. Hovered with the pipe card, the whole span
the right button would take is lit.

## Rejected

- **A focus kept with the plan away:** nothing works by it then; the weapons keep 0107's left-front, right-back.
- **The wheel letting the card go when the focus leaves its layer:** the left would then plan nothing; the card stays.
- **A peg card for the stripper:** a second card for what Ctrl+drag already does.

## Accepted limitations

- Pipes, cables, plans and lights do not fade with a layer out of focus.
- A rectangle in the objects' focus marks a floor and the machine on it alike.
- The layers' pictures are the crate, the scrap wall and the scrap back wall, placeholders.
