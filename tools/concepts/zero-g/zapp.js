'use strict';
window.addEventListener('error', e => { const el = document.getElementById('err'); if (el) el.textContent += (e.message || e) + '\n'; });
const $ = id => document.getElementById(id);
const fmt = v => (Math.round(v * 100) / 100).toString().replace('.', ',');
const SAVE = 'stillwake-zerog-arms-v1';
function store(get, val) { try { if (get) return JSON.parse(localStorage.getItem(SAVE) || 'null'); localStorage.setItem(SAVE, JSON.stringify(val)); } catch (e) { return null; } }
const saved = store(true) || {};
const picks = saved.picks || {};
if (saved.Z) for (const k in saved.Z) if (k in ZDEF) Z[k] = saved.Z[k];
function persist() { store(false, { picks, Z: { ...Z }, mix: $('mixNote').value }); }

// ---------- views ----------
const DPR = Math.min(2, window.devicePixelRatio || 1);
const views = [];
function addView(canvas, run, cells) {
  const v = { canvas, ctx: canvas.getContext('2d'), run, visible: true, cells };
  v.fit = () => { const W = Math.max(1, Math.round(canvas.clientWidth * DPR)), H = Math.max(1, Math.round(canvas.clientHeight * DPR)), changed = canvas.width !== W || canvas.height !== H; if (canvas.width !== W) canvas.width = W; if (canvas.height !== H) canvas.height = H; v.scale = Math.max(2, Math.round(canvas.width / (v.cells * CELL))); if (changed && v.run && v.run.drawn) v.run.draw(v.ctx, W, H, v.scale); };
  new ResizeObserver(v.fit).observe(canvas); v.fit(); views.push(v); return v;
}

// ---------- the wall of panels ----------
let state = STATES[1];
const panels = [];
function buildGrid() {
  const seg = $('states');
  for (const s of STATES) {
    const b = document.createElement('button'); b.textContent = s.name; b.setAttribute('aria-pressed', s === state);
    b.addEventListener('click', () => { state = s; for (const x of seg.children) x.setAttribute('aria-pressed', x === b); remake(); });
    seg.appendChild(b);
  }
  const root = $('grid');
  for (const v of VARIANTS) {
    const el = document.createElement('article'); el.className = 'panel' + (v.id === 'now' ? ' now' : '');
    el.innerHTML = `<div class="ttl"><span class="let">${v.id === 'now' ? '—' : v.id}</span><h3>${v.name}</h3></div><canvas></canvas><p class="short">${v.short}</p>${pickRow(v.id)}`;
    root.appendChild(el);
    const view = addView(el.querySelector('canvas'), null, 6);
    new IntersectionObserver(es => { for (const e of es) view.visible = e.isIntersecting; }, { rootMargin: '120px' }).observe(el);
    panels.push({ v, view });
  }
  bindPicks(root);
  remake();
}
function remake() {
  for (const p of panels) p.view.run = new ZRun({ map: state.map, at: state.at, face: state.face, item: state.item, state, variant: p.v });
  $('what').textContent = state.what;
}

// ---------- picks ----------
const CHOICES = [['take', 'взять'], ['change', 'изменить'], ['drop', 'убрать']];
function pickRow(id) {
  if (id === 'now') return `<div class="pick"><div class="row mute" style="font-size:12.5px">Для сравнения: так в игре сейчас.</div></div>`;
  const p = picks[id] || {};
  const radios = CHOICES.map(([c, l]) => `<label class="opt"><input type="radio" name="p-${id}" value="${c}" ${p.c === c ? 'checked' : ''} data-id="${id}"><span class="${c}">${l}</span></label>`).join('');
  return `<div class="pick"><div class="row">${radios}</div><textarea class="note" rows="1" data-id="${id}" placeholder="Заметка">${p.note || ''}</textarea></div>`;
}
function bindPicks(root) {
  root.addEventListener('change', e => { const t = e.target; if (t.type !== 'radio' || !t.dataset.id) return; picks[t.dataset.id] = { ...(picks[t.dataset.id] || {}), c: t.value }; persist(); summary(); });
  root.addEventListener('input', e => { const t = e.target; if (!t.classList.contains('note')) return; picks[t.dataset.id] = { ...(picks[t.dataset.id] || {}), note: t.value }; persist(); summary(); });
}

