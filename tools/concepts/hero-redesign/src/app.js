'use strict';
// ===== stages, cards, the line-up and the pick form =====
const SUITS = [0xe8b04a, 0x5fc8c0, 0xa08cf0, 0x8cd06a];
const NOW = { id: 'now', name: 'Сейчас', rig: RIG_NOW, joints: CUR_JOINTS, lamp: [4, -9], visor: [4, -5], ant: [-4.2, -10.6], jetAt: [-2, -1], nozzle: [3, 14], visorGlow: null };
const ALL = [NOW, ...DIRS];
const POSES = [['tour', 'По кругу'], ['stand', 'Стойка'], ['walk', 'Шаг'], ['run', 'Бег'], ['jump', 'Прыжок'], ['climb', 'Лестница'], ['drill', 'Бур'], ['float', 'Невесомость']];
const SCENES = [['deck', 'Палуба'], ['planet', 'Планета'], ['cave', 'Пещера'], ['space', 'Космос']];
const TOUR = [['stand', 3200], ['walk', 2600], ['run', 2400], ['jump', 3200], ['climb', 2400], ['drill', 6000]];
const TOUR_MS = TOUR.reduce((s, t) => s + t[1], 0);
const S = { pose: 'tour', zoom: 3, scene: 'deck' };
const DEBUG_LIGHT = /lightmap/.test(location.search);
// what the pick form switches live in the directions (the current hero stays as the game has it)
const LIVE = { antenna: 'dir', visor: true, lag: true, breath: true, shift: true, glance: true, airBlend: true };
const LIVE_NOW = { antenna: 'none', visor: true, lag: false, breath: false, shift: false, glance: false, airBlend: false };

const C_S = CELL / 1000 * 60; // px a second per thousandth of a cell a step
function actOf(name, t, scene) {
  if (scene === 'space') name = 'float';
  switch (name) {
    case 'stand': return { pose: 'Stand' };
    case 'walk': return { pose: 'Walk', trav: t / 1000 * SPEC.walk * 0.06, camX: t / 1000 * SPEC.walk * C_S };
    case 'run': return { pose: 'Run', trav: t / 1000 * SPEC.run * 0.06, camX: t / 1000 * SPEC.run * C_S };
    case 'jump': {
      const P = 1600, p = t % P, v0 = SPEC.jump * C_S, g = SPEC.g * C_S * 60, up = 450, air = 2 * v0 / g * 1000;
      if (p < up) return { pose: 'Stand', landing: t >= P && p < 220 ? 1 - p / 220 : 0, jumpN: Math.floor(t / P) };
      const q = (p - up) / 1000;
      if (p < up + air) return { pose: 'Air', vy: (-v0 + g * q) / C_S, yOff: v0 * q - g * q * q / 2, jumpN: Math.floor(t / P) };
      const l = (p - up - air) / 220;
      return { pose: 'Stand', landing: l < 1 ? 1 - l : 0, landed: Math.floor(t / P), jumpN: Math.floor(t / P) };
    }
    case 'climb': return { pose: 'Climb', trav: t / 1000 * SPEC.climb * 0.06, camY: -t / 1000 * SPEC.climb * C_S, shaft: true };
    case 'drill': {
      // rest, down into the floor running, up to level running, steeply up stopped, back to rest
      const keys = [[0, 0.3, 0], [700, 0.3, 0], [1100, 1.45, 1], [3300, 1.45, 1], [3700, 0, 1], [4600, 0, 1], [5000, -0.85, 0], [5600, -0.85, 0], [6000, 0.3, 0]];
      const p = t % 6000; let i = 1; while (i < keys.length - 1 && keys[i][0] < p) i++;
      const [ta, aa, sa] = keys[i - 1], [tb, ab] = keys[i], k = smooth(clamp((p - ta) / Math.max(1, tb - ta), 0, 1));
      const angle = aa + (ab - aa) * k, spinning = sa === 1 && p > ta;
      return { pose: 'Stand', drill: { angle, spinning, bites: spinning && angle > 1.3 }, look: clamp(angle * 0.45, -0.45, 0.6) };
    }
    case 'float': {
      const P = 5200, p = t % P, on = p > 1600 && p < 3500;
      const k = on ? smooth(clamp((p - 1600) / 400, 0, 1)) : p >= 3500 ? 1 - smooth(clamp((p - 3500) / 600, 0, 1)) : 0;
      const yOff = 42 + 6 * Math.sin(t / 3400 * TAU);
      return { pose: 'Float', swim: k, fast: on ? 0.4 : 0, along: k > 0.05 ? 1 : 0, yOff, jet: true, thrust: on ? { x: 0.8, y: -0.6 } : null, camX: floatCam(t) };
    }
  }
}
// the space treadmill: drifting, then thrusting ahead
function floatCam(t) { const P = 5200, n = Math.floor(t / P), p = t % P; const burst = q => clamp((q - 1600) / 1900, 0, 1); return (n + burst(p)) * 3 * CELL * 1.2 + t * 0.004; }
function tourAt(T) { let t = T % TOUR_MS; for (const [n, ms] of TOUR) { if (t < ms) return [n, t]; t -= ms; } return ['stand', 0]; }

