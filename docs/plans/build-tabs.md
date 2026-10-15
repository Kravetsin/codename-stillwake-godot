# Plan: the build tab is the mode

(Design by design-author, reviewed by design-reviewer and its findings taken in. Mockup: https://claude.ai/artifact/RCmqtpSRM9dnN3jMutsjfL)

## Problem
The build menu (B) and the P and L overlays are separate switches, so an overlay outlives the menu: pick the cable card, L turns on; close the menu and L stays, its buttons dead. Moving a cable needs B and L at once. While the menu is open the "[E] …" prompt hides under it and the thing under the pointer is not outlined.

## Decided with the user
Tab = mode (Air → pipes overlay, Power → power overlay, others none); closing the menu turns it off. P/L toggles go; P/L open the menu on Air/Power. O stays global; while Air/Power is open their overlay shows, closing gives O back. Left = the card (Power, no card: drag peg/plug, no Shift; Cable: lay); right held = the hero works plans; middle = remove (click or drag; Power: cut); Q = pipette. Tools as equipment later. The fire axe is a hotbar item, out of scope. Prompt above the menu; outline in build mode.

## State model
- `PlanHand.PlanOut` as now; `PlanHand.Tab` new (replaces `BuildMenu._tab`, kept when the menu closes); `PlanHand.Selected` cleared on tab change.
- `Ui.AirOn` new bool, set by O only. The overlay shown is **computed on read**, never stored: `Tools.OverlayShown(planOut, tab, airOn)` — the open tab's overlay if it has one, else air if `AirOn`, else none — read each frame by `AirView`, `PowerView`, `Hands`, `BuildingView`. With the menu closed it is air or nothing, by construction; no listener order, no in-between states. `Hands.Frame` clears the world tip when the shown overlay differs from last frame's.
- `Ui.MenuTop`: a plain `float?` property, set by `BuildMenu` each frame while open, null closed.
- A tab the world shows nothing on is never the tab: `Open(tab)` refuses a hidden one, and `BuildMenu.Frame` writes the tab it falls back to into `PlanHand.Tab`, so the pipes overlay never shows over Structure.
- The `Selected → PlanOut` listener (for the old middle pick) goes.
- `Tab.Overlay` field. Deleted: `Tools.OverlayFor`, `Tools.TabFor`, `PlanHand.ShowFor` with `_before` and `_chose`, the listener that puts the cable away.

## Buttons and keys
| Input | Menu closed | Structure / Production / Storage | Air | Power |
|---|---|---|---|---|
| Left | hotbar item; grabs debris | card: plan along the drag | same | no card: move a peg/plug; Cable: lay; Lamp/Solar: plan |
| Right, held | works plans under the pointer in reach | same | same | same |
| Middle | nothing | click/drag: cancels plans if begun on one (every tab), else marks machine, structure, back | pipes, valves and machines | a press cuts a cable within reach, else removes as on the other tabs |
| Q | drops the hotbar item | pipette in the tab's layers; the card switches to its tab | same | the cable card on a node with a cable |
| B / Esc | B opens on the last tab | closes | closes | closes |
| P / L | opens on Air / Power | switches tab; on its own tab closes | same | same |
| O | toggles AirOn | toggles (shows) | toggles (pipes show) | toggles (power shows) |
| E | as now | same | same | same |

Shift means nothing to the hands; `ButtonEvent.Shift` goes. Q never clashes with dropping (drop only with a free hand, pipette only with the menu open). Removal layers: tab → overlay → `Tools.LayersFor` (`LayersFor(pipes)` = conduit, machine), a drag keeps its starting layers. The hero works plans on right held whatever the hand holds; grabbing debris stays a free-hand left click.

## Prompt and outline
`UsePrompt` sits 112 px above the bottom on `ScreenLayers.Bars`; the open menu on the same layer is taller and added later, so it covers it. Fix: `BuildMenu` sets `Ui.MenuTop`; the prompt's bottom goes 8 px above it when higher, placed every frame (its `Show` returns early when the text is unchanged, so a taller tab would not move it). The outline: `HeroView.UseHands` traces it only when `handFree`; that condition goes.

## Supersedes
ADR 0030 "The build tab is the mode" supersedes 0022's "Overlays show and filter", its mouse rules (empty left works plans, Shift+left moves, right removes, middle pipette) and reverses its rejection of removal on the middle button. 0021's move, pegs, sockets and laying stand; its power mode returns as the Power tab.

## Code
`PlanHand` (Tab, derivation, `Open(tab)`, `Pick(card)`), `Tools` (`Tab.Overlay`, `OverlayShown`, `TabOf`, `HintFor`), `BuildMenu` (Tab, B/P/L, hint line, `MenuTop`), `BuildingView` (middle removes, Q pipette, `_drag` keeps its button), `PowerHands` (middle cuts; card → lay, no card → move), `Buttons`/`WorldButtons` (middle events, `HoldsRight`; `TakeRight`, Shift go), `HeroView` (works on `HoldsRight`; outline always), `UsePrompt` (above `MenuTop`), `OverlayBar` (one chip, O toggles `AirOn`), `OverlaySwitch.cs` deleted, `dev/PlanDemo`, READMEs.

## i18n
`use.build` "[Hold RMB] Build", `use.deliver` "[Hold RMB] Bring what it needs", `use.takeApart` "[Hold RMB] Take apart", `build.open` "[B] Build"; one hint line a tab kind: `hint.build`, `hint.air`, `hint.power` (the held card's line already names the card). Deleted: `hand.card`, `hand.cable`, `hand.wires`, `build.close`, `overlay.pipes`, `overlay.power`.

## Tests
BuildingToolsTests: a table of `OverlayShown` over plan out × tab × AirOn; tab change clears the card; pipette switches tab; P/L open/switch/close and refuse a hidden tab; B reopens on the last tab; 1–9 or the wheel with the menu open puts the plan away; every tab's hint key exists. ButtonsTests: middle events, HoldsRight. New PowerHandsTests: no card → Move, Cable → Lay, middle cuts within CutReach. OverlaySwitchTests deleted. A PlanDemo recording checks overlay follows tab and the prompt sits above the menu.

## Migration
1. fix(crew): prompt above the menu, outline while open. 2. feat(building): tab is the mode, ADR 0030. 3. feat(building): the buttons, prompt texts, READMEs.

## Trade-offs
Left-builders now hold right (the prompt says so). Moving a plug with the Cable card in hand: let the card go first. Removal takes any machine on every tab but Power's cable cut. O on Air/Power changes nothing on screen till the menu closes; its chip is lit only while air shows. Dev: middle-drag pans and removes. Outline and ghost may overlap.

## Open questions
1. Right held works plans outside the menu too? (chosen: yes) 2. P/L on its own tab closes the menu, or nothing? (chosen: closes)
