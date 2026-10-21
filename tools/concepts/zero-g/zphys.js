'use strict';
// ===== zero-g physics: approved item 10 on the game's swim (Hero.cs), and the game as it is now =====
// Units as engine.js: thousandths of a cell, 60 steps a second.
const ZDEF = { swim: 30, swimMs: 150, pushOff: 90, pushDrag: 0.6, burstMs: 100, burstX: 2.5, jpAccel: 8, jpTop: 250, jpDrag: 4, puffs: 1 };
const Z = { ...ZDEF };

function stepZ(w, h, inp, old) {
  const n = { ...h, thrust: null, rcs: null, pushed: null, bumped: null };
  const facing = inp.x || h.facing;
  const d = Math.sqrt(inp.x * inp.x + inp.y * inp.y);
  const thrust = inp.jet && inp.sprint && d ? { x: inp.x / d, y: inp.y / d } : null;
  let vx = h.vx, vy = h.vy;
  if (old) {
    // Hero.cs today: swimming is the keys' speed at once; worn, the jetpack adds 8 a step up to 250, drag 4.
    if (inp.jet) [vx, vy] = flown(vx, vy, inp, thrust, SPEC.jpAccel);
    else { vx = inp.x * SPEC.swim; vy = inp.y * SPEC.swim; }
    n.thrust = thrust; n.glide = false;
  } else {
    n.jetSteps = thrust ? (h.jetSteps || 0) + 1 : 0;
    const nrm = inp.jump ? wallNormal(w, h) : null;
    if (nrm && Z.pushOff > 0) {
      let dx = nrm.x, dy = nrm.y;
      if (d) { const kx = inp.x / d, ky = inp.y / d; if (kx * nrm.x + ky * nrm.y > 0.1) { dx = kx; dy = ky; } }
      vx = dx * Z.pushOff; vy = dy * Z.pushOff; n.pushed = { x: -nrm.x, y: -nrm.y }; n.glide = true;
    } else if (thrust) {
      const acc = Z.jpAccel * (n.jetSteps <= Math.round(Z.burstMs / STEP_MS) ? Z.burstX : 1);
      vx += thrust.x * acc; vy += thrust.y * acc; n.glide = false;
      const s = Math.sqrt(vx * vx + vy * vy); if (s > Z.jpTop) { vx = vx / s * Z.jpTop; vy = vy / s * Z.jpTop; }
      n.thrust = thrust;
    } else {
      const tx = inp.x * Z.swim, ty = inp.y * Z.swim, ox = vx - tx, oy = vy - ty, off = Math.sqrt(ox * ox + oy * oy);
      const sp = Math.sqrt(vx * vx + vy * vy), fast = sp > Z.swim + 1;
      if (!fast) n.glide = false;
      // a glide off a wall fades slowly; a jetpack's speed is braked by its gas
      const rate = fast ? (inp.jet && !h.glide ? Z.jpDrag : Z.pushDrag) : Z.swim / Math.max(1, Z.swimMs / STEP_MS);
      if (off <= rate) { vx = tx; vy = ty; } else { vx -= ox / off * rate; vy -= oy / off * rate; }
      if (fast && inp.jet && !h.glide && Z.puffs) n.rcs = { x: vx, y: vy };
    }
  }
  const rx = Math.round(vx), ry = Math.round(vy);
  let nx = moveX(w, h.x, h.y, rx), ny = moveY(w, nx, h.y, ry, false);
  if (!clear(w, nx, ny)) { if (clear(w, nx, h.y)) ny = h.y; else { nx = h.x; ny = h.y; } }
  const before = Math.sqrt(vx * vx + vy * vy);
  const stopX = nx !== h.x + rx, stopY = ny !== h.y + ry;
  if (stopX) vx = 0;
  if (stopY) vy = 0;
  if (!old && before > 25 && ((stopX && rx) || (stopY && ry))) n.bumped = { x: stopX && rx ? Math.sign(rx) : 0, y: stopY && ry ? Math.sign(ry) : 0, speed: before };
  if (old) { vx = nx - h.x; vy = ny - h.y; }
  return { ...n, x: nx, y: ny, vx, vy, dx: nx - h.x, dy: ny - h.y, climbing: false, facing };
}