// ---------- a stage: a scene and some heroes, drawn as the game composes light ----------
const PANELS = [];
class Panel {
  constructor(canvas, o) {
    this.c = canvas; this.o = o; this.w = o.cells[0] * CELL; this.h = o.cells[1] * CELL;
    canvas.width = this.w; canvas.height = this.h;
    this.out = canvas.getContext('2d'); this.out.imageSmoothingEnabled = false;
    const mk = () => { const c = canvasOf(this.w, this.h), g = c.getContext('2d'); g.imageSmoothingEnabled = false; return g; };
    this.a = mk(); this.e = mk(); this.lg = mk();
    this.parts = new Parts(); this.visible = true; this.scene = o.scene;
    this.setHeroes(o.heroes);
    PANELS.push(this);
  }
  setHeroes(list) { this.heroes = list.map((h, i) => ({ ...h, fig: null, i })); this.lastKey = null; }
  figFor(h) {
    const dark = h.dir.visorGlow && !this.live().visor;
    const look = lookOf(h.dir, h.suit, dark);
    if (!h.fig || h.fig.look !== look) { const f = new Figure(look); if (h.fig) Object.assign(f, { shown: h.fig.shown, time: h.fig.time }); h.fig = f; }
    return h.fig;
  }
  live() { return this.o.now ? LIVE_NOW : LIVE; }
  frame(T, dt) {
    const kind = this.o.lockScene || this.scene;
    const [name, t] = S.pose === 'tour' ? tourAt(T) : [S.pose, T];
    const act = actOf(name, t, kind);
    const key = kind + ':' + (act.shaft ? 'shaft' : '') + ':' + name;
    if (!this.sc || this.sc.kind !== kind) this.sc = new Scene(kind, this.w, this.h);
    const sc = this.sc, n = this.heroes.length, live = this.live();
    const camX = act.camX || 0, camY = act.camY || 0;
    if (this.lastKey === key && this.prevCam) this.parts.shift(camX - this.prevCam[0], camY - this.prevCam[1]);
    else this.parts.list = [];
    this.lastKey = key; this.prevCam = [camX, camY];
    const slots = this.heroes.map((h, i) => Math.round(this.w * (i + 0.5) / n / CELL - 0.5) * CELL + 8);
    sc.ladders = act.shaft ? slots : null;
    if (act.shaft) sc.ladderX = slots[0];
    const lights = sc.draw(this.a, this.e, camX, camY, act.shaft);
    if (act.shaft) for (const x of slots) drawLadderColumn(this.a, this.e, x, camY, this.h);
    const lamp = kind === 'cave';
    const cam = new DOMMatrix();
    for (const h of this.heroes) {
      const f = this.figFor(h), x = slots[h.i];
      const feetY = act.shaft ? this.h - 30 : kind === 'space' ? this.h * 0.82 - (act.yOff || 0) + 30 : sc.ground - (act.yOff || 0);
      // the body's acceleration, for the pack's lag
      const wx = camX + x, wy = feetY + camY;
      if (h.pw && dt > 0) { const vx = (wx - h.pw[0]) / dt * 1000, vy = (wy - h.pw[1]) / dt * 1000; if (h.pv) { h.ax = clamp((vx - h.pv[0]) / dt * 1000, -6000, 6000); h.ay = clamp((vy - h.pv[1]) / dt * 1000, -6000, 6000); } h.pv = [vx, vy]; }
      if (this.lastKeyH !== key) { h.pv = null; h.ax = h.ay = 0; }
      h.pw = [wx, wy];
      const st = { ...act, x, y: feetY, facing: 1, ax: h.ax || 0, ay: h.ay || 0, lamp };
      // in the dark each hero looks its own way, so the beams fan out
      if (lamp && !act.drill && act.pose !== 'Climb') st.look = (act.look || 0) + [0.32, -0.12, 0.42, 0.05, 0.25, -0.05][h.i % 6] + 0.12 * Math.sin(T / 1300 + h.i * 1.7);
      f.update(st, dt, live);
      lights.push(...f.draw(this.a, this.e, cam, live));
      this.effects(h, f, st, sc, t);
    }
    this.lastKeyH = key;
    this.parts.step(dt);
    this.parts.draw(this.a, this.e, lights);
    // plumes over the glow mask: a short tapered core of light
    for (const h of this.heroes) {
      const f = h.fig; if (!f.nozzle || !f.s.thrust) continue;
      const th = f.s.thrust, wx = -th.x, wy = -th.y, len = 8 + 3 * Math.sin(T / 47 + h.i);
      const g = this.e; g.setTransform(1, 0, 0, 1, 0, 0); g.globalCompositeOperation = 'lighter';
      const gr = g.createLinearGradient(f.nozzle.x, f.nozzle.y, f.nozzle.x + wx * len, f.nozzle.y + wy * len);
      gr.addColorStop(0, 'rgba(255,255,255,0.95)'); gr.addColorStop(0.25, 'rgba(170,215,255,0.8)'); gr.addColorStop(0.7, 'rgba(70,120,255,0.35)'); gr.addColorStop(1, 'rgba(40,70,255,0)');
      g.strokeStyle = gr; g.lineWidth = 1.5; g.lineCap = 'round'; g.beginPath(); g.moveTo(f.nozzle.x, f.nozzle.y); g.lineTo(f.nozzle.x + wx * len, f.nozzle.y + wy * len); g.stroke();
      g.globalCompositeOperation = 'source-over'; g.lineCap = 'butt';
      lights.push({ x: f.nozzle.x + wx * 4, y: f.nozzle.y + wy * 4, r: 3 * CELL, c: [0.43, 0.67, 1], k: 0.45 });
    }
    // a helmet beam shows faintly in the dark air, as in the cave concepts
    if (kind === 'cave') for (const L of lights) if (L.beam) {
      const g = this.e; g.setTransform(1, 0, 0, 1, 0, 0); g.globalCompositeOperation = 'lighter';
      const gr = g.createRadialGradient(L.x, L.y, 0, L.x, L.y, L.r * 0.8); gr.addColorStop(0, 'rgba(255,240,205,0.13)'); gr.addColorStop(1, 'rgba(255,240,205,0)');
      g.fillStyle = gr; g.beginPath(); g.moveTo(L.x, L.y); g.arc(L.x, L.y, L.r, L.ang - L.spread * 0.8, L.ang + L.spread * 0.8); g.closePath(); g.fill(); g.globalCompositeOperation = 'source-over';
    }
    paintLights(this.lg, this.w, this.h, sc.ambient, lights);
    const o = this.out; o.setTransform(1, 0, 0, 1, 0, 0); o.globalCompositeOperation = 'source-over'; o.globalAlpha = 1; o.filter = 'none';
    o.drawImage(this.a.canvas, 0, 0);
    o.globalCompositeOperation = 'multiply'; o.drawImage(this.lg.canvas, 0, 0);
    o.globalCompositeOperation = 'lighter'; o.drawImage(this.e.canvas, 0, 0);
    o.filter = 'blur(1px)'; o.globalAlpha = 0.55; o.drawImage(this.e.canvas, 0, 0);
    o.filter = 'blur(3px)'; o.globalAlpha = 0.35; o.drawImage(this.e.canvas, 0, 0);
    o.filter = 'none'; o.globalAlpha = 1; o.globalCompositeOperation = 'source-over';
    if (DEBUG_LIGHT) { o.globalCompositeOperation = 'source-over'; o.drawImage(this.lg.canvas, 0, 0); }
    if (this.o.copy) { const g = this.o.copy.getContext('2d'); g.imageSmoothingEnabled = false; g.drawImage(this.c, 0, 0); }
  }
  effects(h, f, st, sc, t) {
    const P = this.parts, fx = st.x, fy = st.y;
    // landing: grains to either side, a faint flat ring
    if (st.landed != null && h.landedN !== st.landed) {
      h.landedN = st.landed;
      for (const s of [-1, 1]) for (let i = 0; i < 5; i++) P.add({ kind: 'mote', x: fx + s * rnd(1, 4), y: fy - 0.5, vx: s * rnd(30, 80), vy: -rnd(2, 16), g: 30, drag: 4, life: rnd(300, 600), col: DUST_OF[sc.kind], a: 0.8, floor: fy - 0.4 });
      P.add({ kind: 'streak', x: fx - 3, y: fy - 0.5, vx: -60, vy: 0, len: 5, life: 200, col: '#e8eef6', a: 0.35, glow: true, drag: 6 });
      P.add({ kind: 'streak', x: fx + 3, y: fy - 0.5, vx: 60, vy: 0, len: 5, life: 200, col: '#e8eef6', a: 0.35, glow: true, drag: 6 });
    }
    if (st.pose === 'Air' && h.jumpedN !== st.jumpN) {
      h.jumpedN = st.jumpN;
      for (const s of [-1, 1]) for (let i = 0; i < 3; i++) P.add({ kind: 'mote', x: fx + s * rnd(1, 4), y: sc.ground - 0.5, vx: s * rnd(10, 40), vy: -rnd(2, 10), life: rnd(250, 450), col: DUST_OF[sc.kind], a: 0.7, drag: 3, floor: sc.ground - 0.4 });
    }
    // a running boot kicks dust
    if (st.pose === 'Run') { const step = Math.floor(st.trav / 2.3); if (h.step != null && step !== h.step) for (let i = 0; i < 4; i++) P.add({ kind: 'mote', x: fx + 4 + rnd(-1.5, 1.5), y: fy - 0.4, vx: rnd(-40, -5), vy: -rnd(4, 15), g: 20, life: rnd(300, 500), col: DUST_OF[sc.kind], a: 0.6, floor: fy - 0.3 }); h.step = step; } else h.step = null;
    // the drill biting the floor: sparks and chips
    if (st.drill && st.drill.bites && f.bit) {
      for (let i = 0; i < 2; i++) { const a = -Math.PI / 2 + rnd(-1.3, 1.3), sp = rnd(60, 140); P.add({ kind: 'streak', x: f.bit.x, y: Math.min(f.bit.y, sc.ground - 0.5), vx: Math.cos(a) * sp, vy: Math.sin(a) * sp, len: rnd(2, 4), life: rnd(120, 260), col: '#ffd27a', g: 300, glow: true, light: 24, lc: [1, 0.75, 0.4] }); }
      if (Math.random() < 0.5) P.add({ kind: 'mote', x: f.bit.x, y: sc.ground - 0.5, vx: rnd(-30, 30), vy: -rnd(20, 50), g: 160, life: rnd(300, 500), col: DUST_OF[sc.kind], a: 0.9, floor: sc.ground - 0.3 });
    }
    // thrust streaks
    if (f.nozzle && st.thrust && Math.random() < 0.8) {
      const th = st.thrust; for (let i = 0; i < 2; i++) P.add({ kind: 'streak', x: f.nozzle.x, y: f.nozzle.y, vx: -th.x * rnd(70, 130) + rnd(-14, 14), vy: -th.y * rnd(70, 130) + rnd(-14, 14), len: rnd(3, 6), life: rnd(140, 260), col: i ? '#ffffff' : '#9fd0ff', a: 0.8, glow: true });
    }
  }
}
const DUST_OF = { deck: '#b8b2a8', planet: '#8a6a4c', cave: '#7d8594', space: '#b8b2a8' };
function drawLadderColumn(a, e, x, camY, h) {
  const X = x - 8, o = ((camY % CELL) + CELL) % CELL;
  for (let y = -o - CELL; y < h + CELL; y += CELL) { a.drawImage(IMG.ladder, X, Math.round(y)); e.drawImage(EM.ladder, X, Math.round(y)); }
}

