// Writes every variant as a PNG (png/<id>-<n>.png; battery: -charged and -empty) and the picking page icons.html.
// node tools/ship-art/part-icons/build.js [outDir, default part-icons/out]; pick.js copies the picked ones.
const fs = require('fs'), path = require('path'), { encode } = require('./png.js');
const { items } = require('./icons.js');
const CUR = path.join(path.join(__dirname, '..', '..', '..', 'game', 'art'), 'items') + '/';
const OUTD = process.argv[2] || path.join(__dirname, 'out');
const OUT = path.join(OUTD, 'png');
fs.mkdirSync(OUT, { recursive: true });
const b64 = buf => 'data:image/png;base64,' + buf.toString('base64');
const save = (name, icon) => { const png = encode(16, 16, icon.rgba()); fs.writeFileSync(path.join(OUT, name + '.png'), png); return b64(png); };

const data = items.map(it => ({
  id: it.id, name: it.name, what: it.what,
  variants: it.variants.map((v, k) => {
    const n = k + 1;
    if (!v.alt) return { n, desc: v.desc, imgs: [{ src: save(`${it.id}-${n}`, v.icon) }] };
    return { n, desc: v.desc, imgs: [{ src: save(`${it.id}-${n}-charged`, v.icon), label: 'заряжена' }, { src: save(`${it.id}-${n}-empty`, v.alt), label: 'пуста' }] };
  }),
}));
const EXIST = [['wire', 'Провод'], ['plate', 'Пластина'], ['gear', 'Шестерня'], ['rotor', 'Ротор'], ['iron', 'Железо'], ['scrap', 'Скрап'], ['circuit', 'Плата'], ['planks', 'Доски'], ['fireAxe', 'Пожарный топор'], ['harpoon', 'Гарпун']]
  .map(([id, name]) => ({ id, name, src: b64(fs.readFileSync(CUR + id + '.png')) }));
// neighbours for each item's comparison strip: existing icons of a similar kind
const NEAR = { tube: ['iron', 'plate', 'rotor'], frame: ['plate', 'gear', 'iron'], coil: ['wire', 'gear', 'circuit'], solarCell: ['circuit', 'plate', 'wire'],
  silicon: ['iron', 'scrap', 'plate'], glass: ['plate', 'iron', 'planks'], asteroidRock: ['scrap', 'iron', 'planks'], battery: ['circuit', 'wire', 'rotor'] };

