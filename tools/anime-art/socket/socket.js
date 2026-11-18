// The helmet socket (docs/plans/helmet-socket.md): GPT paints a helmet's dome only; the rim, the seal and the glass are the
// basic helmet's, and this tool enforces them.
//   node socket.js template <out.png>                 the sheet GPT paints over: six basic helmets on their seal, 3 x 2 cells
//   node socket.js paint <dir> <id>...                one Codex call: <dir>/template.png repainted into <dir>/sheet.png
//   node socket.js cut <sheet.png> <id>... [--write]  cut by the grid, cropped to the zones, checked; --write puts it in the game
// Cells: the top-left is the untouched reference, then the ids in order (at most five).
const { decode, encode, bbox } = require('./png.js'), fs = require('fs'), path = require('path'), { spawn, execSync } = require('child_process');

const GAME = path.join(__dirname, '../../../game/art/hd');
/** The socket helmets: where the card goes, what GPT is asked to paint, and a glass tint for those not clear. */
const HELMETS = {
  heatedHelmet: { card: 'items', ask: 'angular riveted grey steel plates on a brass frame round the glass, a copper heating coil wound round its lower part' },
  heatHelmet: { card: 'items', ask: 'a helmet of pale ceramic tiles like a kiln, a pale rim round its glass', glass: [215, 150, 40] },
  dustHelmet: { card: 'skins', ask: 'a sand-coloured cloth wrap round the dome, a grey round filter canister at the front under the glass' },
  hideHelmet: { card: 'skins', ask: 'olive alien hide stretched over the dome, bone ribs curving over the top and back' },
  boilerHelmet: { card: 'skins', ask: 'a boxy welded grey steel helmet with a small heater box on the back and copper pipes' },
};

// The helmet picture: 76 x 72 hd px (the 13 x 12 box, 3 px round it, 4 hd px a world px). The sheet: 512 px cells, the helmet
// 6 times over at (28, 40) in each.
const W = 76, H = 72, K = 6, CELL = 512, OX = 28, OY = 40, NAVY = [6, 17, 50];
const RIM = 44, GROW = 4, SNOUT = 8, MUST = 0.98;
const pic = (name) => decode(path.join(GAME, `hero_${name}.png`));
const basicShell = pic('helmet_shell'), basicVisor = pic('visor'), seal = pic('helmet_collar');
const alpha = (img, x, y) => (x < 0 || y < 0 || x >= W || y >= H ? 0 : img.px[(y * W + x) * 4 + 3]);
const glassAt = (x, y) => alpha(basicVisor, x, y) > 0;
const mustAt = (x, y) => alpha(basicShell, x, y) > 40 || glassAt(x, y);
const vb = bbox(basicVisor), bb = bbox(basicShell);

/**
 * The basic helmet's neck: the narrow band under its ball, from the dark lip line down onto the seal, as a row per column (the
 * lip's row at x, read off its picture). Every socket helmet wears it, in its own colour: the dome ends on the lip.
 */
const LIP = [[16, 54], [27, 54], [31, 55], [35, 56], [40, 57], [44, 58], [54, 58], [56, 57]];
const lipAt = (x) => {
  if (x > LIP[LIP.length - 1][0]) return Infinity;
  if (x <= LIP[0][0]) return LIP[0][1];
  for (let k = 1; k < LIP.length; k++) if (x <= LIP[k][0]) { const [x0, y0] = LIP[k - 1], [x1, y1] = LIP[k]; return y0 + (y1 - y0) * (x - x0) / (x1 - x0); }
};
const neckAt = (x, y) => alpha(basicShell, x, y) > 0 && y >= lipAt(x) - 1;

