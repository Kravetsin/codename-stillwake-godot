// ---------- wiring: controls, the loop, the pick form ----------
const Q = new URLSearchParams(location.search); // local checks only; the published page has none
const reduce = matchMedia('(prefers-reduced-motion: reduce)').matches;

function seg(el, value, on) {
  el.querySelectorAll('button[data-v]').forEach((b) => { b.setAttribute('aria-pressed', b.dataset.v === value ? 'true' : 'false'); b.onclick = () => { seg(el, b.dataset.v, on); on(b.dataset.v); }; });
}
function toggle(btn, value, on) { btn.setAttribute('aria-pressed', value ? 'true' : 'false'); btn.onclick = () => { value = !value; btn.setAttribute('aria-pressed', value ? 'true' : 'false'); on(value); }; }

function holdKeys(view, el) {
  const down = (e) => { if (['ArrowLeft', 'ArrowRight', 'ArrowUp', 'ArrowDown'].includes(e.key)) { view.keys[e.key] = true; e.preventDefault(); } if (e.key === 'Shift') view.keys.Shift = true; };
  const up = (e) => { view.keys[e.key] = false; if (e.key === 'Shift') view.keys.Shift = false; };
  el.addEventListener('keydown', down); el.addEventListener('keyup', up); el.addEventListener('blur', () => { view.keys = {}; });
}
function dragPan(view, cv, onMove) {
  let last = null;
  cv.addEventListener('pointerdown', (e) => { last = [e.clientX, e.clientY]; cv.setPointerCapture(e.pointerId); cv.focus({ preventScroll: true }); });
  cv.addEventListener('pointermove', (e) => { if (!last) return; const k = cv.width / cv.getBoundingClientRect().width; onMove((e.clientX - last[0]) * k, (e.clientY - last[1]) * k); last = [e.clientX, e.clientY]; });
  const end = () => { last = null; }; cv.addEventListener('pointerup', end); cv.addEventListener('pointercancel', end);
}

