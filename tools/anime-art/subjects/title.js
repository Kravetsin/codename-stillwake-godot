// The title's plate: a torn chunk of hull in the cold, laid on PlateArt's size and its seat, lamp and anchor, each tear and wire placed.
(function () {
'use strict';
const { ell, poly, bake } = Anime;
const SKIN = { base: '#2c3443', shade: '#1c222e', deep: '#141923', lite: '#56627a', spec: '#6f7c94', rim: '#56627a', ink: '#070a10' };
const INSIDE = { base: '#141923', shade: '#0c1017', deep: '#080b11', lite: '#2a3242', spec: '#3a4356', rim: '#2a3242', ink: '#070a10' };
const W = 140, H = 63, TOP = 26, BEND = 12, THICK = 15;

/** The plate's top at x: level at the right, bent down towards the torn left end. */
const top = (x) => TOP + (x < 44 ? Math.pow((44 - x) / 44, 2) * BEND : 0);
/** A strip of the plate from x0 to x1 between depths r0 and r1, following the bend. */
const strip = (x0, x1, r0, r1) => {
  const pts = [];
  for (let x = x0; x < x1; x += 3) pts.push(x, top(x) + r0);
  pts.push(x1, top(x1) + r0, x1, top(x1) + r1);
  for (let x = x1 - 3; x > x0; x -= 3) pts.push(x, top(x) + r1);
  pts.push(x0, top(x0) + r1);
  return poly(pts);
};
const STRETCHES = [[8, 24], [39, 57], [73, 95], [106, 125], [130, W]];
const WIRES = [[28, 6, 1], [33, 9, -1], [62, 5, 1], [67, 8, 1], [99, 7, -1], [102, 4, 1], [127, 5, 1]];

const plate = (a) => {
  // The rib behind the skin: a girder standing out of the left third, leaning, broken off at its top.
  a.shape(poly(30, top(30) + 1, 34, top(30) + 1, 28.6, 9, 27.4, 5.4, 26.4, 8, 25.4, 3.4, 24.2, 6.4), SKIN, {
    lite: poly(30, top(30) + 1, 31, top(30) + 1, 25.2, 5, 24.2, 6.4), shade: poly(32.6, top(30) + 1, 34, top(30) + 1, 28.6, 9, 27.8, 8) });
  for (const y of [13, 21, 29]) { const x = 30.6 - (top(30) - y) / 3; a.flat(poly(x, y, x + 1.7, y, x + 1.5, y + 2.4, x - .2, y + 2.4), '#0c1017'); }

  // The frame in the dark between the skins: ribs across, struts slanting between them.
  a.shape(strip(5, W, 3.6, 11.4), INSIDE, { lite: strip(5, W, 3.6, 4.2) });
  for (let x = 10; x < W; x += 10) {
    a.flat(poly(x, top(x) + 4, x + 1.5, top(x + 1.5) + 4, x + 1.5, top(x + 1.5) + 11, x, top(x) + 11), '#2a3242');
    const down = (x / 10) % 2 === 0, y0 = down ? 4.6 : 10.4, y1 = down ? 10.4 : 4.6;
    a.line(`M${x + 1.5} ${top(x + 1.5) + y0}L${x + 10} ${top(x + 10) + y1}`, { w: 1.2, c: '#2a3242' });
  }

  // Wires hanging out of the tears, then the inner skin in the stretches it still holds.
  for (const [x, l, side] of WIRES) {
    const y = top(x) + 11;
    a.line(`M${x} ${y}L${x + side * .3} ${y + l * .6}L${x + side} ${y + l}`, { w: 1, c: '#4a5060' });
  }
  for (const [x0, x1] of STRETCHES) a.shape(strip(x0, x1, 11.4, THICK), SKIN, { lite: strip(x0, x1, 11.4, 12), shade: strip(x0, x1, 13, THICK) });

  // The outer skin, its left end ragged, its right end square.
  const skin = [3, top(3) + 1.4, 4.4, top(4) + .2];
  for (let x = 7; x < W; x += 3) skin.push(x, top(x));
  skin.push(W, TOP, W, TOP + 3.8);
  for (let x = W - 3; x > 7; x -= 3) skin.push(x, top(x) + 3.8);
  skin.push(6.4, top(6) + 3.8, 5, top(5) + 2.6, 6.2, top(6) + 1.9);
  const edge = [];
  for (let x = 5; x < W; x += 3) edge.push(x, top(x));
  edge.push(W, TOP, W, TOP + .9);
  for (let x = W - 3; x > 5; x -= 3) edge.push(x, top(x) + .9);
  a.shape(poly(skin), SKIN, { lite: poly(edge), shade: strip(7, W, 2.8, 3.8) });
  for (const x of [22, 56, 90, 124]) a.flat(poly(x, top(x) + .9, x + .5, top(x) + .9, x + .5, top(x) + 3, x, top(x) + 3), '#1c222e');
  for (let x = 12; x < W; x += 10) { a.flat(ell(x, top(x) + 1.7, .42), '#6f7c94'); a.flat(ell(x + .1, top(x) + 1.85, .2), '#1c222e'); }

  // The hole punched through it.
  a.flat(poly(60, TOP, 66.6, TOP, 66, TOP + 1.8, 65, TOP + 3.9, 62, TOP + 3.6, 60.8, TOP + 2), '#05070c');
  a.line(`M60 ${TOP}L60.8 ${TOP + 2}L62 ${TOP + 3.6}L65 ${TOP + 3.9}L66 ${TOP + 1.8}L66.6 ${TOP}`, { w: 1, c: '#56627a' });
};

Subjects.title = () => { out('title_plate', bake({ w: W, h: H, draw: plate })); };
})();
