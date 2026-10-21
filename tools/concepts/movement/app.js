'use strict';
window.addEventListener('error', e => { const el = document.getElementById('err'); if (el) el.textContent += (e.message || e) + '\n'; });
const ITEMS = [
  { id: 'coyote', g: 'Земля', t: 'Койот и буфер прыжка', feel: 'Прыжок у самого края и прыжок за миг до земли больше не теряются.', what: 'Физика, <code>Hero.Move</code>: шаги со схода с края и шаги с нажатия. Койот 100 мс (6 шагов), буфер 100 мс (6 шагов).', cost: 'S', art: 'нет', coop: 'Ничего: герой клиентский, по сети идут только позиции.', resp: 'Добавляет только прыжки, которые раньше пропадали. Ждать ничего не нужно.' },
  { id: 'cut', g: 'Земля', t: 'Высота по кнопке, мягкая верхушка', feel: 'Короткое нажатие даёт подскок, долгое даёт полный прыжок, а наверху герой на миг зависает.', what: 'Физика. Отпустил на подъёме: скорость ×0,5. Пока держишь и |Vy| &lt; 60: g ×0,6. При падении g ×1,15. Jump 420 → 415, чтобы осталось «три клетки, не четыре» (подъём 3,95, на планете 6,01). Нужно поле <code>HeroInput.JumpHeld</code>.', cost: 'S', art: 'нет', coop: 'Чужие ловят прыжок по <code>Vy ≤ −Jump/2</code> (<code>Pose.Jumped</code>). Подскок после среза может не прочитаться: порог −Jump/4.', resp: 'Высота под пальцем. Тап на 1,2 клетки уже на 0,33 с.' },
  { id: 'accel', g: 'Земля', t: 'Разгон и торможение', feel: 'Ноги берут скорость и тормозят, а не включаются и выключаются.', what: 'Физика. До 120 за 67 мс (4 шага), до 220 за 117 мс (7 шагов), стоп 67 мс (с бега 120 мс), в воздухе ×0,65. Сейчас всё мгновенно.', cost: 'M', art: 'нет', coop: 'Ничего.', resp: 'Не дольше 7 шагов. Celeste набирает бег за 0,09 с. Тесты, которые считают шаги, придётся пересчитать.' },
  { id: 'lean', g: 'Земля', t: 'Наклон в разгон, занос', feel: 'Тело клонится в разгон и откидывается при торможении, а на развороте подошвы скользят.', what: 'Процедурно, <code>Pose.cs</code>/<code>Figure.cs</code>. Наклон пропорционален ускорению, до 0,25 рад. При торможении с |dx| &gt; 50 поза заноса на 7 шагов и полоски пыли от подошвы.', cost: 'S', art: 'нет', coop: 'Считается из движения (0003). У чужих лучше брать присланный Vx, а не интерполяцию.', resp: 'Только картинка, физика не ждёт.' },
  { id: 'land', g: 'Земля', t: 'Приземление по высоте', feel: 'С табуретки герой пружинит, с крыши садится глубоко, и по полу бежит кольцо.', what: 'Процедурно. Присед ×(падение/700), от 0,35 до 1,6. Длится 150–260 мс, на бегу ×0,6. С 900 за шаг: кольцо 34 px, полоски и встряска камеры 2 px (только своя).', cost: 'S', art: 'нет', coop: 'Ничего: Vy уже приходит.', resp: 'Ввод не блокируется, как и сейчас.' },
  { id: 'air', g: 'Земля', t: 'Поза в воздухе', feel: 'Видны толчок, верхушка и падение, а не две позы, сменяющие друг друга щелчком.', what: 'Процедурно. Смешивание по Vy, 110 мс толчка ногами, колени к груди на верхушке, руки машут после 0,45 с падения, при быстром падении взгляд вниз.', cost: 'S', art: 'нет', coop: 'Ничего.', resp: 'Только картинка.' },
  { id: 'idle', g: 'Земля', t: 'Стойка: дыхание, вес, взгляд', feel: 'Стоящий герой живой: дышит, переминается, оглядывается.', what: 'Процедурно. Тело дышит на 0,45 px. Через 2,5 с переносит вес. Если курсор стоит 3 с, смотрит вверх и вниз (обычно голова следит за курсором).', cost: 'S', art: 'нет', coop: 'Look приходит в <code>Seen</code>. Оглядка только пока Look не меняется.', resp: 'Любое движение сразу её снимает.' },
  { id: 'secondary', g: 'Полировка', t: 'Ранец, шлем, антенна', feel: 'Детали отстают на долю секунды и догоняют, поэтому у героя есть вес.', what: 'Процедурно. Пружина ранца ±2 px за ускорением (k 600, затухание 16), шлем ×0,4. Антенна по желанию: 1 px, 3 звена. ADR 0003 обещал «backpack lag».', cost: 'S', art: 'антенна: линия кодом или спрайт 3×7', coop: 'Ничего.', resp: 'Только картинка.' },
  { id: 'ladder', g: 'Лестница', t: 'Лестница целиком', feel: 'Лестницу ловишь прыжком, слезаешь быстро, спрыгиваешь вбок, наверху выбираешься.', what: 'Физика и позы. ↑ ловит и на ходу, и в прыжке (не раньше 200 мс после схода). Центр за ~5 шагов. Вверх 6,6 кл/с, вниз 9 (сейчас 10,8). Shift+↓ скользит 18 кл/с с искрами. Space с ←/→ прыгает вбок (0,9 бега, 0,85 прыжка). Наверху подтягивание 170 мс.', cost: 'M', art: 'нет', coop: 'Скольжение видно по Vy больше скорости лазания, поле не нужно.', resp: 'Хватать можно в большем числе случаев. Подъём медленнее, спуск со скольжением быстрее. Переписать тесты <code>LaddersAreNotGrabbedWhileWalkingOrJumping</code>.' },
  { id: 'zerog', g: 'Космос', t: 'Толчок от стен, рывок ранца', feel: 'Внутри корабля без воздуха можно оттолкнуться и лететь, а ранец заметно рвёт с места.', what: 'Физика и FX. Плавание набирает скорость за 150 мс. Space у стены толкает на 5,4 кл/с, скорость гаснет на 0,6 за шаг (~5 кл). Ранец: первые 100 мс тяга ×2,5. При гашении скорости струйки газа, голова смотрит по тяге.', cost: 'M', art: 'нет', coop: 'Толчок видно по скорости. Тяга уже в sim (<code>Thrust</code>).', resp: 'Отклик тот же, к плаванию добавлен быстрый способ пересечь комнату. Проверить толчок на привязи.' },
  { id: 'planet', g: 'Планета', t: 'Прыжок в малой гравитации', feel: 'Прыжок высокий, как и задумано, но не ватный: зависает наверху, вниз падает быстро.', what: 'Физика. На планете при падении g ×1,5, плюс верхушка из строки 2. Шесть клеток остаются, время в воздухе 1,0 с то же.', cost: 'S', art: 'нет', coop: 'Ничего.', resp: 'Меньше ожидания на спуске.' },
  { id: 'camera', g: 'Полировка', t: 'Камера: упреждение и привязка', feel: 'Видно, куда бежишь. Прыжок не дёргает экран вверх и вниз.', what: 'Только клиент, <code>HeroView.cs:398</code>. Упреждение по скорости 3 кл в превью (в игре ~6 из 96), сглаживание 180 мс. В прыжке камера держит высоту, пока герой в окне −1…+2,5 кл. Встряска при тяжёлом приземлении.', cost: 'S', art: 'нет', coop: 'Только своя камера.', resp: 'Курсор и прицел считаются от камеры, поэтому сглаживание держим коротким.' },
  { id: 'corner', g: 'Земля', t: 'Обход края потолка', feel: 'Задел головой угол, и прыжок продолжается, а не обрывается.', what: 'Физика. Голова, задевшая край, сдвигается вбок до 250 тысячных (4 px), как в Celeste.', cost: 'S', art: 'нет', coop: 'Ничего.', resp: 'Убирает обидные обрывы прыжка.' },
  { id: 'ledge', g: 'Земля', t: 'Подтягивание на уступ', feel: 'Ноги чуть ниже кромки, и герой всё равно на уступе.', what: 'Физика. Ноги до 350 (5,6 px) ниже кромки: герой поднимается на неё. В превью выключено: с ним прыжок берёт 4 клетки, и Jump надо снизить до ~395.', cost: 'M', art: 'нет', coop: 'Ничего.', resp: 'Конфликтует с правилом «три, не четыре» (0005).', off: true },
  { id: 'dust', g: 'Полировка', t: 'Пыль, искры, газ', feel: 'Каждое действие оставляет тонкий светлый след в стиле игры.', what: 'FX через <code>Glims</code>/<code>Flares</code>. Пыль толчка (6 крупинок и 2 полоски), полоски заноса, искры скольжения, кольцо толчка в невесомости, газ гашения у ранца.', cost: 'S', art: 'нет', coop: 'Всё считается у себя из движения.', resp: 'Только картинка.' },
];
const ITEM = Object.fromEntries(ITEMS.map((it, i) => [it.id, { ...it, n: i + 1 }]));

