// The lifepod's pictures from mockups/lifepod-template.html, PODS[2] ("C · Кокон") and its helpers, pixel
// for pixel, in the ship art's palette: its outline, the furnace's and crate's greys, the analyzer's glass
// blues, the generator's hazard yellow, the tank's teal. Usage: node lifepod.js <game/art dir>
const { encode } = require("./png.js");
const path = require("path");
const out = process.argv[2] || path.join(__dirname, '..', '..', '..', 'game', 'art');
const W = 48, H = 48;

const C = {
  out: "#1a1e25",
  hullHi: "#b3bac6", hull: "#8f99ad", hullMid: "#6b7385", hullLo: "#5b6170", hullDeep: "#474c57",
  stripeY: "#e8c547", stripeK: "#2a2f3a",
  glass: "#3a536a", glassHi: "#9fd8ff", streak: "#5f86a8", glow: "#ffd98a", glowHi: "#fff4cf",
  screen: "#04130d", greenLo: "#167a40",
  tank: "#4f8fa0", tankHi: "#86c3d1", tankLo: "#356a78",
  body: "#8a6a30", limb: "#7a5a2a",
};
const HULL = [C.hullHi, C.hull, C.hullMid, C.hullLo, C.hullDeep];

function canvas() {
  const px = new Array(W * H).fill(null);
  const set = (x, y, c) => { x = Math.round(x); y = Math.round(y); if (x >= 0 && y >= 0 && x < W && y < H) px[y * W + x] = c; };
  const rect = (x, y, w, h, c) => { for (let j = 0; j < h; j++) for (let i = 0; i < w; i++) set(x + i, y + j, c); };
  return { px, set, rect };
}

function shade(g, ox, oy, w, h, inside, ramp, round = 1) {
  const m = (x, y) => x >= 0 && y >= 0 && x < w && y < h && inside(x, y);
  for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) {
    if (!m(x, y)) continue;
    const ul = !m(x - round, y) || !m(x, y - round), lr = !m(x + round, y) || !m(x, y + round);
    const nx = (x / w - 0.5), ny = (y / h - 0.5);
    let k = 0.55 - (nx * 0.8 + ny * 0.9);
    if (ul) k += 0.35; if (lr) k -= 0.35;
    const c = k > 0.95 ? ramp[0] : k > 0.6 ? ramp[1] : k > 0.25 ? ramp[2] : k > -0.1 ? ramp[3] : ramp[4];
    g.set(ox + x, oy + y, c);
  }
  for (let y = -1; y <= h; y++) for (let x = -1; x <= w; x++)
    if (!m(x, y) && (m(x - 1, y) || m(x + 1, y) || m(x, y - 1) || m(x, y + 1))) g.set(ox + x, oy + y, C.out);
}
// screen(g, x, y, w, h, t, on) with on = false: the game draws what it shows over it.
function screen(g, x, y, w, h) { g.rect(x - 1, y - 1, w + 2, h + 2, C.out); g.rect(x, y, w, h, C.screen); g.set(x + w - 2, y + 1, C.greenLo); }
function tray(g, x, y, w) { g.rect(x - 1, y - 1, w + 2, 5, C.out); g.rect(x, y, w, 3, C.hullDeep); g.rect(x, y, w, 1, C.hullLo); }
function tanks(g, x, y, n, h) {
  for (let i = 0; i < n; i++) {
    const tx = x + i * 4;
    g.rect(tx - 1, y - 1, 5, h + 2, C.out); g.rect(tx, y, 3, h, C.tank); g.rect(tx, y, 1, h, C.tankHi); g.rect(tx + 2, y, 1, h, C.tankLo);
    g.set(tx + 1, y - 1, C.hullMid);
  }
}
function stripes(g, x, y, w, h) { for (let j = 0; j < h; j++) for (let i = 0; i < w; i++) g.set(x + i, y + j, ((i + j) >> 1) % 2 ? C.stripeY : C.stripeK); }
function rivets(g, pts) { for (const [x, y] of pts) { g.set(x, y, C.hullHi); g.set(x + 1, y + 1, C.hullDeep); } }

/** The glass, closed or lit from inside with the hero's silhouette: PODS[2]'s glass and its `open` figure. */
function glass(g, open) {
  for (let y = 9; y < 37; y++) for (let x = 15; x < 29; x++)
    g.set(x, y, open ? ((x + y) % 7 === 0 ? C.glowHi : C.glow) : (x - 15 < 3 ? C.glassHi : (x + y) % 9 === 0 ? C.streak : C.glass));
  if (open) { g.rect(19, 13, 6, 5, C.limb); g.rect(18, 18, 8, 12, C.body); g.rect(19, 30, 2, 7, C.limb); g.rect(23, 30, 2, 7, C.limb); }
}

/** PODS[2].draw(g, t, open = false, work = false). */
function pod(g) {
  shade(g, 9, 1, 26, 45, (x, y) => { const r = 12.5; const top = y < r ? ((x - r) / r) ** 2 + ((y - r) / r) ** 2 <= 1 : true; return top && x >= 0 && x < 26; }, HULL);
  g.rect(14, 8, 16, 30, C.out);
  glass(g, false);
  g.rect(9, 42, 26, 6, C.out); g.rect(10, 42, 24, 5, C.hullDeep); stripes(g, 11, 43, 22, 2);
  g.rect(35, 16, 10, 22, C.out); g.rect(36, 17, 8, 20, C.hullMid); g.rect(36, 17, 8, 1, C.hullHi);
  screen(g, 37, 19, 6, 5);
  tray(g, 37, 29, 6);
  tanks(g, 1, 26, 2, 18);
  rivets(g, [[12, 10], [31, 10], [12, 39], [31, 39]]);
}

function write(name, g) {
  const rgba = Buffer.alloc(W * H * 4);
  g.px.forEach((c, i) => {
    if (!c) return;
    rgba[i * 4] = parseInt(c.slice(1, 3), 16); rgba[i * 4 + 1] = parseInt(c.slice(3, 5), 16); rgba[i * 4 + 2] = parseInt(c.slice(5, 7), 16); rgba[i * 4 + 3] = 255;
  });
  encode(`${out}/${name}.png`, W, H, rgba);
}

const base = canvas(); pod(base); write("machine_lifepod", base);
const lit = canvas(); glass(lit, true); write("lifepod_glass", lit);