// ---------- the numbers ----------
const cps = v => fmt(v * 60 / 1000);
const TUNE = [
  ['swim', 'плавание', 10, 60, 1, v => `${cps(v)} кл/с`],
  ['swimMs', 'набор плавания, мс', 0, 400, 10, v => `${fmt(v)} мс`],
  ['pushOff', 'толчок от стены', 0, 180, 5, v => `${cps(v)} кл/с`],
  ['pushDrag', 'гашение толчка', 0.1, 3, 0.1, v => `${fmt(v)} · полёт ${fmt(glide(v))} кл`],
  ['burstMs', 'рывок ранца, мс', 0, 300, 10, v => `${fmt(v)} мс`],
  ['burstX', 'рывок ранца ×', 1, 4, 0.1, v => `×${fmt(v)}`],
  ['jpAccel', 'тяга ранца, за шаг', 2, 20, 1, v => `${fmt(v)}`],
  ['jpTop', 'потолок скорости ранца', 100, 400, 10, v => `${cps(v)} кл/с`],
  ['jpDrag', 'торможение ранцем, за шаг', 1, 10, 0.5, v => `${fmt(v)}`],
  ['puffs', 'струйки газа при торможении', 0, 1, 1, v => (v ? 'да' : 'нет')],
];
const glide = drag => (Z.pushOff * Z.pushOff - Z.swim * Z.swim) / (2 * drag) / 1000;
function buildTune() {
  const root = $('tune'); root.innerHTML = '';
  for (const [k, label, lo, hi, st, show] of TUNE) {
    const d = document.createElement('div'); d.className = 'sl';
    d.innerHTML = `<label for="t-${k}">${label}</label><span><output id="o-${k}"></output> <span class="was">${lo === 0 && hi === 1 ? '' : 'одобр. ' + fmt(ZDEF[k])}</span></span><input type="range" id="t-${k}" min="${lo}" max="${hi}" step="${st}" value="${Z[k]}">`;
    root.appendChild(d);
    const inp = d.querySelector('input');
    inp.addEventListener('input', () => { Z[k] = parseFloat(inp.value); showTune(); persist(); summary(); });
  }
  showTune();
}
function showTune() { for (const [k, , , , , show] of TUNE) $('o-' + k).textContent = show(Z[k]); }