// ---------- sliders ----------
const TUNE = [
  ['Прыжок', [['coyoteMs', 'койот, мс', 0, 200, 5], ['bufferMs', 'буфер, мс', 0, 200, 5], ['cut', 'срез при отпускании ×', 0.2, 1, 0.05], ['apexThr', 'верхушка: |Vy| меньше', 0, 150, 5], ['apexMult', 'верхушка: g ×', 0.3, 1, 0.05], ['fallMult', 'падение: g × (корабль)', 1, 2, 0.05], ['planetFallMult', 'падение: g × (планета)', 1, 2.5, 0.05], ['jump', 'прыжок, тысячных/шаг', 380, 440, 5], ['corner', 'обход угла, тысячных', 0, 400, 25], ['ledge', 'уступ, тысячных', 0, 500, 25]]],
  ['Бег', [['walkAccelMs', 'разгон ходьбы, мс', 0, 250, 5], ['runAccelMs', 'разгон бега, мс', 0, 300, 5], ['stopMs', 'остановка, мс', 0, 250, 5], ['airCtl', 'управление в воздухе ×', 0.2, 1, 0.05]]],
  ['Тело', [['landScale', 'присед при посадке ×', 0, 2, 0.1], ['lean', 'наклон ×', 0, 2, 0.1], ['packLag', 'отставание ранца ×', 0, 2, 0.1], ['antenna', 'антенна', 0, 1, 1], ['skid', 'занос', 0, 1, 1], ['idleLife', 'жизнь в стойке', 0, 1, 1], ['airBlend', 'плавная поза в воздухе', 0, 1, 1]]],
  ['Лестница', [['climbUp', 'вверх, тысячных/шаг', 60, 220, 5], ['climbDown', 'вниз, тысячных/шаг', 60, 260, 5], ['slide', 'скольжение, тысячных/шаг', 150, 450, 10], ['climbStride', 'клеток на цикл рук', 1.2, 4, 0.1], ['grabRising', 'ловить в прыжке', 0, 1, 1]]],
  ['Космос', [['pushOff', 'толчок, тысячных/шаг', 0, 180, 5], ['pushDrag', 'гашение толчка/шаг', 0.1, 3, 0.1], ['swimMs', 'набор плавания, мс', 0, 400, 10], ['burst', 'рывок ранца', 0, 1, 1], ['rcs', 'газ гашения', 0, 1, 1]]],
  ['Камера', [['camLead', 'упреждение, клеток', 0, 6, 0.25], ['camSmoothMs', 'сглаживание, мс', 0, 400, 10], ['camSnap', 'держать высоту в прыжке', 0, 1, 1]]],
];
const fmt = v => (Math.round(v * 100) / 100).toString().replace('.', ',');
const SAVE_KEY = 'stillwake-movement-picks-v1';
function store(get, val) { try { if (get) return JSON.parse(localStorage.getItem(SAVE_KEY) || 'null'); localStorage.setItem(SAVE_KEY, JSON.stringify(val)); } catch (e) { return null; } }
const saved = store(true) || {};
const picks = saved.picks || {};
if (saved.P) for (const k in saved.P) if (k in DEF) P[k] = saved.P[k];
function persist() { store(false, { picks, P: { ...P } }); }

