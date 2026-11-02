# water (client): cisterns' glass, electrolysers' bubbles and the pointer's word on them

## Files

| File | Purpose |
| --- | --- |
| `logic/Water.En.cs` | Its player text (`En`): the refusals, the pointer's tip. |
| `logic/WaterShows.cs` | How water shows without a word and what the pointer says of it: the glass's frame, the bubbles' frame, the tip. |
| `WaterLooks.cs` | Each cistern's water up its sight glass, each electrolyser's bubbles while it works and its lamp red while it stands. |
| `WaterTip.cs` | Out of every overlay, the pointer on a cistern tells its bottles, on an electrolyser what it does or why it stands. |

Water as the player sees it ([water-to-oxygen](../../../docs/decisions/2026-11-02-water-to-oxygen.md)). Reads `sim/Water`
(`WaterIn`, `Stalled`) and the grid's machines by trait (`HoldsWater`, `Electrolyses`); sends nothing itself: the hands send
`PourWater` (crew's `Aim`).

- **Without a word:** a cistern's water shows up its sight glass by quarters (`cistern_fill_0-4`), any water at least one; an
  electrolyser at work shows bubbles rising, four frames of 150 ms, and a green lamp (`electrolyser_work_0-3`); one that
  stands, a red lamp (`electrolyser_stalled`). Drawn unlit over the machines' pictures, read four times a second.
- **By the pointer:** out of every overlay and with no card in hand, a cistern says `Water · 3.5/8 bottles`; an electrolyser
  `Splitting water into liquid oxygen`, or why it stands (`water.noWater`, `water.tanksFull`, `water.noPower`). Off them, only
  its own tip is taken away, so the capacitor bank's stays.
- Every picture has an ink version (`tools/anime-art`, `machines.js` and `items.js`) over the pixel one; both keep one layout.