/** Where paint may be: the must zone, one world px round it at the top, back and front above the rim, and a snout's reach past the glass. */
const may = new Uint8Array(W * H);
for (let y = 0; y < H; y++) for (let x = 0; x < W; x++) {
  let ok = mustAt(x, y);
  if (!ok && y < RIM) for (let d = -GROW; d <= GROW && !ok; d++) for (let e = -GROW; e <= GROW && !ok; e++) ok = d * d + e * e <= GROW * GROW && mustAt(x + e, y + d);
  if (!ok && x >= vb.x0 && x <= vb.x1 + SNOUT && y >= (vb.y0 + vb.y1) >> 1 && y <= bb.y1 && !alpha(seal, x, y)) ok = true;
  may[y * W + x] = ok && y < lipAt(x) - 1 ? 1 : 0;
}

function template(out) {
  // the helmet as worn: the seal, the shell over it, the glass opaque
  const one = Buffer.alloc(W * H * 4);
  for (let i = 0; i < W * H; i++) {
    const o = i * 4; let c = NAVY.slice();
    const over = (img) => { const a = img.px[o + 3] / 255; for (let k = 0; k < 3; k++) c[k] = c[k] * (1 - a) + img.px[o + k] * a; };
    over(seal);
    if (basicVisor.px[o + 3]) c = [basicVisor.px[o], basicVisor.px[o + 1], basicVisor.px[o + 2]];
    over(basicShell);
    one[o] = c[0]; one[o + 1] = c[1]; one[o + 2] = c[2]; one[o + 3] = 255;
  }
  const at = (x, y, k) => one[(Math.max(0, Math.min(H - 1, y)) * W + Math.max(0, Math.min(W - 1, x))) * 4 + k];
  const SW = CELL * 3, SH = CELL * 2, o = Buffer.alloc(SW * SH * 4);
  for (let i = 0; i < SW * SH; i++) { o[i * 4] = NAVY[0]; o[i * 4 + 1] = NAVY[1]; o[i * 4 + 2] = NAVY[2]; o[i * 4 + 3] = 255; }
  for (let cy = 0; cy < 2; cy++) for (let cx = 0; cx < 3; cx++) for (let y = 0; y < H * K; y++) for (let x = 0; x < W * K; x++) {
    const fx = (x + 0.5) / K - 0.5, fy = (y + 0.5) / K - 0.5, x0 = Math.floor(fx), y0 = Math.floor(fy), tx = fx - x0, ty = fy - y0;
    const q = ((cy * CELL + OY + y) * SW + cx * CELL + OX + x) * 4;
    for (let k = 0; k < 3; k++) o[q + k] = (at(x0, y0, k) * (1 - tx) + at(x0 + 1, y0, k) * tx) * (1 - ty) + (at(x0, y0 + 1, k) * (1 - tx) + at(x0 + 1, y0 + 1, k) * tx) * ty;
  }
  fs.writeFileSync(out, encode(SW, SH, o));
}

function prompt(ids) {
  const where = ['top-middle', 'top-right', 'bottom-left', 'bottom-middle', 'bottom-right'];
  return `Use your image generation tool to generate ONE image and save it in the current folder as sheet.png. Do nothing else.
The FIRST attached image is the layout: a 1536 x 1024 sheet, a 3 by 2 grid of 512 px cells, each holding the same game helmet, the hero's basic helmet, in strict side view facing right, sitting on a black rubber neck seal, on a flat dark navy background. The output must be the SAME sheet: same size, same grid, every helmet at exactly the same place and size in its cell.
- Top-left cell: leave the basic helmet EXACTLY as it is.
- The other cells: repaint the basic helmet's dome into a different helmet each. The black rubber seal under it and the round blue-grey glass are fixed: leave both exactly as they are and paint nothing over them. The dome ends where the basic one ends, its bottom edge resting on the black seal exactly as the basic one does: nothing below that edge, no neck ring, no collar, nothing hanging down. Keep the size: the dome may stick out a very little at the back and top, and a small snout or canister may sit at the front under the glass, never lower than the dome's bottom edge.
${ids.map((id, i) => `  - ${where[i]}: the ${['SECOND', 'THIRD', 'FOURTH', 'FIFTH', 'SIXTH'][i]} attached picture's helmet: ${HELMETS[id].ask}.`).join('\n')}
Style: exactly the basic helmet's painting, the game's own ink and watercolour: a thin dark ink outline, soft flat watercolour fills with a little mottling, few big shapes, low detail, muted colours, lit from above. Flat uniform dark navy background #061132 everywhere else: no grid lines, no labels, no text, no shadow, no glow.`;
}