// ---------- the exploded parts of a direction ----------
function drawSheet(canvas, dir, suit) {
  const look = lookOf(dir, suit), Z = 3, gap = 8, names = ['helmet', 'torso', 'pack', 'upperArm', 'forearm', 'thigh', 'shin', 'jetpack'];
  const ws = names.map(n => look.near[n].w), H = Math.max(...names.map(n => look.near[n].h));
  canvas.width = (ws.reduce((s, w) => s + w, 0) + gap * (names.length - 1)) * Z + 8; canvas.height = (H + 2) * Z + 8;
  const g = canvas.getContext('2d'); g.imageSmoothingEnabled = false;
  let x = 4;
  names.forEach((n, i) => {
    const p = look.near[n], y = 4 + (H - p.h) * Z;
    g.drawImage(p.img, x, y, p.w * Z, p.h * Z);
    g.globalCompositeOperation = 'lighter'; g.drawImage(p.em, x, y, p.w * Z, p.h * Z); g.globalCompositeOperation = 'source-over';
    const j = n === 'jetpack' ? null : look.joints[n];
    if (j) { g.fillStyle = '#ff5a6e'; g.fillRect(x + j[0] * Z - 2, y + j[1] * Z - 2, 4, 4); g.fillStyle = '#fff'; g.fillRect(x + j[0] * Z - 1, y + j[1] * Z - 1, 2, 2); }
    x += (p.w + gap) * Z;
  });
}

