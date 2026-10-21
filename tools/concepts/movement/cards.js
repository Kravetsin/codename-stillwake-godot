'use strict';
// ===== scripted runs for each card: the same keys for both heroes, pressed by where each one is =====
const I = (x = 0, y = 0, jump = false, jumpHeld = false, sprint = false, jet = false) => ({ x, y, jump, jumpHeld, sprint, jet });
const onGround = (r, h) => h.vy === 0 && grounded(r.grid, h.x, h.y);
// a press held for `n` steps from the step it was made
function press(m, key, t, n) { if (m[key] == null) m[key] = t; return { jump: t === m[key], held: t - m[key] < n }; }

const SCRIPTS = {
  // walks off the pit's edge and jumps three steps late; across, hops and presses again just before landing
  coyote(h, t, m, r) {
    if (t < 20) return I();
    if (!m.late && h.x - HW >= 10000 + 3 * SPEC.walk) m.late = t;
    if (m.late && t - m.late < 22 && !m.across) return I(1, 0, t === m.late, true);
    if (h.x > 15500 && onGround(r, h)) m.across = true;
    if (m.across && !m.hop && h.x > 17000) m.hop = t;
    if (m.hop && !m.buf && t - m.hop > 8 && h.vy > 0 && h.y < 7000 && (7000 - h.y) < 4 * h.vy) m.buf = t;
    const x = h.x < 23000 ? 1 : 0;
    if (m.buf) return I(x, 0, t === m.buf, t - m.buf < 25);
    if (m.hop) return I(x, 0, t === m.hop, t - m.hop < 25);
    return I(x);
  },
  // a tap, then a held jump, walking
  cut(h, t, m) {
    if (t < 20) return I();
    const x = t < 175 ? 1 : 0;
    if (t >= 30 && t < 33) return I(x, 0, t === 30, true);
    if (t >= 95 && t < 140) return I(x, 0, t === 95, true);
    return I(x);
  },
  // sprint right, turn and sprint left, let go; walk a little, stop
  runstop(h, t) {
    if (t < 20) return I();
    if (t < 70) return I(1, 0, false, false, true);
    if (t < 115) return I(-1, 0, false, false, true);
    if (t < 150) return I();
    if (t < 175) return I(1);
    return I();
  },
  // off a seven-cell ledge, then a tap and a full jump on the floor
  land(h, t, m, r) {
    if (t < 25) return I();
    if (!m.down && h.x < 9500) return I(1);
    m.down = true;
    if (t >= 120 && t < 123) return I(0, 0, t === 120, true);
    if (t >= 175 && t < 215) return I(1, 0, t === 175, true);
    return I();
  },
  // stands still for a long while, then runs a few steps, hops and stops
  idle(h, t) {
    if (t < 300) return I();
    if (t < 330) return I(1, 0, false, false, true);
    if (t < 345) return I();
    if (t >= 345 && t < 370) return I(0, 0, t === 345, true);
    if (t > 400 && t < 425) return I(-1, 0, false, false, true);
    return I();
  },
  // to the ladder and up; down again sliding; to the free ladder by a jump; off it sideways
  ladder(h, t, m, r) {
    if (t < 15) return I();
    const c = cellOf(h.x);
    if (!m.top) {
      if (!h.climbing && !(m.on)) { if (Math.abs(h.x - 4500) > 120) return I(h.x > 4500 ? -1 : 1, -1); m.on = true; }
      if (h.climbing || m.on) { if (!h.climbing && h.y <= 5000 && onGround(r, h)) { m.top = t; } else return I(0, -1); }
    }
    if (m.top && t - m.top < 25) return I();
    if (m.top && !m.slid) { if (!onGround(r, h) || t - m.top < 40) return I(0, 1, false, false, true); m.slid = t; }
    if (m.slid && t - m.slid < 15) return I();
    if (m.slid && !m.leap) { if (h.x < 14400) return I(1); m.leap = t; }
    if (m.leap && !m.caught) {
      if (h.climbing) { m.caught = t; return I(0, -1); }
      const k = t - m.leap; if (k > 70) { m.caught = t; return I(); }
      if (cellOf(h.x) >= 16) m.over = true; return I(m.over ? 0 : 1, -1, k === 0, k < 25);
    }
    if (m.caught && t - m.caught < 30) return I(0, -1);
    if (m.caught && t - m.caught < 45) return I();
    if (m.caught && t - m.caught < 75) { const k = t - m.caught - 45; return I(1, 0, k === 0, k < 20); }
    return I();
  },
  // pushes off the wall and glides; later the jetpack: up, coast, right, coast
  zerog(h, t, m) {
    if (t < 20) return I();
    if (t < 140) return I(t < 40 ? 0 : 1, 0, t === 20, false);
    if (t < 150) return I();
    const jet = true;
    if (t < 185) return I(0, -1, false, false, true, jet);
    if (t < 260) return I(0, 0, false, false, false, jet);
    if (t < 290) return I(1, 1, false, false, true, jet);
    return I(0, 0, false, false, false, jet);
  },
  // the planet: a walk with full jumps onto the two-, four- and six-cell blocks
  planet(h, t, m, r) {
    if (t < 20) return I();
    if (m.j != null && t - m.j < 55) return I(1, 0, t === m.j, true);
    const ahead = h.x + HW + 900;
    if (onGround(r, h) && !clear(r.grid, ahead, h.y)) { m.j = t; return I(1, 0, true, true); }
    return I(h.x < 46000 ? 1 : 0);
  },
  // the deck at a run: jumps onto the platforms and over the pits
  deck(h, t, m, r) {
    if (t < 20) return I();
    if (Math.abs(h.x - (m.px ?? 0)) < 40) m.still = (m.still || 0) + 1; else m.still = 0; m.px = h.x;
    if (m.still > 50) { m.dir = -(m.dir || 1); m.still = 0; m.j = null; }
    if (m.j != null && t - m.j < 40) return I(m.dir || 1, 0, t === m.j, t - m.j < 30, true);
    m.dir = m.dir || 1;
    if (h.x > 43000) m.dir = -1; if (h.x < 3000) m.dir = 1;
    const ahead = h.x + m.dir * (HW + 700);
    const pit = !grounded(r.grid, ahead, h.y), wall = !clear(r.grid, ahead, h.y);
    if (onGround(r, h) && (pit || wall) && (m.last == null || t - m.last > 30)) { m.j = t; m.last = t; return I(m.dir, 0, true, true, true); }
    return I(m.dir, 0, false, false, true);
  },
};

