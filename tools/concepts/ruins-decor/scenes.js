// Three homes of the arc makers cut in the rock, each in two moods: 'lit' (their lamps still burn) and 'dim' (the last
// lights, mostly the helmet). Writes out/scene-<id>-<mood>.png (1x and @2x) and out/scenes.json (size, emitters).
const fs = require('fs'), path = require('path');
const E = require('../../planet-art/caves/engine');
const O = require('../../planet-art/caves/objects');
const { C, P, Pic, lightUp } = E;
const { Room, paint } = require('./room');
const D = require('./decor');
const { put } = D;

const OUT = path.join(__dirname, 'out'); fs.mkdirSync(OUT, { recursive: true });
const FLOOR = 10 * C, CEIL = 5 * C, H = 13, CROP = 2 * C;

// a room x0..x1 (cells), rows 5..9 (five cells, 80 px: low and close), alloy floor and ceiling, plaster over an alloy
// wainscot, and a rock tunnel coming in at the left
function shell(W, x0, x1) {
  const r = new Room(W, H);
  r.carve(x0, 5, x1 - x0 + 1, 5, (x, y) => (y === 9 ? 'alloy' : 'plaster'));
  r.fill(x0 - 1, 4, x1 - x0 + 3, 1, 'alloy'); r.fill(x0 - 1, 10, x1 - x0 + 3, 1, 'alloy');
  r.fill(x0 - 1, 5, 1, 5, 'alloy'); r.fill(x1 + 1, 5, 1, 5, 'alloy');
  r.carve(0, 7, x0, 3, 'rock'); r.carve(x0 - 1, 7, 1, 3, 'alloy');
  return r;
}
const surface = (r, px) => { let y = r.PH - 1; while (y > 0 && r.sp(px, y)) y--; return y + 1; };
const ceilAt = (r, px) => { let y = surface(r, px) - 2; while (y > 0 && !r.sp(px, y - 1)) y--; return y; };