// ---------- the page ----------
function seg(el, items, get, set) {
  el.innerHTML = items.map(([k, t]) => `<button type="button" data-k="${k}">${t}</button>`).join('');
  const sync = () => el.querySelectorAll('button').forEach(b => b.setAttribute('aria-pressed', String(b.dataset.k === String(get()))));
  el.addEventListener('click', ev => { const b = ev.target.closest('button'); if (!b) return; set(b.dataset.k); sync(); });
  sync(); return sync;
}
function card(dir, i) {
  const el = document.createElement('article'); el.className = 'card'; el.id = 'dir-' + dir.id;
  const isNow = dir.id === 'now', n = isNow ? NOW_NOTES : dir.notes;
  const partsCount = isNow ? '7 + ранец' : '7 + ранец' + (dir.visorGlow ? ' + свечение' : '') + (dir.antenna ? ' + антенна' : '');
  el.innerHTML = `
    <div class="card-head">
      <div class="badge" aria-hidden="true">${isNow ? '·' : dir.id}</div>
      <div class="ttl"><h3>${dir.name}</h3><p class="sub">${isNow ? 'герой в игре сегодня, из настоящих картинок' : dir.sub}</p></div>
      <div class="scn seg" role="group" aria-label="Сцена"></div>
    </div>
    <div class="card-body">
      <div class="stage"><canvas class="big" aria-label="${dir.name}: четыре цвета костюма в анимации"></canvas></div>
      <div class="side">
        <figure class="one"><canvas class="x1"></canvas><figcaption>1×, как в окне 1280×720</figcaption></figure>
        <figure class="sheetf"><canvas class="sheet"></canvas><figcaption>детали ×3, красная точка шарнир</figcaption></figure>
        ${isNow ? `<dl class="notes"><div><dt>Что это</dt><dd>${n.what}</dd></div></dl>` : `
        <dl class="notes">
          <div><dt>Что нового</dt><dd>${n.what}</dd></div>
          <div><dt>Рисование</dt><dd>${n.cost}</dd></div>
          <div><dt>Риг</dt><dd>${n.rig}</dd></div>
          <div><dt>Читаемость</dt><dd>${n.read}</dd></div>
        </dl>`}
      </div>
    </div>`;
  const big = el.querySelector('.big'), x1 = el.querySelector('.x1');
  const p = new Panel(big, { cells: [15, 9], scene: S.scene, heroes: SUITS.map(s => ({ dir, suit: s })), now: isNow, copy: x1 });
  x1.width = p.w; x1.height = p.h;
  p.syncScene = seg(el.querySelector('.scn'), SCENES, () => p.scene, k => { p.scene = k; });
  drawSheet(el.querySelector('.sheet'), dir, SUITS[0]);
  return el;
}

