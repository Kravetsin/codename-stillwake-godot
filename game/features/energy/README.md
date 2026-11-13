# energy (client): the charge row of the vitals

The charge row in the vitals block at the screen's bottom-left, under the suit's air
([0053](../../../docs/decisions/0053-charge-belongs-to-the-item.md), [energy-ring](../../../docs/decisions/2026-11-06-energy-ring.md),
[hud-vitals](../../../docs/decisions/2026-11-13-hud-vitals.md)). It shows what the belt's stores hold and whether a tool is
drawing on them. It reads the sim through `IItemsQueries` (`Inventory`, `BeltCharge`, `SlotOpen`, `Running`) and sends
nothing. What a slot's bar and a card's meter show of charge is in `game/shared/logic/Charges.cs`. The block's layout and its
pieces of light are `game/shared/`'s (`logic/Vitals.cs`, `LightStrip.cs`).

## Files

| File | Holds |
| --- | --- |
| `EnergyBar.cs` | the row: the bolt, a piece a store, their light added over them, a tip a store |
| `logic/EnergyRead.cs` | what it reads: the belt summed, each open belt slot's store and its item, the running tool's draw |
| `logic/EnergyRing.cs` | what each store shows: its share gliding, the store drained, a store put on, the hop to the next |
| `logic/BoltPulse.cs` | the bolt: its beat and current, its sputter and kindling, its discharges |

## Rules

- **Read four times a second**, never every frame; between reads it moves on its own.
- **A piece per store** on the belt, as long as it holds full: a battery `Vitals.BatteryWidth`, an accumulator three. Its
  light is the store's charge of its own full, gliding to each read. There is no piece for an open slot with no store,
  and nothing with none worn.
- **Reverse slot order:** the sim drains the first store in slot order holding a tick of the draw, so that store stands at
  the right end and the row empties from the right. The order is reversed in the layout only.
- **The store drained** is lit brighter, a glint running back along it while drawing; the rest are dimmer. A store put on
  fills in; when draining passes to the next store, a spark hops there.
- **Fit:** past `Vitals.MostWidth` the whole row shrinks alike; the gaps stay.
- **No number.** Hover over a piece gives its store and what it holds, in power units grouped by thin spaces:
  «Аккумулятор: 3 200 / 5 400».
- **The bolt** is the game's sign at 1.5×, centred in the sign column on the row. It beats while a tool draws, ten beats a
  second for each power unit of its draw, with a current running down it. A beat jolts it up to 0.7 px (the grinder's
  1.2 px), settling as the beat fades. A beat throws a thin discharge one time in four, and the grinder's (a draw over 1)
  nearly one in two, wider and forked. When low, a beat now and then fails and discharges halve. Going flat, it sputters
  out to the dark bolt; a store put on kindles it from its tip.
- **Colour:** charge's green, amber and pulsing under a quarter of what the worn stores hold. With none worn the bolt is at
  `WristLook.NoBattery`.
- **A flat press** of an electric tool or the railgun, when the belt cannot pay a tick of its draw or wears no store,
  blinks the bolt and the pieces red three times, throwing red light, the first blink jolting it. `crew` raises
  `Ui.PressFlat` once a press ([no-charge-flash](../../../docs/decisions/2026-11-07-no-charge-flash.md)).

## Deliberately not handled

- A tool's beat follows its content draw: a drill fitted with the arc draws twice and beats as a plain one. The row knows
  what runs, not which stack the sim draws through.
- What drains the belt without a tool, the regen suit and the railgun's shots, moves the pieces but does not beat the bolt.
