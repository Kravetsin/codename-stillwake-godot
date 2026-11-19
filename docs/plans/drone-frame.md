# Design: the drone in place of the build tablet

## Problem

The build menu, Handbook, research board and market sit on a rugged tablet that the hero carries from the first second.
The drone already projects the bag, but none of these. Every personal window the hero calls up should come from the
drone. The look is the pick «Угли» (2026-11-19).

## Player experience

1. A new hero wakes with the hotbar, the suit's equipment unit and the pod's window. There is no bag and no drone
   window: Tab shows the equipment unit alone, and the edge shows no keys for drone windows.
2. Once paired, B, J, T, M or Tab sends soot off the drone. Over 0.3 s it runs to the window's near corner and round its
   outline. A span between two specks glows orange once both have landed. Slots take clicks at once.
3. While a window is open:
   - its glass is hologram blue;
   - the orange spans breathe slowly, each at its own pace;
   - a crackle runs along one span every 4–8 s;
   - a faint, still film fills the window;
   - the window trails the hero («Отставание»).
4. The drone stays by the head: the core and plates keep the mode's shape and the lamp keeps shining. A thin cloud of
   soot stays on three orbit rings.
5. The plates always ride orange arcs from the core, window or not.
6. On close, the spans break and the soot flies home in 0.2 s.
7. A friend sees the soot rise off your drone and dissolve, then settle back.
8. The edge's keys stay at the foot of the screen, whatever is open.

**Gone wrong:**

- Before pairing, B does nothing and the edge never shows its key.
- Passing out or dying closes the open drone window.
- Esc opens the game menu over a drone window, which stays open under it.

**Terms.** A drone window is a personal window: the bag, the build menu, the Handbook, the board or the market. A
station's or machine's window (a crate, a furnace, the ship's computer) is not one; it keeps «Перо» from its machine. The
drone is **bare** while a drone window is open.

## Out of scope

- Build mode (placement in the dark), any page's contents, sound, the drone's habits and alarms, the magnet.
- The game menu (Esc) keeps today's tablet until it has its own pick; it must work before pairing.
- More soot (the drone's grown level).
- A drone lost after pairing: it never is (crafted once, never removed), so windows never close for want of one.

## Scale

- One drone window at a time. Its specks follow its outline: about 80 on the bag, more on the board, never counted
  against the drone's 170. While bare, 18 specks stay on the drone's rings.
- Others receive one flag per hero, at about 20 Hz.

## Approach

**Who uses it.** Now: the bag, the build menu (with its wings and hint screen), the Handbook, the board and the market.
Next: the plate shield (drone-swarm), which rides the same arcs.

**Logic** (`game/shared/logic`, Godot-free, numbers in a `Tune` block):

- `SootFrame` (the pick's `frameOf`) takes a closed rectilinear outline and returns spots:
  - the gap is the outline's length ÷ 80, clamped to 14–32 px, however many specks that takes;
  - one spot sits in each corner;
  - the spots are ordered both ways from the corner nearest the drone.
- `SootRun` (`placeFrame`, `ember`, `stepFx`):
  - its motes fly from where they are to their spots;
  - a span links once both its ends land, and breaks on close;
  - spans breathe, and the crackle is timed here.

`MachineHolo` and `HoloPen` cannot be reused: they trace a fixed frame with two pens from a beam, and nothing in them
places moving points on an outline.

**Client.**

- **`DroneFrame`**, one per client, draws a window's specks, spans and film above the windows. It uses triangle arrays
  in `LightDrone`'s batch, moved to shared; the film is one polygon with a vertex-colour gradient.
  - It has one owner at a time: opening a second drone window closes the first, and the soot re-flies from where it is.
  - It publishes whether a drone window is open, which replaces `BagProjected`.
- **`Tablet` is reshaped in place**:
  - its bezel, metal and slide go, and the hologram glass and the sway come in;
  - its calls and static helpers stay, and so do the pages;
  - the build menu's wings and hint screen lose their metal and join one outline;
  - the game menu takes a copy of today's tablet (`MenuTablet`).
- **The bag** keeps its `HoloWindow` (layout, sway, contents shown whole); its `MachineHolo` goes.
  - Unpaired, there is no bag: the hotbar and the equipment unit stay. An open machine or crate takes the bag's place
    under the hotbar.
- **`TabletEdge`** keeps the keys, credits, tips and `Follow`, always at the foot; the rim and its metal go.
- **`LightDrone` and `DroneSwarm`.** While bare:
  - `DroneSwarm` keeps 18 specks on the pick's three linked rings and skips the rest;
  - the plates keep the mode's shape.

  At all times, the in-flight threads are orange arcs from the core to each plate. With no window open, the drone's
  crackle runs on those arcs. `Projects` and the bag's sign shape go.
- **Before pairing**, every opener of a drone window does nothing, and the edge lists only paired keys. Turning creative
  on pairs the drone.

## Determinism and co-op

There is no sim change: windows, soot and charge are client-only. The only command involved is `PairDrone`, which
creative sends.

The hero's wire gains one bool, "drone window open", next to `Seen.Drone`:

- A friend's drone draws bare as one's own does, and the specks that leave rise and dissolve over 0.3 s, settling back
  in 0.2 s.
- A late joiner gets the flag with the next hero packet.
- Both ends run one build, checked when a peer joins. A hero packet from a different build fails to decode, and that
  hero is hidden.

Nothing new is saved. Creative's pairing is the sim's own pairing: it is saved and stays after creative is turned off.

## Trade-offs accepted

- **Specks leave from the core, scattered**, not from their own orbit spots. The orbits live in `crew`, and a speck may
  jump at hand-off; the 0.3 s flight hides it.
- **The drone not drawn** (the hero is seated): the soot fades in on the outline.
- **Two crackle clocks** (the window's, the drone's) step in different nodes and may be a frame apart at a hand-off; at
  worst, two crackles in one frame.
- **Creative pairs for free**, and the drone stays after creative is off: a dev-only effect.

## Tests

- **`SootFrame`:**
  - a spot sits in every corner;
  - gaps are even and 14–32 px on any outline, the board's included;
  - the order starts at the near corner.
- **`SootRun`:**
  - opening ends within 0.3 s, with every mote landed and every span linked;
  - a span lights only after both its ends land;
  - closing ends within 0.2 s;
  - a switch mid-run moves no mote further than a set bound per frame;
  - breathing stays within its bounds;
  - crackles come 4–8 s apart and last 0.13 s, never mid-run.
- **`DroneSwarm`:**
  - when bare, it keeps exactly the 18 ring specks and the mode's plates;
  - a friend's leaving specks rise, dissolve and settle back.
- **Frames, by the lead:**
  - the bag, the board and the build menu, each opening and closing;
  - unpaired Tab;
  - a run with two players.

## Build order, each step playable

1. The bag on the soot frame: the bare drone and the orange arcs on its plates, with the crackle on the drone while no
   window is open.
2. The four pages on the reshaped `Tablet`; the rim goes.
3. No bag and no drone windows before pairing; creative pairs.
4. Friends see the soot rise and settle.
