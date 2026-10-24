'use strict';
// ===== Stillwake hero, ported from game/features/crew/logic/Hero.cs, Pose.cs, Rig.cs, Figure.cs =====
// Units: thousandths of a cell; 60 controller steps a second; 16 px a cell.
const U = 1000, HW = 475, HH = 2625, MID = 1312, STEP_MS = 1000 / 60, CELL = 16;
const SPEC = { walk: 120, run: 220, climb: 180, jump: 420, g: 21, maxFall: 1200, swim: 30, jpAccel: 8, jpTop: 250, jpDrag: 4 };
const DEF = {
  coyoteMs: 100, bufferMs: 100, cut: 0.5, apexThr: 60, apexMult: 0.6, fallMult: 1.15, jump: 415,
  walkAccelMs: 67, runAccelMs: 117, stopMs: 67, airCtl: 0.65, corner: 250, ledge: 0,
  landScale: 1, lean: 1, packLag: 1, antenna: 1, idleLife: 1, skid: 1, airBlend: 1,
  climbUp: 110, climbDown: 150, slide: 300, grabRising: 1, climbStride: 2.2,
  pushOff: 90, pushDrag: 0.6, swimMs: 150, burst: 1, rcs: 1,
  planetFallMult: 1.5,
  camLead: 3, camSmoothMs: 180, camSnap: 1,
};
const P = { ...DEF };
const cellOf = v => Math.floor(v / U);
const clamp = (v, a, b) => Math.max(a, Math.min(b, v));
const approach = (v, t, d) => v < t ? Math.min(t, v + d) : Math.max(t, v - d);

// ---------- scenes ----------
const MAPS = {
  deck: { g: 21, rows: [
    '################################################',
    '#..............................................#',
    '#..............................................#',
    '#..............................................#',
    '#..............................................#',
    '#..................................####........#',
    '#..................................####..#####.#',
    '#..................====......====..####........#',
    '#.......##.........................####........#',
    '#.......##.........................####........#',
    '##########....########....######################',
    '##########....########....######################'] },
  flat: { g: 21, rows: [
    '##############################################',
    '#............................................#',
    '#............................................#',
    '#............................................#',
    '#............................................#',
    '#............................................#',
    '#............................................#',
    '##############################################'] },
  gap: { g: 21, rows: [
    '############################',
    '#..........................#',
    '#..........................#',
    '#..........................#',
    '#..........................#',
    '#..........................#',
    '#..........................#',
    '##########....##############',
    '##########....##############'] },
  drop: { g: 21, rows: [
    '##############################',
    '#............................#',
    '#............................#',
    '#............................#',
    '#######......................#',
    '#######......................#',
    '#######......................#',
    '#######......................#',
    '#######......................#',
    '#######......................#',
    '#######......................#',
    '##############################'] },
  ladder: { g: 21, rows: [
    '######################',
    '#....................#',
    '#....................#',
    '#....................#',
    '#....................#',
    '####H######.....H....#',
    '#...H...........H....#',
    '#...H...........H....#',
    '#...H...........H....#',
    '#...H...........H....#',
    '#...H................#',
    '#...H................#',
    '#...H................#',
    '######################'] },
  zerog: { g: 21, rows: [
    '##########################',
    '#~~~~~~~~~~~~~~~~~~~~~~~~#',
    '#~~~~~~~~~~~~~~~~~~~~~~~~#',
    '#~~~~~~~~~~~~~~~~~~~~~~~~#',
    '#~~~~~~~###~~~~~~~~~~~~~~#',
    '#~~~~~~~###~~~~~~~~~~~~~~#',
    '#~~~~~~~~~~~~~~~~~~~~~~~~#',
    '#~~~~~~~~~~~~~~~~~##~~~~~#',
    '#~~~~~~~~~~~~~~~~~##~~~~~#',
    '#~~~~~~~~~~~~~~~~~~~~~~~~#',
    '#~~~~~~~~~~~~~~~~~~~~~~~~#',
    '#~~~~~~~~~~~~~~~~~~~~~~~~#',
    '#~~~~~~~~~~~~~~~~~~~~~~~~#',
    '##########################'] },
  planet: { g: 14, planet: true, rows: [
    '##################################################',
    '#................................................#',
    '#................................................#',
    '#................................................#',
    '#................................................#',
    '#..................................######........#',
    '#..................................######........#',
    '#........................####......######........#',
    '#........................####......######........#',
    '#...........##...........####......######........#',
    '#...........##...........####......######........#',
    '################......############################',
    '################......############################'] },
};
for (const k in MAPS) { const m = MAPS[k]; m.w = Math.max(...m.rows.map(r => r.length)); m.h = m.rows.length; m.rows = m.rows.map(r => r.padEnd(m.w, '#')); }

