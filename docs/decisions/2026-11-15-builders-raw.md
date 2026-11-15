# The screwdriver, power driver and blasting charge drawn raw

- Status: accepted; redraws the looks of [power-driver](2026-11-03-power-driver.md) and
  [blasting-charge](2026-11-08-blasting-charge.md), raw as [raw tool pictures](2026-11-14-raw-tool-pictures.md) says
- Date: 2026-11-15

## Context

Every weapon had been redrawn from a picked GPT reference. The screwdriver, the power driver and the blasting charge were the
last held items still drawn in code. Each got a page of five references, and the user picked the current look, redrawn, for
all three. The pipe-with-caps charge had been refused by GPT before. Asked as a plumber's pipe nipple, it went through.

## Decision

- **Shipped raw:** `refs/screwdriver.png`, `refs/powerDriver.png`, `refs/blastCharge.png`, transparent and cut by alpha.
  - The screwdriver keeps its 10 × 3 box. It is fitted along 9 px and drawn 1.4 times as thick as the reference. At the
    reference's thickness the handle was thinner than the fist.
  - The power driver grows from 10 × 8 to **14 × 11**. At the old size the fist covered most of it (the user: "рука как-то
    слишком большая для него").
  - The charge keeps its 9 × 4 box. The picture spans the drawn pipe's length, cap to plug.
- **Code draws what moves or lights:**
  - The screwdriver's twist is a light streak down its handle, a third of its height a frame.
  - The power driver's chuck and bit turn as a steel streak.
  - The power driver's lamp and charge lights are glows over the reference's own: dark and steady green at rest, bright while
    running, the first light red when flat.
  - The charge's fuse runs out of the plug; it sparks once lit, as before.
- **At work, a builder lies along the forearm.** Both drivers point the way the forearm points, not straight at the plan. The
  arm reaches 0.97 of its length toward the plan, nearly straight, tilted at most 0.5 rad from level. Before, the tool pointed
  at the plan while a bent elbow pointed the forearm elsewhere, and the two read as askew.
- **The screwdriver sits above the fist's middle:** its grip is 0.7 px below its shaft. The user asked for it a little above
  the middle; centred, it stuck out by the little finger.

## Not handled

- The streaks are drawn over each reference's own chuck and handle. Another picture would need its boxes measured again.
- The power driver's lights are smaller in the reference than the drawn ones were. The glows are larger than the painted
  lights, so they read at the fist's size.