let LINEUP_SUIT = 0;
function lineups() {
  const box = document.getElementById('lineups'); const ps = [];
  for (const [k, t] of SCENES) {
    const f = document.createElement('figure'); f.className = 'lu';
    f.innerHTML = `<canvas aria-label="Все варианты рядом: ${t}"></canvas><figcaption><b>${t}</b> · слева направо: сейчас, A, B, C, D, E</figcaption>`;
    box.appendChild(f);
    ps.push(new Panel(f.querySelector('canvas'), { cells: [20, 9], scene: k, lockScene: k, heroes: ALL.map(d => ({ dir: d, suit: SUITS[LINEUP_SUIT] })), lineup: true }));
  }
  const dots = document.getElementById('lu-suit');
  dots.innerHTML = SUITS.map((s, i) => `<button type="button" data-i="${i}" aria-label="Цвет ${i + 1}" style="--dot:#${s.toString(16).padStart(6, '0')}"></button>`).join('');
  const sync = () => dots.querySelectorAll('button').forEach(b => b.setAttribute('aria-pressed', String(+b.dataset.i === LINEUP_SUIT)));
  dots.addEventListener('click', ev => { const b = ev.target.closest('button'); if (!b) return; LINEUP_SUIT = +b.dataset.i; for (const p of ps) p.setHeroes(ALL.map(d => ({ dir: d, suit: SUITS[LINEUP_SUIT] }))); sync(); });
  sync();
}