class Grid {
  constructor(map) { this.m = map; this.w = map.w; this.h = map.h; this.g = map.g; this.planet = !!map.planet; }
  ch(c, r) { return c < 0 || r < 0 || c >= this.w || r >= this.h ? '#' : this.m.rows[r][c]; }
  solid(c, r) { return this.ch(c, r) === '#'; }
  ladder(c, r) { return this.ch(c, r) === 'H'; }
  afloat(c, r) { const k = this.ch(c, r); return k === '~' || k === ' '; }
  holds(c, r, climbing) { const k = this.ch(c, r); return k === '=' || (!climbing && k === 'H' && this.ch(c, r - 1) !== 'H'); }
}

function moveX(w, x, y, dx) {
  if (!dx) return x;
  const top = cellOf(y - HH), bot = cellOf(y - 1), to = x + dx;
  if (dx > 0) { for (let c = cellOf(x + HW - 1) + 1; c <= cellOf(to + HW - 1); c++) for (let r = top; r <= bot; r++) if (w.solid(c, r)) return c * U - HW; }
  else { for (let c = cellOf(x - HW) - 1; c >= cellOf(to - HW); c--) for (let r = top; r <= bot; r++) if (w.solid(c, r)) return (c + 1) * U + HW; }
  return to;
}
function moveY(w, x, y, dy, climbing) {
  const l = cellOf(x - HW), rr = cellOf(x + HW - 1);
  if (dy > 0) {
    for (let r = Math.ceil(y / U); r * U <= y + dy; r++)
      for (let c = l; c <= rr; c++) if (w.solid(c, r) || w.holds(c, r, climbing)) return r * U;
    return y + dy;
  }
  if (dy < 0) {
    const head = y - HH;
    for (let r = cellOf(head - 1e-6); r >= cellOf(head + dy); r--) {
      if ((r + 1) * U > head + 1e-6) continue;
      for (let c = l; c <= rr; c++) if (w.solid(c, r)) return (r + 1) * U + HH;
    }
    return y + dy;
  }
  return y;
}
const grounded = (w, x, y) => moveY(w, x, y, 1, false) === y;
function touchesLadder(w, x, y, belowFeet) {
  const col = cellOf(x), last = cellOf(belowFeet ? y : y - 1);
  for (let r = cellOf(y - HH); r <= last; r++) if (w.ladder(col, r)) return true;
  return false;
}
function onFloors(w, x, y) {
  const row = cellOf(y); let floor = false;
  for (let c = cellOf(x - HW); c <= cellOf(x + HW - 1); c++) { if (w.solid(c, row) || w.ladder(c, row)) return false; floor = floor || w.ch(c, row) === '='; }
  return floor;
}
function clear(w, x, y) {
  for (let c = cellOf(x - HW); c <= cellOf(x + HW - 1); c++) for (let r = cellOf(y - HH); r <= cellOf(y - 1); r++) if (w.solid(c, r)) return false;
  return true;
}
const bodyAfloat = (w, h) => w.afloat(cellOf(h.x), cellOf(h.y - MID));

function climbTo(w, x, y, dir, speed, facing, h) {
  const ny = moveY(w, x, y, dir * speed, true);
  const col = cellOf(x), before = cellOf(y - 1), after = cellOf(ny - 1);
  if (after < before && w.ladder(col, before) && !w.ladder(col, after)) return { ...h, x, y: before * U, vx: 0, vy: 0, dx: x - h.x, dy: before * U - h.y, climbing: false, facing, topped: true };
  return { ...h, x, y: ny, vx: 0, vy: ny - y, dx: x - h.x, dy: ny - h.y, climbing: touchesLadder(w, x, ny, false), facing };
}