const CARDS = [
  { id: 'jump', camNow: true, items: ['coyote'], title: 'Койот и буфер', map: 'gap', spawn: [3, 7], script: 'coyote', loop: 270, scale: 2.4,
    feel: 'Прыжок, нажатый чуть поздно у края или чуть рано до земли, срабатывает. Слева тот же поздний прыжок теряется, и герой падает в яму.' },
  { id: 'cut', camNow: true, camY: 5.7, items: ['cut'], title: 'Высота по кнопке', map: 'hall', spawn: [4, 9], script: 'cut', loop: 200, trail: true, scale: 2.2,
    feel: 'Короткое нажатие даёт подскок на клетку, долгое даёт полный прыжок с мягкой верхушкой. Слева оба прыжка одинаковые.' },
  { id: 'run', camNow: true, items: ['accel', 'lean'], title: 'Старт, стоп, занос', map: 'hall', spawn: [8, 9], script: 'runstop', loop: 210, scale: 2.4,
    feel: 'Ноги берут скорость за 4–7 шагов, тело наклоняется в разгон и откидывается при торможении, а на развороте подошвы скользят.' },
  { id: 'land', camNow: true, items: ['land', 'air'], title: 'Полёт и приземление', map: 'drop', spawn: [3, 4], script: 'land', loop: 250, scale: 2.2,
    feel: 'Поза в воздухе меняется плавно: толчок, верхушка, падение. Глубина приседа зависит от высоты падения, после тяжёлого падения по полу идёт кольцо пыли.' },
  { id: 'idle', camNow: true, items: ['idle', 'secondary'], title: 'Стойка, ранец, антенна', map: 'hall', spawn: [10, 9], script: 'idle', loop: 470, scale: 3,
    feel: 'Стоя, герой дышит, переносит вес и оглядывается. Ранец и шлем чуть отстают от тела, а антенна качается.' },
  { id: 'ladder', camNow: true, items: ['ladder'], title: 'Лестница', map: 'ladder', spawn: [8, 13], script: 'ladder', loop: 560, scale: 2.2,
    feel: 'Лестницу ловит прыжок с зажатым ↑. Вверх герой лезет спокойнее, Shift+↓ скользит вниз с искрами, Space с ← или → спрыгивает вбок, а наверху подтягивается.' },
  { id: 'zerog', camNow: true, items: ['zerog'], title: 'Невесомость', map: 'zerog', spawn: [1, 9], script: 'zerog', loop: 420, scale: 2.2,
    feel: 'Space у стены отталкивает героя, и он долго летит по инерции. У ранца есть стартовый рывок, а при торможении видны короткие струйки газа.' },
  { id: 'planet', camNow: true, items: ['planet'], title: 'Планета, g 0,7', map: 'planet', spawn: [3, 11], script: 'planet', loop: 420, trail: true, scale: 2,
    feel: 'Высота та же, шесть клеток. Наверху герой на миг зависает, вниз падает быстрее, а время в воздухе не растёт.' },
  { id: 'camera', items: ['camera'], title: 'Камера', map: 'deck', spawn: [3, 10], script: 'deck', loop: 900, scale: 2.2,
    feel: 'Камера смотрит туда, куда герой бежит. В прыжке она стоит на месте и догоняет его только на приземлении.' },
];
MAPS.hall = { g: 21, rows: [
  '##############################################',
  '#............................................#',
  '#............................................#',
  '#............................................#',
  '#............................................#',
  '#............................................#',
  '#............................................#',
  '#............................................#',
  '#............................................#',
  '##############################################'] };
MAPS.hall.w = 46; MAPS.hall.h = 10;
const SCRIPT_OF = c => SCRIPTS[c.script];
if (typeof module !== 'undefined') module.exports = { CARDS, SCRIPTS, I };
