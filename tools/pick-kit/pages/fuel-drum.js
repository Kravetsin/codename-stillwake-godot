// «Топливная бочка»: five looks for the fuel drum (docs/decisions/2026-11-23-fuel-drums.md), a wreckage kind 4 × 4 cells that bursts when broken.
// Each is drawn by tools/anime-art's own engine and finish with the debris subject's kit (DebrisKit), pasted in as they are, so
// the picked drawing goes into subjects/debris.js and items.js unchanged. The scene, the hero and the burst's flash are the page's.
// For a frame: index.html#at=<s into the loop>&pause. node tools/pick-kit/build.js tools/pick-kit/pages/fuel-drum.js <out-dir>
'use strict';
const fs = require('fs'), path = require('path');

const ANIME = (f) => fs.readFileSync(path.join(__dirname, '..', '..', 'anime-art', f), 'utf8').split('\r\n').join('\n');
// the engine, the finish and the debris subject in an object of their own; Path2D is the browser's (a stand-in under the stub check)
const ART_SRC = `const ART = ((window, Path2D) => {
${ANIME('engine.js')}
const Anime = window.Anime, Subjects = window.Subjects;
${ANIME('wash.js')}
${ANIME('subjects/debris.js')}
return window;
})({}, typeof Path2D === 'function' ? Path2D : class { constructor(d) { this.d = d; } });`;

// the pieces drifting by, the game's own pictures, and their sizes in px read from them (drawn 1:1)
const PICTURES = { tanks: 'game/art/hd/debris/tanks_01.png', scrap: 'game/art/hd/debris/scrap_02.png', crate: 'game/art/hd/debris/crate_03.png' };
const SIZES = Object.fromEntries(Object.entries(PICTURES).map(([k, f]) => { const b = fs.readFileSync(path.join(__dirname, '..', '..', '..', f)); return [k, [b.readUInt32BE(16), b.readUInt32BE(20)]]; }));