// ---------- the game's controller as it is now ----------
function stepNow(w, h, inp) {
  const facing = inp.x || h.facing;
  const base = { ...h, topped: false, ev: null };
  if (bodyAfloat(w, base)) return swimNow(w, base, inp, facing);
  const grab = !h.climbing && h.vy >= 0 && inp.y !== 0 && inp.x === 0 && !inp.jump && touchesLadder(w, h.x, h.y, inp.y > 0);
  if (grab || h.climbing) {
    const holdsL = !inp.jump && (inp.x === 0 || (h.climbing && !grounded(w, h.x, h.y)));
    if (holdsL) {
      const cx = cellOf(h.x) * U + U / 2;
      const c = climbTo(w, cx, h.y, inp.y, SPEC.climb, facing, base);
      if (clear(w, c.x, c.y)) return c;
      if (h.climbing) return { ...base, vx: 0, vy: 0, dx: 0, dy: 0, facing };
    } else return fallNow(w, base, inp.x, inp.jump ? -SPEC.jump : 0, facing, inp.sprint, false);
  }
  const vy = inp.jump && grounded(w, h.x, h.y) ? -SPEC.jump : h.vy;
  return fallNow(w, base, inp.x, vy, facing, inp.sprint, inp.y > 0 && !inp.jump);
}
function fallNow(w, h, dir, vy, facing, sprint, down) {
  const { x, y } = h;
  const speed = Math.min(vy + w.g, SPEC.maxFall);
  const standing = vy >= 0 && grounded(w, x, y);
  const nx = moveX(w, x, y, dir * (sprint ? SPEC.run : SPEC.walk));
  const ny = standing && down && onFloors(w, nx, y) ? moveY(w, nx, y + 1, speed - 1, false) : moveY(w, nx, y, speed, false);
  return { ...h, x: nx, y: ny, vx: nx - x, vy: Math.abs(ny - (y + speed)) < 1e-6 ? speed : 0, dx: nx - x, dy: ny - y, climbing: false, facing };
}
function flown(vx, vy, inp, thrust, accel) {
  let x = vx, y = vy;
  if (thrust) { x += thrust.x * accel; y += thrust.y * accel; }
  else {
    const tx = inp.x * SPEC.swim, ty = inp.y * SPEC.swim, dx = x - tx, dy = y - ty, off = Math.sqrt(dx * dx + dy * dy);
    if (off <= SPEC.jpDrag) { x = tx; y = ty; } else { x -= dx / off * SPEC.jpDrag; y -= dy / off * SPEC.jpDrag; }
  }
  const s = Math.sqrt(x * x + y * y);
  if (s > SPEC.jpTop) { x = x / s * SPEC.jpTop; y = y / s * SPEC.jpTop; }
  return [Math.round(x), Math.round(y)];
}
function thrustOf(inp) {
  if (!inp.jet || !inp.sprint || (!inp.x && !inp.y)) return null;
  const d = Math.sqrt(inp.x * inp.x + inp.y * inp.y);
  return { x: inp.x / d, y: inp.y / d };
}
function swimNow(w, h, inp, facing) {
  const thrust = thrustOf(inp);
  const [vx, vy] = inp.jet ? flown(h.vx, h.vy, inp, thrust, SPEC.jpAccel) : [inp.x * SPEC.swim, inp.y * SPEC.swim];
  let nx = moveX(w, h.x, h.y, vx), ny = moveY(w, nx, h.y, vy, false);
  if (!clear(w, nx, ny)) { if (clear(w, nx, h.y)) ny = h.y; else { nx = h.x; ny = h.y; } }
  return { ...h, x: nx, y: ny, vx: nx - h.x, vy: ny - h.y, dx: nx - h.x, dy: ny - h.y, climbing: false, facing, thrust };
}

