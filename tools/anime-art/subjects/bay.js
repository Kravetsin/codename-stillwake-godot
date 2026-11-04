// The drone bay, as picked: «Хребет с кронштейнами» (wall), a spine bolted to the wall with four cantilever arms at staggered
// heights; a canister laid aslant in a tilted breech, a regulator and a dial over it (docs/decisions/2026-11-03-salvage-drones.md).
// Ported whole from the pick page (bay-art/bays.js): its body behind the docked drones, its front over them, and the small parts the
// game swaps: each pad's lamps, the fuel slot's valve, vane and lamp, the beacon, the canister. The dial's needle stays where it is:
// too small to read (the user's call). Writes features/logistics/logic/BayInk.cs, where the game puts them.
(function () {
'use strict';
const { ell, rr, poly, rng, bake } = Anime;
/** The footprint in world pixels: 4 cells by 4. */
const W = 32, H = 32;

// The machines' materials (machines.js), a rubber hose and the canister's paint (items.js).
const STEEL = { base: '#a7afbc', shade: '#6a708c', deep: '#474c68', lite: '#d4dae2', spec: '#ffffff' };
const GUN = { base: '#5c6474', shade: '#383c58', deep: '#262a42', lite: '#868fa2', spec: '#dfe7f2' };
const DARK = { base: '#2a2d3c', shade: '#1a1c2a', deep: '#12131e', lite: '#40445a', spec: '#a8b4c6' };
const RUBBER = { base: '#3a3b42', shade: '#24252c', deep: '#18191e', lite: '#585a62', spec: '#8a8d96' };
const CAN = { base: '#3f78b0', shade: '#2a5488', deep: '#1c3c68', lite: '#70a4d0', spec: '#c4e0f0' };
const LABEL = { base: '#d4d4cc', shade: '#9a9c9c', deep: '#7a7c80', lite: '#ecece6' };

// Lamps: the charge's green, a flat battery's red, the beacon's sky; dark when the power is gone.
const ON = '#7cf06a', ON_CORE = '#e0ffd0', UNLIT = '#1a3a20', DEAD = '#121614', LOW = '#ff5040', LOW_UNLIT = '#3a1818', SKY = '#7dd3fc';

const f2 = (v) => +v.toFixed(2);
const band = (x0, y0, x1, y1) => poly(x0, y0, x1, y0, x1, y1, x0, y1);
const pg = (pts) => poly(pts.flat());

/** A panel: its lit top edge, its shaded foot and side. */
function box(a, x, y, w, h, m, o) {
  o = o || {};
  const s = o.s ?? Math.min(1.1, w * .12), b = o.b ?? Math.min(1.2, h * .14), l = o.l ?? Math.min(.7, h * .2);
  a.shape(rr(x, y, w, h, o.r ?? .35), m, { outer: o.outer, lite: band(x, y, x + w, y + l), shade: [band(x + w - s, y, x + w, y + h), band(x, y + h - b, x + w, y + h)], spec: o.spec ? band(x + .6, y + .12, x + .6 + w * .3, y + .38) : undefined, lines: o.lines });
}
const rivets = (a, ...pts) => { for (const [x, y] of pts) { a.flat(ell(x, y, .38), '#474c68'); a.flat(ell(x - .09, y - .1, .18), '#c4cad4'); } };
/** Rust where water sat: a torn edge, a darker heart. Only the edge is scattered. */
function rust(a, x, y, r, seed) {
  const R = rng(seed), pts = [];
  for (let i = 0; i < 9; i++) { const t = (i / 9) * Math.PI * 2, k = r * (.7 + R() * .45); pts.push(x + Math.cos(t) * k * 1.15, y + Math.sin(t) * k * .8); }
  a.flat(poly(pts), '#8a5a3c');
  a.flat(ell(x + r * .1, y + r * .05, r * .55, r * .42), '#7a4428');
  a.flat(ell(x + r * .15, y + r * .1, r * .26, r * .2), '#4c2a1c');
}
/** A body's power inlet: a gland plate with the cable's collar. */
function gland(a, x, y) {
  a.shape(rr(x, y - 1, 1.8, 2, .3), DARK, { lite: band(x, y - 1, x + 1.8, y - .6) });
  a.flat(ell(x + .9, y, .55), '#14151c');
  a.flat(ell(x + .8, y - .1, .25), '#5c6474');
}

/** A canister 6.8 long and 3.8 across, its middle at (cx, cy), its valve pointing at angle th; lit from the upper left whichever way it lies. */
function canister(a, cx, cy, th) {
  const half = 3.4, R = 1.9, dx = Math.cos(th), dy = Math.sin(th);
  let ex = -dy, ey = dx;
  if (ex * -.6 + ey * -.8 > 0) { ex = -ex; ey = -ey; }
  const T = (u, v) => [f2(cx + (u - half) * dx + v * ex), f2(cy + (u - half) * dy + v * ey)];
  const P = (pts) => pg(pts.map(([u, v]) => T(u, v)));
  // The valve: its neck and handwheel.
  a.shape(P([[5.2, -.6], [6.8, -.6], [6.8, .6], [5.2, .6]]), GUN, { lite: P([[5.2, -.6], [6.8, -.6], [6.8, -.2], [5.2, -.2]]) });
  a.shape(P([[6.0, -1.15], [6.45, -1.15], [6.45, 1.15], [6.0, 1.15]]), DARK, { outer: false });
  a.shape(P([[0, -R + .5], [.5, -R], [4.3, -R], [5.1, -1.2], [5.4, -.7], [5.4, .7], [5.1, 1.2], [4.3, R], [.5, R], [0, R - .5]]), CAN, {
    lite: P([[.4, -R], [4.3, -R], [4.8, -1.4], [.4, -1.2]]), shade: P([[.4, 1.05], [4.7, 1.05], [4.3, R], [.4, R]]), deep: P([[0, -R], [.55, -R], [.55, R], [0, R]]),
    spec: P([[1, -1.6], [3.9, -1.6], [3.9, -1.35], [1, -1.35]]) });
  a.shape(P([[1.4, -1.45], [3.3, -1.45], [3.3, 1.45], [1.4, 1.45]]), LABEL, { outer: false, shade: P([[1.4, .9], [3.3, .9], [3.3, 1.45], [1.4, 1.45]]) });
  a.flat(P([[2.05, -1.1], [2.6, -1.1], [2.6, 1.1], [2.05, 1.1]]), '#2a5488');
  // A scuff by its foot, where it was dragged.
  const s0 = T(.9, .4), s1 = T(.6, 1.3);
  a.line(`M${s0[0]} ${s0[1]}L${s1[0]} ${s1[1]}`, { w: .8 });
}

/** A pad's lamp i of four: lv is its charge (1..4), 'L' flat, 0 no drone, 'off' no power. */
function ledColour(i, lv) { if (lv === 'off') return DEAD; if (lv === 'L') return i === 0 ? LOW : LOW_UNLIT; return i < lv ? ON : UNLIT; }
function led(a, x, y, w, h, c) {
  a.flat(rr(x - .2, y - .2, w + .4, h + .4, .25), '#0e0f16');
  a.flat(rr(x, y, w, h, .18), c);
  if (c === ON || c === LOW) a.flat(rr(x + w * .18, y + h * .2, w * .38, h * .38, .1), c === ON ? ON_CORE : '#ffd0c0');
}
/** Four lamps from (x, y), each the next (dx, dy) on: the first is the one a flat battery shows red. */
const lamps = (a, x, y, dx, dy, w, h, lv) => { for (let i = 0; i < 4; i++) led(a, x + dx * i, y + dy * i, w, h, ledColour(i, lv)); };
function sector(cx, cy, r0, r1, d0, d1) {
  const pts = [], n = 10, at = (r, d) => [cx + Math.cos(d * Math.PI / 180) * r, cy - Math.sin(d * Math.PI / 180) * r];
  for (let i = 0; i <= n; i++) pts.push(at(r1, d0 + (d1 - d0) * i / n));
  for (let i = n; i >= 0; i--) pts.push(at(r0, d0 + (d1 - d0) * i / n));
  return pg(pts);
}
/** A pressure dial: empty at the lower left, full at the lower right, a red sector where it runs out; level 0..4. */
function dial(a, cx, cy, r, level) {
  a.shape(ell(cx, cy, r), GUN, { lite: ell(cx - r * .12, cy - r * .16, r * .86), shade: r * .22 });
  a.flat(ell(cx, cy, r * .74), '#d2ccbc');
  a.flat(sector(cx, cy, r * .46, r * .72, 196, 226), '#b8402c');
  for (let k = 0; k <= 4; k++) {
    const d = (225 - k * 67.5) * Math.PI / 180, c = Math.cos(d), s = -Math.sin(d);
    a.line(`M${f2(cx + c * r * .5)} ${f2(cy + s * r * .5)}L${f2(cx + c * r * .7)} ${f2(cy + s * r * .7)}`, { w: .9 });
  }
  const d = (225 - level * 67.5) * Math.PI / 180, c = Math.cos(d), s = -Math.sin(d), wd = r * .1;
  a.flat(poly(cx - s * wd, cy + c * wd, cx + c * r * .68, cy + s * r * .68, cx + s * wd, cy - c * wd), '#1a1418');
  a.flat(ell(cx, cy, r * .17), '#262a42');
  a.flat(poly(cx - r * .55, cy - r * .2, cx - r * .3, cy - r * .55, cx - r * .22, cy - r * .48, cx - r * .46, cy - r * .14), '#f4f0e6');
}
/** A quarter-turn valve: its lever along the pipe (axis) when open, across it when shut; the knob green or red. */
function lever(a, x, y, open, axis, len) {
  const t = open ? axis : axis - Math.PI / 2, ex = x + Math.cos(t) * (len || 1.8), ey = y + Math.sin(t) * (len || 1.8);
  a.tube(`M${f2(x)} ${f2(y)}L${f2(ex)} ${f2(ey)}`, .45, GUN, { spec: false });
  a.shape(ell(ex, ey, .5), open ? { base: '#3fae5c', shade: '#256a3c', lite: '#86e08c', spec: '#e0ffe0' } : { base: '#d0483c', shade: '#8a2c30', lite: '#f08c74', spec: '#ffe0d0' }, { lite: ell(ex - .12, ey - .14, .3) });
  a.flat(ell(x, y, .5), '#262a42');
  a.flat(ell(x - .1, y - .12, .2), '#a7afbc');
}
/** A flow window: a vane behind glass, turned a step each frame while gas runs (flow 1 or 2), still at 0. */
function vane(a, cx, cy, r, flow) {
  a.shape(ell(cx, cy, r), DARK, { lite: ell(cx - r * .1, cy - r * .14, r * .9) });
  a.flat(ell(cx, cy, r * .72), '#1c3a50');
  const t = .35 + flow * Math.PI / 4, col = flow ? '#a8ecff' : '#6a7a88';
  for (const k of [0, Math.PI / 2]) {
    const c = Math.cos(t + k) * r * .62, s = Math.sin(t + k) * r * .62, wd = r * .13;
    a.flat(poly(cx - c - s * wd / r, cy - s + c * wd / r, cx + c - s * wd / r, cy + s + c * wd / r, cx + c + s * wd / r, cy + s - c * wd / r, cx - c + s * wd / r, cy - s - c * wd / r), col);
  }
  a.flat(ell(cx - r * .3, cy - r * .32, r * .14), '#e6f6fc');
}
/** The fuel slot's own lamp: red without a canister, amber holding one, green blinking while it feeds, dark without power. */
function slotLed(a, x, y, st) {
  const c = !st.power ? '#1c1414' : !st.can ? LOW : st.flow ? (st.flow === 1 ? ON : '#3a8a3a') : '#f0c460';
  led(a, x - .55, y - .4, 1.1, .8, c);
}
/** The power lamp: sky while the bay has power. */
function beaconLens(a, x, y, w, h, on) {
  a.flat(rr(x, y, w, h, .25), on ? SKY : '#1c2a34');
  if (on) a.flat(rr(x + w * .2, y + h * .2, w * .35, h * .4, .15), '#e0f6ff');
}
const FEET = 11.4;

// ---------------- The spine bolted to the wall: cantilever arms at staggered heights ----------------
const wallPads = [[-1, 1], [19, 4], [-1, 15], [19, 18]];
const WALL_TH = -118 * Math.PI / 180;
/** Where the canister lies: its middle and its valve's way; it slides in from its middle moved by from. */
const SLOT = { x: 16.2, y: 20.2, th: WALL_TH, from: [1.2, 2.2] };
/** The breech's frame: u along the canister from its foot, v across it. */
const breech = (u, v) => { const dx = Math.cos(WALL_TH), dy = Math.sin(WALL_TH); return [SLOT.x + (u - 3.4) * dx + v * -dy, SLOT.y + (u - 3.4) * dy + v * dx]; };
/** The strap across the breech that holds a canister in: over the empty breech in the body, over the canister in its own picture. */
const strap = (a) => a.shape(pg([breech(.65, -2.3), breech(1.2, -2.3), breech(1.2, 2.3), breech(.65, 2.3)]), GUN, { lite: pg([breech(.65, -2.3), breech(1.2, -2.3), breech(1.2, -1.9), breech(.65, -1.9)]) });
/** The dial's needle, fixed: the user's call, too small to read. */
const GAUGE = 3;
function body(a) {
  a.shape(rr(10.8, .5, 10.4, 30, .4), DARK, { lite: band(11, .6, 21, 1) });
  for (const [x, y] of [[11.6, 1.4], [20.4, 1.4], [11.6, 29.6], [20.4, 29.6]]) { a.flat(ell(x, y, .5), '#14151c'); a.flat(ell(x - .1, y - .12, .25), '#868fa2'); }
  box(a, 12.6, 1.2, 6.8, 28.2, GUN, { spec: true });
  a.flat(band(12.6, 5.6, 19.4, 5.85), '#262a42');
  box(a, 14.4, 29.4, 3.2, 1.8, GUN, { l: .3, b: .4 });
  a.shape(rr(11, 31, 9.8, 1, .3), DARK, { lite: band(11.2, 31, 20.6, 31.3) });
  // The arms, each under its pad's feet, with a gusset back to the spine and a lightening hole.
  wallPads.forEach(([px, py], slot) => {
    const fy = py + FEET, left = slot % 2 === 0, x0 = left ? .4 : 19.4, x1 = left ? 12.6 : 31.6, root = left ? 12.6 : 19.4, tip = left ? 6.6 : 25.4;
    const low = Math.min(fy + 5, 31.6), reach = low < fy + 5 ? root + (tip - root) * .45 : tip;
    a.shape(poly(root, fy + 1.3, root, low, reach, fy + 1.3), GUN, { lite: poly(root, fy + 1.3, reach, fy + 1.3, root + (reach - root) * .9, fy + 1.8, root, fy + 1.8) });
    if (low === fy + 5) a.flat(ell(root + (tip - root) * .28, fy + 2.4, .7), '#14151c');
    box(a, x0, fy, x1 - x0, 1.3, STEEL, { l: .35, b: .35, s: .3 });
    rivets(a, [left ? 12 : 20, fy + .65]);
  });
  rust(a, 1, 27.1, .4, 51); rust(a, 19.8, 16.4, .4, 52);
  // The regulator, its pipe down to the breech, the hose to the bottom left arm; the breech, tilted, its hinge pin.
  box(a, 13.4, 12.2, 5.2, 2.4, STEEL, { l: .4, b: .4 });
  a.tube('M14.8 14.6L14.6 17', .8, GUN);
  a.tube('M13.4 13.4C12.2 13.4 11.6 14.8 11.2 17.2', .9, RUBBER);
  a.shape(pg([breech(-.5, -2.4), breech(5.2, -2.4), breech(5.2, 2.4), breech(-.5, 2.4)]), DARK, { outer: false, deep: pg([breech(-.5, -2.4), breech(.2, -2.4), breech(.2, 2.4), breech(-.5, 2.4)]) });
  const pin = breech(-.2, 0);
  a.flat(ell(pin[0], pin[1], .55), '#474c68'); a.flat(ell(pin[0] - .1, pin[1] - .12, .25), '#c4cad4');
  gland(a, 11, 31.3);
  strap(a);
  dial(a, 16, 9.4, 2, GAUGE);
}
/** Lugs at the arms' ends that keep a drone from sliding off. */
function front(a) {
  wallPads.forEach(([px, py], slot) => {
    const fy = py + FEET, x = slot % 2 === 0 ? .4 : 30.7;
    a.shape(rr(x, fy - 1, .9, 1.1, .2), GUN, { lite: band(x, fy - 1, x + .9, fy - .75) });
  });
}

/** The parts' pictures: whole pixels from a pad's top-left (the lamps) or the bay's (the rest), w by h, drawn with a margin of PART round them. */
const PART = 1;
const LAMPS = { x: 4, y: 11, w: 6, h: 2 }, FUEL = { x: 11, y: 11, w: 10, h: 17 }, BEACON = { x: 14, y: 1, w: 4, h: 3 };
/** The canister's picture: its middle on the slot, a box of CAN_BOX round it with a margin of PART, its top-left in whole pixels. */
const CAN_BOX = 10, CAN_AT = [Math.floor(SLOT.x - CAN_BOX / 2), Math.floor(SLOT.y - CAN_BOX / 2)];
const part = (b, draw) => bake({ w: b.w, h: b.h, margin: PART, shift: [-b.x, -b.y], draw });
/** The lamp's middle in the slot and the beacon's: where their light is. */
const SLOT_LAMP = [16, 26.4], BEACON_LIGHT = [16, 2.6];

Subjects.bay = function () {
  out('machine_droneBay', bake({ w: W, h: H, draw: body }));
  out('droneBay_front', bake({ w: W, h: H, margin: PART, draw: front }));
  // One pad's lamps, drawn once, set under each pad: the pick's lamps sit alike on every arm.
  for (const [name, lv] of [['0', 0], ['1', 1], ['2', 2], ['3', 3], ['4', 4], ['low', 'L'], ['off', 'off']])
    out(`droneBay_lamps_${name}`, part(LAMPS, (a) => lamps(a, 4.85, FEET + .38, 1.15, 0, .8, .55, lv)));
  const fuel = (st) => part(FUEL, (a) => {
    vane(a, 17.5, 13.4, .7, st.flow);
    lever(a, 14.6, 13.3, st.valve, Math.PI, 1.3);
    slotLed(a, SLOT_LAMP[0], SLOT_LAMP[1], st);
  });
  out('droneBay_fuel_off', fuel({ power: 0, can: 0, flow: 0, valve: 0 }));
  out('droneBay_fuel_empty', fuel({ power: 1, can: 0, flow: 0, valve: 0 }));
  out('droneBay_fuel_can', fuel({ power: 1, can: 1, flow: 0, valve: 0 }));
  out('droneBay_fuel_feed_0', fuel({ power: 1, can: 1, flow: 1, valve: 1 }));
  out('droneBay_fuel_feed_1', fuel({ power: 1, can: 1, flow: 2, valve: 1 }));
  for (const on of [true, false]) out(`droneBay_beacon_${on ? 'on' : 'off'}`, part(BEACON, (a) => { a.flat(rr(14.6, 1.9, 2.8, 1.4, .3), '#14151c'); beaconLens(a, 14.9, 2.15, 2.2, .9, on); }));
  out('droneBay_canister', part({ x: CAN_AT[0], y: CAN_AT[1], w: CAN_BOX, h: CAN_BOX }, (a) => { canister(a, SLOT.x, SLOT.y, SLOT.th); strap(a); }));

  const pt = (p) => `(${p[0]}, ${p[1]})`, f = (v) => (+v.toFixed(4)) + 'f';
  writeText('../../features/logistics/logic/BayInk.cs', `// Generated by tools/anime-art/subjects/bay.js: export again, never edit.
namespace Stillwake.Game.Features.Logistics;

/// <summary>
/// The picked drone bay's numbers (wall): where its parts' pictures go, in the world's pixels from the bay's top-left, each
/// picture's top-left with its margin of <see cref="Margin"/> outside the box it was drawn in.
/// </summary>
public static class BayInk
{
    /// <summary>The world's pixels <c>droneBay_front</c> and the parts' pictures reach past their boxes on every side.</summary>
    public const int Margin = ${PART};

    /// <summary>The four pads, slot by slot: where an ordinary docked drone's picture (<c>drone_dock</c>) is set by its top-left.</summary>
    public static readonly (int X, int Y)[] Pads = [${wallPads.map(pt).join(', ')}];

    /// <summary>How far under a pad's top-left the arm a docked drone stands on is: the feet's line.</summary>
    public const float Feet = ${f(FEET)};

    /// <summary>A pad's lamps' box (<c>droneBay_lamps_*</c>), from the pad's top-left: alike on every arm.</summary>
    public static readonly (int X, int Y) Lamps = ${pt([LAMPS.x, LAMPS.y])};

    /// <summary>The fuel slot's valve, vane and lamp's box (<c>droneBay_fuel_*</c>), and the beacon's (<c>droneBay_beacon_*</c>).</summary>
    public static readonly (int X, int Y) Fuel = ${pt([FUEL.x, FUEL.y])}, Beacon = ${pt([BEACON.x, BEACON.y])};

    /// <summary>The middles of the fuel slot's lamp and of the beacon: where their light is.</summary>
    public static readonly (float X, float Y) SlotLamp = (${f(SLOT_LAMP[0])}, ${f(SLOT_LAMP[1])}), BeaconLight = (${f(BEACON_LIGHT[0])}, ${f(BEACON_LIGHT[1])});

    /// <summary>
    /// The canister in the breech (<c>droneBay_canister</c>, its strap over it): its picture's box; its middle and its
    /// valve's way, radians; and how far from there it comes in as it slides into place.
    /// </summary>
    public static readonly (int X, int Y) Canister = ${pt(CAN_AT)};
    public const float BreechX = ${f(SLOT.x)}, BreechY = ${f(SLOT.y)}, BreechAngle = ${f(SLOT.th)}, SlideX = ${f(SLOT.from[0])}, SlideY = ${f(SLOT.from[1])};
}
`);
};
})();