function setupSurface(root) {
  const v = new SurfaceView(root);
  const layerBox = root.querySelector('.layers');
  const renderChips = () => {
    const d = DIRS[v.s.dir], items = [['sky', 'Небо'], ['cel', 'Светила'], ['clouds', d.key === 'now' ? 'Облака' : 'Облака, пыль']];
    (d.key === 'now' ? ['Дальние мезы · 0.1', 'Гряда · 0.18', 'Холмы с деревьями · 0.3'] : d.land.map((l) => `${l.name} · ${l.follows}`)).forEach((n, i) => items.push(['L' + i, n]));
    if (d.key !== 'now') items.push(['haze', 'Дымка между слоями']);
    items.push(['life', 'Жизнь'], ['fg', 'Передний план']);
    layerBox.innerHTML = '';
    for (const [id, n] of items) {
      const b = document.createElement('button'); b.type = 'button'; b.className = 'chip'; b.textContent = n; b.setAttribute('aria-pressed', v.s.hidden.has(id) ? 'false' : 'true');
      b.onclick = () => { if (v.s.hidden.has(id)) v.s.hidden.delete(id); else v.s.hidden.add(id); b.setAttribute('aria-pressed', v.s.hidden.has(id) ? 'false' : 'true'); };
      layerBox.appendChild(b);
    }
    root.querySelector('.dirnote').textContent = d.short;
  };
  const setDir = (k) => { v.s.dir = k; v.s.hidden.clear(); renderChips(); seg(root.querySelector('[data-g=dir]'), k, setDir); document.querySelectorAll('.dircard').forEach((c) => c.classList.toggle('on', c.dataset.dir === k)); root.querySelector('[data-g=tod]').classList.toggle('off', k === 'now'); root.querySelector('[data-g=planet]').classList.toggle('off', k === 'now'); };
  seg(root.querySelector('[data-g=dir]'), v.s.dir, setDir);
  seg(root.querySelector('[data-g=tod]'), v.s.tod, (x) => { v.s.tod = x; });
  seg(root.querySelector('[data-g=zoom]'), v.s.zoom, (x) => { v.s.zoom = x; });
  seg(root.querySelector('[data-g=trans]'), v.s.trans, (x) => { v.s.trans = x; });
  toggle(root.querySelector('[data-t=gray]'), v.s.gray, (x) => { v.s.gray = x; });
  toggle(root.querySelector('[data-t=life]'), v.s.life, (x) => { v.s.life = x; });
  toggle(root.querySelector('[data-t=beacon]'), v.s.beacon, (x) => { v.s.beacon = x; });
  toggle(root.querySelector('[data-t=fly]'), false, (x) => { v.fly = x; });
  renderChips();
  setupPlanetControls(v, root);
  holdKeys(v, v.cv);
  dragPan(v, v.cv, (dx, dy) => { v.free -= dx; v.look = clamp(v.look - dy, -260, 200); });
  for (const b of root.querySelectorAll('[data-hold]')) {
    const k = b.dataset.hold, on = (e) => { e.preventDefault(); v.keys[k] = true; v.free = 0; }, off = () => { v.keys[k] = false; };
    b.addEventListener('pointerdown', on); b.addEventListener('pointerup', off); b.addEventListener('pointerleave', off); b.addEventListener('pointercancel', off);
  }
  v.mini.addEventListener('click', (e) => { const r = v.mini.getBoundingClientRect(); v.hero.x = clamp(((e.clientX - r.left) / r.width) * WORLD_W, 40, WORLD_W - 40) * CELL; v.free = 0; });
  document.querySelectorAll('[data-goto]').forEach((b) => b.addEventListener('click', () => { const g = b.dataset.goto; if (v.s.dir === 'now') setDir('A'); v.hero.x = (g === 'b0' ? v.planet.borders[0] + 70 : g === 'b1' ? v.planet.borders[1] - 70 : Number(g)) * CELL; v.free = 0; if (b.dataset.dir) setDir(b.dataset.dir); root.scrollIntoView({ behavior: reduce ? 'auto' : 'smooth', block: 'start' }); }));
  document.querySelectorAll('[data-show]').forEach((b) => b.addEventListener('click', () => { setDir(b.dataset.show); if (b.dataset.tod) { v.s.tod = b.dataset.tod; seg(root.querySelector('[data-g=tod]'), v.s.tod, (x) => { v.s.tod = x; }); } root.scrollIntoView({ behavior: reduce ? 'auto' : 'smooth', block: 'start' }); }));
  // local checks: ?dir=B&x=1500&t=12&tod=dusk&zoom=far&look=-100
  if (Q.get('dir')) setDir(Q.get('dir'));
  if (Q.get('x')) v.hero.x = Number(Q.get('x')) * CELL;
  if (Q.get('tod')) v.s.tod = Q.get('tod');
  if (Q.get('zoom')) v.s.zoom = Q.get('zoom');
  if (Q.get('look')) v.look = Number(Q.get('look'));
  if (Q.get('trans')) v.s.trans = Q.get('trans');
  if (Q.get('t')) v.tFix = Number(Q.get('t'));
  if (Q.get('gray')) v.s.gray = true;
  if (Q.get('planet')) v.setPlanet(PRESETS[Q.get('planet')]);
  if (Q.get('roll')) { let s0 = Number(Q.get('roll')); Math.random = () => (s0 = (s0 * 16807) % 2147483647) / 2147483647; v.setPlanet(randomPlanet()); }
  setDir(v.s.dir);
  window.__surface = v;
  return v;
}

function setupUnder(root) {
  const u = new UnderView(root);
  const setBand = (k) => { u.band = k; seg(root.querySelector('[data-g=band]'), k, setBand); root.querySelector('.bandnote').textContent = `${BANDS[k].name} · ${BANDS[k].depth}`; document.querySelectorAll('.bandcard').forEach((c) => c.classList.toggle('on', c.dataset.band === k)); };
  setBand(Q.get('band') || u.band);
  toggle(root.querySelector('[data-t=ugray]'), false, (x) => { u.gray = x; });
  holdKeys(u, u.cv);
  dragPan(u, u.cv, (dx, dy) => { u.cam.x = clamp(u.cam.x - dx, u.W / 2, HW * CELL - u.W / 2); u.cam.y = clamp(u.cam.y - dy, u.H / 2, HHt * CELL - u.H / 2); });
  document.querySelectorAll('[data-band-show]').forEach((b) => b.addEventListener('click', () => { setBand(b.dataset.bandShow); root.scrollIntoView({ behavior: reduce ? 'auto' : 'smooth', block: 'start' }); }));
  if (Q.get('t')) u.tFix = Number(Q.get('t'));
  if (Q.get('ux')) u.cam.x = Number(Q.get('ux')) * CELL;
  window.__under = u;
  return u;
}

function loop(views) {
  let last = performance.now();
  const io = new IntersectionObserver((es) => es.forEach((e) => { const v = views.find((x) => x.el === e.target); if (v) v.visible = e.isIntersecting; }), { rootMargin: '200px' });
  views.forEach((v) => io.observe(v.el));
  const frame = (now) => {
    const dt = Math.min(0.05, (now - last) / 1000); last = now;
    for (const v of views) {
      if (!v.visible) continue;
      if (reduce && v.tFix == null) v.tFix = 8;
      v.step(dt); v.draw();
    }
    requestAnimationFrame(frame);
  };
  requestAnimationFrame(frame);
  // build the other zones while idle, a layer at a time
  const idle = () => { const did = views[0].prefetch(); setTimeout(idle, did ? 60 : 400); };
  setTimeout(idle, 1200);
}