function buildTune() {
  const root = document.getElementById('tune'); root.innerHTML = '';
  for (const [name, list] of TUNE) {
    const fs = document.createElement('fieldset'); fs.innerHTML = `<legend>${name}</legend>`;
    for (const [k, label, lo, hi, st] of list) {
      const id = 't-' + k, d = document.createElement('div'); d.className = 'sl';
      const bool = lo === 0 && hi === 1 && st === 1;
      d.innerHTML = `<label for="${id}">${label}</label><span><output id="o-${k}"></output> <span class="was">${bool ? '' : 'предл. ' + fmt(DEF[k])}</span></span><input type="range" id="${id}" min="${lo}" max="${hi}" step="${st}" value="${P[k]}">`;
      fs.appendChild(d);
      const inp = d.querySelector('input'), out = d.querySelector('output');
      const show = () => { out.textContent = bool ? (P[k] ? 'да' : 'нет') : fmt(P[k]); };
      show();
      inp.addEventListener('input', () => { P[k] = parseFloat(inp.value); show(); persist(); summary(); });
    }
    root.appendChild(fs);
  }
}

// ---------- canvases ----------
const DPR = Math.min(2, window.devicePixelRatio || 1);
const views = [];
function addView(canvas, run, opts) {
  const v = { canvas, ctx: canvas.getContext('2d'), run, visible: true, ...opts };
  const fit = () => { const W = Math.max(1, Math.round(canvas.clientWidth * DPR)), H = Math.max(1, Math.round(canvas.clientHeight * DPR)); if (canvas.width !== W) canvas.width = W; if (canvas.height !== H) canvas.height = H; v.scale = Math.max(2, Math.round(canvas.width / (v.cells * CELL))); };
  v.fit = fit; new ResizeObserver(fit).observe(canvas); fit();
  views.push(v); return v;
}

