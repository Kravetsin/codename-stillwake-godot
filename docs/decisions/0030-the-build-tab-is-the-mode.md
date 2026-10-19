# 0030 — The build tab is the mode

- Status: accepted; air under the Air and Power tabs superseded by [0072](0072-air-shows-under-every-build-tab.md): it shows under every tab
- Date: 2026-10-15
- Supersedes in part: [0022](0022-one-hand.md) (overlays as filters, the mouse), [0021](0021-cables-by-hand-in-the-power-mode.md) (Shift to move)

## Context

The build menu (B) and the P and L overlays were separate switches. An overlay outlived the menu:
pick the cable card, L came on; close the menu and L stayed, its buttons dead. Moving a cable took B
and L at once, and the team kept trying it in the wrong one. While the menu was open the "[E] …"
prompt hid under it and nothing under the pointer was outlined. Oxygen Not Included shows the way:
picking a wire brings up the power overlay, and nobody switches overlays by hand.

## Decision

- **The open tab is the mode.** Air shows the pipes overlay, Power the cables; the other tabs show
  none. The overlay is never kept: `Ui.OverlayOn` is worked out on each read from whether the plan is
  out, the tab, and `Ui.AirOn`, so with the menu closed it is air or nothing. No mode outlives the menu.
- **P and L go.** O stays, outside building too, for air keeps the crew alive; while the Air or Power
  tab is open its overlay shows instead, and air comes back when the menu closes.
- **The menu opens on the tab it was closed on;** a card picked, by click or Q, opens its own tab; another
  tab lets the card go. A tab the world knows nothing on is never the mode.
- **The mouse:**
  - left: the card — plans along a drag; on the Power tab with no card it carries a peg or a plug,
    with the Cable card it lays (no Shift);
  - right, held: the hero works the plans under the pointer, whatever the hand holds, menu open or not —
    where building tools as equipment will go;
  - middle: removes in the tab's layers, a click or a drag (Air: pipes, valves and machines); on the
    Power tab a press near a cable cuts it, elsewhere it removes as on the others;
  - Q with the menu open: the pipette, onto the card's tab;
  - left with a free hand still grabs debris; E works as before.
- **The prompt for E sits above the open menu;** the aim is outlined whatever the hand holds.

## Rejected

- **P and L as shortcuts to their tabs:** the team saw no use for them beside the tabs.
- **Removal on the right button:** the right button goes to the hero's work; removing with the wheel
  held is the price, eased by it being rarer once building is its own button.

## Consequences

- Who built with the left button now holds the right; the prompts say so.
- Moving a plug with the Cable card in hand means letting the card go first.
- O pressed on the Air or Power tab shows nothing until the menu closes; its chip is lit only while air shows.
- In development, a middle drag also pans the camera.