// ---------- the rooms: '#' wall, '.' back wall, ' ' open to the stars, 'h' a ladder ----------
const ZMAPS = {
  drift: ['############',
          '#..........#',
          '#...   ....#',
          '#...   ....#',
          '#..........#',
          '#..........#',
          '#..........#',
          '#..........#',
          '#..........#',
          '############'],
  swim: ['########################',
         '#......................#',
         '#......................#',
         '#........    ..........#',
         '#........    ..........#',
         '#......................#',
         '#......................#',
         '#......................#',
         '#......................#',
         '#......................#',
         '#......................#',
         '########################'],
  wall: ['################',
         '#..............#',
         '#..............#',
         '#...h..........#',
         '#...h......  ..#',
         '#...h......  ..#',
         '#...h..........#',
         '#...h..........#',
         '#...h..........#',
         '#...h..........#',
         '#..............#',
         '################'],
  jet: ['######################',
        '#....................#',
        '#....................#',
        '#.........   ........#',
        '#.........   ........#',
        '#....................#',
        '#....................#',
        '#....................#',
        '#....................#',
        '#....................#',
        '#....................#',
        '#....................#',
        '#....................#',
        '######################'],
  brake: ['##############################',
          '#............................#',
          '#............................#',
          '#.........    ...............#',
          '#............................#',
          '#............................#',
          '#............................#',
          '#............................#',
          '#............................#',
          '##############################'],
  push: ['################',
         '#..............#',
         '#..............#',
         '#.......   ....#',
         '#..............#',
         '#..............#',
         '#..............#',
         '#..............#',
         '#..............#',
         '################'],
  bump: ['#######',
         '#.....#',
         '#.....#',
         '#.....#',
         '#.....#',
         '#.....#',
         '#.....#',
         '#.....#',
         '#.....#',
         '#######'],
  play: ['##################################',
         '#................................#',
         '#................................#',
         '#........h..........      .......#',
         '#........h..........      .......#',
         '#........h..........      .......#',
         '#........h......................#',
         '#........h..............####....#',
         '#........h..............####....#',
         '#........h......................#',
         '#........h......................#',
         '#................................#',
         '#....######.................#####',
         '#................................#',
         '#................................#',
         '##################################'],
};
for (const k in ZMAPS) { const rows = ZMAPS[k]; const m = { rows, g: 0, zg: true }; m.w = Math.max(...rows.map(r => r.length)); m.h = rows.length; m.rows = rows.map(r => r.padEnd(m.w, '#')); MAPS['z_' + k] = m; }