// ---------- the sandbox ----------
const PLAY = {
  deck: { name: 'Палуба', map: 'deck', spawn: [3, 10], script: 'deck', loop: 0 },
  ladder: { name: 'Лестница', map: 'ladder', spawn: [8, 13], script: 'ladder', loop: 560 },
  zerog: { name: 'Без воздуха', map: 'zerog', spawn: [1, 9], script: 'zerog', loop: 420 },
  planet: { name: 'Планета', map: 'planet', spawn: [3, 11], script: 'planet', loop: 420 },
};
const keys = new Set(); let armed = false, pad = new Set(), scene = 'deck';
const play = { now: null, new: null };
function liveInput(run) {
  const has = k => keys.has(k) || pad.has(k);
  const x = (has('right') ? 1 : 0) - (has('left') ? 1 : 0), y = (has('down') ? 1 : 0) - (has('up') ? 1 : 0);
  const jump = !!run.jq; run.jq = false;
  return { x, y, jump, jumpHeld: has('jump'), sprint: has('shift'), jet: scene === 'zerog' && document.getElementById('jet').checked };
}
function makePlay() {
  const cfg = { ...PLAY[scene], script: SCRIPTS[PLAY[scene].script] };
  const nowV = views.find(v => v.canvas.id === 'cNow'), newV = views.find(v => v.canvas.id === 'cNew');
  play.now = new Run(cfg, 'now'); play.new = new Run(cfg, 'new');
  for (const r of [play.now, play.new]) { r.trailOn = document.getElementById('trail').checked; r.showLabels = document.getElementById('labels').checked; }
  nowV.run = play.now; newV.run = play.new;
  document.getElementById('jetWrap').hidden = scene !== 'zerog';
}
function setArmed(on) {
  armed = on; const el = document.getElementById('armed');
  el.textContent = on ? 'Клавиши у песочницы. Esc отпускает' : 'Автопилот. Кликните по полю, чтобы управлять';
  el.classList.toggle('on', on);
  if (!on) keys.clear();
}
const KEYMAP = { KeyA: 'left', ArrowLeft: 'left', KeyD: 'right', ArrowRight: 'right', KeyW: 'up', ArrowUp: 'up', KeyS: 'down', ArrowDown: 'down', ShiftLeft: 'shift', ShiftRight: 'shift', Space: 'jump' };
window.addEventListener('keydown', e => {
  if (e.code === 'Escape' && armed) { setArmed(false); return; }
  if (!armed) return;
  const k = KEYMAP[e.code]; if (!k) return;
  e.preventDefault();
  if (e.repeat) return;
  keys.add(k); if (k === 'jump') { play.now.jq = true; play.new.jq = true; }
});
window.addEventListener('keyup', e => { const k = KEYMAP[e.code]; if (k) keys.delete(k); });
window.addEventListener('blur', () => keys.clear());

