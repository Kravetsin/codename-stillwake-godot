# Pointer tips that tell the truth

Items 2 and 5 of the 2026-11-04 UI batch: the air overlay's tip in the bubble model
([air-bubbles](../decisions/2026-11-05-air-bubbles.md)), icons for O₂, leak and power, and a machine's draw before it runs.

## Rules

- **Where the room tip shows.** With O on, only over a cell of a room holding no structure (`StructureAt == Empty`) and no
  machine: a back wall or an empty cell. Over a door, ladder, platform or machine the room says nothing.
- **What it says.** Bold: the state as today (`air.aired`, `.filling`, `.holding`, `.vacuum`, `.venting`), aired cells of the
  volume. Then, a minute each (units per `Air.LeakEvery` ÷ 10, rounded down; the wall card's unit):
  - `[O₂] +1200/min`: the room's **budget**, its share of its live vents' making (new `RoomAir.Budget`), only while above 0;
  - `[leak] −192/min`: what its aired cells leak, red when the room reads `atmosphere.leaky`; only with a budget;
  - `[canister] 9000 · 47 min`: the liquid behind its vaporizers and the sim's `Lasts`; none for the pod (no liquid), red at 0;
  - the reason it falls short (`atmosphere.leaky`, `.noVent`, `.notPiped`, `.dry`, `.unpowered`), red.
  - Venting with piped vents: `[canister] 9000 · 3 min` (the drain), else nothing more.
- **Icons.** A tip line carries a sign (O₂, leak, power) drawn at the item icon's 14 px, or an item's icon as now. Signs on:
  the room tip, `AirSays` (vaporizer `[O₂] +1200/min`, tank `[canister] 9000/36000`), the wall card's leak, the power lines.
- **Power draw.** Out of every overlay, the pointer on any powered machine adds `[power] 4` under what its feature says
  (the power module's `Draw`, modules counted), working or not; a maker shows `[power] +6`. The build card adds the spec's
  draw. A station's panel shows its draw dim at rest instead of 0, lit while it works.

## Steps

1. **Sim:** `RoomAir.Budget`, thousandths of a unit per `Air.LeakEvery`, from `_budget` in `AllRoomsAir`; scenarios: two rooms
   on one vaporizer get half each, the pod's room all, an unpowered vaporizer none, a venting room none. README API line.
2. **Tip types:** `TipLine.Sign` (`None`, `Oxygen`, `Leak`, `Power`); `Ui.WorldTip` holds a `Tip`; `PointerTip` draws a sign by
   `Signs`; `MachineTips` speakers return `TipLine`s and it appends the power line.
3. **Room tip:** `Explain.Of` returns a `Tip`; `AirView.Tell` filters the cell (`RoomTipAt`, logic, tested). Keys: drop
   `air.lastsNot`, `.keepsUp`, `.fallsShort`, `.pipes`, `.drains`; add `air.made`, `air.leak`, `air.liquid`; Russian beside.
4. **Machines and cards:** `AirSays` a minute with signs; power line (`PowerSays`, logic, tested); build card draw;
   `MachineFrame` dim draw; `WallStats` leak line with its sign.
5. **Signs' looks:** the user's picks from the page, drawn by `tools/anime-art`'s `signs` subject (`sign_oxygen`, `sign_leak`,
   and the bolt), exported to `game/art/hd/`. ADR `2026-11-05-tip-signs` records the picks and the minute unit.
6. Frames by `tools/snap.ps1`: a room filling, short (`.leaky`), in vacuum; a resting smelter's tip and panel; a build card.

## Deliberately not handled

- **Breathers leave the forecast:** the span is the sim's `Lasts`; heroes in the room shorten it unseen. The client
  would repeat a sim rule, and the number would jump as heroes walk in and out.
- **Tanks shared by several rooms:** each room's span counts only its own draw, as the sim's `Lasts` does.
- **Rounding:** budget and leak each round down, so a leak may show 1 above a budget it fits.
- **Machine tips stay off under an overlay,** as today; pipes and cables do not hide the room tip.
- **A vaporizer's making on its build card** and satisfaction in any tip: not asked.
