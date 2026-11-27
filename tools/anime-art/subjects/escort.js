// The guard robot A1 «Конвоир», as picked (derelict-guards): ported from the pick page's drawA1 (ROBOTS[0], leg2, clawArm, pincer,
// drawGun 'rail', head, lamp), in cells as the page drew it, 8 world pixels a cell, facing right, y down. Each part is its own picture, its
// pivot its origin; a bone hangs straight down from its pivot (+y) and the game turns it. `torso` and `shoulder` keep their place about the
// feet's middle. Shining parts (`gun_coil`, `lamp`, `lamp_heart`) are plain. Pivots go into Content/logic/EscortInk.cs; the kit is guards.js's.
(function () {
'use strict';
const { ell } = Anime;

Subjects.escort = function () {
  const G = window.GuardKit, { pal, mix, INK, STEEL, STEEL_D, DARK, GUNM, RUST_D, ring, path, rect, wear, cells, cellBox } = G;
  const COPPER = pal('#9a6842'), ENAMEL1 = pal('#b8b3a2'), COIL_ON = '#a8ecff', RED = '#ff2b1c', STENCIL = '#2b2f35';
  const k = cells(0, 0);
  const { X, Y, R8, thin, solid, stripes, rivet, cable, hose } = k;

  /** A bone from (0, 0) straight down `L` cells, `w` wide, a little past both joints as the page's limb. */
  const bone = (a, L, w, m) => a.shape(path(R8(-w / 2, -.06, w, L + .12)), m, { shade: 1, deep: .4, lite: path(R8(-w / 2, -.06, .07, L + .12)) });
  /** A limb between two points, as the page's: `w` wide, .06 past each end. */
  function limb(a, p, q, w, m) {
    const dx = q[0] - p[0], dy = q[1] - p[1], L = Math.hypot(dx, dy), nx = dy / L * w / 2, ny = -dx / L * w / 2, ux = dx / L * .06, uy = dy / L * .06;
    solid(a, [[p[0] + nx - ux, p[1] + ny - uy], [q[0] + nx + ux, q[1] + ny + uy], [q[0] - nx + ux, q[1] - ny + uy], [p[0] - nx - ux, p[1] - ny - uy]], m);
  }
  const disc = (a, c, r, m) => a.shape(ell(X(c[0]), Y(c[1]), r * 8), m, { shade: .5, deep: .2 });
  const boltRing = (a, c, r) => { for (let i = 0; i < 6; i++) rivet(a, c[0] + Math.cos(i * 1.047 + .3) * r, c[1] + Math.sin(i * 1.047 + .3) * r); };
  const rust = (a, x, y, r) => wear(a, X(x), Y(y), r * 8);
  /** A chip in the enamel: bare grey metal, rust at its heart. */
  function chip(a, x, y, r, m) {
    const cx = (m || k).X(x), cy = (m || k).Y(y);
    const blob = (rr) => path(ring(cx, cy, rr * 8, 9).map(([px, py], i) => [cx + (px - cx) * (.65 + .35 * ((i * 5) % 4) / 3), cy + (py - cy) * .8]));
    a.flat(blob(r), '#5c626a');
    a.flat(blob(r * .35), RUST_D);
  }
  const vents = (a, x, y, w, n, step) => { for (let i = 0; i < n; i++) a.flat(path(R8(x, y + i * step, w, step * .45)), '#1e2227'); };
  /** A yellow warning tag with an inked triangle and its bar. */
  function warnTag(a, x, y, s) {
    a.flat(path(R8(x, y, s, s)), '#d6a12c');
    const t = [[x + s / 2, y + s * .18], [x + s * .84, y + s * .8], [x + s * .16, y + s * .8]];
    for (let i = 0; i < 3; i++) cable(a, ...t[i], ...t[(i + 1) % 3], s * .09, INK);
    a.flat(path(R8(x + s * .46, y + s * .4, s * .08, s * .22)), INK);
  }
  /** Stencilled letters, drawn plainly over the finished picture (the pen has no letters), in world pixels about the pivot. */
  const stencil = (txt, x, y, size) => (c) => {
    c.globalAlpha = .85; c.fillStyle = STENCIL; c.font = `700 ${size * 8}px "JetBrains Mono", Consolas, monospace`;
    c.textAlign = 'center'; c.textBaseline = 'middle'; c.fillText(txt, x * 8, y * 8);
  };
  const withAfter = (q, after) => ({ ...q, after });

  /* ---------- legs: leg2 with LG = { l1 1.15, l2 1.15, ank .36, wt .52, ws .46, foot 'flat', piston, bolts, hose } ---------- */
  const LG = { l1: 1.15, l2: 1.15, ank: .36, wt: .52, ws: .46 };
  const thigh = (far) => (a) => {
    bone(a, LG.l1, LG.wt, far ? STEEL_D : STEEL);
    if (!far) hose(a, [-.2, .15], [-.21, 1.02], 0, .08, -.16);
    // The piston's cylinder from a quarter down the thigh to the knee, its rod over it; the rest of the rod is under the shin.
    cable(a, -.18, .29, -.17, 1.15, .12, far ? '#5a5048' : '#3b4048');
    cable(a, -.18, .29, -.17, 1.15, .06, far ? '#6a5a48' : '#b8bec4');
  };
  const shin = (far) => (a) => {
    bone(a, LG.l2, LG.ws, far ? DARK : GUNM);
    disc(a, [0, 0], LG.wt * .5, far ? DARK : COPPER);
    if (!far) boltRing(a, [0, 0], LG.wt * .36);
  };
  const foot = (far) => (a) => {
    const A = LG.ank;
    solid(a, [[-.42, 0], [.45, 0], [.62, A], [-.5, A]], far ? DARK : GUNM);
    if (!far) thin(a, .3, A - .16, .38, .16, DARK);
    disc(a, [0, 0], .12, DARK);
  };

  /* ---------- the gun: drawGun 'rail' at angle 0 about its pivot, len 2.35, cap, tag, stripe ---------- */
  const LEN = 2.35, COILS = [];
  for (let x = .45; x < LEN - .3; x += .36) COILS.push(x);
  function gun(a) {
    cable(a, -.75, .12, -1, .42, .09, '#1d2024');
    solid(a, rect(-.62, .22, .75, .28), COPPER);
    cable(a, -.4, .22, -.4, .5, .04, '#5a3b24'); cable(a, -.15, .22, -.15, .5, .04, '#5a3b24');
    solid(a, rect(-.75, -.24, 1, .48), GUNM);
    thin(a, .2, -.22, LEN - .2, .09, DARK);
    thin(a, .2, .13, LEN - .2, .09, DARK);
    for (const x of COILS) thin(a, x - .07, -.17, .14, .34, COPPER);
    solid(a, rect(LEN - .22, -.27, .24, .54), GUNM);
    stripes(a, LEN - .6, -.3, .32, .08, .08);
    rivet(a, -.5, -.1); rivet(a, -.1, -.1);
    warnTag(a, -.45, -.12, .26);
    rust(a, .15, .24, .07);
  }
  /** The coils charged: only they, lit, for the game to fade in. */
  function coils(c) {
    const lite = mix(COIL_ON, '#ffffff', .4);
    for (const x of COILS) {
      c.fillStyle = COIL_ON; c.fillRect((x - .07) * 8, -.17 * 8, .14 * 8, .34 * 8);
      c.fillStyle = lite; c.fillRect((x - .07) * 8, -.17 * 8, .14 * 8, .07 * 8);
      c.fillStyle = '#ffffff'; c.fillRect((x - .02) * 8, -.12 * 8, .04 * 8, .24 * 8);
    }
  }

  /* ---------- the body: all of drawA1 but legs, gun, head and claw arm, about the feet's middle ---------- */
  function torso(a) {
    limb(a, [-.72, -4.4], [-.5, -4.92], .26, DARK);
    solid(a, [[-1.45, -4.2], [-.95, -4.25], [-.95, -2.9], [-1.38, -2.98]], DARK);
    for (let i = 0; i < 5; i++) cable(a, -1.4, -4 + i * .22, -1, -4 + i * .22, .05, '#23272d');
    rust(a, -1.25, -3.02, .1);
    hose(a, [-1.15, -4.22], [-.25, -4.78], -.4);
    solid(a, rect(-.72, -2.8, 1.4, .55), DARK); vents(a, -.6, -2.7, 1.1, 3, .13);
    a.shape(path(k.at([[-1, -4.5], [.7, -4.5], [1.05, -3.7], [.85, -2.65], [-.85, -2.65], [-1.1, -3.6]])), STEEL, { shade: 1.2, deep: .5, lite: path(R8(-1, -4.5, 2, .16)) });
    a.shape(path(k.at([[-.88, -4.4], [.64, -4.4], [.92, -3.78], [-.92, -3.78]])), ENAMEL1, { shade: .5, lite: path(R8(-.88, -4.4, 2, .08)) });
    a.shape(path(k.at([[-.78, -3.64], [.88, -3.64], [.74, -2.82], [-.76, -2.82]])), ENAMEL1, { shade: .5, lite: path(R8(-.78, -3.64, 2, .08)) });
    cable(a, -.82, -3.92, .85, -3.92, .07, '#9a3a26');
    warnTag(a, .32, -3.45, .28);
    vents(a, -.6, -3.45, .7, 4, .12);
    chip(a, .62, -4.36, .1); chip(a, -.85, -3.84, .09); chip(a, .78, -2.92, .11); chip(a, -.7, -2.88, .08);
    rivet(a, -.8, -4.32); rivet(a, .55, -4.32); rivet(a, -.7, -2.92); rivet(a, .66, -2.92);
    rust(a, .3, -2.7, .12); rust(a, -.98, -3.62, .08);
  }

  /* ---------- the head about its pivot (0.1, -4.5): antenna, enamel box, camera, its lamp dark, window, «07» ---------- */
  const HEAD = [.1, -4.5], LAMP = [.8, -4.71], LR = .16;
  function head(a) {
    const h = cells(HEAD[0], HEAD[1]);
    h.cable(a, -.3, -5.02, -.42, -5.38, .06, '#2a2e35'); a.flat(path(h.R8(-.47, -5.42, .1, .08)), '#9a3a26');
    h.solid(a, [[-.4, -5.05], [.5, -5.05], [.6, -4.95], [.6, -4.5], [-.4, -4.5]], ENAMEL1);
    h.solid(a, rect(.5, -4.93, .34, .44), DARK);
    h.thin(a, .82, -4.97, .1, .52, GUNM);
    a.flat(ell(h.X(LAMP[0]), h.Y(LAMP[1]), (LR + .05) * 8), '#121418');
    a.flat(ell(h.X(LAMP[0]), h.Y(LAMP[1]), LR * 8), '#3a1915');
    a.flat(path(h.R8(LAMP[0] - LR * .1, LAMP[1] - LR * .6, LR * .5, LR * .2)), '#5a2a22');
    h.thin(a, .02, -5, .32, .14, DARK); a.flat(path(h.R8(.24, -4.97, .07, .07)), '#3d5560');
    chip(a, -.32, -4.58, .07, h);
  }

  /* ---------- the claw arm: upper from the shoulder, fore from the elbow, the pincer and its jaws ---------- */
  const clawUpper = (a) => {
    bone(a, 1, .46, STEEL);
    cable(a, -.12, .25, .02, 1, .07, '#a9afb5');
    hose(a, [-.2, .05], [-.18, .95], 0, .08, -.2);
  };
  const clawFore = (a) => {
    bone(a, 1.4, .5, GUNM);
    disc(a, [0, 0], .46 * .45, COPPER);
    boltRing(a, [0, 0], .46 * .32);
  };
  const pincer = (a) => { solid(a, rect(-.12, -.25, .42, .5), DARK); rivet(a, .1, 0); };
  const jawUpper = (a) => solid(a, [[0, -.12], [.5, -.17], [.82, -.02], [.8, .1], [.62, 0], [0, .06]], STEEL);
  const jawLower = (a) => solid(a, [[0, -.06], [.62, 0], [.8, -.1], [.82, .02], [.5, .17], [0, .12]], STEEL_D);
  /** The copper shoulder pad, its hazard band cut to its top edge. */
  function shoulder(a) {
    const pad = [[.02, -4.52], [.85, -4.46], [.98, -4], [.2, -3.82]];
    a.shape(path(k.at(pad)), COPPER, { shade: 1, deep: .4, lite: path(R8(0, -4.52, 1, .1)) });
    const band = path(k.at([[.1, -4.5], [.297, -4.5], [.85, -4.46], [.881, -4.34], [.1, -4.34]]));
    a.flat(band, '#d6a12c');
    for (let s = -.16 - .85; s < .85 + .16; s += .32) a.flat(path(k.at([[.1 + s, -4.34], [.26 + s, -4.34], [.42 + s, -4.5], [.26 + s, -4.5]])), '#17181b', { clip: band });
    rivet(a, .3, -4.1); rivet(a, .7, -4.1); rust(a, .85, -3.98, .08);
  }

  // In the game's draw order. Bounds in cells about each pivot.
  const parts = G.snapAll({
    thighFar: cellBox(-.45, -.2, .35, 1.3, thigh(true)),
    shinFar: cellBox(-.35, -.35, .35, 1.3, shin(true)),
    footFar: cellBox(-.62, -.2, .78, .48, foot(true)),
    gun: cellBox(-1.15, -.4, 2.45, .62, gun),
    gun_coil: cellBox(-1.15, -.4, 2.45, .62, coils, true),
    torso: withAfter(cellBox(-1.6, -5.1, 1.15, -2.15, torso), stencil('ОХР-07', -.12, -4.1, .3)),
    head: withAfter(cellBox(-.67, -1.02, 1.02, .15, head), stencil('07', -.05 - HEAD[0], -4.72 - HEAD[1], .24)),
    lamp: cellBox(-.22, -.22, .22, .22, (c) => { c.fillStyle = RED; c.beginPath(); c.arc(0, 0, LR * 8, 0, Math.PI * 2); c.fill(); }, true),
    lamp_heart: cellBox(-.22, -.22, .22, .22, (c) => { c.fillStyle = '#ffd0c4'; c.beginPath(); c.arc(LR * .25 * 8, -LR * .25 * 8, LR * .38 * 8, 0, Math.PI * 2); c.fill(); }, true),
    thigh: cellBox(-.45, -.2, .35, 1.3, thigh(false)),
    shin: cellBox(-.35, -.35, .35, 1.3, shin(false)),
    foot: cellBox(-.62, -.2, .78, .48, foot(false)),
    clawUpper: cellBox(-.45, -.2, .32, 1.15, clawUpper),
    clawFore: cellBox(-.32, -.3, .32, 1.55, clawFore),
    pincer: cellBox(-.2, -.33, .38, .33, pincer),
    jawUpper: cellBox(-.1, -.27, .92, .2, jawUpper),
    jawLower: cellBox(-.1, -.2, .92, .27, jawLower),
    shoulder: cellBox(-.08, -4.62, 1.08, -3.72, shoulder),
  });
  G.bakeAll('inkEscort', parts);
  writeText('../../Content/logic/EscortInk.cs', G.generated('escort.js', G.partsDesign('InkEscort', 'inkEscort',
    'The guard robot A1 «Конвоир» in ink, its parts in <c>art/hd/creatures/inkEscort</c>, each about its pivot, facing right.', parts)));
};
})();