function buildPlay() {
  const seg = document.getElementById('scenes');
  for (const id in PLAY) {
    const b = document.createElement('button'); b.textContent = PLAY[id].name; b.setAttribute('aria-pressed', id === scene);
    b.addEventListener('click', () => { scene = id; for (const x of seg.children) x.setAttribute('aria-pressed', x === b); makePlay(); });
    seg.appendChild(b);
  }
  const pv = document.getElementById('playViews');
  addView(document.getElementById('cNow'), null, { cells: 16, live: true });
  addView(document.getElementById('cNew'), null, { cells: 16, live: true });
  makePlay();
  pv.addEventListener('pointerdown', () => { setArmed(true); pv.focus(); for (const r of [play.now, play.new]) r.reset(); });
  document.addEventListener('pointerdown', e => { if (armed && !pv.contains(e.target) && !document.getElementById('pad').contains(e.target)) setArmed(false); });
  document.getElementById('auto').addEventListener('click', () => { setArmed(false); makePlay(); });
  document.getElementById('reset').addEventListener('click', () => { for (const r of [play.now, play.new]) r.reset(); });
  document.getElementById('trail').addEventListener('change', e => { for (const r of [play.now, play.new]) { r.trailOn = e.target.checked; r.trail = []; } });
  document.getElementById('labels').addEventListener('change', e => { for (const r of [play.now, play.new]) r.showLabels = e.target.checked; });
  for (const b of document.querySelectorAll('#pad button')) {
    const k = b.dataset.k;
    b.addEventListener('pointerdown', e => { e.preventDefault(); if (!armed) { setArmed(true); for (const r of [play.now, play.new]) r.reset(); } pad.add(k); if (k === 'jump') { play.now.jq = true; play.new.jq = true; } });
    for (const ev of ['pointerup', 'pointerleave', 'pointercancel']) b.addEventListener(ev, () => pad.delete(k));
  }
  document.getElementById('defaults').addEventListener('click', () => { Object.assign(P, DEF); buildTune(); persist(); summary(); });
}