function applyZoom() {
  for (const p of PANELS) { const z = p.o.lineup ? Math.max(2, S.zoom - 1) : S.zoom; p.c.style.width = p.w * z + 'px'; }
  document.documentElement.style.setProperty('--z', S.zoom);
}

// ---------- the form ----------
const FORM_KEY = 'stillwake-hero-redesign-v1';
function readForm() {
  const f = document.getElementById('pick'), fd = new FormData(f), o = {};
  for (const [k, v] of fd.entries()) o[k] = o[k] ? [].concat(o[k], v) : v;
  return o;
}
const LABEL = id => { const d = ALL.find(x => x.id === id); return d ? (d.id === 'now' ? 'оставить как сейчас' : `${d.id} «${d.name}»`) : id; };
function summary() {
  const o = readForm(), lines = [];
  lines.push('Герой: новый облик');
  lines.push('Направление: ' + (o.dir ? LABEL(o.dir) : 'не выбрано'));
  if (o.mix && o.mix.trim()) lines.push('Смешать: ' + o.mix.trim());
  const pick = (k, t) => { const el = document.querySelector(`#pick [name="${k}"]:checked`); if (el) lines.push(t + ': ' + el.dataset.t); };
  pick('prop', 'Пропорции'); pick('helmet', 'Шлем'); pick('visor', 'Свечение визора'); pick('outline', 'Контур'); pick('pack', 'Ранец');
  pick('ant', 'Антенна'); pick('wear', 'Износ');
  const checks = [...document.querySelectorAll('#pick input[type=checkbox]')];
  const on = checks.filter(c => c.checked).map(c => c.dataset.t), off = checks.filter(c => !c.checked).map(c => c.dataset.t);
  if (on.length) lines.push('Да: ' + on.join('; '));
  if (off.length) lines.push('Нет: ' + off.join('; '));
  if (o.note && o.note.trim()) lines.push('Ещё: ' + o.note.trim());
  return lines.join('\n');
}
function syncLive() {
  const f = document.getElementById('pick');
  const v = n => (f.querySelector(`[name="${n}"]:checked`) || {}).value;
  LIVE.antenna = v('ant') === 'none' ? 'none' : v('ant') === 'all' ? 'all' : 'dir';
  LIVE.visor = v('visor') !== 'none';
  LIVE.lag = f.querySelector('#c-lag').checked;
  LIVE.breath = f.querySelector('#c-breath').checked;
  LIVE.shift = f.querySelector('#c-shift').checked;
  LIVE.glance = f.querySelector('#c-glance').checked;
  document.getElementById('sum').value = summary();
  try { const keep = {}; for (const el of f.elements) if (el.id) keep[el.id] = el.type === 'checkbox' || el.type === 'radio' ? el.checked : el.value; localStorage.setItem(FORM_KEY, JSON.stringify(keep)); } catch (e) { /* storage off: the form still works */ }
}
function restoreForm() {
  try { const keep = JSON.parse(localStorage.getItem(FORM_KEY) || 'null'); if (!keep) return; for (const [id, v] of Object.entries(keep)) { const el = document.getElementById(id); if (!el) continue; if (el.type === 'checkbox' || el.type === 'radio') el.checked = v; else el.value = v; } } catch (e) { /* nothing kept */ }
}

