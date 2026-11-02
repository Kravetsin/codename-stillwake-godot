// The electric smelter: its body with cold coils, and the three frames of its coils alight, over the same box.
(function () {
'use strict';
const { ell, rr, poly, bake } = Anime;
const STEEL = { base: '#a7afbc', shade: '#6a708c', deep: '#474c68', lite: '#d4dae2', spec: '#ffffff', rim: '#86d2e8' };
const GUN = { base: '#5c6474', shade: '#383c58', deep: '#262a42', lite: '#868fa2', spec: '#dfe7f2', rim: '#5fb2d0' };
const DARK = { base: '#2a2d3c', shade: '#1a1c2a', deep: '#12131e', lite: '#40445a', spec: '#a8b4c6', rim: '#4f9cbc' };
const COPPER = { base: '#a8693c', shade: '#6c3a30', deep: '#4c2830', lite: '#cf8c58', spec: '#f4c896', rim: '#d09060' };
const SCREEN = { base: '#2b7a52', shade: '#1a5040', deep: '#123a30', lite: '#54ba74', spec: '#c8f4c0', rim: '#54ba74' };
const CREAM = { base: '#d8d2c2', shade: '#8e8a84', deep: '#6a666a', lite: '#f2eee2', spec: '#ffffff', rim: '#d8d2c2' };

const COILS = [[8.5, 12.75], [17.25, 12.75]], COIL_W = 6.25, COIL_H = 9.5, TURNS = 5;

function body(a) {
  // Two posts on top, the feet under it.
  for (const x of [5, 23]) {
    a.shape(rr(x + .5, .5, 3, 2.75, .4), CREAM, { shade: `M${x + 2.25} 0L${x + 4} 0L${x + 4} 4L${x + 2.25} 4Z`, lite: rr(x + .75, .75, 1, 1.75, .2) });
    a.shape(rr(x - .25, 3, 4.5, 2.5, .3), DARK, { lite: rr(x, 3.1, 3.5, .6, .1) });
  }
  for (const x of [3, 25.5]) a.shape(rr(x, 27.5, 3.5, 4.25, .3), GUN, { shade: `M${x + 2.25} 27L${x + 4} 27L${x + 4} 32L${x + 2.25} 32Z`, lite: rr(x + .25, 27.7, 1.25, 3.6, .2) });
  // The cabinet.
  a.shape(rr(2.25, 5, 27.5, 20, .75), STEEL, {
    lite: poly(2.25, 5, 29.75, 5, 29.75, 6, 2.25, 6), shade: [poly(27.75, 5, 29.75, 5, 29.75, 25, 27.75, 25), poly(2.25, 22.5, 29.75, 22.5, 29.75, 25, 2.25, 25)],
    lines: ['M15.5 5.2L15.5 11.25'] });
  a.shape(poly(2.25, 11.25, 29.75, 11.25, 29.75, 12.5, 2.25, 12.5), GUN, { outer: false, lite: poly(2.25, 11.25, 29.75, 11.25, 29.75, 11.65, 2.25, 11.65) });
  // Its vents, left; its screen, right.
  for (const y of [7, 9]) a.shape(rr(5.5, y, 7.25, 1.1, .3), DARK, { outer: false, lite: poly(5.5, y + .75, 12.75, y + .75, 12.75, y + 1.1, 5.5, y + 1.1) });
  a.flat(ell(3.75, 7.25, .45), '#d4dae2');
  a.flat(ell(13.9, 7.25, .35), '#6a708c');
  a.shape(rr(17.5, 6.25, 10.25, 4.75, .35), DARK, { outer: false, lite: poly(17.5, 6.25, 27.75, 6.25, 27.75, 6.6, 17.5, 6.6) });
  a.shape(poly(18.25, 7, 27, 7, 27, 10.25, 18.25, 10.25), SCREEN, { outer: false, lite: poly(18.25, 7, 27, 7, 27, 7.9, 18.25, 7.9), shade: poly(18.25, 9.1, 22.75, 9.1, 22.75, 10.25, 18.25, 10.25) });
  a.flat(rr(20, 8.4, .75, .75, .1), '#f0d060');
  a.flat(poly(23.25, 8.5, 25.75, 8.5, 25.75, 10.25, 23.25, 10.25), '#c8c8bc');
  // Side pillars, a bolted plate on the left one.
  a.shape(poly(2.25, 12.5, 7, 12.5, 7, 24.5, 2.25, 24.5), STEEL, { outer: false, lite: poly(2.25, 12.5, 7, 12.5, 7, 13.25, 2.25, 13.25), shade: poly(6, 12.5, 7, 12.5, 7, 24.5, 6, 24.5) });
  a.shape(rr(3, 15, 2.5, 6.5, .2), GUN, { outer: false, shade: poly(4.75, 15, 5.5, 15, 5.5, 21.5, 4.75, 21.5), lite: poly(3, 15, 5.5, 15, 5.5, 15.4, 3, 15.4) });
  a.shape(poly(25, 12.5, 29.75, 12.5, 29.75, 24.5, 25, 24.5), STEEL, { outer: false, lite: poly(25, 12.5, 29.75, 12.5, 29.75, 13.25, 25, 13.25), shade: poly(28.5, 12.5, 29.75, 12.5, 29.75, 24.5, 28.5, 24.5) });
  a.flat(ell(27, 14.25, .4), '#5c6474');
  a.flat(ell(27, 22.5, .4), '#5c6474');
  // The grate's well, with its cold coils behind the bars.
  a.shape(poly(7, 12.25, 25, 12.25, 25, 24.25, 7, 24.25), DARK, { outer: false, deep: poly(7, 12.25, 25, 12.25, 25, 13.25, 7, 13.25) });
  for (const [x, y] of COILS) {
    a.shape(rr(x - .5, y - .25, COIL_W + 1, COIL_H + .5, .2), GUN, { outer: false, shade: poly(x + COIL_W - .25, y, x + COIL_W + .5, y, x + COIL_W + .5, y + COIL_H, x + COIL_W - .25, y + COIL_H) });
    for (let i = 0; i < TURNS; i++) {
      const ty = y + .35 + i * (COIL_H / TURNS);
      a.shape(rr(x, ty, COIL_W, 1.2, .35), COPPER, { outer: false, lite: poly(x, ty, x + COIL_W, ty, x + COIL_W, ty + .4, x, ty + .4), shade: poly(x + COIL_W - 1.5, ty, x + COIL_W, ty, x + COIL_W, ty + 1.2, x + COIL_W - 1.5, ty + 1.2) });
    }
    a.flat(poly(x + 2.25, y, x + 3.75, y, x + 3.75, y + COIL_H, x + 2.25, y + COIL_H), '#474c68');
    a.flat(poly(x + 2.25, y, x + 2.6, y, x + 2.6, y + COIL_H, x + 2.25, y + COIL_H), '#6a708c');
  }
  // The slab it stands on.
  a.shape(rr(1, 24.5, 30, 3.5, .4), STEEL, { lite: poly(1, 24.5, 31, 24.5, 31, 25.4, 1, 25.4), shade: poly(1, 26.75, 31, 26.75, 31, 28, 1, 28), spec: poly(3, 24.7, 11, 24.7, 10.6, 25.1, 2.6, 25.1) });
}

const HOT = ['#ffd060', '#ff9a36', '#f06a22'];
function coils(frame) {
  return (a) => {
    for (const [x, y] of COILS) {
      a.flat(poly(x - .5, y - .25, x + COIL_W + .5, y - .25, x + COIL_W + .5, y + COIL_H + .25, x - .5, y + COIL_H + .25), '#3a1408');
      for (let i = 0; i < TURNS; i++) {
        const ty = y + .35 + i * (COIL_H / TURNS), hot = (i + frame + (x > 10 ? 1 : 0)) % 3;
        a.flat(rr(x, ty, COIL_W, 1.2, .35), HOT[hot]);
        a.flat(rr(x + .75, ty + .2, COIL_W - 1.5, .4, .1), hot === 0 ? '#fff4c8' : '#ffc070');
      }
      a.flat(poly(x + 2.25, y, x + 3.75, y, x + 3.75, y + COIL_H, x + 2.25, y + COIL_H), '#401808');
    }
  };
}

Subjects.smelter = function () {
  out('machine_smelter', bake({ w: 32, h: 32, draw: body }));
  for (let f = 0; f < 3; f++) out(`smelter_work_${f}`, bake({ w: 32, h: 32, draw: coils(f) }));
};
})();