// ---------- cards ----------
const CHOICES = [['take', 'взять'], ['change', 'изменить'], ['drop', 'убрать']];
function pickRow(id, where) {
  const it = ITEM[id], p = picks[id] || {};
  const radios = CHOICES.map(([v, l]) => `<label class="opt"><input type="radio" name="p-${where}-${id}" value="${v}" ${p.c === v ? 'checked' : ''} data-id="${id}"><span class="${v}">${l}</span></label>`).join('');
  return `<div class="row"><span class="name">${it.n}. ${it.t}</span>${radios}</div><textarea class="note" rows="1" data-id="${id}" placeholder="Заметка: что поменять">${p.note || ''}</textarea>`;
}
function bindPicks(root) {
  root.addEventListener('change', e => {
    const t = e.target; if (t.type !== 'radio' || !t.dataset.id) return;
    const id = t.dataset.id; picks[id] = { ...(picks[id] || {}), c: t.value };
    for (const r of document.querySelectorAll(`input[type=radio][data-id="${id}"][value="${t.value}"]`)) r.checked = true;
    persist(); summary();
  });
  root.addEventListener('input', e => {
    const t = e.target; if (!t.classList.contains('note')) return;
    const id = t.dataset.id; picks[id] = { ...(picks[id] || {}), note: t.value };
    for (const n of document.querySelectorAll(`textarea.note[data-id="${id}"]`)) if (n !== t) n.value = t.value;
    persist(); summary();
  });
}
function buildCards() {
  const root = document.getElementById('cards');
  CARDS.forEach((c, i) => {
    const el = document.createElement('article'); el.className = 'card';
    const its = c.items.map(id => ITEM[id]);
    const cost = its.map(it => it.cost).sort().pop();
    el.innerHTML = `<div class="ttl"><h3>${c.title}</h3><span class="rank">${its.map(it => '№' + it.n).join(' · ')}</span></div>
      <p class="mute" style="font-size:13.5px">${c.feel}</p>
      <div class="views"><div class="view"><canvas></canvas><span class="tag now">сейчас</span></div><div class="view"><canvas></canvas><span class="tag new">предлагаю</span></div></div>
      <div class="chips"><span class="chip c-${cost}">цена ${cost}</span><span class="chip">графика: ${its.map(it => it.art).filter((v, j, a) => a.indexOf(v) === j).join('; ')}</span><span class="chip">кооп: ${its.every(it => /^Ничего/.test(it.coop)) ? 'ничего не нужно' : 'см. таблицу'}</span></div>
      <div class="pick">${c.items.map(id => pickRow(id, 'card')).join('')}</div>`;
    root.appendChild(el);
    const [cn, cw] = el.querySelectorAll('canvas');
    const cfg = { ...c, script: SCRIPTS[c.script] };
    const rn = new Run(cfg, 'now'), rw = new Run(cfg, 'new');
    rn.trailOn = rw.trailOn = !!c.trail;
    const vn = addView(cn, rn, { cells: c.view || 9 }), vw = addView(cw, rw, { cells: c.view || 9 });
    new IntersectionObserver(es => { for (const e of es) vn.visible = vw.visible = e.isIntersecting; }, { rootMargin: '100px' }).observe(el);
  });
  bindPicks(root);
}
function buildTable() {
  const tb = document.querySelector('#table tbody');
  tb.innerHTML = ITEMS.map((it, i) => `<tr class="${it.off ? 'off' : ''}"><td class="n">${i + 1}</td><td><b>${it.t}</b><span class="grp">${it.g}</span></td><td>${it.feel}</td><td>${it.what}</td><td><span class="chip c-${it.cost}">${it.cost}</span></td><td>${it.art}</td><td>${it.coop}</td><td>${it.resp}</td><td style="min-width:200px"><div class="pick" style="border:0;padding:0">${pickRow(it.id, 'table')}</div></td></tr>`).join('');
  bindPicks(tb);
}
function summary() {
  const lines = ['Stillwake: движение героя, выбор', ''];
  const word = { take: 'взять', change: 'изменить', drop: 'убрать' };
  for (const it of ITEMS) {
    const p = picks[it.id]; if (!p || (!p.c && !p.note)) continue;
    lines.push(`${ITEM[it.id].n}. ${it.t}: ${p.c ? word[p.c] : 'без решения'}${p.note ? '. ' + p.note.trim() : ''}`);
  }
  if (lines.length === 2) lines.push('(пока ничего не выбрано)');
  const diff = Object.keys(DEF).filter(k => P[k] !== DEF[k]);
  lines.push('', diff.length ? 'Числа, изменённые ползунками:' : 'Числа: как предложено.');
  for (const k of diff) lines.push(`  ${k}: ${fmt(DEF[k])} → ${fmt(P[k])}`);
  document.getElementById('sum').value = lines.join('\n');
}
document.getElementById('copy').addEventListener('click', () => {
  const ta = document.getElementById('sum'), done = document.getElementById('copied');
  const fallback = () => { ta.focus(); ta.select(); done.textContent = 'Текст выделен: Ctrl+C'; };
  try { navigator.clipboard.writeText(ta.value).then(() => { done.textContent = 'Скопировано'; }, fallback); } catch (e) { fallback(); }
});