// ---------- the five drums: each a picture 32 × 32 world px (debris/fueldrum_01) and its item icon 16 × 16 (items/fuelDrum) ----------
// Called with ART; returns the candidates' drawings. What moves or lights is the page's code (marks), as the game's would be.
function drums(ART) {
  const { ell, rr, poly } = ART.Anime;
  const { band, plate, rv, rusted, hazard, wire, PLATE, OLD, GUN, SOOT, COPPER, PAINT, ARMY, CREAM, OX, FROST, STRIPE } = ART.DebrisKit;
  const RED = { base: '#86362a', shade: '#58241e', deep: '#3a1816', lite: '#a04c36', spec: '#b8765a', ink: '#140a08' };
  const WHEEL = { base: '#a8392c', shade: '#702420', deep: '#4c1a18', lite: '#c45a44', spec: '#d88a70', ink: '#140a08' };
  const ENAMEL = { base: '#a9a79c', shade: '#6a6a68', deep: '#46464a', lite: '#c4c2b6', spec: '#e6e2d4', ink: '#0e1018' };
  const WEB = { base: '#5f5a4c', shade: '#3f3c34', deep: '#2c2a26', lite: '#7a7462', spec: '#8e887a', ink: '#100f0c' };
  const FACE = { base: '#d0cdc1', shade: '#8a887e', deep: '#5c5a54', lite: '#e0ddd2', ink: '#0e1018' };
  const NEEDLE = '#a83c30', FLAME = '#1c1612';
  const turned = (cx, cy, t) => { const c = Math.cos(t), s = Math.sin(t); return (u, v) => [cx + u * c - v * s, cy + u * s + v * c]; };
  const P = (pts) => poly(pts.flat());
  // a strip of a turned body: u along it, v across
  const strip = (T, u0, u1, v0, v1) => P([T(u0, v0), T(u1, v0), T(u1, v1), T(u0, v1)]);
  // a band round a round body seen a little from above: between two sagging curves, inside the circle
  function roundBand(cx, cy, r, y0, y1, sag) {
    const top = [], bot = [];
    for (let i = 0; i <= 24; i++) {
      const dx = -r + (2 * r * i) / 24, h = Math.sqrt(Math.max(0, r * r - dx * dx)), k = sag * (1 - (dx / r) ** 2);
      const a = Math.max(cy + y0 + k, cy - h), b = Math.min(cy + y1 + k, cy + h);
      if (a < b) { top.push([cx + dx, a]); bot.unshift([cx + dx, b]); }
    }
    return P(top.concat(bot));
  }
  // the hazmat diamond: a pale border, red inside, a black flame
  function diamond(a, x, y, r) {
    a.shape(P([[x, y - r], [x + r, y], [x, y + r], [x - r, y]]), FACE, { outer: false, shade: P([[x + r * .3, y - r * .7], [x + r, y], [x, y + r], [x - r * .3, y + r * .7]]) });
    a.flat(P([[x, y - r * .74], [x + r * .74, y], [x, y + r * .74], [x - r * .74, y]]), WHEEL.base);
    const f = r * .34;
    a.flat(`M${x} ${y - f * 1.5}Q${x + f * 1.3} ${y - f * .2} ${x + f * .7} ${y + f * .8}Q${x} ${y + f * 1.3} ${x - f * .7} ${y + f * .8}Q${x - f * 1.1} ${y - f * .1} ${x - f * .2} ${y - f * .5}Q${x - f * .1} ${y + f * .1} ${x + f * .2} ${y - f * .1}Q${x + f * .3} ${y - f * .8} ${x} ${y - f * 1.5}Z`, FLAME);
  }
  // a dial: its ring, its pale face, the needle in the red
  function gauge(a, x, y, r) {
    a.shape(ell(x, y, r), PLATE, { shade: r * .2 });
    a.shape(ell(x, y, r * .7), FACE, { outer: false, shade: r * .16 });
    a.flat(poly(x + r * .1, y - r * .62, x + r * .62, y - r * .1, x + r * .5, y + r * .1, x + r * .05, y - r * .3), NEEDLE);
    a.line(`M${x} ${y}L${x + r * .5} ${y - r * .44}`, { w: .9, c: '#20100c' });
  }

  // 1. The drum: a squat steel drum in worn red, rolling hoops, a hazard band round its middle, a bung weeping fuel.
  function drum(a) {
    const body = 'M5 5.6L5 28Q5 30.8 16 30.8Q27 30.8 27 28L27 5.6Q27 8 16 8Q5 8 5 5.6Z';
    a.shape(body, RED, { lite: band(5, 4, 7.6, 31), shade: band(23.2, 4, 27.2, 31), deep: 1.1 });
    a.line('M10.6 8.6L10.6 30', { w: .8 });
    const belt = 'M5 16Q16 18.6 27 16L27 20.2Q16 22.8 5 20.2Z';
    hazard(a, 5, 15.4, 22, 7.6, { clip: belt, step: 4.2, gone: [3] });
    a.flat(poly(23.2, 15.6, 27.2, 15.6, 27.2, 22.8, 23.2, 22.8), '#00000033', { clip: belt });
    a.line('M5 16Q16 18.6 27 16', { w: 1 }); a.line('M5 20.2Q16 22.8 27 20.2', { w: 1 });
    for (const y of [11.6, 25]) a.tube(`M5.2 ${y}Q16 ${y + 2.6} 26.8 ${y}`, 1.2, RED, { spec: false });
    a.shape('M5 5.6Q5 3.2 16 3.2Q27 3.2 27 5.6Q27 8 16 8Q5 8 5 5.6Z', RED, { lite: 'M5 5.6Q5 3.2 16 3.2Q27 3.2 27 5.6Q20 4.4 16 4.4Q8 4.4 5 5.6Z', shade: 'M5 5.6Q12 7 16 7Q22 7 27 5.6Q27 8 16 8Q5 8 5 5.6Z' });
    a.flat(ell(16, 5.7, 9.6, 1.6), RED.shade);
    a.flat(ell(16.4, 6, 9, 1.2), RED.deep);
    // the bung: its cap, the fuel it weeps pooled round it and darkening the rim below
    a.flat(ell(20.4, 5.8, 3.2, 1.1), '#2a1812');
    a.flat(poly(18.8, 7.4, 22.4, 7.2, 22.2, 8.6, 21.2, 9.4, 20, 9, 19, 8.4), '#3a1e16', { clip: body });
    a.flat(ell(21.6, 5.6, .9, .3), '#7a5a48');
    a.shape(rr(18.6, 3.4, 3.6, 2.4, .5), GUN, { lite: band(18.6, 3.4, 22.2, 4), shade: band(21.4, 3.4, 22.2, 5.8) });
    a.flat(ell(11, 5.8, 1.4, .6), SOOT.deep);
    // a dent where it struck something, rust at the chime and where a hoop meets the seam
    a.flat(ell(21.6, 27, 2.2, 1.5), RED.shade, { clip: body }); a.line('M19.6 26.2Q21.6 25.4 23.6 26.4', { w: 1 });
    rusted(a, 7.4, 29.6, 1.6, .9, body); rusted(a, 10.8, 12.8, .8, .7, body); rusted(a, 25.6, 25.6, .7, .9, body);
    rv(a, [6.4, 13], [25.6, 13]);
  }
  function drumIcon(a) {
    const body = 'M3.6 3L3.6 13.4Q3.6 15 8 15Q12.4 15 12.4 13.4L12.4 3Q12.4 4.2 8 4.2Q3.6 4.2 3.6 3Z';
    a.shape(body, RED, { lite: band(3.6, 2, 5, 15), shade: band(10.6, 2, 12.4, 15), deep: .5 });
    const belt = 'M3.6 7.6Q8 8.8 12.4 7.6L12.4 9.8Q8 11 3.6 9.8Z';
    hazard(a, 3.6, 7.2, 8.8, 3.8, { clip: belt, step: 2.6 });
    a.line('M3.6 7.6Q8 8.8 12.4 7.6', { w: .8 }); a.line('M3.6 9.8Q8 11 12.4 9.8', { w: .8 });
    a.tube('M3.7 5.8Q8 7 12.3 5.8', .7, RED, { spec: false }); a.tube('M3.7 12.2Q8 13.4 12.3 12.2', .7, RED, { spec: false });
    a.shape('M3.6 3Q3.6 1.8 8 1.8Q12.4 1.8 12.4 3Q12.4 4.2 8 4.2Q3.6 4.2 3.6 3Z', RED, { shade: 'M3.6 3Q8 3.8 12.4 3Q12.4 4.2 8 4.2Q3.6 4.2 3.6 3Z' });
    a.flat(ell(8, 3.1, 3.8, .6), RED.deep);
    a.shape(rr(9, 1.6, 1.8, 1.3, .3), GUN, { shade: band(10.4, 1.6, 10.8, 2.9) });
    rusted(a, 4.8, 14, .8, .5, body);
  }

  // 2. The sphere: a pressure sphere torn off its mount, grey enamel with a red band, the hazmat diamond, a handwheel valve
  // and its gauge, rime round the neck where the gas escapes.
  function sphere(a) {
    // the stub of the bracket it was torn from, its bolt hole, the tear ragged
    a.shape(poly(1.6, 13.8, 6.4, 13.2, 6.6, 19.6, 1.4, 19.8, 2.2, 18.4, 1.2, 17.2, 2, 16.2, 1.2, 15), GUN, { lite: band(0, 12.6, 7, 14.2), shade: band(0, 18.6, 7, 20) });
    a.flat(ell(3.8, 16.4, .8, .9), SOOT.deep);
    for (const d of ['M12.6 27L10.6 30.8', 'M19.4 27L21.4 30.8', 'M16 27.6L16 31']) a.tube(d, 1.5, GUN, { spec: false });
    a.shape(ell(16, 27.4, 6.2, 1.8), GUN, { lite: ell(15, 26.9, 5, 1), shade: band(19, 26, 23, 30) });
    const s = ell(16, 16.6, 11.2), dark = poly(34, 4, 34, 34, 4, 34, 10, 30, 20, 26.6, 26, 20, 29.4, 10);
    a.shape(s, ENAMEL, { shade: dark, deep: 1.3, lite: ell(12.4, 12.4, 6, 5) , spec: ell(10.4, 10.4, 1.5, 1) });
    const red = roundBand(16, 16.6, 11.2, -1.4, 2.4, 3.6);
    a.flat(red, WHEEL.base, { clip: s });
    a.flat(dark, WHEEL.shade, { clip: red });
    a.flat(ell(11.6, 18.4, 4, 1.6), WHEEL.lite, { clip: red });
    a.line('M4.8 15.2Q16 22.8 27.2 15.2', { w: .9 }); a.line('M4.8 19Q16 26.6 27.2 19', { w: .9 });
    diamond(a, 20.8, 10.8, 3.5);
    rusted(a, 8.2, 24.4, 1.4, 1, s); rusted(a, 25.2, 20.6, .9, 1.1, s);
    a.line('M24.6 12.4Q25.6 14.2 25.4 16', { w: 1 });
    // the valve on top: its neck, the handwheel edge-on, the gauge on its arm; rime on the neck
    a.shape(band(14.4, 3.2, 17.6, 6.4), GUN, { lite: band(14.4, 3.2, 15.2, 6.4), shade: band(16.8, 3.2, 17.6, 6.4) });
    a.flat(ell(14.2, 6.2, 1.6, .8), FROST[1]); a.flat(ell(17.8, 5.9, 1.3, .7), FROST[0]); a.flat(ell(15.8, 4.6, .8, .5), FROST[1]);
    a.shape(rr(11.4, 1.6, 9.2, 1.8, .8), WHEEL, { lite: band(11.4, 1.6, 20.6, 2.2), shade: band(11.4, 2.8, 20.6, 3.4) });
    a.flat(ell(16, 2.5, 1.1, .6), SOOT.deep);
    a.tube('M17.6 4.6L20.4 4.6', .8, GUN, { spec: false });
    gauge(a, 22.2, 4.6, 2);
  }
  function sphereIcon(a) {
    for (const d of ['M5.6 13.6L4.6 15.2', 'M10.4 13.6L11.4 15.2']) a.tube(d, .9, GUN, { spec: false });
    const s = ell(8, 8.6, 5.8), dark = poly(16, 2, 16, 16, 2, 16, 6, 14, 11, 12, 13, 8, 14.4, 4);
    a.shape(s, ENAMEL, { shade: dark, deep: .6, lite: ell(6.2, 6.6, 3, 2.6) });
    const red = roundBand(8, 8.6, 5.8, -.6, 1.4, 1.8);
    a.flat(red, WHEEL.base, { clip: s }); a.flat(dark, WHEEL.shade, { clip: red });
    diamond(a, 10.4, 5.8, 1.9);
    a.shape(band(7.1, 1.6, 8.9, 3.2), GUN, { shade: band(8.4, 1.6, 8.9, 3.2) });
    a.shape(rr(5.2, .8, 5.6, 1.2, .5), WHEEL, { shade: band(5.2, 1.6, 10.8, 2) });
  }

  // 3. The bottle: a long gas bottle in oxblood, lying across its box, its shoulder striped black and yellow, a brass valve
  // with a torn hose, rime at the neck.
  function bottle(a) {
    const T = turned(16, 16.1, -Math.PI / 4);
    const R = 5.4, outline = [[-14.8, -R], [8.6, -R], [11.4, -R * .9], [13.4, -R * .7], [14.6, -R * .44], [15.1, -1.5], [15.1, 1.5], [14.6, R * .44], [13.4, R * .7], [11.4, R * .9], [8.6, R], [-14.8, R], [-15.6, R * .82], [-16, R * .42], [-16, -R * .42], [-15.6, -R * .82]];
    const body = P(outline.map(([u, v]) => T(u, v)));
    a.shape(body, OX, { lite: strip(T, -17, 16, -6, -3.4), shade: strip(T, -17, 16, 2.6, 6), deep: .9 });
    // the shoulder painted yellow, black bands slanting across it
    const shoulder = strip(T, 8.2, 16, -6, 6);
    a.flat(shoulder, STRIPE[0], { clip: body });
    a.flat(strip(T, 8.2, 16, 2.6, 6), '#7a6224', { clip: body });
    a.flat(strip(T, 8.2, 16, -6, -3.6), '#cfae58', { clip: body });
    for (const u of [8.8, 11.8]) a.flat(P([T(u, -6), T(u + 1.4, -6), T(u + 2.8, 6), T(u + 1.4, 6)]), SOOT.base, { clip: body });
    a.line(`M${T(8.2, -R).join(' ')}L${T(8.2, R).join(' ')}`, { w: 1 });
    a.flat(strip(T, 2.6, 3.8, -6, 6), CREAM.base, { clip: body });
    a.flat(strip(T, 2.6, 3.8, 2.6, 6), CREAM.shade, { clip: body });
    // the rubber foot, a dent and a long scrape, rust at the foot
    a.shape(P([T(-16, -R - .3), T(-13, -R - .3), T(-13, R + .3), T(-16, R + .3)]), SOOT, { lite: strip(T, -16.2, -13, -6, -4.2), shade: strip(T, -16.2, -13, 3.4, 6) });
    a.flat(ell(...T(-5, -1.4), 2, 1.4), OX.shade, { clip: body }); a.line(`M${T(-6.8, -2.6).join(' ')}Q${T(-5, -3.4).join(' ')} ${T(-3.2, -2.4).join(' ')}`, { w: 1 });
    a.line(`M${T(-11, 1.6).join(' ')}L${T(1, 2.4).join(' ')}`, { w: .8 });
    rusted(a, ...T(-12.6, 3.4), 1.2, .9, body); rusted(a, ...T(6.4, -3.6), .8, .7, body);
    // the neck, the brass valve and its torn hose; rime where the gas escapes
    a.shape(strip(T, 15, 17.8, -1.6, 1.6), GUN, { lite: strip(T, 15, 17.8, -1.6, -.8), shade: strip(T, 15, 17.8, .8, 1.6) });
    for (const u of [15.8, 16.8]) a.line(`M${T(u, -1.6).join(' ')}L${T(u, 1.6).join(' ')}`, { w: .7 });
    a.shape(strip(T, 17.4, 19.6, -2.4, 2.4), COPPER, { lite: strip(T, 17.4, 19.6, -2.4, -1.4), shade: strip(T, 17.4, 19.6, 1.2, 2.4) });
    a.flat(ell(...T(18.5, 0), .7), SOOT.deep);
    a.tube(`M${T(18.5, 2.2).join(' ')}Q${T(18.4, 4.4).join(' ')} ${T(16.6, 5.4).join(' ')}`, 1.3, SOOT, { spec: false });
    a.flat(ell(...T(16.4, 5.5), .6, .5), '#7a5c44');
    a.flat(ell(...T(14.2, -2.2), 1.2, .8), FROST[1]); a.flat(ell(...T(14.6, 2.2), 1, .7), FROST[0]);
  }
  function bottleIcon(a) {
    const T = turned(7.8, 8.4, -Math.PI / 4);
    const body = P([[-7.8, -2.6], [4.6, -2.6], [6.4, -2.2], [7.6, -1], [7.6, 1], [6.4, 2.2], [4.6, 2.6], [-7.8, 2.6], [-8.4, 1.6], [-8.4, -1.6]].map(([u, v]) => T(u, v)));
    a.shape(body, OX, { lite: strip(T, -9, 8, -3, -1.6), shade: strip(T, -9, 8, 1.2, 3), deep: .5 });
    a.flat(strip(T, 4, 8, -3, 3), STRIPE[0], { clip: body });
    a.flat(strip(T, 4, 8, 1.2, 3), '#7a6224', { clip: body });
    a.flat(P([T(4.8, -3), T(5.6, -3), T(6.6, 3), T(5.8, 3)]), SOOT.base, { clip: body });
    a.line(`M${T(4, -2.6).join(' ')}L${T(4, 2.6).join(' ')}`, { w: .8 });
    a.shape(P([T(-8.4, -2.7), T(-6.8, -2.7), T(-6.8, 2.7), T(-8.4, 2.7)]), SOOT, { shade: strip(T, -8.4, -6.8, 1.6, 3) });
    a.shape(strip(T, 7.4, 9.6, -1.4, 1.4), COPPER, { shade: strip(T, 7.4, 9.6, .7, 1.4) });
  }

  // 4. The pair: two stubby fuel cells in hazard yellow lashed with worn webbing, a crossover pipe with its gauge in the red,
  // a hazard band on each, one lashing torn loose.
  function pair(a) {
    a.tube('M10.4 27.4Q10 29.6 8.4 30.8', 1.6, WEB, { spec: false });
    a.tube('M27.2 9.4Q30.4 9.6 30.4 13L30.4 18Q30.4 21.4 28 21.6', 1.3, GUN, { spec: false });
    const cell = (x, y, w, h) => {
      const r = h / 2, d = rr(x, y, w, h, r);
      a.shape(d, OLD, { lite: band(x, y, x + w, y + 1.2), shade: band(x, y + h - 2, x + w, y + h), deep: .7 });
      a.shape(band(x + r, y, x + w - r, y + h), PAINT, { outer: false, lite: band(x + r, y, x + w - r, y + 1.4), shade: band(x + r, y + h - 2.4, x + w - r, y + h), deep: band(x + r, y + h - .8, x + w - r, y + h) });
      a.line(`M${x + r} ${y + .4}L${x + r} ${y + h - .4}`, { w: 1 }); a.line(`M${x + w - r} ${y + .4}L${x + w - r} ${y + h - .4}`, { w: 1 });
      hazard(a, x + w - r - 4.6, y, 3.2, h, { step: 2.4, clip: band(x + w - r - 4.6, y + .2, x + w - r - 1.4, y + h - .2) });
      return d;
    };
    const top = cell(2.6, 4.4, 25.6, 10.4), low = cell(3.6, 16.2, 25.4, 10.8);
    // the lashings and their buckles
    for (const x of [9.2, 19.4]) {
      a.shape(rr(x, 3.6, 2.4, 24.2, .6), WEB, { lite: band(x, 3.6, x + .6, 28), shade: band(x + 1.8, 3.6, x + 2.4, 28), lines: [`M${x + 1.2} 5L${x + 1.2} 27`] });
      a.shape(rr(x - .5, 14, 3.4, 2.8, .4), PLATE, { shade: band(x + 2.2, 14, x + 2.9, 16.8) }); a.flat(band(x + .2, 15, x + 2.2, 15.8), SOOT.deep);
    }
    gauge(a, 30.2, 15.6, 1.7);
    a.flat(ell(25, 8.6, 2, 1.3), PAINT.shade, { clip: top }); a.line('M23.2 7.8Q25 7 26.8 8', { w: 1 });
    rusted(a, 12.2, 24.8, 1, .8, low); rusted(a, 8.6, 6, .8, .7, top); rusted(a, 22.4, 13.2, .9, .6, top);
    rv(a, [4.4, 9.6], [5.4, 21.6]);
  }
  function pairIcon(a) {
    a.tube('M13.6 4.6Q15.2 4.6 15.2 6.4L15.2 9.4Q15.2 11.2 13.6 11.2', .9, GUN, { spec: false });
    for (const [x, y, w, h] of [[1, 1.8, 13.4, 5.4], [1.6, 8.4, 13.2, 5.6]]) {
      const r = h / 2;
      a.shape(rr(x, y, w, h, r), OLD, { shade: band(x, y + h - 1, x + w, y + h) });
      a.shape(band(x + r, y, x + w - r, y + h), PAINT, { outer: false, lite: band(x + r, y, x + w - r, y + .8), shade: band(x + r, y + h - 1.3, x + w - r, y + h) });
      hazard(a, x + w - r - 2.4, y, 1.8, h, { step: 1.6, clip: band(x + w - r - 2.4, y + .1, x + w - r - .6, y + h - .1) });
    }
    for (const x of [4.6, 9.8]) a.shape(rr(x, 1.4, 1.3, 13, .3), WEB, { shade: band(x + .9, 1.4, x + 1.3, 14.4) });
  }

  // 5. The service cell: an armoured fuel cell off a dead fleet tender, olive drab, corner guards striped black and yellow,
  // the hazmat diamond, a carry handle, a brass coupling with a torn hose and a red warning lamp still blinking.
  function cellBox(a) {
    a.tube('M10.4 4.6L10.4 2.2L17.6 2.2L17.6 4.6', 1.2, GUN, { spec: false });
    a.tube('M29.4 12.2Q31.2 12.4 31 15.2', 1.4, SOOT, { spec: false }); a.flat(ell(31, 15.4, .7, .5), '#7a5c44');
    const d = plate(a, [[6.4, 4], [25.6, 4], [29, 7.4], [29, 25.6], [25.6, 29], [6.4, 29], [3, 25.6], [3, 7.4]], ARMY, { l: 1.2, b: 1.6, e: 1.4 });
    // the corner guards, two striped
    for (const [pts, striped] of [[[[3, 7.4], [6.4, 4], [10.6, 4], [3, 11.6]], 1], [[[29, 25.6], [25.6, 29], [21.4, 29], [29, 21.4]], 1], [[[25.6, 4], [29, 7.4], [29, 9.6], [23.4, 4]], 0], [[[3, 25.6], [6.4, 29], [8.6, 29], [3, 23.4]], 0]]) {
      const g = P(pts);
      a.shape(g, GUN, { outer: false, shade: band(0, 26, 32, 32) });
      if (striped) hazard(a, 2, 3, 28, 27, { step: 2.6, bare: true, clip: g });
    }
    // the recessed face, its diamond; the ribs down its sides
    a.shape(rr(8.4, 8.6, 15.2, 15.4, 1), ARMY, { outer: false, shade: [band(8.4, 8.6, 23.6, 9.6), band(8.4, 8.6, 9.4, 24)], lite: band(8.4, 23, 23.6, 24) });
    diamond(a, 16, 16.2, 4.6);
    for (const x of [5.4, 26.6]) a.line(`M${x} 11L${x} 22`, { w: 1.1 });
    // the coupling on the right flank, the lamp down at the left
    a.shape(rr(27.6, 10.4, 2.6, 3.6, .5), COPPER, { lite: band(27.6, 10.4, 30.2, 11), shade: band(29.4, 10.4, 30.2, 14) });
    a.shape(ell(6.6, 24.8, 1.9), GUN, { shade: .4 });
    a.shape(ell(6.6, 24.8, 1.2), { base: '#5a1c18', shade: '#3c1210', deep: '#2a0c0a', lite: '#7a2a22', ink: '#120808' }, { outer: false, spec: ell(6.2, 24.4, .4) });
    rusted(a, 24.6, 27.4, 1.2, .8, d); rusted(a, 4.6, 14.6, .8, 1.2, d); rusted(a, 21, 5.4, 1, .6, d);
    rv(a, [8.4, 6], [23.6, 6], [8.4, 27], [23.6, 27]);
  }
  function cellBoxIcon(a) {
    a.tube('M5.6 2.6L5.6 1.2L10.4 1.2L10.4 2.6', .8, GUN, { spec: false });
    const d = plate(a, [[3.4, 2.4], [12.6, 2.4], [14.4, 4.2], [14.4, 13], [12.6, 14.8], [3.4, 14.8], [1.6, 13], [1.6, 4.2]], ARMY, { l: .7, b: .9, e: .8 });
    for (const pts of [[[1.6, 4.2], [3.4, 2.4], [5.6, 2.4], [1.6, 6.4]], [[14.4, 13], [12.6, 14.8], [10.4, 14.8], [14.4, 10.6]]]) { const g = P(pts); a.shape(g, GUN, { outer: false }); hazard(a, 1, 2, 14, 13, { step: 1.6, bare: true, clip: g }); }
    diamond(a, 8, 8.6, 3.4);
    a.shape(ell(3.6, 12.4, .9), { base: '#8a2a20', shade: '#5a1c18', lite: '#b04030' }, { outer: false });
    rusted(a, 12, 13.6, .7, .5, d);
  }

  return [
    { id: 'drum', name: 'Бочка', idea: 'Приземистая стальная бочка в облезлой красной краске: обручи, полоса опасности по поясу, из пробки сочится топливо.', draw: drum, icon: drumIcon },
    { id: 'sphere', name: 'Шар под давлением', idea: 'Шар-баллон, сорванный с крепления: серая эмаль, красный пояс, ромб «огнеопасно», вентиль с манометром в красном, иней у горла. Клапан травит газ.', draw: sphere, icon: sphereIcon,
      hiss: { at: [17.6, 3.2], dir: [.55, -.83] } },
    { id: 'bottle', name: 'Длинный баллон', idea: 'Длинный баллон цвета запёкшейся крови лежит наискосок; плечо в чёрно-жёлтую полосу, латунный вентиль с оборванным шлангом, иней у горла.', draw: bottle, icon: bottleIcon },
    { id: 'pair', name: 'Связка ячеек', idea: 'Две короткие топливные ячейки цвета опасности, стянутые ремнями; переходная труба с манометром в красном, одна стяжка оборвана.', draw: pair, icon: pairIcon },
    { id: 'tender', name: 'Ячейка заправщика', idea: 'Бронированная топливная ячейка с заправщика мёртвого флота: хаки, углы в полосу, ромб «огнеопасно», латунная муфта, красная лампа ещё мигает.', draw: cellBox, icon: cellBoxIcon,
      lamp: { at: [6.6, 24.8], r: 1.2 } },
  ];
}