// ---------- the pick form ----------
const PICK = [
  ['Система: сотни планет', [
    ['sys-system', 'Фон собирается из данных: палитра + зерно + веса реквизита', 'одна система на все планеты, ни одной картины на планету'],
    ['sys-recolour', 'Реквизит в индексных тонах, перекраска палитрой', 'одна акация — восемь биомов'],
    ['sys-code-props', 'Реквизит рисует код (экспорт, как в 0079)', 'или художник рисует силуэты в 4 серых тонах'],
    ['sys-savanna', 'Тип «Саванна»', ''],
    ['sys-badlands', 'Тип «Бэдлендс»', ''],
    ['sys-crystal', 'Тип «Кристальный»', ''],
    ['sys-ice', 'Тип «Ледяной»', ''],
    ['sys-volcanic', 'Тип «Вулканический»', ''],
    ['sys-fungal', 'Тип «Грибной»', ''],
    ['sys-fg', 'Земля и флора переднего плана тоже от палитры биома', 'мох, оттенок грунта, листва — как в сцене'],
  ]],
  ['Направления', [
    ['dir-A', 'A · Ясный простор', 'нынешний день, нарисованный в s9: 5 слоёв земли, гигант, 2 луны, корабль'],
    ['dir-B', 'B · Чужие формы', 'парящие скалы, арки, останцы, гигантская флора, пылевые полосы'],
    ['dir-C', 'C · Небо-событие', 'огромный гигант с тенью кольца, сияние, звёзды днём, низкая земля'],
    ['dir-D', 'D · Дымка и столбы света', 'низкое солнце с лучами, лес силуэтов в слоях тумана'],
  ]],
  ['Зоны внутри планеты', [
    ['zone-three', 'Три зоны на планету: биом спавна и два соседа', 'соседи — из пула типа, границы — от зерна'],
    ['zone-sav', 'Саванна у спавна', 'бирюзово-зелёные холмы, акации, семена светятся'],
    ['zone-bad', 'Бэдлендс слева', 'ржавые мезы со слоями пород, останцы, пыль, янтарные огни'],
    ['zone-cry', 'Кристальные поля справа', 'фиолетовые зубчатые хребты, кристаллы с бликами'],
  ]],
  ['Под землёй', [
    ['band-where', 'Окна в глубину только в больших залах', 'середина зала без естественной задней стены, кайма 1–3 клетки; дневной свет туда не идёт'],
    ['band-cold', 'Холодные пещеры', 'корни со свода, ледяные кристаллы, голубые жилы, капель'],
    ['band-garden', 'Бирюзовый сад', 'гигантские светящиеся шляпки, нити с бусинами, споры'],
    ['band-amber', 'Янтарная глубь', 'жеода, кристальные колонны, дальние дома создателей арок'],
    ['band-lava', 'Лавовое ядро', 'базальтовые столбы, лавовое озеро светит снизу, искры, марево'],
  ]],
  ['Детали', [
    ['d-giant', 'Гигант крупнее, в полосах, с кольцом', ''],
    ['d-moons', 'Одна-две луны', ''],
    ['d-ship', 'Корабль на орбите — бегущая точка света', 'пересекает небо примерно за 2 минуты'],
    ['d-clouds', 'Перистые + кучевые облака, плывут', ''],
    ['d-dust', 'Пылевые полосы (B)', ''],
    ['d-aurora', 'Сияние днём (C)', ''],
    ['d-stars', 'Звёзды у зенита днём (C)', ''],
    ['d-rays', 'Лучи от низкого солнца (D)', ''],
    ['d-spires', 'Дальние шпили со столбами света на хребтах', 'декорация, не настоящие шпили'],
    ['d-beacon', 'Отсвет настоящего шпиля на небе издалека', 'ориентир: где ближайший шпиль, пока он за краем экрана'],
    ['d-floaters', 'Парящие скалы покачиваются', ''],
    ['d-arches', 'Арки и останцы', ''],
    ['d-flora', 'Гигантская светящаяся флора на ближних холмах', ''],
    ['d-haze', 'Полосы дымки между слоями, дрейфуют', ''],
    ['d-outline', 'Мягкий контур у ближайшего слоя (свой тёмный, не чёрный)', ''],
    ['d-motes', 'Мошки у земли днём (уже есть)', ''],
    ['d-life', 'Существа-«птицы» вдали', 'опционально: существ в игре пока нет'],
    ['d-twinkle', 'Блики кристаллов (зона 3)', ''],
    ['d-trans', 'Переход T2: дальние слои меняются раньше и шире', 'T1 — все слои вместе, ±40 клеток'],
    ['d-dusk', 'Закат как вариант', 'смена суток — отдельное решение'],
  ]],
];
const VERDICT = [['take', 'Беру'], ['change', 'Изменить'], ['drop', 'Не надо']];
function setupPick(root) {
  const KEY = 'stillwake-planet-bg-pick';
  let saved = {}; try { saved = JSON.parse(localStorage.getItem(KEY) || '{}'); } catch (e) { saved = {}; }
  const out = root.querySelector('#pick-summary');
  for (const [group, items] of PICK) {
    const fs = document.createElement('fieldset'); fs.className = 'pickgroup';
    fs.innerHTML = `<legend>${group}</legend>`;
    for (const [id, name, hint] of items) {
      const row = document.createElement('div'); row.className = 'pickrow';
      row.innerHTML = `<div class="pickname"><span>${name}</span>${hint ? `<small>${hint}</small>` : ''}</div>
        <div class="verdicts" role="radiogroup" aria-label="${name}">${VERDICT.map(([v, l]) => `<label><input type="radio" name="${id}" id="${id}-${v}" value="${v}"><span>${l}</span></label>`).join('')}</div>
        <input type="text" class="note" id="${id}-note" placeholder="заметка" aria-label="Заметка: ${name}">`;
      fs.appendChild(row);
    }
    root.querySelector('.pickgroups').appendChild(fs);
  }
  const fill = () => {
    for (const [, items] of PICK) for (const [id] of items) {
      const s = saved[id]; if (!s) continue;
      if (s.v) { const r = document.getElementById(`${id}-${s.v}`); if (r) r.checked = true; }
      if (s.n) document.getElementById(`${id}-note`).value = s.n;
    }
    const g = document.getElementById('pick-general'); if (saved.general) g.value = saved.general;
  };
  const sum = () => {
    const lines = ['Фон планеты — выбор'];
    for (const [group, items] of PICK) {
      const ls = [];
      for (const [id, name] of items) {
        const r = root.querySelector(`input[name="${id}"]:checked`), n = document.getElementById(`${id}-note`).value.trim();
        saved[id] = { v: r ? r.value : '', n };
        if (r || n) ls.push(`- ${name}: ${r ? VERDICT.find((x) => x[0] === r.value)[1].toLowerCase() : '—'}${n ? ' — ' + n : ''}`);
      }
      if (ls.length) lines.push('', group + ':', ...ls);
    }
    const g = document.getElementById('pick-general').value.trim(); saved.general = g;
    if (g) lines.push('', 'Общее: ' + g);
    if (lines.length === 1) lines.push('', '(пока ничего не отмечено)');
    out.value = lines.join('\n');
    try { localStorage.setItem(KEY, JSON.stringify(saved)); } catch (e) { /* the page works without it */ }
  };
  fill(); sum();
  root.addEventListener('input', sum); root.addEventListener('change', sum);
  root.querySelector('#pick-copy').addEventListener('click', async () => {
    const msg = root.querySelector('#pick-copied');
    try { await navigator.clipboard.writeText(out.value); msg.textContent = 'Скопировано'; }
    catch (e) { out.focus(); out.select(); msg.textContent = 'Выделено — нажми Ctrl+C'; }
    setTimeout(() => { msg.textContent = ''; }, 2500);
  });
  root.querySelector('#pick-clear').addEventListener('click', () => { root.querySelectorAll('input[type=radio]').forEach((r) => { r.checked = false; }); root.querySelectorAll('input.note, #pick-general').forEach((n) => { n.value = ''; }); saved = {}; sum(); });
}

function layerLists() {
  document.querySelectorAll('[data-layers]').forEach((ul) => {
    const d = DIRS[ul.dataset.layers];
    const items = [`Небо и светила · у экрана`, ...d.clouds.map((c) => `${c.kind === 'cirrus' ? 'Перистые облака' : c.kind === 'dust' ? 'Пылевые полосы' : 'Кучевые облака'} · ${c.follows} + дрейф`), ...d.land.map((l) => `${l.name} · ${l.follows}`)];
    ul.innerHTML = items.map((x) => { const [a, b] = x.split(' · '); return `<li><span>${a}</span><b>${b}</b></li>`; }).join('');
  });
}

loadArt().then(() => {
  buildHero(); layerLists(); setupRecolour();
  const views = [setupSurface(document.getElementById('viewer')), setupUnder(document.getElementById('uviewer'))];
  setupPick(document.getElementById('pick'));
  setupGallery(views[0]);
  document.documentElement.classList.add('ready');
  loop(views);
});