async function boot() {
  for (const [k, src] of Object.entries(ART)) IMG[k] = await loadImage(src);
  prepScenes();
  const cards = document.getElementById('cards'), nowBox = document.getElementById('now-card');
  nowBox.appendChild(card(NOW, 0));
  DIRS.forEach((d, i) => cards.appendChild(card(d, i + 1)));
  lineups();
  const syncs = [];
  seg(document.getElementById('pose'), POSES, () => S.pose, k => { S.pose = k; });
  seg(document.getElementById('zoom'), [['2', '×2'], ['3', '×3'], ['4', '×4']], () => S.zoom, k => { S.zoom = +k; applyZoom(); });
  seg(document.getElementById('scene'), SCENES, () => S.scene, k => { S.scene = k; for (const p of PANELS) if (!p.o.lockScene) { p.scene = k; p.syncScene && p.syncScene(); } });
  applyZoom();
  const f = document.getElementById('pick');
  restoreForm();
  f.addEventListener('input', syncLive); f.addEventListener('change', syncLive);
  f.addEventListener('submit', ev => ev.preventDefault());
  syncLive();
  document.getElementById('copy').addEventListener('click', async () => {
    const t = document.getElementById('sum'), msg = document.getElementById('copied');
    try { await navigator.clipboard.writeText(t.value); msg.textContent = 'Скопировано'; } catch (e) { t.focus(); t.select(); msg.textContent = 'Выделено: нажмите Ctrl+C'; }
    setTimeout(() => { msg.textContent = ''; }, 2400);
  });
  // only stages in view are drawn
  const io = new IntersectionObserver(es => { for (const en of es) { const p = PANELS.find(q => q.c === en.target); if (p) p.visible = en.isIntersecting; } }, { rootMargin: '120px' });
  for (const p of PANELS) io.observe(p.c);
  const reduce = matchMedia('(prefers-reduced-motion: reduce)').matches;
  let last = performance.now(), T = 0;
  const tick = now => {
    const dt = Math.min(50, now - last); last = now; T += reduce ? dt * 0.5 : dt;
    for (const p of PANELS) if (p.visible) p.frame(T, dt);
    requestAnimationFrame(tick);
  };
  // a first frame for every stage, so the page is complete at rest
  for (let i = 0; i < 12; i++) for (const p of PANELS) p.frame(i * 16, 16);
  const q = new URLSearchParams(location.search);
  if (q.has('freeze')) {
    // a still at a moment, for checking frames: ?freeze&t=ms&pose=walk&scene=cave&zoom=3
    if (q.get('pose')) S.pose = q.get('pose'); if (q.get('zoom')) { S.zoom = +q.get('zoom'); applyZoom(); }
    if (q.get('scene')) for (const p of PANELS) if (!p.o.lockScene) p.scene = q.get('scene');
    const T0 = +(q.get('t') || 0); for (let t = Math.max(0, T0 - 900); t <= T0; t += 16) for (const p of PANELS) p.frame(t, 16);
    if (q.has('probe')) {
      // only the stages, side by side in a grid, zoomed: ?probe=big|lu
      const box = document.createElement('div'); box.style.cssText = 'position:fixed;left:0;top:0;display:flex;flex-wrap:wrap;gap:6px;padding:6px;background:#000;width:1600px';
      for (const p of PANELS) if (q.get('probe') === 'lu' ? p.o.lineup : !p.o.lineup) { const c = p.c; c.style.width = p.w * (+(q.get('pz') || 3)) + 'px'; box.appendChild(c); }
      document.body.innerHTML = ''; document.body.style.padding = '0'; document.body.appendChild(box);
    }
  } else requestAnimationFrame(tick);
  window.__hero = { PANELS, S, LIVE, frameAll: (T, n = 1, dt = 16) => { for (let i = 0; i < n; i++) for (const p of PANELS) p.frame(T + i * dt, dt); } };
}
function loadImage(src) { return new Promise((ok, no) => { const i = new Image(); i.onload = () => ok(i); i.onerror = no; i.src = src; }); }
boot();