const html = `<!doctype html>
<html lang="ru"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>Иконки деталей</title>
<style>
:root{--bg:#0b0e14;--panel:#111823;--card:#0e141d;--slot:rgba(30,41,59,.9);--slotLine:#475569;--line:#334155;--amber:#fcd34d;--text:#d6dde8;--dim:#7d8aa0}
*{box-sizing:border-box}
body{margin:0;background:var(--bg);color:var(--text);font:14px/1.4 system-ui,"Segoe UI",sans-serif}
header{position:sticky;top:0;z-index:5;background:rgba(11,14,20,.97);border-bottom:1px solid var(--line);padding:12px 16px}
h1{font-size:16px;margin:0 0 4px;font-weight:600}
.sub{color:var(--dim);font-size:12px;margin:0 0 10px}
.strip{display:flex;flex-direction:column;gap:6px;margin-bottom:8px}
.lab{font-size:11px;color:var(--dim);text-transform:uppercase;letter-spacing:.06em}
.hot{display:flex;gap:4px;flex-wrap:wrap}
.slot{width:36px;height:36px;background:var(--slot);border:1px solid var(--slotLine);border-radius:3px;display:grid;place-items:center;position:relative;flex:none}
.slot img{image-rendering:pixelated;display:block;width:28px;height:28px}
.slot.x2{width:40px;height:40px}.slot.x2 img{width:32px;height:32px}
.slot.x1{width:24px;height:24px}.slot.x1 img{width:16px;height:16px}
.slot.new{border-color:#64748b}
.slot.dim{opacity:.45}
.slot.sel{border-color:var(--amber)}
.tally{display:flex;gap:8px;align-items:center;flex-wrap:wrap}
.tally code{background:var(--panel);border:1px solid var(--line);border-radius:4px;padding:6px 8px;font:12px ui-monospace,Consolas,monospace;color:var(--amber);flex:1 1 320px;max-width:760px;overflow-wrap:anywhere}
button{font:inherit;cursor:pointer}
.btn{background:var(--panel);color:var(--text);border:1px solid var(--line);border-radius:4px;padding:5px 10px}
.btn:hover{border-color:var(--amber)}
main{padding:12px 16px 40px;display:flex;flex-direction:column;gap:12px}
.item{background:var(--panel);border:1px solid #1d2635;border-radius:6px;padding:10px 12px}
.item h2{font-size:14px;margin:0 0 8px;font-weight:600}.item h2 span{color:var(--dim);font-weight:400;font-size:12px;margin-left:6px}
.row{display:flex;gap:10px;overflow-x:auto;padding-bottom:4px}
.card{flex:none;border:1px solid #1d2635;border-radius:6px;padding:8px;display:flex;flex-direction:column;gap:6px;background:var(--card);max-width:100%}
.card.on{border-color:var(--amber);box-shadow:0 0 0 1px var(--amber) inset}
.top{display:flex;align-items:center;justify-content:space-between;gap:8px}
.tag{font-weight:700;font-size:13px}
.pick{background:transparent;border:1px solid var(--line);color:var(--dim);border-radius:4px;padding:2px 8px;font-size:12px}
.card.on .pick{background:var(--amber);border-color:var(--amber);color:#1a1205;font-weight:700}
.states{display:flex;gap:10px}
.state{display:flex;flex-direction:column;gap:4px}
.state .sl{font-size:11px;color:var(--dim)}
.views{display:flex;gap:6px;align-items:flex-end}
.zoom{width:64px;height:64px;image-rendering:pixelated;background:var(--slot);border:1px solid var(--line);border-radius:4px;display:block}
.small{display:flex;flex-direction:column;gap:6px;align-items:center}
.one{width:16px;height:16px;image-rendering:pixelated;display:block}
.onebox{width:36px;height:24px;display:grid;place-items:center;background:var(--slot);border:1px solid var(--line);border-radius:3px}
.desc{font-size:12px;color:var(--dim);line-height:1.3;width:176px}
.cmp{display:flex;gap:4px;align-items:center;margin-top:8px;flex-wrap:wrap}
.cmp .lab{margin-right:6px}
</style></head><body>
<header>
  <h1>Иконки новых деталей и материалов</h1>
  <p class="sub">Слоты 36 px с иконкой 28 px, как в инвентаре игры. Новые иконки: выбранный вариант, или первый, приглушённый, пока ничего не выбрано.</p>
  <div class="strip"><div class="lab">в игре рядом с нынешними</div><div class="hot" id="hot"></div></div>
  <div class="strip"><div class="lab">×1</div><div class="hot" id="hot1"></div></div>
  <div class="tally"><code id="tally"></code><button class="btn" id="copy">Копировать</button><button class="btn" id="reset">Сброс</button></div>
</header>
<main id="list"></main>
<script>
const DATA = ${JSON.stringify(data)};
const EXIST = ${JSON.stringify(EXIST)};
const NEAR = ${JSON.stringify(NEAR)};
const KEY = 'stillwake-part-icon-picks';
let picks = {};
try { picks = JSON.parse(localStorage.getItem(KEY) || '{}') || {}; } catch (e) { picks = {}; }
const store = () => { try { localStorage.setItem(KEY, JSON.stringify(picks)); } catch (e) {} };
const el = (t, cls, text) => { const e = document.createElement(t); if (cls) e.className = cls; if (text != null) e.textContent = text; return e; };
const img = (src, cls) => { const i = new Image(); i.src = src; i.alt = ''; if (cls) i.className = cls; return i; };
const slot = (src, cls, title) => { const s = el('div', 'slot' + (cls ? ' ' + cls : '')); if (title) s.title = title; s.append(img(src)); return s; };
const exist = id => EXIST.find(e => e.id === id);

function card(it, v) {
  const c = el('div', 'card' + (picks[it.id] === v.n ? ' on' : ''));
  const top = el('div', 'top'); top.append(el('span', 'tag', 'Вариант ' + v.n));
  const b = el('button', 'pick', 'беру');
  b.onclick = () => { if (picks[it.id] === v.n) delete picks[it.id]; else picks[it.id] = v.n; store(); render(); };
  top.append(b); c.append(top);
  const states = el('div', 'states');
  for (const im of v.imgs) {
    const st = el('div', 'state');
    if (im.label) st.append(el('span', 'sl', im.label));
    const views = el('div', 'views');
    views.append(img(im.src, 'zoom'));
    const sm = el('div', 'small');
    const one = el('div', 'onebox'); one.title = '×1'; one.append(img(im.src, 'one'));
    sm.append(one, slot(im.src, '', 'слот как в игре'));
    views.append(sm); st.append(views); states.append(st);
  }
  c.append(states);
  const d = el('div', 'desc', v.desc); if (v.imgs.length > 1) d.style.width = '100%';
  c.append(d);
  return c;
}

function render() {
  const list = document.getElementById('list'); list.innerHTML = '';
  for (const it of DATA) {
    const box = el('section', 'item');
    const h = el('h2', '', it.name); h.append(el('span', '', it.id + ' — ' + it.what)); box.append(h);
    const row = el('div', 'row'); for (const v of it.variants) row.append(card(it, v)); box.append(row);
    const cmp = el('div', 'cmp'); cmp.append(el('span', 'lab', 'рядом с игрой'));
    const near = NEAR[it.id].map(exist);
    cmp.append(slot(near[0].src, '', near[0].name));
    for (const v of it.variants) for (const im of v.imgs) cmp.append(slot(im.src, 'new' + (picks[it.id] === v.n ? ' sel' : ''), 'вариант ' + v.n + (im.label ? ', ' + im.label : '')));
    for (const e of near.slice(1)) cmp.append(slot(e.src, '', e.name));
    box.append(cmp); list.append(box);
  }
  for (const [id, cls] of [['hot', ''], ['hot1', 'x1']]) {
    const h = document.getElementById(id); h.innerHTML = '';
    EXIST.forEach((e, k) => { h.append(slot(e.src, cls, e.name)); if (k === 4) for (const it of DATA) {
      const v = it.variants.find(x => x.n === picks[it.id]) || it.variants[0];
      for (const im of (it.id === 'battery' ? v.imgs : v.imgs.slice(0, 1))) h.append(slot(im.src, (cls + ' new' + (picks[it.id] ? '' : ' dim')).trim(), it.name + (picks[it.id] ? ' — ' + v.n : ' — не выбрано')));
    } });
  }
  document.getElementById('tally').textContent = DATA.map(it => it.name + ': ' + (picks[it.id] || '—')).join(' · ');
}
document.getElementById('copy').onclick = async () => {
  const t = document.getElementById('tally').textContent, b = document.getElementById('copy');
  try { await navigator.clipboard.writeText(t); b.textContent = 'Скопировано'; setTimeout(() => b.textContent = 'Копировать', 1200); }
  catch (e) { const r = document.createRange(); r.selectNodeContents(document.getElementById('tally')); const s = getSelection(); s.removeAllRanges(); s.addRange(r); }
};
document.getElementById('reset').onclick = () => { picks = {}; store(); render(); };
render();
</script>
</body></html>`;
fs.writeFileSync(path.join(OUTD, 'icons.html'), html);
console.log('icons.html', html.length, 'bytes;', fs.readdirSync(OUT).length, 'png');