const SCENES = {
  // the hearth room: the family's table with a meal left on it, the kitchen through an arch
  hearth(mood) {
    const r = shell(36, 4, 33);
    r.fill(19, 5, 1, 1, 'alloy');
    r.shapes();
    const p = new Pic(r); paint(p);
    const on = mood === 'lit', lit = (keep) => ({ lit: on || !!keep });
    put(p, 'cobweb', 65, CEIL);
    put(p, 'heightMarks', 66, 120);
    put(p, 'coatPegs', 76, 86);
    put(p, 'basket', 74, FLOOR);
    put(p, 'lightWindow', 100, 86, lit());
    put(p, 'bench', 98, FLOOR);
    put(p, 'rug', 138, FLOOR);
    put(p, 'chair', 140, FLOOR);
    put(p, 'table', 154, FLOOR);
    put(p, 'dishes', 156, FLOOR - 17);
    put(p, 'arcCandle', 180, FLOOR - 17, lit(true));
    put(p, 'chair', 188, FLOOR, { flip: true });
    put(p, 'ceilLamp', 163, CEIL, lit());
    put(p, 'frame', 206, 90);
    put(p, 'stool', 210, FLOOR);
    put(p, 'holoPortrait', 234, 88, lit(true));
    put(p, 'plantAlive', 230, FLOOR);
    put(p, 'banner', 262, 84);
    put(p, 'clock', 282, 88);
    put(p, 'pots', 276, FLOOR);
    // the kitchen
    put(p, 'herbs', 324, CEIL); put(p, 'herbs', 338, CEIL);
    put(p, 'hearth', 358, FLOOR, { ...lit(true), flue: FLOOR - 50 - CEIL });
    put(p, 'utensils', 400, 88);
    put(p, 'shelfJars', 434, 100, lit(true));
    put(p, 'stool', 404, FLOOR);
    put(p, 'kettle', 403, FLOOR - 11, lit());
    put(p, 'sconce', 466, 92, lit());
    put(p, 'basin', 472, FLOOR);
    put(p, 'wallShelf', 500, 90);
    put(p, 'pots', 516, FLOOR);
    const v = O.hero(p, 14, FLOOR); O.helmetBeam(p, v.x, v.y, 8);
    return p;
  },
  // the bedroom: the grown-ups' bed and wardrobe, a curtain, behind it the child's corner and the cradle
  bedroom(mood) {
    const r = shell(32, 3, 30);
    r.shapes();
    const p = new Pic(r); paint(p);
    const on = mood === 'lit', lit = (keep) => ({ lit: on || !!keep });
    put(p, 'cobweb', 49, CEIL);
    put(p, 'wardrobe', 52, FLOOR);
    put(p, 'sconce', 80, 94, lit());
    put(p, 'tapestry', 94, 86);
    put(p, 'bed', 92, FLOOR, lit(true));
    put(p, 'chest', 142, FLOOR, lit());
    put(p, 'floorLamp', 166, FLOOR, lit());
    put(p, 'lightWindow', 186, 86, lit());
    put(p, 'basket', 186, FLOOR);
    put(p, 'plantDead', 202, FLOOR);
    put(p, 'curtain', 252, CEIL);
    // the child's corner
    put(p, 'garland', 272, CEIL, lit(true));
    put(p, 'rug', 270, FLOOR);
    put(p, 'cradle', 278, FLOOR, lit());
    put(p, 'drawings', 302, 112);
    put(p, 'toys', 304, FLOOR, lit(true));
    put(p, 'stool', 336, FLOOR);
    put(p, 'musicBox', 335, FLOOR - 11, lit(true));
    put(p, 'holoPortrait', 354, 90, lit());
    put(p, 'chair', 380, FLOOR, { flip: true });
    put(p, 'wallShelf', 398, 92);
    put(p, 'plantAlive', 408, FLOOR);
    put(p, 'lantern', 436, FLOOR, lit());
    put(p, 'banner', 452, 84, { col: D.PAL.indigo });
    put(p, 'pots', 470, FLOOR);
    const v = O.hero(p, 222, FLOOR); O.helmetBeam(p, v.x, v.y, 14);
    return p;
  },
  // the study: scroll cases, a desk, a tool board, a resonator still talking to itself; the far end caved in, grown over
  study(mood) {
    const r = shell(36, 3, 34);
    for (let x = 26; x <= 34; x++) {
      const mound = Math.max(0, Math.floor((x - 26) * 0.6)), sag = Math.max(0, Math.floor((x - 27) * 0.4));
      for (let y = 10 - mound; y <= 9; y++) r.F[r.i(x, y)] = 'rock';
      for (let y = 5; y < 5 + sag; y++) r.F[r.i(x, y)] = 'rock';
      if (x >= 28) { for (let y = 5; y <= 9; y++) if (!r.F[r.i(x, y)]) r.B[r.i(x, y)] = 'rock'; r.F[r.i(x, 4)] = 'rock'; }
    }
    r.shapes();
    const p = new Pic(r); paint(p);
    const on = mood === 'lit', lit = (keep) => ({ lit: on || !!keep });
    put(p, 'cobweb', 49, CEIL);
    put(p, 'bookshelf', 52, FLOOR, lit(true));
    put(p, 'wallShelf', 92, 92);
    put(p, 'desk', 88, FLOOR, lit(true));
    put(p, 'chair', 126, FLOOR, { flip: true });
    put(p, 'toolRack', 144, 90);
    put(p, 'table', 144, FLOOR);
    put(p, 'lantern', 150, FLOOR - 17, lit());
    put(p, 'pots', 160, FLOOR - 17);
    put(p, 'mural', 182, 84, lit(true));
    put(p, 'stool', 196, FLOOR);
    put(p, 'brokenLamp', 222, CEIL, { lit: true });
    put(p, 'plantDead', 224, FLOOR);
    put(p, 'arcDevice', 256, FLOOR, lit());
    put(p, 'sconce', 290, 94, lit());
    put(p, 'chest', 300, FLOOR, lit());
    put(p, 'crack', 332, 86);
    put(p, 'fallenBeam', 340, FLOOR - 1);
    put(p, 'rubble', 394, FLOOR);
    p.mossMinY = CEIL + 6;
    O.moss(p, 400, 552, O.PAL.teal, 7, 0.75);
    O.glowcaps(p, 446, surface(r, 446), O.PAL.teal, 1, 4, { r: 4.4, k: 0.9, scale: 1.15 });
    O.glowcaps(p, 508, surface(r, 508), O.PAL.violet, 3, 3, { r: 3.6, k: 0.8 });
    O.glowcaps(p, 476, ceilAt(r, 476), O.PAL.teal, 5, 2, { hang: true, r: 3, k: 0.7 });
    O.threads(p, 420, CEIL, O.PAL.teal, 9, 4);
    O.roots(p, 500, ceilAt(r, 500), 18, 4, O.PAL.violet);
    const v = O.hero(p, 12, FLOOR); O.helmetBeam(p, v.x, v.y, 6);
    return p;
  },
};

const manifest = {};
const only = process.argv[2];
for (const [id, fn] of Object.entries(SCENES)) for (const mood of ['lit', 'dim']) {
  if (only && only !== id) continue;
  const p = fn(mood);
  // their lamps burn brighter than ours: every arc light a little wider and stronger
  for (const l of p.lights) if (l.col === D.ARC.light) { l.k *= 1.3; l.r *= 1.2; }
  const full = lightUp(p, { amb: mood === 'lit' ? 0.05 : 0.03, bloom: 0.85 }), img = P.create(full.w, full.h - CROP);
  full.data.copy(img.data, 0, CROP * full.w * 4);
  const sh = (pt) => (Array.isArray(pt) ? [pt[0], pt[1] - CROP] : pt);
  for (const e of p.emitters) { if (e.a) e.a = sh(e.a); if (e.b) e.b = sh(e.b); if (e.y != null) e.y -= CROP; if (e.y0 != null) { e.y0 -= CROP; e.y1 -= CROP; } if (e.pts) e.pts = e.pts.map(sh); if (e.to != null) e.to -= CROP; }
  P.encode(img, path.join(OUT, `scene-${id}-${mood}.png`)); P.encode(P.scale(img, 2), path.join(OUT, `scene-${id}-${mood}@2x.png`));
  manifest[`${id}-${mood}`] = { size: [img.w, img.h], emitters: p.emitters.map((e) => ({ ...e, col: e.col && (e.col.every((v) => v <= 1) ? e.col.map((v) => Math.round(v * 255)) : e.col) })) };
  console.log(id, mood, p.lights.length, 'lights', p.emitters.length, 'emitters');
}
const mf = path.join(OUT, 'scenes.json');
const prev = only && fs.existsSync(mf) ? JSON.parse(fs.readFileSync(mf, 'utf8')) : {};
fs.writeFileSync(mf, JSON.stringify({ ...prev, ...manifest }));
