// Writes every candidate of the workbench, the crusher and the new items' icons into outDir, and pick.html to
// choose from: the machines at rest and at work beside their neighbours, the icons in inventory slots.
// node tools/ship-art/economy/build.js [outDir, default economy/out] [kinds, e.g. workbench or crusher,items: only those];
// pick.js copies the picked ones (picks.json).
const fs = require('fs'), path = require('path');
const { encode } = require('../part-icons/png.js');
const { Cv } = require('./lib.js');
const ONLY = process.argv[3] ? process.argv[3].split(',') : null;
const machines = [['workbench', 'Верстак', require('./bench.js').D], ['crusher', 'Дробилка', require('./crusher.js').D]].filter(([k]) => !ONLY || ONLY.includes(k));
const items = !ONLY || ONLY.includes('items') ? require('./icons.js').items : [];
const ART = path.join(__dirname, '..', '..', '..', 'game', 'art');
const OUT = process.argv[2] || path.join(__dirname, 'out');

const b64 = (buf) => 'data:image/png;base64,' + buf.toString('base64');
const write = (file, buf) => { fs.mkdirSync(path.dirname(file), { recursive: true }); fs.writeFileSync(file, buf); return b64(buf); };
const art = (name) => b64(fs.readFileSync(path.join(ART, name + '.png')));

/** Every candidate's pictures: out/<kind>/<id>/machine_<kind>.png and <kind>_work_0-3.png. */
function paintMachines() {
  return machines.map(([kind, name, D]) => ({
    kind, name,
    designs: D.map((d) => {
      const dir = path.join(OUT, kind, d.id), rest = d.rest();
      return {
        id: d.id, name: d.name, what: d.what, rise: d.rise || 0, w: rest.w, h: rest.h,
        rest: write(path.join(dir, `machine_${kind}.png`), rest.png()),
        work: [0, 1, 2, 3].map((f) => write(path.join(dir, `${kind}_work_${f}.png`), d.work(f).png())),
      };
    }),
  }));
}

/** Every icon variant: out/items/<id>-<n>.png. */
function paintIcons() {
  return items.map((it) => ({ id: it.id, name: it.name, what: it.what, variants: it.variants.map((v, k) => ({ n: k + 1, desc: v.desc, src: write(path.join(OUT, 'items', `${it.id}-${k + 1}.png`), encode(16, 16, v.icon.rgba())) })) }));
}

// The neighbours each is seen beside: a machine of its size and kind, the icons of its kind.
const NEAR = {
  workbench: ['machine_arcBench', 'machine_furnace'],
  crusher: ['machine_former', 'machine_smelter', 'machine_furnace'],
};
const NEAR_ICONS = { asteroidOre: ['rock', 'magnetite', 'asteroid'], ironOre: ['magnetite', 'copperOre', 'iron'], gravel: ['rock', 'scrap', 'soil'], quartz: ['crystal', 'glass', 'silicon'], alloyPlate: ['alloyScrap', 'plate', 'hullPlate'], hide: ['cloth', 'biomass', 'cape'], fat: ['biofuel', 'biomass', 'soil'] };

