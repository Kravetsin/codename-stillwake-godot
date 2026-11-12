# energy (client): the energy tab on the wrist

The tab at the gauge row's right end, beside the suit's ([0053](../../../docs/decisions/0053-charge-belongs-to-the-item.md),
[0081](../../../docs/decisions/0081-the-wrist-is-one-column-of-gauges-and-keys.md), [energy-ring](../../../docs/decisions/2026-11-06-energy-ring.md)): what the
belt's stores hold and whether a tool is drawing on them. It reads the sim through `IItemsQueries` (`Inventory`,
`BeltCharge`, `SlotOpen`, `Running`) and sends nothing. What a slot's bar and a card's meter show of charge is
`game/shared/logic/Charges.cs`; the wrist's metal and screen are `game/shared/`'s (`Wrist*`).

## Files

| File | Holds |
| --- | --- |
| `EnergyBar.cs` | the tab: its screen, the ring and the number drawn when they change, the bolt every frame; its light added over them |
| `logic/EnergyRead.cs` | what it reads: the belt summed, each open belt slot's store, the running tool's draw |
| `logic/EnergyCount.cs` | the number's count between reads |
| `logic/EnergyRing.cs` | the ring: an arc a slot, the store drained, a store put on, the hop to the next |
| `logic/BoltPulse.cs` | the bolt: its beat and current, its sputter and kindling, its discharges |

## Rules

- **Read four times a second**, never every frame; between reads it moves on its own.
- **The number** is the belt's charge in whole power units, grouped by thin spaces, never seconds: a fall glides there in
  one read's span, so a steady drain counts down evenly; a rise eases out over 0.6 s. It never flares.
- **The ring** round the bolt has an arc for each open belt slot, clockwise from the top, the circle shared alike with a
  3 px gap; an arc is its store's charge of its own full, a track with none. The store a tool drains is the first holding
  a tick of its draw, as the sim pays; its arc is lit, a glint running back along it while drawing, the rest dim. A store
  put on fills its arc in; when draining passes to the next store a spark hops along the ring there.
- **The bolt** beats while a tool draws, ten beats a second a power unit of its draw, a current running down it; a beat
  jolts it up to 0.7 px off its place (the grinder's 1.2 px), settling as the beat fades, so it rattles; a beat
  throws a thin discharge onto the frame one time in four, the grinder's (a draw over 1) nearly one in two, wider and forked.
  Low, a beat now and then fails and discharges halve. Going flat it sputters out to the dark bolt; a store put on
  kindles it from its tip.
- **Colour:** charge's green, amber and pulsing under a quarter of what the worn stores hold; everything at
  `WristLook.NoBattery` with none worn. Discharges and the current burn warm white over ember.
- **A flat press** of an electric tool or the railgun, the belt unable to pay a tick of its draw or wearing none, blinks the
  bolt red three times, hard on and nearly off, throwing red light round it past the tab's edge, the first blink jolting it a pixel:
  `crew` raises `Ui.PressFlat`, once a press ([no-charge-flash](../../../docs/decisions/2026-11-07-no-charge-flash.md)).
- **Width:** `WristLook.EnergyWidth`, enough for "16 200", three accumulators, beside the ring.

## Deliberately not handled

- A tool's beat follows its content draw: a drill fitted with the arc draws twice and beats as a plain one. The tab knows
  what runs, not which stack the sim draws through.
- What drains the belt without a tool, the regen suit and the railgun's shots, moves the number and the ring but does not
  beat the bolt.
