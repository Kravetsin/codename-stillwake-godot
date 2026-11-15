// A door, shut and open (DoorKit.picture), one piece long here (door_*_0); room.js lays out the three pieces of a longer door from the
// same kit, so they are one door. The frame and the panel are washed apart and laid one over the other, the frame on top, so the
// shut picture differs from the open one exactly by the panel: ShipTiles slides what differs.
(function () {
'use strict';
const { poly, bake, canvas } = Anime;

/** The world pixels of a door's picture: its jambs at either side of the panel's run, the panel between. */
const DOOR = { left: 3, panel: 4, panelEnd: 12, right: 13, top: 3, foot: 15 };
const ORANGE = { base: '#d97f3a', shade: '#a9542a', lite: '#f6b06a', spec: '#ffe4b8', ink: '#2a140c' };
const JAMB = { base: '#5a3a26', shade: '#3d2519', lite: '#7a5236', spec: '#7a5236', ink: '#160c08' };
const GLASS = { base: '#2c4a70', shade: '#1f3656', lite: '#9ad2f0', spec: '#e4f6ff', ink: '#1a1c28' };
const HAZARD = { base: '#f0c43c', shade: '#b8922c', lite: '#f8dc78', spec: '#fff0b0', ink: '#2a2008' };
// Drawn M px past a joined end and cut back to the cell, so the wash's edge, where paint pools, falls outside it and pieces meet clean.
const M = 2, past = (head, foot) => [head ? 0 : -M, foot ? 16 : 16 + M];
const band = (x0, y0, x1, y1) => poly(x0, y0, x1, y0, x1, y1, x0, y1);
const seg = (x0, y0, x1, y1) => `M${x0} ${y0}L${x1} ${y1}`;

/**
 * The panel of a piece: its window at a head, hazard stripes at a foot, the seam across where its plates meet. No line round it:
 * its sides lie under the jambs, its ends under the header and the sill, and a middle piece runs on into the next.
 */
const panel = (head, foot) => (a) => {
  const [y0, y1] = past(head, foot), from = head ? DOOR.top : y0, to = foot ? DOOR.foot : y1, P = DOOR.panel, E = DOOR.panelEnd;
  a.shape(band(P, from, E, to), ORANGE, {
    outer: false,
    lite: [band(P, from, P + 1.25, to), band(P, 9.5, E, 10)],
    spec: band(P + .25, from, P + .75, to),
    shade: [band(E - 1.75, from, E, to), band(P, 8.25, E, 9)],
    lines: [seg(P + 1.35, from, P + 1.35, to), seg(E - 1.65, from, E - 1.65, to), seg(P, 9.25, E, 9.25)],
  });
  if (head) a.shape(band(6.25, 4.25, 9.75, 7.25), GLASS, { lite: poly(6.25, 7.25, 6.25, 5.75, 7.75, 4.25, 9, 4.25), spec: poly(6.5, 5.75, 7.75, 4.5, 8.25, 4.5, 6.5, 6.25) });
  if (foot) {
    const stripes = band(P, 11.75, E, 15);
    a.shape(stripes, HAZARD, { outer: false, lines: [seg(P, 11.65, E, 11.65)] });
    for (let k = -2; k < 12; k += 2.5) a.flat(poly(P + k, 15, P + k + 1.25, 15, P + k + 4.5, 11.75, P + k + 3.25, 11.75), '#1a1820', { clip: stripes });
  }
};

/** The frame of a piece, the same shut and open: the jambs, the header with its lamps at a head, the sill at a foot. */
const frame = (head, foot) => (a) => {
  const L = DOOR.left - .75, R = DOOR.right + .75, P = DOOR.panel, E = DOOR.panelEnd, [y0, y1] = past(head, foot);
  for (const [x0, x1] of [[L, P], [E, R]])
    a.shape(band(x0, y0, x1, y1), JAMB, { outer: false, lite: band(x0 + .5, y0, x0 + 1, y1), shade: band(x1 - .5, y0, x1, y1), lines: [seg(x0 + .2, y0, x0 + .2, y1), seg(x1 - .2, y0, x1 - .2, y1)] });
  if (head) {
    a.shape(band(L, 0, R, DOOR.top), JAMB, { outer: false, lite: band(L, .5, R, 1), lines: [seg(L, .2, R, .2), seg(L, DOOR.top - .2, R, DOOR.top - .2), seg(L + .2, 0, L + .2, DOOR.top), seg(R - .2, 0, R - .2, DOOR.top)] });
    for (const lx of [5.5, 9]) a.shape(band(lx, 1.25, lx + 1.5, 2.25), HAZARD, { lite: band(lx + .25, 1.25, lx + .75, 1.5) });
  }
  if (foot) a.shape(band(L, DOOR.foot, R, 16), JAMB, { outer: false, shade: band(L, DOOR.foot + .25, R, 16), lines: [seg(L, DOOR.foot + .2, R, DOOR.foot + .2)] });
};

/** A piece of a door, shut and open, by whether it has a head (the top) and a foot (the bottom): the frame washed once for both. */
function pictures(head, foot) {
  const f = bake({ w: 16, h: 16, margin: M, draw: frame(head, foot) }), p = bake({ w: 16, h: 16, margin: M, draw: panel(head, foot) });
  const lay = (shut) => {
    const c = canvas(64, 64), x = c.getContext('2d');
    if (shut) x.drawImage(p, M * 4, M * 4, 64, 64, 0, 0, 64, 64);
    x.drawImage(f, M * 4, M * 4, 64, 64, 0, 0, 64, 64);
    return c;
  };
  return { shut: lay(true), open: lay(false) };
}

window.DoorKit = { pictures };

Subjects.doors = function () {
  // Links: up 1, right 2, down 4, left 8. Only up and down shape a door; room.js writes 1, 4 and 5, the rest are the same four pictures, as in the logical set.
  for (let links = 0; links < 16; links++) {
    if (links === 1 || links === 4 || links === 5) continue;
    const { shut, open } = pictures(!(links & 1), !(links & 4));
    out(`door_closed_${links}`, shut);
    out(`door_open_${links}`, open);
  }
};
})();