// ---------- the loop ----------
let lastT = null;
function tick(dt, all) {
  const slow = document.getElementById('slow').checked ? 0.25 : 1;
  for (const v of views) {
    if ((!v.visible && !all) || !v.run) continue;
    const input = v.live && armed ? liveInput : null;
    v.run.frame(dt, slow, input);
  }
}
function paint(all) { for (const v of views) if ((v.visible || all) && v.run) v.run.draw(v.ctx, v.canvas.width, v.canvas.height, v.scale); }
function frame(t) {
  const dt = lastT == null ? 16.7 : Math.min(50, t - lastT); lastT = t;
  tick(dt); paint();
  requestAnimationFrame(frame);
}
(async () => {
  await loadArt(0xe8b04a);
  buildTune(); buildPlay(); buildCards(); buildTable(); summary();
  const qs = new URLSearchParams(location.search), T = +qs.get('t'), focus = qs.get('focus');
  if (qs.get('strip')) {
    const c = CARDS.find(c => c.id === qs.get('strip')), times = qs.get('at').split(',').map(Number), sc = +(qs.get('sc') || 3), cw = +(qs.get('w') || 400), chh = +(qs.get('h') || 220);
    document.querySelector('.wrap').hidden = true;
    const box = document.createElement('div'); box.style.cssText = 'display:grid;grid-template-columns:auto 1fr 1fr;gap:4px;align-items:center;font:12px monospace;color:#ccc'; document.body.prepend(box);
    const cfg = { ...c, script: SCRIPTS[c.script], loop: 0 }, runs = [new Run(cfg, 'now'), new Run(cfg, 'new')];
    runs.forEach(r => r.trailOn = !!c.trail);
    let now = 0;
    for (const at of times) {
      while (now < at) { for (const r of runs) r.frame(16.67, 1, null); now += 16.67; }
      const lab = document.createElement('div'); lab.textContent = at + ' ms'; box.appendChild(lab);
      for (const r of runs) { const cv = document.createElement('canvas'); cv.width = cw; cv.height = chh; box.appendChild(cv); r.draw(cv.getContext('2d'), cw, chh, sc); }
    }
    return;
  }
  if (focus) { const cards = [...document.querySelectorAll('.card')], el = cards[CARDS.findIndex(c => c.id === focus)]; for (const c of document.querySelector('.wrap').children) c.hidden = !c.contains(el); for (const c of cards) c.hidden = c !== el; el.parentElement.previousElementSibling.hidden = true; }
  if (T) { await new Promise(r => setTimeout(r, 120)); for (const v of views) v.fit(); for (let i = 0; i < T / 16.67; i++) tick(16.67, true); await new Promise(r => setTimeout(r, 60)); for (const v of views) v.fit(); paint(true); return; }
  requestAnimationFrame(frame);
})();