function paint(dir, ids) {
  const codex = path.join(execSync('npm root -g').toString().trim(), '@openai/codex/bin/codex.js');
  const args = [codex, 'exec', '--skip-git-repo-check', '--sandbox', 'workspace-write', '-C', dir, '--image', path.join(dir, 'template.png')];
  for (const id of ids) args.push('--image', path.join(__dirname, 'refs', `${id}.png`));
  args.push('--', prompt(ids));
  const log = fs.openSync(path.join(dir, 'sheet.log'), 'w');
  spawn(process.execPath, args, { stdio: ['ignore', log, log] }).on('exit', (c) => console.log('paint', c, fs.existsSync(path.join(dir, 'sheet.png')) ? 'saved' : 'MISSING'));
}

const near4 = (x, y) => [[x + 1, y], [x - 1, y], [x, y + 1], [x, y - 1]];

function cut(sheetFile, ids, write) {
  const sheet = decode(sheetFile), { w, h, px } = sheet, f = w / (CELL * 3), s = K * f;
  if (Math.abs(h / (CELL * 2) - f) > 0.01) throw new Error(`sheet ${w}x${h} is not 3:2`);
  let bg = [0, 0, 0];
  for (const [cx, cy] of [[2, 2], [w - 6, 2], [2, h - 6], [w - 6, h - 6]]) for (let y = cy; y < cy + 4; y++) for (let x = cx; x < cx + 4; x++) for (let k = 0; k < 3; k++) bg[k] += px[(y * w + x) * 4 + k] / 64;
  const dist = (i) => Math.abs(px[i * 4] - bg[0]) + Math.abs(px[i * 4 + 1] - bg[1]) + Math.abs(px[i * 4 + 2] - bg[2]);

  /** A cell's paint, the background flooded away from its border: per sheet pixel, 1 paint, 0 background. */
  function paintOf(k) {
    const x0 = Math.round((k % 3) * CELL * f), y0 = Math.round(Math.floor(k / 3) * CELL * f), x1 = Math.round(x0 + CELL * f), y1 = Math.round(y0 + CELL * f);
    const bgm = new Uint8Array(w * h), st = [];
    const seed = (x, y) => { const i = y * w + x; if (!bgm[i] && dist(i) < 36) { bgm[i] = 1; st.push(i); } };
    for (let x = x0; x < x1; x++) { seed(x, y0); seed(x, y1 - 1); }
    for (let y = y0; y < y1; y++) { seed(x0, y); seed(x1 - 1, y); }
    while (st.length) { const i = st.pop(); for (const [a, b] of near4(i % w, (i / w) | 0)) if (a >= x0 && b >= y0 && a < x1 && b < y1) seed(a, b); }
    return { x0, y0, x1, y1, bgm };
  }
  /** The cell's paint resampled onto the helmet picture, shifted by (dx, dy) hd px. */
  function resample({ x0, y0, x1, y1, bgm }, dx, dy) {
    const acc = new Float64Array(W * H * 4);
    for (let y = y0; y < y1; y++) for (let x = x0; x < x1; x++) {
      const i = y * w + x; if (bgm[i]) continue;
      const tx0 = (x - x0) / s - OX / K + dx, ty0 = (y - y0) / s - OY / K + dy, tx1 = tx0 + 1 / s, ty1 = ty0 + 1 / s;
      for (let ty = Math.floor(ty0); ty < ty1; ty++) for (let tx = Math.floor(tx0); tx < tx1; tx++) {
        if (tx < 0 || ty < 0 || tx >= W || ty >= H) continue;
        const cov = (Math.min(tx + 1, tx1) - Math.max(tx, tx0)) * (Math.min(ty + 1, ty1) - Math.max(ty, ty0)) * s * s; if (cov <= 0) continue;
        const q = (ty * W + tx) * 4; for (let c = 0; c < 3; c++) acc[q + c] += px[i * 4 + c] * cov; acc[q + 3] += cov;
      }
    }
    const out = Buffer.alloc(W * H * 4);
    for (let q = 0; q < W * H * 4; q += 4) { const c = acc[q + 3]; if (c < 0.03) continue; for (let k = 0; k < 3; k++) out[q + k] = acc[q + k] / c; out[q + 3] = Math.min(255, Math.round(c * 255)); }
    return out;
  }
  // the grid's drift, measured on the untouched reference against the basic helmet as the template draws it: shell, glass, seal
  const sb = bbox(seal), ref = bbox({ w: W, h: H, px: resample(paintOf(0), 0, 0) });
  const worn = { x0: Math.min(bb.x0, vb.x0, sb.x0), x1: Math.max(bb.x1, vb.x1, sb.x1), y0: Math.min(bb.y0, vb.y0), y1: Math.max(bb.y1, sb.y1) };
  const dx = Math.round(((worn.x0 + worn.x1) - (ref.x0 + ref.x1)) / 2 * 4) / 4, dy = Math.round((worn.y1 - ref.y1) * 4) / 4;
  console.log(`grid drift ${dx}, ${dy} (reference ${ref.x0},${ref.y0}-${ref.x1},${ref.y1}, basic ${worn.x0},${worn.y0}-${worn.x1},${worn.y1})`);

  ids.forEach((id, n) => {
    const cfg = HELMETS[id]; if (!cfg) throw new Error(`no socket helmet ${id}`);
    const raw = resample(paintOf(n + 1), dx, dy), dome = Buffer.from(raw);
    const a = (b, x, y) => (x < 0 || y < 0 || x >= W || y >= H ? 0 : b[(y * W + x) * 4 + 3]);
    // its own glass: the glass-coloured patch over the engine's glass, holes and all, dropped with it
    const glassy = (q) => { const r = raw[q], g = raw[q + 1], b = raw[q + 2]; return (b - r > 18 && b > 55) || (r - b > 90 && g - b > 50 && r > 140); };
    const glass = new Uint8Array(W * H), st = [];
    for (let y = 0; y < H; y++) for (let x = 0; x < W; x++) if (glassAt(x, y)) { glass[y * W + x] = 1; st.push(y * W + x); }
    while (st.length) { const i = st.pop(); for (const [x, y] of near4(i % W, (i / W) | 0)) { const j = y * W + x; if (x < 0 || y < 0 || x >= W || y >= H || glass[j] || !a(raw, x, y) || !glassy(j * 4)) continue; glass[j] = 1; st.push(j); } }
    // drop what is not the dome's: glass, below the rim, outside the may zone
    const dropped = new Uint8Array(W * H); let below = 0, outside = 0;
    for (let i = 0; i < W * H; i++) {
      if (!dome[i * 4 + 3]) continue;
      const x = i % W, y = (i / W) | 0;
      if (glass[i] && glassAt(x, y)) { dome.fill(0, i * 4, i * 4 + 4); continue; }
      if (glass[i]) { dropped[i] = 2; continue; } // its glass past the engine's: inked below, a rim, never a hole
      if (!may[i]) { dropped[i] = 1; if (y >= RIM) below++; else outside++; dome.fill(0, i * 4, i * 4 + 4); }
    }
    // the cut edge inked in the dome's own outline colour: the darkest fifth of its outer edge
    const edge = [];
    for (let y = 0; y < H; y++) for (let x = 0; x < W; x++) if (a(raw, x, y) > 200 && near4(x, y).some(([p, q]) => !a(raw, p, q))) { const q = (y * W + x) * 4; edge.push([raw[q] + raw[q + 1] + raw[q + 2], raw[q], raw[q + 1], raw[q + 2]]); }
    edge.sort((p, q) => p[0] - q[0]);
    const darkest = edge.slice(0, Math.max(1, edge.length / 5 | 0)), ink = [1, 2, 3].map((k) => darkest.reduce((t, e) => t + e[k], 0) / darkest.length);
    for (let y = 0; y < H; y++) for (let x = 0; x < W; x++) {
      const i = y * W + x; if (!dome[i * 4 + 3]) continue;
      let cutNear = dropped[i] === 2;
      for (let d = -1; d <= 1 && !cutNear; d++) for (let e = -1; e <= 1 && !cutNear; e++) { const p = x + e, q = y + d; cutNear = p >= 0 && q >= 0 && p < W && q < H && dropped[q * W + p] === 1 && !neckAt(p, q); }
      if (cutNear) { dome[i * 4] = ink[0]; dome[i * 4 + 1] = ink[1]; dome[i * 4 + 2] = ink[2]; dome[i * 4 + 3] = 255; }
    }
    // the neck laid on, the basic one's pixels in the dome's colour just above its lip (its ink kept dark): one shape for all
    const band = (img) => { const t = [0, 0, 0, 0]; for (let y = 40; y < 60; y++) for (let x = 22; x <= 52; x++) { const q = (y * W + x) * 4; if (y < lipAt(x) - 7 || y >= lipAt(x) - 1 || img[q + 3] < 200 || img[q] + img[q + 1] + img[q + 2] < 150) continue; for (let k = 0; k < 3; k++) t[k] += img[q + k]; t[3]++; } return t.slice(0, 3).map((v) => v / Math.max(1, t[3])); };
    const tint = band(dome), base = band(basicShell.px);
    for (let y = 0; y < H; y++) for (let x = 0; x < W; x++) {
      if (!neckAt(x, y)) continue;
      const q = (y * W + x) * 4, A = basicShell.px[q + 3] / 255, B = dome[q + 3] / 255, out = A + B * (1 - A);
      for (let k = 0; k < 3; k++) dome[q + k] = (Math.min(255, basicShell.px[q + k] * tint[k] / base[k]) * A + dome[q + k] * B * (1 - A)) / out;
      dome[q + 3] = Math.round(out * 255);
    }
    // the check: the must zone painted, so the lamp and the glass's rim have their surface; its edge 2 px in aside, the soft edge
    // and the basic helmet's own knobs (its valve at the back) with it
    const inner = (x, y) => { if (!mustAt(x, y) || glassAt(x, y) || neckAt(x, y)) return false; for (let d = -2; d <= 2; d++) for (let e = -2; e <= 2; e++) if (!mustAt(x + e, y + d)) return false; return true; };
    let must = 0, held = 0;
    for (let y = 0; y < H; y++) for (let x = 0; x < W; x++) if (inner(x, y)) { must++; if (a(dome, x, y) >= 128) held++; }
    const cover = held / must, ok = cover >= MUST;
    if (process.env.SOCKET_DEBUG) {
      const o = Buffer.alloc(W * H * 4);
      for (let i = 0; i < W * H; i++) { const x = i % W, y = (i / W) | 0, c = mustAt(x, y) && !glassAt(x, y) && a(dome, x, y) < 128 ? (glass[i] ? [255, 0, 255] : [255, 0, 0]) : a(dome, x, y) ? [dome[i * 4], dome[i * 4 + 1], dome[i * 4 + 2]] : glassAt(x, y) ? [0, 0, 120] : [40, 40, 40]; o.set([...c, 255], i * 4); }
      fs.writeFileSync(path.join(process.env.SOCKET_DEBUG, `${id}.png`), encode(W, H, o));
    }
    console.log(`${id}: ${ok ? 'ok' : 'REFUSED'}, must zone ${(cover * 100).toFixed(1)}% painted; cropped ${below} px below the rim, ${outside} outside the zone`);
    if (!ok || !write) return;

    const put = (name, buf) => fs.writeFileSync(path.join(GAME, `hero_${id}_${name}.png`), encode(W, H, buf));
    const rm = (name) => { for (const f of [`hero_${id}_${name}.png`, `hero_${id}_${name}.png.import`]) if (fs.existsSync(path.join(GAME, f))) fs.unlinkSync(path.join(GAME, f)); };
    put('shell', dome); rm('seal'); rm('collar');
    // the engine's glass, tinted if asked, and open where the dome stands in front of it (a snout under it): the game draws it over the dome
    const visor = Buffer.from(basicVisor.px);
    for (let q = 0; q < visor.length; q += 4) {
      if (!visor[q + 3]) continue;
      if (dome[q + 3] >= 128) { visor.fill(0, q, q + 4); continue; }
      if (cfg.glass) { const l = (visor[q] + visor[q + 1] + visor[q + 2]) / 3 / 110; for (let k = 0; k < 3; k++) visor[q + k] = Math.min(255, cfg.glass[k] * l); }
    }
    put('visor', visor);
    card(dome, visor, path.join(GAME, cfg.card, `${id}.png`));
  });
}