// ---------- the sandbox ----------
const sb = { item: '', old: false, variant: { ...VAR.M }, preset: 'M' };
const keys = new Set(), pad = new Set(); let armed = false, playView = null;
function sbVariant() {
  if (sb.preset) return VAR[sb.preset];
  const v = { id: 'mix', name: 'свой микс', base: $('pBase').value };
  for (const c of document.querySelectorAll('#pLayers input[data-l]')) if (c.checked) v[c.dataset.l] = 1;
  return v;
}
function showLayers(v) { $('pBase').value = v.base; for (const c of document.querySelectorAll('#pLayers input[data-l]')) c.checked = !!v[c.dataset.l]; }
function makePlay(keep) {
  const was = playView.run;
  const run = new ZRun({ map: 'play', at: [3.5, 11.6], face: 1, item: sb.item || null, old: sb.old, variant: sbVariant() });
  if (keep && was) { run.hero = { ...was.hero }; run.prev = run.hero; }
  playView.run = run;
}
function liveInput(run) {
  const has = k => keys.has(k) || pad.has(k);
  const x = (has('right') ? 1 : 0) - (has('left') ? 1 : 0), y = (has('down') ? 1 : 0) - (has('up') ? 1 : 0);
  const jump = !!run.jq; run.jq = false;
  return { x, y, jump, jumpHeld: has('jump'), sprint: has('shift'), jet: $('pJet').checked };
}
// the autopilot before a click: a loop through what the sandbox has
const DEMO = { len: 0, tl: [[40, K()], [70, K(1)], [20, K()], [70, K(0, -1)], [30, K()], [14, K(1, 0, { sprint: true })], [70, K()], [10, K(0, 1, { sprint: true })], [70, K()], [16, K(-1, 0, { sprint: true })], [80, K()], [12, K(0, 1, { sprint: true })], [60, K()]], jet: true };
DEMO.len = DEMO.tl.reduce((a, [n]) => a + n, 0);
function setArmed(on) {
  armed = on; const el = $('armed');
  el.textContent = on ? 'Клавиши у песочницы · Esc отпускает' : 'Автопилот · кликните по полю, чтобы управлять';
  el.classList.toggle('on', on); if (!on) keys.clear();
}
const KEYMAP = { KeyA: 'left', ArrowLeft: 'left', KeyD: 'right', ArrowRight: 'right', KeyW: 'up', ArrowUp: 'up', KeyS: 'down', ArrowDown: 'down', ShiftLeft: 'shift', ShiftRight: 'shift', Space: 'jump' };
window.addEventListener('keydown', e => {
  if (e.code === 'Escape' && armed) { setArmed(false); return; }
  if (!armed) return;
  if (e.code === 'KeyJ') { $('pJet').checked = !$('pJet').checked; return; }
  const k = KEYMAP[e.code]; if (!k) return;
  e.preventDefault(); if (e.repeat) return;
  keys.add(k); if (k === 'jump' && playView.run) playView.run.jq = true;
});
window.addEventListener('keyup', e => { const k = KEYMAP[e.code]; if (k) keys.delete(k); });
window.addEventListener('blur', () => keys.clear());
function buildPlay() {
  playView = addView($('cPlay'), null, 18);
  const seg = $('pVars');
  for (const v of VARIANTS) {
    const b = document.createElement('button'); b.textContent = v.id === 'now' ? 'сейчас' : v.id === 'M' ? 'микс' : v.id; b.title = v.name; b.dataset.v = v.id; b.setAttribute('aria-pressed', v.id === sb.preset);
    b.addEventListener('click', () => { sb.preset = v.id; for (const x of seg.children) x.setAttribute('aria-pressed', x === b); showLayers(v); playView.run.fig.setVariant(v); });
    seg.appendChild(b);
  }
  showLayers(VAR[sb.preset]);
  $('pLayers').addEventListener('change', () => { sb.preset = null; for (const x of seg.children) x.setAttribute('aria-pressed', false); playView.run.fig.setVariant(sbVariant()); });
  for (const b of $('pItem').children) b.addEventListener('click', () => { sb.item = b.dataset.i; for (const x of $('pItem').children) x.setAttribute('aria-pressed', x === b); playView.run.cfg.item = sb.item || null; });
  for (const b of $('pPhys').children) b.addEventListener('click', () => { sb.old = b.dataset.p === 'old'; for (const x of $('pPhys').children) x.setAttribute('aria-pressed', x === b); playView.run.cfg.old = sb.old; });
  $('pReset').addEventListener('click', () => makePlay(false));
  makePlay(false);
  const stage = $('stage');
  stage.addEventListener('pointerdown', () => { if (!armed) { setArmed(true); makePlay(false); } stage.focus(); });
  document.addEventListener('pointerdown', e => { if (armed && !stage.contains(e.target) && !$('pad').contains(e.target) && !e.target.closest('.play')) setArmed(false); });
  for (const b of document.querySelectorAll('#pad button')) {
    const k = b.dataset.k;
    b.addEventListener('pointerdown', e => { e.preventDefault(); if (!armed) { setArmed(true); makePlay(false); } pad.add(k); if (k === 'jump') playView.run.jq = true; });
    for (const ev of ['pointerup', 'pointerleave', 'pointercancel']) b.addEventListener(ev, () => pad.delete(k));
  }
  $('defaults').addEventListener('click', () => { Object.assign(Z, ZDEF); buildTune(); persist(); summary(); });
  setArmed(false);
}
function readout() {
  const r = playView.run; if (!r) return;
  const h = r.hero, sp = Math.hypot(h.vx, h.vy) * 60 / 1000;
  $('readout').textContent = `${fmt(Math.round(sp * 10) / 10)} кл/с${h.thrust ? ' · тяга' : h.rcs ? ' · газ' : ''}`;
}