function page() {
  const CANDS = drums(ART);
  // ---------- the game's numbers: features/debris/DebrisView.cs, a cell 8 world px drawn ×4 ----------
  const PIC = 32 * Z, ICON = 16 * 4, ROCK = .08, ROCK_S = 7, JOLT_S = .22, JOLT_PX = 3, JOLT_TURN = .07, FLARE = .16, FLARE_DECAY = .84;
  const DRIFT = 2 * Z;   // px a second: a slow stream past the ship
  const LOOP = 9, HITS = [4.6, 5.5], BURST = 6.4, BACK = 7.8;
  const SCENE_W = 700, START = [540, 292], HERO = [214, 410];
  const LAMP = [232, 62, 44];

  // ---------- one clock for every tile, a moment from the address: #at=6.4&pause ----------
  let clock = 0, frameSeen = null;
  function open(address) {
    for (const part of String(address || '').replace(/^#/, '').split('&')) {
      const [key, v] = part.split('=');
      if (key === 'pause') OPT.playing = false;
      else if (key === 'at' && v && isFinite(+v) && +v >= 0) { clock = +v; dirtyAll(); }
    }
  }
  if (typeof location !== 'undefined') open(location.hash);
  function tick(dt) { if (frameSeen === last) return; frameSeen = last; clock += dt; }

  // ---------- the drum's life in the loop: drift, two hits, the burst, back ----------
  // it comes back faded in where it started, as a new drum would drift into view; with «только дрейф» it never bursts
  function lifeAt(t) {
    const u = ((t % LOOP) + LOOP) % LOOP, drift = OPT.loop === 'drift';
    const shown = drift || u < BURST;
    let jolt = 0, flare = 0, hit = -1;
    if (!drift) for (let i = 0; i < HITS.length; i++) {
      const s = u - HITS[i];
      if (s >= 0 && s < JOLT_S) { const k = 1 - s / JOLT_S; jolt = Math.max(jolt, k * k); hit = i; }
      if (s >= 0 && u < BURST) flare = Math.max(flare, Math.pow(FLARE_DECAY, s * 60));
    }
    const since = u >= BACK ? u - BACK : u + (LOOP - BACK);
    const alpha = drift ? Math.min(sm(since / .5), sm((LOOP - since) / .5)) : sm(since / .5);
    const x = START[0] - DRIFT * since, y = START[1];
    const turn = ROCK * Math.sin(t / ROCK_S * 2 * Math.PI) + JOLT_TURN * jolt;
    const burst = !drift && u >= BURST && u < BURST + .7 ? (u - BURST) / .7 : -1;
    return { u, shown, alpha, x: x + JOLT_PX * Z * jolt, y, turn, jolt, flare: shown && flare > .02 ? flare : 0, hit, burst };
  }
  const burstAt = () => [START[0] - DRIFT * (BURST - BACK + LOOP), START[1]];

  // ---------- drawing ----------
  const text = (c, s, x, y, size = 13, col = 'rgba(214,226,240,.7)', align = 'left') => { c.font = `${size}px system-ui,"Segoe UI",sans-serif`; c.textAlign = align; c.textBaseline = 'alphabetic'; c.fillStyle = col; c.fillText(s, x, y); c.textAlign = 'left'; };
  function sky(c, x0, x1) {
    const g = c.createLinearGradient(0, 0, 0, LH); g.addColorStop(0, '#04060a'); g.addColorStop(1, '#0a0f17');
    c.fillStyle = g; c.fillRect(x0, 0, x1 - x0, LH);
    for (const [x, y, sz, a] of STARS) { const xx = x - 300; if (xx < x0 || xx > x1 - 2) continue; c.fillStyle = `rgba(220,228,240,${a.toFixed(3)})`; c.fillRect(xx, y + 40, sz, sz); }
  }
  // a piece drifting by: the game's own picture, a wreck rocking, a small piece tumbling
  function piece(c, key, x, y, turn) {
    const im = IMG[key], [w, h] = SIZES[key];
    if (!ready(im)) return;
    c.save(); c.translate(x, y); c.rotate(turn); c.drawImage(im, -w / 2, -h / 2, w, h); c.restore();
  }
  const wrap = (x, a, b) => a + ((((x - a) % (b - a)) + (b - a)) % (b - a));
  function drawStream(c, t) {
    piece(c, 'tanks', wrap(640 - DRIFT * t, -90, SCENE_W + 90), 104, ROCK * Math.sin(t / ROCK_S * 2 * Math.PI + 3));
    piece(c, 'scrap', wrap(420 - DRIFT * t, -60, SCENE_W + 60), 470, t / 4 * -1 + 2);
    piece(c, 'crate', wrap(120 - DRIFT * t, -60, SCENE_W + 60), 150, t / 4 + 5);
  }
  // a puff of the gas the sphere's valve lets out, in the drum's own world px, turning with it
  function hiss(c, mark, t) {
    const life = 1.2, n = 5;
    for (let i = 0; i < n; i++) {
      const age = ((t + i * life / n) % life + life) % life, k = age / life, far = 6 * Math.sqrt(k);
      const p = [mark.at[0] + mark.dir[0] * far, mark.at[1] + mark.dir[1] * far];
      halo(c, p[0] * Z, p[1] * Z, (1 + k * 2.6) * Z, [206, 218, 224], .3 * (1 - k));
    }
  }
  // the tender's lamp: lit a quarter second in every 1.6, as a contraband crate's lamp is drawn
  function lamp(c, mark, t) {
    if (((t % 1.6) + 1.6) % 1.6 > .28) return;
    const x = mark.at[0] * Z, y = mark.at[1] * Z, r = mark.r * Z;
    c.beginPath(); c.arc(x, y, 6.7 * r, 0, 6.283); c.fillStyle = css(LAMP, .12); c.fill();
    c.beginPath(); c.arc(x, y, 3.4 * r, 0, 6.283); c.fillStyle = css(LAMP, .24); c.fill();
    c.beginPath(); c.arc(x, y, r, 0, 6.283); c.fillStyle = '#e0503a'; c.fill();
    c.beginPath(); c.arc(x - .15 * r, y - .17 * r, .45 * r, 0, 6.283); c.fillStyle = '#ffd0b8'; c.fill();
  }
  // the drum at its place, as DebrisView draws a wreck: its picture 1:1, rocking, jolted and flaring when struck
  function drawDrum(T, c, L, t) {
    const im = T.art.drum, cand = T.cand;
    c.save(); c.translate(L.x, L.y); c.rotate(L.turn); c.globalAlpha = L.alpha;
    c.drawImage(im, -PIC / 2, -PIC / 2, PIC, PIC);
    T.drawn = { w: PIC, turn: L.turn, t };
    if (L.flare) { c.globalCompositeOperation = 'lighter'; c.globalAlpha = FLARE * L.flare; c.drawImage(im, -PIC / 2, -PIC / 2, PIC, PIC); c.globalCompositeOperation = 'source-over'; c.globalAlpha = L.alpha; }
    c.translate(-PIC / 2, -PIC / 2);
    if (cand.hiss) hiss(c, cand.hiss, t);
    if (cand.lamp) lamp(c, cand.lamp, t);
    c.restore();
    // the sparks of a hit, on the side facing the hero
    if (L.hit >= 0 && L.jolt > .3) for (let i = 0; i < 5; i++) {
      const a = Math.PI + (i - 2) * .38, r0 = PIC * .42, r1 = r0 + 26 * (1.2 - L.jolt);
      c.strokeStyle = css([255, 214, 150], L.jolt); c.lineWidth = 2; c.beginPath();
      c.moveTo(L.x + Math.cos(a) * r0, L.y + Math.sin(a) * r0 * .6); c.lineTo(L.x + Math.cos(a) * r1, L.y + Math.sin(a) * r1 * .6); c.stroke();
    }
  }
  // the burst: only a flash to frame it; the game draws a blasting charge's burst here
  function drawBurst(c, L) {
    const k = L.burst, [x, y] = burstAt();
    halo(c, x, y, 60 + 200 * sm(k * 1.6), [255, 206, 150], .9 * (1 - k));
    if (k < .2) halo(c, x, y, 70, [255, 250, 236], 1 - k / .2);
  }
  function drawHeroAt(T, t) {
    const h = T.hero, bob = Math.sin(t * 1.1) * 3;
    h.x = HERO[0]; h.y = HERO[1] + bob; h.face = 1; h.air = true; h.amp = 0;
    const n = toS(h, NECK()), L = lifeAt(t), aim = L.shown ? [L.x, L.y] : burstAt();
    h.look = headTurn(aim[0] - n[0], aim[1] - n[1]);
    h.P = poseOf(h);
    drawHero(T.c, h);
    const d = toS(h, [-9, -44]);
    drawDrone(T.c, d[0], d[1] + Math.sin(t * 1.7 + 1.3) * 1.5, Math.atan2(aim[1] - d[1], aim[0] - d[0]), 0, t + 1.3, DSC);
  }
  function drawPanel(T, c) {
    const x0 = SCENE_W;
    c.fillStyle = '#0b0e14'; c.fillRect(x0, 0, LW - x0, LH);
    sky(c, x0 + 14, LW - 14);
    c.fillStyle = 'rgba(8,10,15,.55)'; c.fillRect(x0 + 14, 0, LW - x0 - 28, LH);
    text(c, 'крупнее ×2', x0 + 22, 30, 13, '#8f8b82');
    c.drawImage(T.art.drum, x0 + 22, 40, PIC * 2, PIC * 2);
    if (T.cand.lamp) { c.save(); c.translate(x0 + 22, 40); c.scale(2, 2); lamp(c, T.cand.lamp, clock); c.restore(); }
    text(c, 'значок 1:1 и в сумке', x0 + 22, 344, 13, '#8f8b82');
    c.drawImage(T.art.icon, x0 + 22, 356, ICON, ICON);
    rr(c, x0 + 112.5, 368.5, 42, 42, 2); c.fillStyle = 'rgba(20,32,50,.78)'; c.fill(); c.strokeStyle = css([140, 196, 255], .36); c.lineWidth = 1; c.stroke();
    c.drawImage(T.art.icon, x0 + 114, 370, 40, 40);
    const moves = T.cand.hiss ? 'движется: газ из клапана (код)' : T.cand.lamp ? 'движется: мигает лампа (код)' : 'движется: только качание';
    text(c, moves, x0 + 22, 450, 13, '#c9b27a');
    text(c, 'трещины и осколки рисует игра', x0 + 22, 472, 13, '#8f8b82');
  }

  const cands = CANDS.map((k) => ({ id: k.id, name: k.name, idea: k.idea, time: `Круг ${LOOP} с: дрейфует и качается, два удара, вспышка вместо взрыва заряда, снова.`, hiss: k.hiss, lamp: k.lamp, draw: k.draw, icon: k.icon }));
  return {
    cands,
    expose: { lifeAt, LOOP, BURST, BACK, HITS, ROCK, JOLT_TURN, PIC, ICON, open, clock: () => clock },
    init(T) {
      // the drum and its icon baked once, by the engine and finish that export the game's pictures
      T.art = { drum: ART.Anime.bake({ w: 32, h: 32, draw: T.cand.draw }), icon: ART.Anime.bake({ w: 16, h: 16, light: [-0.6, -0.8], draw: T.cand.icon }) };
      T.hero = newHero('me', HERO[0], 1.3); T.drawn = null;
      T.cv.style.touchAction = 'pan-y';
    },
    scene(T, dt) {
      tick(dt);
      T.time = clock;
      const c = T.c, t = clock, L = lifeAt(t);
      c.setTransform(T.S, 0, 0, T.S, 0, 0); c.globalAlpha = 1; c.globalCompositeOperation = 'source-over';
      c.save(); c.beginPath(); c.rect(0, 0, SCENE_W, LH); c.clip();
      sky(c, 0, SCENE_W);
      drawStream(c, t);
      drawHeroAt(T, t);
      T.drawn = null;
      if (L.shown) drawDrum(T, c, L, t);
      if (L.burst >= 0) { drawBurst(c, L); T.burstSeen = L.burst; } else T.burstSeen = -1;
      text(c, '×1, как в игре: клетка 8 px, бочка 4 × 4 клетки', 18, LH - 16, 13, '#8f8b82');
      c.restore();
      drawPanel(T, c);
    },
  };
}

module.exports = {
  title: 'Топливная бочка',
  heading: 'Топливная бочка: как она выглядит',
  window: false,
  tile: [1000, 540],
  kitBars: ['speedMode', 'playMode'],
  lead: `<p class="lead">Новый обломок в потоке: <b>бочка с топливом</b>. Разбил — взрывается, раня всех рядом, и тебя тоже. Она должна читаться опасной ещё до удара, без слов: цветом, формой, знаком. Пять разных вещей, каждая 4 × 4 клетки, рядом с героем в размере игры; справа она же крупнее и её значок в сумке.</p>
<p class="lead">Нарисованы тем же кодом и тушью с акварелью, что и обломки игры: выбранная уйдёт в игру как есть. Вверху слева проплывает связка кислородных баллонов, на неё бочка походить не должна.</p>
<p class="miss">Не показано: трещины от ударов и осколки (их рисует игра), сам взрыв (в игре это взрыв заряда, здесь только вспышка), свет игры и звук.</p>`,
  toggles: [{ id: 'loopMode', bar: 1, label: 'Круг:', key: 'loop', opts: [['дрейф, удары, взрыв', 'all'], ['только дрейф', 'drift']] }],
  pick: { doc: 'picks/fuelDrum', subject: 'Топливная бочка', what: ['бочку', 'бочкой'] },
  pictures: PICTURES,
  // the engine, the finish and the debris kit go in before the page, then the drawings: never copied by hand
  page: { toString: () => `function () {\n${ART_SRC}\nconst SIZES = ${JSON.stringify(SIZES)};\n${drums.toString()}\nreturn (${page.toString()})();\n}` },
  drums, ART_SRC,

  // build.js --break <name>: each breaks one rule of check(K)
  breaks: {
    scale: [['PIC = 32 * Z', 'PIC = 32 * 3']],
    tumble: [['const turn = ROCK * Math.sin(t / ROCK_S * 2 * Math.PI)', 'const turn = t / 4']],
    twice: [['const shown = drift || u < BURST;', 'const shown = drift || u < BURST || u > BURST + 1;']],
  },

  check(K) {
    const X = K.X, FPS = 60, end = Math.round((X.LOOP * 2 + .5) * FPS);
    let ok = true;
    const need = (good, words) => { if (!good) { ok = false; K.fail(words); } };
    const ids = ['drum', 'sphere', 'bottle', 'pair', 'tender'];
    need(K.tiles.map((T) => T.cand.id).join() === ids.join(), 'five tiles in order');
    for (const T of K.tiles) {
      need(T.art.drum.width === 128 && T.art.drum.height === 128, `${T.cand.id}: the drum is baked 128 px, 32 world px at the game's 4 (${T.art.drum.width})`);
      need(T.art.icon.width === 64 && T.art.icon.height === 64, `${T.cand.id}: the icon is baked 64 px (${T.art.icon.width})`);
    }
    // every frame: drawn 1:1, rocking no further than the game's, gone from the burst till it comes back, one burst a loop
    let turned = 0, bursts = 0, wasBurst = false, shownInBurst = 0;
    K.run(end, (T, f) => {
      if (T !== K.tiles[0]) return;
      const t = X.clock(), L = X.lifeAt(t);
      if (T.drawn) {
        if (T.drawn.w !== 128) need(false, `drawn ${T.drawn.w} px wide, not 128`);
        turned = Math.max(turned, Math.abs(T.drawn.turn));
        if (L.u >= X.BURST && L.u < X.BACK) shownInBurst++;
      }
      const b = T.burstSeen >= 0;
      if (b && !wasBurst) bursts++;
      wasBurst = b;
    });
    need(turned > .02 && turned <= X.ROCK + X.JOLT_TURN + 1e-6, `it rocks within the game's ${X.ROCK} and a jolt's ${X.JOLT_TURN} (${turned.toFixed(3)})`);
    need(bursts === 2, `one burst a loop (${bursts} in two)`);
    need(shownInBurst === 0, `gone from its burst till it comes back (${shownInBurst} frames shown)`);
    K.click(2, '.pick');
    const p = K.picked();
    need(p && p.pick === 'bottle', `a click on a tile picks it (${JSON.stringify(p)})`);
    console.log(`  turned at most ${turned.toFixed(3)} rad, ${bursts} bursts in ${(end / FPS).toFixed(1)} s`);
    return ok;
  },
};