/** Its card: the helmet as worn, glass opaque, fit into 64 x 64 with 2 px round it. */
function card(dome, visor, out) {
  const C = 64, all = Buffer.alloc(W * H * 4);
  for (let q = 0; q < W * H * 4; q += 4) {
    const a = dome[q + 3] / 255, va = visor[q + 3] ? 1 : 0, A = a + va * (1 - a); if (!A) continue;
    for (let c = 0; c < 3; c++) all[q + c] = (dome[q + c] * a + visor[q + c] * va * (1 - a)) / A;
    all[q + 3] = Math.round(A * 255);
  }
  const ab = bbox({ w: W, h: H, px: all }), cs = Math.min((C - 4) / (ab.x1 - ab.x0 + 1), (C - 4) / (ab.y1 - ab.y0 + 1));
  const cox = (C - (ab.x1 - ab.x0 + 1) * cs) / 2 - ab.x0 * cs, coy = (C - (ab.y1 - ab.y0 + 1) * cs) / 2 - ab.y0 * cs, o = Buffer.alloc(C * C * 4);
  for (let y = 0; y < C; y++) for (let x = 0; x < C; x++) {
    const sx = (x + 0.5 - cox) / cs - 0.5, sy = (y + 0.5 - coy) / cs - 0.5, x0 = Math.floor(sx), y0 = Math.floor(sy), tx = sx - x0, ty = sy - y0, p = (y * C + x) * 4;
    let r = 0, g = 0, b = 0, a = 0;
    for (const [xx, yy, wt] of [[x0, y0, (1 - tx) * (1 - ty)], [x0 + 1, y0, tx * (1 - ty)], [x0, y0 + 1, (1 - tx) * ty], [x0 + 1, y0 + 1, tx * ty]]) {
      if (xx < 0 || yy < 0 || xx >= W || yy >= H) continue; const q = (yy * W + xx) * 4, aa = all[q + 3] / 255 * wt;
      r += all[q] * aa; g += all[q + 1] * aa; b += all[q + 2] * aa; a += aa;
    }
    if (a > 0) { o[p] = r / a; o[p + 1] = g / a; o[p + 2] = b / a; o[p + 3] = Math.round(a * 255); }
  }
  fs.writeFileSync(out, encode(C, C, o));
}

const [cmd, ...rest] = process.argv.slice(2);
if (cmd === 'template') template(rest[0]);
else if (cmd === 'paint') paint(path.resolve(rest[0]), rest.slice(1));
else if (cmd === 'cut') cut(rest[0], rest.slice(1).filter((a) => a !== '--write'), rest.includes('--write'));
else console.log('node socket.js template <out.png> | paint <dir> <id>... | cut <sheet.png> <id>... [--write]');
