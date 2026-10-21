// one-off: five-cell rooms, wall pieces moved down to suit, the rock above cropped
const fs = require('fs'), f = __dirname + '/scenes.js';
let s = fs.readFileSync(f, 'utf8');
const rep = (a, b) => { if (!s.includes(a)) throw new Error('miss ' + a); s = s.split(a).join(b); };
rep('FLOOR = 10 * C, CEIL = 4 * C, H = 13', 'FLOOR = 10 * C, CEIL = 5 * C, H = 13, CROP = 2 * C');
rep('rows 4..9 (six cells, 96 px: low and close)', 'rows 5..9 (five cells, 80 px: low and close)');
rep('r.carve(x0, 4, x1 - x0 + 1, 6,', 'r.carve(x0, 5, x1 - x0 + 1, 5,');
rep("r.fill(x0 - 1, 3, x1 - x0 + 3, 1, 'alloy')", "r.fill(x0 - 1, 4, x1 - x0 + 3, 1, 'alloy')");
rep("r.fill(x0 - 1, 4, 1, 6, 'alloy'); r.fill(x1 + 1, 4, 1, 6, 'alloy')", "r.fill(x0 - 1, 5, 1, 5, 'alloy'); r.fill(x1 + 1, 5, 1, 5, 'alloy')");
rep("r.fill(19, 4, 1, 1, 'alloy')", "r.fill(19, 5, 1, 1, 'alloy')");
const ys = [["coatPegs', 76, 70", "coatPegs', 76, 86"], ["lightWindow', 100, 68", "lightWindow', 100, 86"], ["frame', 206, 74", "frame', 206, 90"],
  ["holoPortrait', 236, 76", "holoPortrait', 234, 88"], ["banner', 262, 66", "banner', 262, 84"], ["clock', 282, 70", "clock', 282, 88"],
  ["utensils', 400, 74", "utensils', 400, 88"], ["shelfJars', 434, 92", "shelfJars', 434, 100"], ["sconce', 466, 74", "sconce', 466, 92"], ["wallShelf', 500, 74", "wallShelf', 500, 90"],
  ["sconce', 80, 84", "sconce', 80, 94"], ["tapestry', 94, 70", "tapestry', 94, 86"], ["lightWindow', 186, 70", "lightWindow', 186, 86"], ["drawings', 302, 108", "drawings', 302, 112"],
  ["holoPortrait', 356, 80", "holoPortrait', 354, 90"], ["wallShelf', 398, 74", "wallShelf', 398, 92"], ["banner', 452, 68", "banner', 452, 84"],
  ["wallShelf', 92, 76", "wallShelf', 92, 92"], ["toolRack', 144, 74", "toolRack', 144, 90"], ["mural', 182, 72", "mural', 182, 84"], ["sconce', 290, 82", "sconce', 290, 94"], ["crack', 332, 70", "crack', 332, 86"]];
for (const [a, b] of ys) rep(a, b);
rep('const mound = Math.max(0, Math.floor((x - 26) * 0.75)), sag = Math.max(0, Math.floor((x - 27) * 0.45));', 'const mound = Math.max(0, Math.floor((x - 26) * 0.6)), sag = Math.max(0, Math.floor((x - 27) * 0.4));');
rep('for (let y = 4; y < 4 + sag; y++)', 'for (let y = 5; y < 5 + sag; y++)');
rep("for (let y = 4; y <= 9; y++) if (!r.F[r.i(x, y)]) r.B[r.i(x, y)] = 'rock'; r.F[r.i(x, 3)] = 'rock';", "for (let y = 5; y <= 9; y++) if (!r.F[r.i(x, y)]) r.B[r.i(x, y)] = 'rock'; r.F[r.i(x, 4)] = 'rock';");
rep('O.glowcaps(p, 476, CEIL + 2 * C - 2, O.PAL.teal, 5, 2,', 'O.glowcaps(p, 476, ceilAt(r, 476), O.PAL.teal, 5, 2,');
rep('O.roots(p, 468, CEIL + 8, 22, 4, O.PAL.violet);', 'O.roots(p, 500, ceilAt(r, 500), 18, 4, O.PAL.violet);');
rep('const surface = (r, px) =>', 'const ceilAt = (r, px) => { let y = FLOOR - 4; while (y > 0 && !r.sp(px, y - 1)) y--; return y; };\nconst surface = (r, px) =>');
rep("const img = lightUp(p, { amb: mood === 'lit' ? 0.05 : 0.03, bloom: 0.85 });",
  "const full = lightUp(p, { amb: mood === 'lit' ? 0.05 : 0.03, bloom: 0.85 }), img = P.create(full.w, full.h - CROP);\n" +
  '  full.data.copy(img.data, 0, CROP * full.w * 4);\n' +
  '  const sh = (pt) => (Array.isArray(pt) ? [pt[0], pt[1] - CROP] : pt);\n' +
  '  for (const e of p.emitters) { if (e.a) e.a = sh(e.a); if (e.b) e.b = sh(e.b); if (e.y != null) e.y -= CROP; if (e.y0 != null) { e.y0 -= CROP; e.y1 -= CROP; } if (e.pts) e.pts = e.pts.map(sh); if (e.to != null) e.to -= CROP; }');
fs.writeFileSync(f, s);
