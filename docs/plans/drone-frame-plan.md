# Plan: the drone in place of the build tablet

Design: [drone-frame.md](drone-frame.md); it wins where the two differ. There are four steps, each a try the user
plays before the next. Tests come first in `game.tests`, with the filter named per step. Frames come from
`tools/snap.ps1`. Reference look: the pick page's source, `drone-soot-frame-src/template.html` in the session
scratchpad.

## Notes against the code

- **The Esc menu is a `Tablet`.** `GameMenu` owns one, and `Main` calls `edge.Follow(menu.Tablet, keysAway: true)`.
  - Today's class is copied to `game/menu/MenuTablet.cs` for the menu alone, which calls `Tablet`'s static helpers.
  - `TabletPeek.Pose` and the art (`tablet_frame`, `tablet_plate`, `tablet_stylus`) stay for it.
  - `TabletPeek.RimArt` and `RowAt` stay as the foot row's geometry, which `EdgeRow.RimLeft`, `Ui.EdgeKeysLeft` and
    `VitalsTests` read.
- **The pod's window already lists the drone** (`Stations.cs` lifepod row, `Crafts.cs`).
- **The board opens only from its key.** `Ui.TreeOpen` is set by `ResearchTablet` alone; its comment ("crew opens it
  on E") is stale and gets fixed. The ship's computer (`ComputerPanel`) is a station window: it stays as it is.
- **Two crackle clocks.** The window's `SootRun` runs in `DroneFrame`, the drone's in `LightDrone`. Both read the same
  open flag: the drone's crackles only while no window is open, the window's only while it is fully open.

## Tune (`SootRun.Tune`, from the pick page)

**Legend.**

| Symbol | Means |
| --- | --- |
| `p` | a run's progress, 0..1, over `OpenS` or `CloseS` |
| `t` | seconds since the run began |
| `u` | seconds since a span linked |
| `φ` | a mote's or span's own phase, 0..1, from its seed |
| `fly` | the distance a mote flies, px |
| `d`, `dmax` | a mote's distance along the outline from the near corner, and the largest |
| `a` | the film's alpha |
| `glass` | the glass's alpha |
| `break` | when the links began to break, s |
| `c01` | clamp to 0..1 |

Screen px and seconds unless marked. `Share` is the design's "length ÷ 80".

**Timing and gaps**

- `OpenS = 0.3`, `CloseS = 0.2`.
- `Share = 80`, `GapMin = 14`, `GapMax = 32`.
- `Stagger = 6` px per rank.
- `Head`: reach = (fly + half-perimeter) · (1 − (1 − p/0.88)²).

**Specks**

- `Mote = 2.3`, with a +1 px rim `#16171b`; tones `#4c4f55` / `#606369`.
- A hot speck flickers at 0.45 + 0.35·sin(4t + 7φ).
- `Lift = min(60, 0.22·fly)` on take-off; `HomeLift = 24`.
- Closing: k = smooth(p / (0.9 − 0.3·d/dmax)).
- `Wobble = 0.35` px, at 0.9 and 0.7 rad/s.

**Spans**

- `LinkIn = 0.05` s.
- Landing flash: exp(−u/0.16), until u = 0.8 s.
- `Break = 0.08` s, with flash 0.7.
- Breathing: b = 0.5 + 0.5·sin(t·(0.4 + 0.5φ) + 40φ); alpha = 0.14 + 0.24b² + 0.6·flash.
- `SpanWidth = 0.9`, `SpanHalo = 3.5`, halo alpha 0.13, core pale 0.45.

**Crackle**

- `CrackleEvery = (4, 8)` s; the first comes 2.4 s after the place changes.
- `CrackleLife = 0.13` s.
- `ArcAmp = 2.4`, `ArcBow = 2`, 5 segments, reseeded 40 times a second.
- Flicker 0.75 + 0.25·sin(seed + 90t).
- `ArcCore = 1.2`, `ArcHalo = 5`; end halos 7 + 2 px at alpha 0.45.

**Glass, contents and film**

- `Glass`: opening c01((p − 0.3)/0.55), closing c01((p − 0.3)/0.6).
- `Contents`: opening c01((p − 0.5)/0.4).
- Clicks: opening at p > 0, closing at p > 0.6.
- `Film`: a = 0.02 + 0.06·(1 − glass) opening, a = 0.02·c01(1 − (t − break)/0.1) closing; gradient pale(0.3) at
  1.6a, 0.5a, then a.

**Colours and ring**

- `Ember = (255, 138, 58)`. The glass light is `HoloLook.Drone.Light` (8cc4ff).
- `Ring = 3 × 6` motes.
- **Drone side, in world px.** The page drew the drone at 2 page px per screen px and a 0.556 zoom, so world = page ×
  0.5 × 0.556. That gives: span 0.25, halo 0.97, arc amp 0.67, bow 0.56, core 0.33, arc halo 1.39, end halos 3.06; an
  arc starts 1.15 from the core. The lead checks them on the step 1 frames.

## Step 1: the bag on the soot frame, the bare drone, the arcs

**Tests first** (`tools/test.ps1 "Soot|DroneSwarm" game.tests`)

- **`SootFrameTests`**
  - `ASpotSitsInEveryCorner`
  - `GapsAreEvenAndWithin14To32`, on the bag 380×200, the build menu 1120×470, the board 1800×1000 and a small 40×30.
  - `OrderStartsAtTheCornerNearestTheDrone`: rank 0 both ways at the nearest corner, rising along the outline.
  - `ABigBoardTakesMoreThan170Spots`
- **`SootRunTests`**
  - `OpeningLandsEveryMoteAndLinksEverySpanWithin0_3s`
  - `ASpanLightsOnlyAfterBothEndsLand`
  - `ClosingIsHomeWithin0_2s`
  - `ASwitchMidRunNeverJumps`: no mote moves more in one 1/60 s frame than 1.5× the fastest frame of an uninterrupted
    open. Cases: the outline changes mid-open, a close mid-open, and both on the same frame.
  - `BreathingStaysWithin0_14And0_38`
  - `CracklesCome4To8sApartLast0_13sNeverMidRun`: 120 s with a seeded `Random`.
  - `TheDronesCrackleWaitsWhileAWindowIsOpen`: one window run and one drone run, on one open flag.
- **`SootClaimsTests`**: `NewestOpenWindowOwnsTheSoot`, `TheOneItTookFromIsTold`, `ClosingReleases`.
- **`DroneSwarmTests`** (added)
  - `BareKeepsExactlyTheEighteenRingMotes`
  - `RingIsSixOnEachOrbit`
  - `BareKeepsTheModesPlates` (Lamp and Scan)

**Add**

- `game/shared/logic/SootFrame.cs`: the pick's `frameOf`.
  - `Spots(IReadOnlyList<(float X, float Y)> outline, int nearCorner)` returns spots, ranks, normals and shares.
  - `NearCorner(outline, drone)` is called once per open, so the order never shifts mid-flight.
  - A speck's seed is that of mote `i % 170`.
- `game/shared/logic/SootRun.cs`: the pick's `placeFrame`, the ember draw and `stepFx`.
  - `Tune`.
  - `SootFade`: p with its `Glass`, `Contents` and `Clicks` curves, stepped by each window.
  - `SootRun(bool window, Random)` with `Step(dt, bool open, spots?, from?)`. A null `from` (no drone drawn) fades the
    soot in on the outline.
  - It exposes positions, `Strength[]`, `Flash[]`, `Breath(j, t)`, `Film` and `Crackle` (span, age, seed).
- `game/shared/logic/SootClaims.cs`: `Hand(object owner, outline, bool open)`, `Owner`, `Outline`, `Open`, and the
  event `Taken(owner)`. The newest open window wins; the one it took from closes on `Taken`.
- `game/shared/Triangles.cs`: `LightDrone.Triangles` moved here, plus `Quad(a, b, width, colour)` and
  `Polygon(points, colours)`.
- `game/shared/DroneFrame.cs`: a `CanvasLayer` reading `ui.Soot`.
  - The drone's screen point is `Ui.DroneLens` (world) through the canvas transform.
  - It steps its `SootRun` and draws specks, spans, the crackle and the film in one `Triangles`.
  - It sets `Ui.DroneWindow`.
- `game/dev/demos/DroneWindowScenario.cs` (the `new-demo` skill): `[Dev("--drone-window", …, Shape = DevShape.Equals)]`
  takes a comma list (`bag`, `build`, `handbook`, `board`, `market`).
  - Each window opens at 1 s for 2 s, the next opening while the last is open (a switch); the last one closes.
  - It opens them through the binds' own `Toggle`/`Open`.

**Change**

- `game/shared/logic/Ui.cs`: `BagProjected` goes. `(bool Open, double Out) DroneWindow` comes in, `Out` 0..1, the run's
  p; so does `SootClaims Soot`.
- `game/render/logic/Layers.cs`: `ScreenLayers.DroneFrame = 11`, under `TabletEdge`'s 12, so the keys draw over the soot.
- `game/shared/HoloWindow.cs`: `Glass(float alpha)`, a `StyleBoxFlat` ground of `HoloLook.Drone.Ground` at
  `GroundAlpha`. Machine windows keep 0.
- `game/features/items/InventoryPanel.cs`: `_bagHolo`, `BagHolo`, the paired `MachineHolo` in `Build()`, and
  `_bagHolo?.Snap()` in `Frame` go. `SootFade _bagFade` comes in.
- `InventoryPanel.Machine.cs`: `BagHologram` becomes `BagSoot(delta)`.
  - It calls `_ui.Soot.Hand(this, corners of _inventory, Shown)` and `window.Glass(...)`.
  - It shows the contents whole at the `Contents` alpha.
  - It calls `Close()` on `Taken`.
- `game/features/crew/logic/DroneSwarm.cs`: `Ring` (18 motes), `Bare` and `Kept(i)`.
  - `Ring` takes six motes on each orbit (`i % 3 == o`), sorted by phase and evenly spaced (`ids[k·n/6]`).
  - While bare, the soot's shape is `Idle`.
- `game/features/crew/LightDrone.cs`:
  - `Projects` goes; `Sign` uses `ShapeOf(_sign.Visible, Scanning)`.
  - `DrawThreads` becomes `DrawSpans`: 4 ember arcs from core to plate at all times, the 18 ring spans while bare, and
    the crackle from its own `SootRun(window: false)`.
  - `Window(bool open, double s)` comes in; `DrawSoot` skips the motes not `Kept`.
- `game/features/crew/Figure.cs`: `DroneProjects` becomes `DroneWindow`.
- `game/features/crew/HeroView.cs`, where it sets `_figure.DroneProjects`: the turn toward the bag goes. It sets
  `_figure.DroneWindow = _ui.DroneWindow.Open`; `LampAim` is the pointer alone.
- `game/Main.cs`, `_Ready`: `AddChild(new DroneFrame(_hands.Ui))` before the edge.

**Frames for the lead**

- `tools/snap.ps1 s1-bag.png -Movie 5 -Every 6 -- --testbed --paired --drone-window=bag --zoom 4`
- `tools/snap.ps1 s1-pod.png -Movie 4 -- --pod --paired`
- `tools/snap.ps1 s1-crate.png -At 3 -- --testbed --paired --open=crate`: the bag beside «Перо».
- `tools/snap.ps1 s1-crackle.png -Movie 12 -Every 4 -- --testbed --paired`: the drone crackles with no window open.

**Done:** the tests pass, and the sheet shows:

- the soot running 0.3 s to the near corner and round the outline;
- orange spans breathing, blue glass, and a 0.2 s flight home;
- the bare drone with 18 linked motes;
- slots that click at once.

## Step 2: the four pages on the reshaped Tablet

**Tests first** (`tools/test.ps1 "SootOutline|TabletPeek|EdgeRow|TabletBezel" game.tests`)

- **`SootOutlineTests`**: `WingsJoinTheGlassInOneOutline`, `EveryCornerOfTheUnionIsAnOutlineCorner`,
  `ASingleRectIsItsFourCorners`.
- **`TabletBezelTests`**: `TheMarginPassesTheWheelAndZoomToThePage`. The logic of which control a wheel event reaches is
  pulled out as `logic/BezelInput.cs`.
- `TabletPeekTests` stays, since the menu still uses `Pose`. `EdgeRowTests` change only where the rim's art leaves.

**Add**

- `game/shared/logic/SootOutline.cs`: `Of(IReadOnlyList<Rect>)`, the union of touching axis-aligned rects traced as one
  rectilinear polygon (coordinates compressed to a grid).
- `game/menu/MenuTablet.cs`: today's `Tablet` without its static helpers, which it calls on `Tablet`.

**Change**

- `game/shared/Tablet.cs` becomes `Tablet(Ui ui)`.
  - These go: `_frame`, `_plate`, `_stylus`, `_ledOff`, `_ledGlow`, `_wash`, `_glass`/`DrawGlass`, `TabletMotion
    _motion` and `LayAsRim`.
  - The glass is a `StyleBoxFlat` of `HoloLook.Drone.Ground`. `Step` steps a `SootFade` and hands `_ui.Soot` its outline
    (`SootOutline.Of(glass + Wings)`).
  - `Up` returns the fade's p; `Shown` is p > 0.
  - Mouse input follows `Clicks`. `Lay` adds `_ui.HoloSway` to `Position`.
  - `Drop` returns 0.
  - `Bezel` becomes a transparent 6 px margin that takes clicks and passes the wheel (`BezelInput`). `Covers` is the
    glass plus that margin.
  - `Closed` is a new event, raised on `Taken` and when `Ui.DroneLens` goes null (passed out, dead); its owner closes
    on it.
  - `Text`, `Tab`, `PaintTab`, `ScrollBar` and `PlaceThumb` stay.
- **Callers that name `Tablet`**, each checked to build and look the same: `SettingsView`, `SearchField`, `StripParts`,
  `BoardFind`, `BoardPage`, `ITabletPage`, `BoardDemo`, `TreeDemo`.
- `game/menu/GameMenu.cs`: uses `MenuTablet`.
- `game/shared/TabletEdge.cs`:
  - `_rim` and `_metal` go. `Art` and the foot row come from `TabletPeek.RimArt`'s geometry, with nothing drawn.
  - The keys stay at the foot always; `Follow(Tablet)` keeps only the readouts' pad.
  - `Follow(MenuTablet, keysAway)` becomes `HideUnder(Func<bool> shown)`. A drone window under the menu stays open.
  - `ReadoutBeforeStylus` becomes a pad from the screen's right edge.
- `game/features/building/BuildMenu.cs`: `new Tablet(ui)`; `Wings = [_focus, _modes, _hints]` rects; the wings butt
  the glass instead of sliding under it.
- `game/features/building/PlanWing.cs` and `HintWing.cs`: `Metal`, `Light`, `Side` and the scuff go; the glass ground
  comes in.
- `game/features/handbook/HandbookPanel.cs` and `game/features/research/ResearchTablet.cs`: `new Tablet(ui)`; the
  board's size drops `ArtAround`.
- `game/shared/logic/Ui.cs`: `TreeOpen`'s comment says the board's key opens it.
- `game/shared/HoloWindow.cs`: `Holds` also counts a `Tablet`, so pointing at one calms the sway.
- `game/Main.cs`, where it wires the edge: the new `Follow` / `HideUnder` calls.

**Frames for the lead**

- `tools/snap.ps1 s2-build.png -Movie 5 -- --testbed --paired --drone-window=build`
- `tools/snap.ps1 s2-hints.png -At 4 -- --testbed --paired --hints`: the wings in one outline.
- `--drone-window=handbook`, `--drone-window=board`, `--drone-window=market`, and `--drone-window=bag,board` (the
  switch).
- `--board=first --paired`, `--menu-demo` (the old look kept), `--flash` (the find flash on the foot line).

**Done:** every page opens on the soot with blue glass and no metal. The keys stay at the foot. Esc looks as it does
today and leaves the window under it.

## Step 3: no bag and no drone windows before pairing; creative pairs

**Tests first** (`tools/test.ps1 "DroneKeys|Hotbar|InventoryLayout" game.tests`)

- **`DroneKeysTests`**
  - `DroneWindowsAnswerOnlyWhenPaired`: B, T, M, J and an edge `Cap`.
  - `TabUnpairedShowsTheEquipmentAlone`.
  - `CreativeSendsPairDroneOnceWhenUnpaired`: on, off and on again, with `PairDrone` in flight, send it once.
- **`InventoryLayoutTests`** (added): `UnpairedAMachineDocksUnderTheHotbar`.

**Add**

- `game/shared/logic/DroneKeys.cs`: `Answers(DroneWindow which, bool paired)`, where `DroneWindow` is the enum
  `Bag, Build, Handbook, Board, Market`. `Creative(bool on, bool hasDrone, bool pairInFlight)` says whether to send
  `PairDrone`.

**Change**

- **Every opener of a drone window** checks `DroneKeys.Answers` once, at its `Open`/`Toggle`, so binds, edge `Cap`s,
  tips and find links all go through it. The openers are `BuildMenu`, `HandbookPanel`, `ResearchTablet` (board and
  market) and `InventoryPanel` (the bag).
  - Each gets `Func<bool> Paired`, set in `Main` as `_items.Panel.Paired` is.
  - Dev flags that open a window (`--drone-window`, `--board`, `--hints`) pair first.
- `game/features/items/InventoryPanel.cs`, unpaired:
  - the bag's grid is not built and `Hotbar.Shut` goes;
  - Tab shows the equipment unit alone;
  - `Ui.InventoryFoot` is the hotbar's foot, so a crate or machine opens there.
  - Pairing rebuilds the panel, as today.
- `game/Main.cs`, where it lists the edge keys: build, research, market and handbook show only while paired.
- `game/features/dev-panel/DevPanel.cs` `ToggleCreative`: turning creative on sends `PairDrone` when `DroneKeys.Creative`
  says so (no drone, none in `client.InFlight`). It goes after `SetCreative`, so it is not refused for want of scrap.

**Frames for the lead**

- `tools/snap.ps1 s3-unpaired.png -At 4 -- --testbed --open=crate`: no bag; the crate under the hotbar; the equipment
  unit; only the air and suit keys on the edge.
- `tools/snap.ps1 s3-paired.png -At 4 -- --testbed --paired`

**Done:** before pairing there is no bag, the drone-window openers do nothing and their keys never show. Creative pairs.

## Step 4: friends see the soot rise and settle

**Tests first** (`tools/test.ps1 "Remote|DroneSwarm|NetTraffic" game.tests`)

- **`RemoteTests`**
  - `TheDroneWindowFlagCrossesTheWire`
  - `ALateJoinersFirstSampleWithTheFlagDrawsTheDroneBare`
  - `AChangedDroneWindowIsSent`, proven to bite by leaving the flag out of `Seen` once.
- **`DroneSwarmTests`**: `LeavingSootRisesThinsAndSettlesBack`.
  - At e = 0 a mote sits on its orbit; at e = 1 its alpha is 0 and its size ×0.6.
  - A ring mote never rises: the case it is written against.
  - The rise is 14.4 + 16.7·r1 world px, with a drift of ±11.1 by tone.
  - e is c01(p/0.9) opening and smooth(p) closing.

**Change**

- `game/features/crew/logic/Remote.cs`: `Seen` gains `bool DroneWindow = false` after `Drone`, and `Wire` gains it.
  `Encode` and `Decode` pass it.
- `game/features/crew/logic/DroneSwarm.cs`: `Rise(i, e, aim, t)`.
- `HeroView.cs`, where it fills its `Seen`: `DroneWindow = _ui.DroneWindow.Open`.
- `HeroView.Others.cs`, where it shows a remote figure: `Figure.DroneWindow = HasDrone && seen.DroneWindow`.
- `Figure.cs` / `LightDrone.cs`: a remote drone draws `Rise` for the motes not kept. A local one skips them, since its
  own soot is on the window.

**Frames for the lead**

- Host: `godot --path game -- --host --testbed --paired --aside --drone-window=bag`
- Then: `tools/snap.ps1 s4-friend.png -Movie 6 -- --join`

**Done:** the joiner sees the host's soot rise, dissolve and settle back, and the host's drone stays bare while the bag
is open.

## Risks

- **Tablet's callers.**
  - The margin must pass the wheel to `BuildMenu.Scroll` and the board's zoom; a test covers it.
  - `Drop` going to 0 changes `BuildMenu.Dock` timing.
  - `ComputerPanel` still sizes by `TabletLayout`.
- **The Esc menu keeps its look.** It needs `MenuTablet`, `TabletPeek.Pose` and the tablet art; the READMEs must say
  they stay for it.
- **`FindFlashView`** runs along `edge.Art`, which becomes an undrawn foot line. Check `--flash`.
- **Unpaired, there is no bag.** Machine windows dock under `Ui.InventoryFoot`, which then falls back to the hotbar.
  `MachineScenarios` frames show any window that overlaps.
- **The wire.** Both ends must run one build; a hero packet of another length fails to decode and that hero is hidden.
  `NetTrafficTests` may count bytes, and every hero packet grows by 1.
- **Size and cost.** The board's spot count passes 170, so seeds are taken `i % 170`. It costs about 1.5k triangles,
  only while soot is out; measure with `--frames --paired --drone-window=board`.