// ---------- the summary ----------
function summary() {
  const word = { take: 'взять', change: 'изменить', drop: 'убрать' };
  const lines = ['Stillwake: руки в невесомости, выбор', ''];
  for (const v of VARIANTS) {
    if (v.id === 'now') continue;
    const p = picks[v.id]; if (!p || (!p.c && !p.note)) continue;
    lines.push(`${v.id}. ${v.name}: ${p.c ? word[p.c] : 'без решения'}${p.note && p.note.trim() ? '. ' + p.note.trim() : ''}`);
  }
  if (lines.length === 2) lines.push('(варианты пока не выбраны)');
  const mix = $('mixNote').value.trim(); if (mix) lines.push('', 'Микс: ' + mix);
  const diff = Object.keys(ZDEF).filter(k => Z[k] !== ZDEF[k]);
  lines.push('', diff.length ? 'Физика п. 10, изменено ползунками:' : 'Физика п. 10: числа как одобрено.');
  for (const k of diff) { const row = TUNE.find(t => t[0] === k); lines.push(`  ${row ? row[1] : k}: ${fmt(ZDEF[k])} → ${fmt(Z[k])}${row && /кл\/с/.test(row[5](Z[k])) ? ' (' + row[5](Z[k]) + ')' : ''}`); }
  $('sum').value = lines.join('\n');
}
$('mixNote').value = saved.mix || '';
$('mixNote').addEventListener('input', () => { persist(); summary(); });
$('copy').addEventListener('click', () => {
  const ta = $('sum'), done = $('copied');
  const fallback = () => { ta.focus(); ta.select(); done.textContent = 'Текст выделен: Ctrl+C'; };
  try { navigator.clipboard.writeText(ta.value).then(() => { done.textContent = 'Скопировано'; }, fallback); } catch (e) { fallback(); }
});
$('sync').addEventListener('click', remake);

// ---------- the loop ----------
let lastT = null, roT = 0;
function tick(dt, all) {
  const slow = $('slow').checked ? 0.25 : 1;
  for (const v of views) {
    if ((!v.visible && !all) || !v.run) continue;
    if (v === playView) v.run.frame(dt, 1, armed ? liveInput : r => keysAt(DEMO, r.t % DEMO.len));
    else v.run.frame(dt, slow, null);
  }
}
function paint(all) { for (const v of views) if ((v.visible || all) && v.run) v.run.draw(v.ctx, v.canvas.width, v.canvas.height, v.scale); }
function frame(t) {
  const dt = lastT == null ? 16.7 : Math.min(50, t - lastT); lastT = t;
  tick(dt); paint();
  if ((roT += dt) > 120) { roT = 0; readout(); }
  requestAnimationFrame(frame);
}
(async () => {
  await loadZArt();
  const qs = new URLSearchParams(location.search);
  if (qs.get('strip')) return strip(qs);
  buildTune(); buildGrid(); buildPlay(); summary();
  const T = +qs.get('t');
  if (qs.get('state')) { const s = STATES.find(s => s.id === qs.get('state')); if (s) { state = s; [...$('states').children].forEach((b, i) => b.setAttribute('aria-pressed', STATES[i] === s)); remake(); } }
  if (T) { await new Promise(r => setTimeout(r, 120)); for (const v of views) v.fit(); for (let i = 0; i < T / 16.67; i++) tick(16.67, true); await new Promise(r => setTimeout(r, 60)); for (const v of views) v.fit(); paint(true); readout(); return; }
  requestAnimationFrame(frame);
})();
// ?strip=<state>&at=ms,ms&vars=now,A&sc=3: frames over time, one column per variant, to look at
function strip(qs) {
  document.querySelector('.wrap').hidden = true;
  const s = STATES.find(s => s.id === qs.get('strip')), times = qs.get('at').split(',').map(Number), sc = +(qs.get('sc') || 3);
  const vars = (qs.get('vars') || VARIANTS.map(v => v.id).join(',')).split(','), cw = +(qs.get('w') || 170), chh = +(qs.get('h') || 170), dbg = !!qs.get('dbg');
  if (qs.get('nbp')) { const n = qs.get('nbp').split(',').map(Number); NBP.f = [n[0], n[1]]; NBP.b = [n[2], n[3]]; }
  const box = document.createElement('div'); box.style.cssText = `display:grid;grid-template-columns:52px repeat(${vars.length}, ${cw}px);gap:3px;align-items:center;font:11px monospace;color:#ccc;padding:4px`;
  document.body.prepend(box);
  box.appendChild(document.createElement('div'));
  for (const id of vars) { const d = document.createElement('div'); d.textContent = id + ' ' + VAR[id].name; box.appendChild(d); }
  const runs = vars.map(id => new ZRun({ map: s.map, at: s.at, face: s.face, item: s.item, state: s, variant: VAR[id] }));
  let now = 0;
  for (const at of times) {
    while (now < at) { for (const r of runs) r.frame(16.67, 1, null); now += 16.67; }
    const lab = document.createElement('div'); lab.textContent = at + 'ms'; box.appendChild(lab);
    for (const r of runs) { r.cam = null; const cv = document.createElement('canvas'); cv.width = cw; cv.height = chh; box.appendChild(cv); r.draw(cv.getContext('2d'), cw, chh, sc, dbg); }
  }
}