// ---------- what each state does: segments of [steps, keys]; the same keys for every panel ----------
const K = (x = 0, y = 0, o = {}) => ({ x, y, jump: !!o.jump, jumpHeld: !!o.jump, sprint: !!o.sprint, jet: false, ...o });
const STATES = [
  { id: 'drift', name: 'Дрейф', map: 'drift', at: [5.5, 7.2], face: 1, tl: [[420, K()]],
    what: 'Ничего не нажато. Сейчас поза уже спокойная: так стоит дрейф во всех вариантах.' },
  { id: 'swim', name: 'Плавание', map: 'swim', at: [7, 7.5], face: 1, jet: false,
    tl: [[20, K()], [95, K(1)], [45, K()], [70, K(-1)], [40, K()], [50, K(0, -1)], [35, K()], [50, K(1, 1)], [45, K()]],
    what: 'Стрелки без ранца: вправо, влево, вверх, по диагонали вниз. Здесь сейчас и гребут по-собачьи.' },
  { id: 'wall', name: 'У стены и лестницы', map: 'wall', at: [1.475, 9.6], face: -1,
    tl: [[25, K()], [80, K(0, -1)], [25, K()], [80, K(0, 1)], [20, K()], [100, K(1)], [25, K()], [80, K(0, -1)], [25, K()], [80, K(0, 1)], [30, K()]],
    what: 'Лицом к стене плывёт вверх и вниз вдоль неё, потом к лестнице и вдоль неё. Лестница в невесомости не ловит, тело плывёт (0092).' },
  { id: 'jet', name: 'Ранец по сторонам', map: 'jet', at: [4.5, 10.6], face: 1, jet: true,
    tl: [[25, K()], [14, K(1, 0, { sprint: true })], [55, K()], [12, K(0, -1, { sprint: true })], [55, K()], [14, K(-1, 0, { sprint: true })], [55, K()], [12, K(0, 1, { sprint: true })], [60, K()]],
    what: 'Shift со стрелкой: тяга вправо, вверх, влево, вниз. Каждый раз первые 100 мс рывок ×2,5, потом герой летит по инерции.' },
  { id: 'brake', name: 'Торможение', map: 'brake', at: [3, 7.6], face: 1, jet: true,
    tl: [[20, K()], [20, K(1, 0, { sprint: true })], [32, K()], [6, K(-1, 0, { sprint: true })], [45, K()], [20, K(-1, 0, { sprint: true })], [32, K()], [6, K(1, 0, { sprint: true })], [45, K()]],
    what: 'Разгон на ранце, потом ранец сам гасит скорость струйками газа, потом короткая встречная тяга.' },
  { id: 'push', name: 'Толчок от стены', map: 'push', at: [1.475, 7.6], face: 1,
    tl: [[40, K()], [1, K(1, 0, { jump: true })], [8, K(1)], [105, K()], [40, K()]],
    what: 'Space у стены: толчок на 5,4 кл/с, полёт по инерции около 5 клеток.' },
  { id: 'bump', name: 'Удар о стену', map: 'bump', at: [1.475, 7.6], face: 1, jet: true,
    tl: [[30, K()], [1, K(1, 0, { jump: true })], [7, K(1)], [50, K()], [30, K()], [1, K(-1, 0, { jump: true })], [7, K(-1)], [50, K()], [30, K()], [24, K(1, 0, { sprint: true })], [45, K()]],
    what: 'Толчок через узкую комнату: удар о стену на 3,4 кл/с, обратно, потом ранцем в стену на ~14 кл/с.' },
  { id: 'axe', name: 'С топором', map: 'swim', at: [7, 9.5], face: 1, jet: true, item: 'axe',
    tl: [[40, K()], [80, K(1)], [35, K()], [12, K(0, -1, { sprint: true })], [55, K()], [14, K(-1, 0, { sprint: true })], [60, K()]],
    what: 'Топор в ближней руке: плавание, тяга вверх, тяга влево.' },
  { id: 'drill', name: 'С дрелью', map: 'swim', at: [7, 9.5], face: 1, jet: true, item: 'drill',
    tl: [[40, K()], [80, K(1)], [35, K()], [12, K(0, -1, { sprint: true })], [55, K()], [14, K(-1, 0, { sprint: true })], [60, K()]],
    what: 'Дрель двумя руками (0078), в покое на 0,3 рад вниз: плавание, тяга вверх, тяга влево.' },
];
for (const s of STATES) s.len = s.tl.reduce((a, [n]) => a + n, 0);
function keysAt(state, t) {
  let at = t % state.len;
  for (const [n, k] of state.tl) { if (at < n) return { ...k, jump: k.jump && at === 0, jumpHeld: k.jump && at < 8, jet: !!state.jet }; at -= n; }
  return K();
}

if (typeof module !== 'undefined') module.exports = { stepZ, STATES, keysAt, Z, ZDEF };