// ---------- the proposal ----------
const steps = ms => Math.round(ms / STEP_MS);
function stepNew(w, h, inp) {
  const n = { ...h, topped: false, ev: null, thrust: null, rcs: null, pushed: null, slide: false };
  n.sinceLadder = (h.sinceLadder ?? 99) + 1;
  n.buffer = inp.jump ? steps(P.bufferMs) + 1 : Math.max(0, (h.buffer || 0) - 1);
  n.bufferAge = inp.jump ? 0 : (h.bufferAge || 0) + 1;
  const facing = inp.x || h.facing;
  if (bodyAfloat(w, n)) return swimNew(w, n, inp, facing);
  const gr = h.vy >= 0 && grounded(w, h.x, h.y);
  n.coyote = gr ? steps(P.coyoteMs) : Math.max(0, (h.coyote || 0) - 1);
  n.offFor = gr ? 0 : (h.offFor || 0) + 1;

  // ladders: up grabs even walking or rising; down grabs as before
  let grab = false;
  if (!h.climbing && !inp.jump) {
    if (inp.y < 0 && n.sinceLadder > 12 && (P.grabRising || h.vy >= 0) && touchesLadder(w, h.x, h.y, false)) grab = true;
    if (inp.y > 0 && inp.x === 0 && h.vy >= 0 && touchesLadder(w, h.x, h.y, true)) grab = true;
  }
  if (grab || h.climbing) {
    const onGround = grounded(w, h.x, h.y);
    if (inp.jump) {
      n.climbing = false; n.sinceLadder = 0; n.buffer = 0; n.cutArmed = true;
      n.vy = -P.jump * (inp.x ? 0.85 : 1); n.vx = inp.x * SPEC.run * 0.9; n.ev = inp.x ? 'прыжок вбок' : null;
      return integrate(w, n, inp, facing, false);
    }
    if (!(inp.x !== 0 && onGround)) {
      const cx = cellOf(h.x) * U + U / 2;
      let x = h.x + Math.round((cx - h.x) * 0.45); if (Math.abs(cx - x) < 25) x = cx;
      if (!clear(w, x, h.y)) x = h.x;
      const slide = inp.y > 0 && inp.sprint;
      const speed = inp.y < 0 ? P.climbUp : slide ? P.slide : P.climbDown;
      const c = climbTo(w, x, h.y, inp.y, speed, facing, n);
      c.vx = 0; c.slide = slide && c.climbing;
      if (grab && !h.climbing && h.vy < -60) c.ev = 'схватил в прыжке';
      if (c.topped) c.ev = 'вылез';
      if (clear(w, c.x, c.y)) return c;
      if (h.climbing) return { ...n, vx: 0, vy: 0, dx: 0, dy: 0, facing };
    } else { n.climbing = false; n.sinceLadder = 0; }
  }

  // run: speed eased to the keys' target, short both ways
  const top = inp.sprint ? SPEC.run : SPEC.walk, target = inp.x * top, ctl = gr ? 1 : P.airCtl;
  let vx = h.vx;
  if (inp.x !== 0 && (Math.sign(vx) === inp.x || vx === 0) && Math.abs(vx) <= top) {
    const ms = inp.sprint ? P.runAccelMs : P.walkAccelMs;
    vx = ms <= 0 ? target : approach(vx, target, top / Math.max(1, ms / STEP_MS) * ctl);
  } else {
    vx = P.stopMs <= 0 ? target : approach(vx, target, SPEC.walk / Math.max(1, P.stopMs / STEP_MS) * ctl);
  }
  n.vx = vx;
  // jump: buffered press, coyote grace
  let vy = h.vy;
  if (n.buffer > 0 && (gr || n.coyote > 0)) {
    vy = -P.jump;
    if (!gr) n.ev = 'койот +' + Math.round(n.offFor * STEP_MS) + ' мс';
    else if (!inp.jump) n.ev = 'буфер ' + Math.round(n.bufferAge * STEP_MS) + ' мс';
    n.buffer = 0; n.coyote = 0; n.cutArmed = true;
  }
  n.vy = vy;
  return integrate(w, n, inp, facing, gr);
}
function integrate(w, n, inp, facing, gr) {
  const { x, y } = n;
  let vy = n.vy;
  if (n.cutArmed && !inp.jumpHeld && vy < 0) { vy *= P.cut; n.cutArmed = false; n.ev = n.ev || 'срез'; }
  if (vy >= 0) n.cutArmed = false;
  let g = w.g;
  if (!gr || vy < 0) {
    if (Math.abs(vy) < P.apexThr && inp.jumpHeld) g *= P.apexMult;
    else if (vy > 0) g *= w.planet ? P.planetFallMult : P.fallMult;
  }
  const speed = Math.min(vy + g, SPEC.maxFall);
  const standing = vy >= 0 && gr;
  const want = Math.round(n.vx);
  let nx = moveX(w, x, y, want);
  // ledge assist: a jump that meets a wall's top edge by the feet is lifted onto it
  if (nx !== x + want && !standing && speed > -80 && P.ledge > 0) {
    const dir = Math.sign(want), row = cellOf(y - 1), lt = row * U;
    if (y - lt <= P.ledge && clear(w, x + want, lt) && clear(w, x, lt)) { n.x = x + want; n.y = lt; n.vy = 0; n.dx = want; n.dy = lt - y; n.ev = 'подтянулся'; n.climbing = false; n.facing = facing; return n; }
    n.vx = 0;
  } else if (nx !== x + want) n.vx = 0;
  const down = inp.y > 0 && !inp.jump;
  let ny = standing && down && onFloors(w, nx, y) ? moveY(w, nx, y + 1, speed - 1, false) : moveY(w, nx, y, speed, false);
  // corner correction: a head that clips a ceiling's edge slides past it
  if (speed < 0 && ny > y + speed + 0.5 && P.corner > 0) {
    const pref = Math.sign(n.vx) || facing;
    outer: for (let d = 50; d <= P.corner; d += 50) for (const s of [pref, -pref]) {
      const tx = nx + s * d;
      if (clear(w, tx, y) && moveY(w, tx, y, speed, false) === y + speed && moveX(w, nx, y, s * d) === tx) { nx = tx; ny = y + speed; n.ev = 'угол'; break outer; }
    }
  }
  n.x = nx; n.y = ny; n.dx = nx - x; n.dy = ny - y; n.climbing = false; n.facing = facing;
  n.vy = Math.abs(ny - (y + speed)) < 1e-6 ? speed : 0;
  return n;
}
function wallNormal(w, h) {
  let nx = 0, ny = 0; const d = 60;
  if (!clear(w, h.x - d, h.y)) nx += 1; if (!clear(w, h.x + d, h.y)) nx -= 1;
  if (!clear(w, h.x, h.y + d) || moveY(w, h.x, h.y, d, false) < h.y + d) ny -= 1; if (!clear(w, h.x, h.y - d)) ny += 1;
  if (!nx && !ny) return null;
  const l = Math.sqrt(nx * nx + ny * ny); return { x: nx / l, y: ny / l };
}
function swimNew(w, n, inp, facing) {
  let vx = n.vx, vy = n.vy;
  const thrust = thrustOf(inp);
  if (inp.jet) {
    n.jetSteps = thrust ? (n.jetSteps || 0) + 1 : 0;
    const acc = SPEC.jpAccel * (P.burst && n.jetSteps <= 6 ? 2.5 : 1);
    const sp = Math.sqrt(vx * vx + vy * vy);
    [vx, vy] = flown(vx, vy, inp, thrust, acc);
    if (!thrust && sp > SPEC.swim + 2 && P.rcs) n.rcs = { x: vx, y: vy };
    n.thrust = thrust;
  } else {
    const nrm = inp.jump ? wallNormal(w, n) : null;
    if (nrm && P.pushOff > 0) {
      let dx = nrm.x, dy = nrm.y;
      if (inp.x || inp.y) { const l = Math.sqrt(inp.x * inp.x + inp.y * inp.y), kx = inp.x / l, ky = inp.y / l; if (kx * nrm.x + ky * nrm.y > 0.1) { dx = kx; dy = ky; } }
      vx = dx * P.pushOff; vy = dy * P.pushOff; n.pushed = { x: -nrm.x, y: -nrm.y }; n.ev = 'оттолкнулся';
    } else {
      const tx = inp.x * SPEC.swim, ty = inp.y * SPEC.swim, ox = vx - tx, oy = vy - ty, off = Math.sqrt(ox * ox + oy * oy);
      const sp = Math.sqrt(vx * vx + vy * vy);
      const rate = sp > SPEC.swim + 1 ? P.pushDrag : SPEC.swim / Math.max(1, P.swimMs / STEP_MS);
      if (off <= rate) { vx = tx; vy = ty; } else { vx -= ox / off * rate; vy -= oy / off * rate; }
    }
  }
  let nx = moveX(w, n.x, n.y, Math.round(vx)), ny = moveY(w, nx, n.y, Math.round(vy), false);
  if (!clear(w, nx, ny)) { if (clear(w, nx, n.y)) ny = n.y; else { nx = n.x; ny = n.y; } }
  if (nx !== n.x + Math.round(vx)) vx = 0;
  if (ny !== n.y + Math.round(vy)) vy = 0;
  return { ...n, x: nx, y: ny, vx, vy, dx: nx - n.x, dy: ny - n.y, climbing: false, facing };
}

if (typeof module !== 'undefined') module.exports = { U, HW, HH, MID, SPEC, DEF, P, MAPS, Grid, stepNow, stepNew, grounded, clear, bodyAfloat, cellOf };