function page(ms, icons) {
  const near = Object.fromEntries(Object.entries(NEAR).map(([k, ns]) => [k, ns.map((n) => ({ n, src: art(n) }))]));
  const nearIcons = Object.fromEntries(Object.entries(NEAR_ICONS).map(([k, ns]) => [k, ns.map((n) => ({ n, src: art('items/' + n) }))]));
  return `<!doctype html>
<html lang="ru"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>Верстак, дробилка, ресурсы</title>
<style>
:root{--bg:#0b0e14;--panel:#111823;--card:#0e141d;--slot:rgba(30,41,59,.9);--slotLine:#475569;--line:#334155;--amber:#fcd34d;--text:#d6dde8;--dim:#7d8aa0;--deck:#2a2f3a}
*{box-sizing:border-box}
body{margin:0;background:var(--bg);color:var(--text);font:14px/1.4 system-ui,"Segoe UI",sans-serif}
header{position:sticky;top:0;z-index:5;background:rgba(11,14,20,.97);border-bottom:1px solid var(--line);padding:12px 16px}
h1{font-size:16px;margin:0 0 4px;font-weight:600}
.sub{color:var(--dim);font-size:12px;margin:0 0 8px}
.tally{display:flex;gap:8px;align-items:center;flex-wrap:wrap}
.tally code{background:var(--panel);border:1px solid var(--line);border-radius:4px;padding:6px 8px;font:12px ui-monospace,Consolas,monospace;color:var(--amber);flex:1 1 320px;max-width:900px;overflow-wrap:anywhere}
button{font:inherit;cursor:pointer}
.btn{background:var(--panel);color:var(--text);border:1px solid var(--line);border-radius:4px;padding:5px 10px}
.btn:hover{border-color:var(--amber)}
main{padding:12px 16px 40px;display:flex;flex-direction:column;gap:14px}
section{background:var(--panel);border:1px solid #1d2635;border-radius:6px;padding:10px 12px}
h2{font-size:15px;margin:0 0 8px;font-weight:600}h2 span{color:var(--dim);font-weight:400;font-size:12px;margin-left:6px}
.row{display:flex;gap:10px;overflow-x:auto;padding-bottom:4px;flex-wrap:wrap}
.card{flex:none;border:1px solid #1d2635;border-radius:6px;padding:8px;display:flex;flex-direction:column;gap:6px;background:var(--card);max-width:100%}
.card.on{border-color:var(--amber);box-shadow:0 0 0 1px var(--amber) inset}
.top{display:flex;align-items:center;justify-content:space-between;gap:8px}
.tag{font-weight:700;font-size:13px}
.pick{background:transparent;border:1px solid var(--line);color:var(--dim);border-radius:4px;padding:2px 8px;font-size:12px}
.card.on .pick{background:var(--amber);border-color:var(--amber);color:#1a1205;font-weight:700}
canvas{image-rendering:pixelated;display:block;background:var(--deck);border-radius:4px}
.views{display:flex;gap:8px;align-items:flex-end;flex-wrap:wrap}
.desc{font-size:12px;color:var(--dim);line-height:1.3;max-width:420px}
.lab{font-size:11px;color:var(--dim);text-transform:uppercase;letter-spacing:.06em}
.slot{width:36px;height:36px;background:var(--slot);border:1px solid var(--slotLine);border-radius:3px;display:grid;place-items:center;flex:none}
.slot img{image-rendering:pixelated;display:block;width:28px;height:28px}
.slot.sel{border-color:var(--amber)}
.zoom{width:64px;height:64px;image-rendering:pixelated;background:var(--slot);border:1px solid var(--line);border-radius:4px;display:block}
.cmp{display:flex;gap:4px;align-items:center;margin-top:8px;flex-wrap:wrap}
</style></head><body>
<header>
  <h1>Верстак, дробилка и иконки ресурсов</h1>
  <p class="sub">Машины: покой ×6, работа ×6 (кадры по кругу) и ×3 на палубе рядом с соседями, низ картинки на полу. Иконки: ×4 и в слоте 36 px, рядом с похожими из игры.</p>
  <div class="tally"><code id="tally"></code><button class="btn" id="copy">Копировать</button><button class="btn" id="reset">Сброс</button></div>
</header>
<main id="list"></main>
<script>
const MS = ${JSON.stringify(ms)};
const ICONS = ${JSON.stringify(icons)};
const NEAR = ${JSON.stringify(near)};
const NEAR_ICONS = ${JSON.stringify(nearIcons)};
const KEY = 'stillwake-economy-picks';
let picks = {};
try { picks = JSON.parse(localStorage.getItem(KEY) || '{}') || {}; } catch (e) { picks = {}; }
const store = () => { try { localStorage.setItem(KEY, JSON.stringify(picks)); } catch (e) {} };
const el = (t, cls, text) => { const e = document.createElement(t); if (cls) e.className = cls; if (text != null) e.textContent = text; return e; };
const loaded = {};
const img = (src) => loaded[src] || (loaded[src] = Object.assign(new Image(), { src }));
const anims = [];
// A canvas of pictures laid on one floor at scale k: each [srcs, work?]; work pictures cycle over the rest one.
function deck(k, parts) {
  const w = parts.reduce((s, p) => s + p.w + 4, 4), h = Math.max(...parts.map((p) => p.h)) + 4;
  const c = el('canvas'); c.width = w * k; c.height = h * k; const g = c.getContext('2d'); g.imageSmoothingEnabled = false;
  const draw = (t) => {
    g.clearRect(0, 0, c.width, c.height); g.fillStyle = '#1a1e25'; g.fillRect(0, (h - 2) * k, c.width, 2 * k);
    let x = 4;
    for (const p of parts) { const y = h - 2 - p.h; if (p.hero) { hero(g, x, y, k); x += p.w + 4; continue; } g.drawImage(img(p.rest), x * k, y * k, p.w * k, p.h * k); if (p.work) g.drawImage(img(p.work[Math.floor(t / p.step) % 4]), x * k, y * k, p.w * k, p.h * k); x += p.w + 4; }
  };
  anims.push(draw); return c;
}
// A hero's outline for scale, 40 px tall (5 cells).
function hero(g, x, y, k) {
  g.fillStyle = '#56607a';
  for (const [a, b, w, hh] of [[3, 0, 6, 7], [1, 7, 10, 14], [2, 21, 3, 19], [7, 21, 3, 19]]) g.fillRect((x + a) * k, (y + b) * k, w * k, hh * k);
}
function machineCard(m, d) {
  const c = el('div', 'card' + (picks[m.kind] === d.id ? ' on' : ''));
  const top = el('div', 'top'); top.append(el('span', 'tag', d.name));
  const b = el('button', 'pick', 'беру'); b.onclick = () => { if (picks[m.kind] === d.id) delete picks[m.kind]; else picks[m.kind] = d.id; store(); render(); };
  top.append(b); c.append(top);
  const v = el('div', 'views');
  v.append(deck(6, [{ ...d, work: null }]), deck(6, [{ ...d, step: m.kind === 'crusher' ? 120 : 160 }]));
  c.append(v);
  const nb = NEAR[m.kind].map((n) => ({ rest: n.src, w: 32, h: 32 }));
  c.append(deck(3, [nb[0], { hero: true, w: 12, h: 40 }, { ...d, step: m.kind === 'crusher' ? 120 : 160 }, ...nb.slice(1)]));
  c.append(el('div', 'desc', d.what + (d.rise ? ' (картинка ' + d.w + '×' + d.h + ', встаёт над футпринтом на ' + d.rise + ' кл.)' : '')));
  return c;
}
const slot = (src, cls, title) => { const s = el('div', 'slot' + (cls ? ' ' + cls : '')); if (title) s.title = title; const i = new Image(); i.src = src; s.append(i); return s; };
function iconCard(it, v) {
  const c = el('div', 'card' + (picks[it.id] === v.n ? ' on' : ''));
  const top = el('div', 'top'); top.append(el('span', 'tag', 'Вариант ' + v.n));
  const b = el('button', 'pick', 'беру'); b.onclick = () => { if (picks[it.id] === v.n) delete picks[it.id]; else picks[it.id] = v.n; store(); render(); };
  top.append(b); c.append(top);
  const vw = el('div', 'views'); const z = new Image(); z.src = v.src; z.className = 'zoom'; vw.append(z, slot(v.src)); c.append(vw);
  const d = el('div', 'desc', v.desc); d.style.maxWidth = '180px'; c.append(d);
  return c;
}
function render() {
  anims.length = 0;
  const list = document.getElementById('list'); list.innerHTML = '';
  for (const m of MS) {
    const s = el('section'); s.append(el('h2', '', m.name)); s.firstChild.append(el('span', '', m.kind));
    const row = el('div', 'row'); for (const d of m.designs) row.append(machineCard(m, d)); s.append(row); list.append(s);
  }
  for (const it of ICONS) {
    const s = el('section'); const h = el('h2', '', it.name); h.append(el('span', '', it.id + ' — ' + it.what)); s.append(h);
    const row = el('div', 'row'); for (const v of it.variants) row.append(iconCard(it, v)); s.append(row);
    const cmp = el('div', 'cmp'); cmp.append(el('span', 'lab', 'рядом с игрой '));
    const nb = NEAR_ICONS[it.id]; cmp.append(slot(nb[0].src, '', nb[0].n));
    for (const v of it.variants) cmp.append(slot(v.src, picks[it.id] === v.n ? 'sel' : '', 'вариант ' + v.n));
    for (const n of nb.slice(1)) cmp.append(slot(n.src, '', n.n));
    s.append(cmp); list.append(s);
  }
  document.getElementById('tally').textContent = MS.map((m) => m.name + ': ' + (picks[m.kind] ? m.designs.find((d) => d.id === picks[m.kind]).name : '—')).concat(ICONS.map((it) => it.name + ': ' + (picks[it.id] || '—'))).join(' · ');
}
document.getElementById('copy').onclick = async () => {
  const t = document.getElementById('tally').textContent, b = document.getElementById('copy');
  try { await navigator.clipboard.writeText(t); b.textContent = 'Скопировано'; setTimeout(() => b.textContent = 'Копировать', 1200); } catch (e) {}
};
document.getElementById('reset').onclick = () => { picks = {}; store(); render(); };
render();
(function tick(t) { for (const a of anims) a(t || 0); requestAnimationFrame(tick); })();
</script>
</body></html>`;
}

const ms = paintMachines(), icons = paintIcons();
fs.writeFileSync(path.join(OUT, 'pick.html'), page(ms, icons));
console.log('pick.html;', ms.map((m) => m.designs.length + ' ' + m.kind).join(', ') + ', ' + icons.length + ' icons x3 in', OUT);
