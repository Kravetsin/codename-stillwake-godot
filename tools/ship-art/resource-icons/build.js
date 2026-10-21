// Writes every variant as a PNG and the picking page resource-icons.html (images inlined as base64).
// node tools/ship-art/resource-icons/build.js [outDir, default resource-icons/out]; pick.js copies the picked ones.
const fs = require('fs'), path = require('path'), { encode, decode } = require('./png.js');
const { items } = require('./icons.js');
const CUR = path.join(path.join(__dirname, '..', '..', '..', 'game', 'art'), 'items') + '/';
const OUTD = process.argv[2] || path.join(__dirname, 'out');
const L = 'ABCD';
fs.mkdirSync(path.join(OUTD, 'png'), { recursive: true });
const b64 = buf => 'data:image/png;base64,' + buf.toString('base64');
const curImg = id => { try { return b64(fs.readFileSync(CUR + id + '.png')); } catch { return null; } };

const data = items.map(it => ({
  id: it.id, name: it.name, what: it.what, current: curImg(it.id),
  variants: it.variants.map((v, k) => {
    const png = encode(16, 16, v.icon.rgba());
    fs.writeFileSync(path.join(OUTD, 'png', `${it.id}-${L[k]}.png`), png);
    return { letter: L[k], desc: v.desc, src: b64(png) };
  }),
}));
// other items already in the game, so the preview shows the new icons beside them
const others = ['canister', 'cargoCrate', 'fireAxe', 'supplyPod', 'rotor'].map(id => ({ id, src: curImg(id) })).filter(o => o.src);

const html = `<!doctype html>
<html lang="ru"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>Иконки ресурсов</title>
<style>
:root{--bg:#0b0e14;--panel:#111823;--slot:rgba(30,41,59,.7);--line:#334155;--amber:#f2b84b;--text:#d6dde8;--dim:#7d8aa0;--cur:#2a1d1d}
*{box-sizing:border-box}
body{margin:0;background:var(--bg);color:var(--text);font:14px/1.4 system-ui,"Segoe UI",sans-serif}
header{position:sticky;top:0;z-index:5;background:rgba(11,14,20,.96);border-bottom:1px solid var(--line);padding:12px 16px}
h1{font-size:16px;margin:0 0 8px;font-weight:600}
.bar{display:flex;flex-wrap:wrap;gap:16px;align-items:flex-start}
.strip{display:flex;flex-direction:column;gap:6px}
.strip .lab{font-size:11px;color:var(--dim);text-transform:uppercase;letter-spacing:.06em}
.hot{display:flex;gap:4px;flex-wrap:wrap}
.slot{width:44px;height:44px;background:var(--slot);border:1px solid var(--line);border-radius:4px;display:grid;place-items:center;position:relative;flex:none}
.slot img{image-rendering:pixelated;display:block}
.slot.x2 img{width:32px;height:32px}.slot.x1 img{width:16px;height:16px}
.slot.sel{border-color:var(--amber);box-shadow:0 0 0 1px var(--amber)}
.slot .n{position:absolute;right:3px;bottom:1px;font-size:10px;color:#cbd5e1;text-shadow:0 1px 0 #000}
.slot.old{opacity:.55}
.tally{display:flex;gap:8px;align-items:center;margin-top:2px}
.tally code{background:var(--panel);border:1px solid var(--line);border-radius:4px;padding:6px 8px;font:12px ui-monospace,Consolas,monospace;color:var(--amber);min-width:280px;max-width:640px;overflow-wrap:anywhere}
button{font:inherit;cursor:pointer}
.copy{background:var(--panel);color:var(--text);border:1px solid var(--line);border-radius:4px;padding:5px 10px}
.copy:hover{border-color:var(--amber)}
main{padding:12px 16px 40px;display:flex;flex-direction:column;gap:12px}
.item{background:var(--panel);border:1px solid #1d2635;border-radius:6px;padding:10px 12px}
.item h2{font-size:14px;margin:0 0 8px;font-weight:600}.item h2 span{color:var(--dim);font-weight:400;font-size:12px;margin-left:6px}
.row{display:flex;gap:10px;overflow-x:auto;padding-bottom:4px}
.card{flex:none;width:176px;border:1px solid #1d2635;border-radius:6px;padding:8px;display:flex;flex-direction:column;gap:6px;background:#0e141d}
.card.cur{background:var(--cur);border-color:#3a2a2a}
.card.on{border-color:var(--amber);box-shadow:0 0 0 1px var(--amber) inset}
.top{display:flex;align-items:center;justify-content:space-between}
.tag{font-weight:700;font-size:13px}.card.cur .tag{color:#d9a0a0;font-weight:600;font-size:12px}
.pick{background:transparent;border:1px solid var(--line);color:var(--dim);border-radius:4px;padding:2px 8px;font-size:12px}
.card.on .pick{background:var(--amber);border-color:var(--amber);color:#1a1205;font-weight:700}
.views{display:flex;gap:6px;align-items:flex-end}
.zoom{width:96px;height:96px;image-rendering:pixelated;background:var(--slot);border:1px solid var(--line);border-radius:4px;padding:0}
.small{display:flex;flex-direction:column;gap:6px}
.desc{font-size:12px;color:var(--dim);line-height:1.3}
</style></head><body>
<header>
  <h1>Иконки стартовых ресурсов — выбери по одной</h1>
  <div class="bar">
    <div class="strip"><div class="lab">хотбар ×2</div><div class="hot" id="hot2"></div></div>
    <div class="strip"><div class="lab">×1</div><div class="hot" id="hot1"></div></div>
  </div>
  <div class="tally"><code id="tally"></code><button class="copy" id="copy">Копировать</button><button class="copy" id="reset">Сброс</button></div>
</header>
<main id="list"></main>
<script>
const DATA = ${JSON.stringify(data)};
const OTHERS = ${JSON.stringify(others)};
let picks = {};
try { picks = JSON.parse(localStorage.getItem('stillwake-icon-picks') || '{}') || {}; } catch (e) { picks = {}; }
const save = () => { try { localStorage.setItem('stillwake-icon-picks', JSON.stringify(picks)); } catch (e) {} };
const el = (t, cls, html) => { const e = document.createElement(t); if (cls) e.className = cls; if (html != null) e.innerHTML = html; return e; };
const img = (src, w) => { const i = new Image(); i.src = src; i.width = w; i.height = w; i.alt = ''; return i; };

function card(it, v, isCur) {
  const c = el('div', 'card' + (isCur ? ' cur' : ''));
  const top = el('div', 'top'); top.append(el('span', 'tag', isCur ? 'сейчас' : v.letter));
  if (!isCur) { const b = el('button', 'pick', 'беру'); b.onclick = () => { picks[it.id] = picks[it.id] === v.letter ? undefined : v.letter; if (!picks[it.id]) delete picks[it.id]; save(); render(); }; top.append(b); }
  c.append(top);
  const views = el('div', 'views');
  const z = img(isCur ? it.current : v.src, 96); z.className = 'zoom'; views.append(z);
  const sm = el('div', 'small');
  const s2 = el('div', 'slot x2'); s2.append(img(isCur ? it.current : v.src, 32));
  const s1 = el('div', 'slot x1'); s1.append(img(isCur ? it.current : v.src, 16));
  sm.append(s2, s1); views.append(sm); c.append(views);
  c.append(el('div', 'desc', isCur ? 'текущая иконка в игре' : v.desc));
  if (!isCur && picks[it.id] === v.letter) c.classList.add('on');
  return c;
}

function render() {
  const list = document.getElementById('list'); list.innerHTML = '';
  for (const it of DATA) {
    const box = el('section', 'item'); box.append(el('h2', '', it.name + '<span>' + it.id + ' — ' + it.what + '</span>'));
    const row = el('div', 'row');
    if (it.current) row.append(card(it, null, true));
    for (const v of it.variants) row.append(card(it, v, false));
    box.append(row); list.append(box);
  }
  for (const [id, scale] of [['hot2', 2], ['hot1', 1]]) {
    const h = document.getElementById(id); h.innerHTML = '';
    DATA.forEach((it, k) => {
      const v = it.variants.find(x => x.letter === picks[it.id]);
      const s = el('div', 'slot x' + scale + (v ? '' : ' old') + (k === 0 && scale === 2 ? ' sel' : ''));
      s.title = it.name + (v ? ' — ' + v.letter : ' — сейчас');
      s.append(img(v ? v.src : it.current, 16 * scale));
      if (scale === 2) s.append(el('span', 'n', String([500, 12, 3, 60, 24, 40, 30, 8][k] || '')));
      h.append(s);
    });
    for (const o of OTHERS) { const s = el('div', 'slot x' + scale); s.title = o.id + ' (как есть)'; s.append(img(o.src, 16 * scale)); h.append(s); }
  }
  document.getElementById('tally').textContent = DATA.map(it => it.id + ': ' + (picks[it.id] || '—')).join(' · ');
}
document.getElementById('copy').onclick = async () => {
  const t = document.getElementById('tally').textContent;
  try { await navigator.clipboard.writeText(t); document.getElementById('copy').textContent = 'Скопировано'; setTimeout(() => document.getElementById('copy').textContent = 'Копировать', 1200); }
  catch (e) { const r = document.createRange(); r.selectNodeContents(document.getElementById('tally')); const s = getSelection(); s.removeAllRanges(); s.addRange(r); }
};
document.getElementById('reset').onclick = () => { picks = {}; save(); render(); };
render();
</script>
</body></html>`;
fs.writeFileSync(path.join(OUTD, 'resource-icons.html'), html);
console.log('html', html.length, 'bytes;', data.map(d => d.id + ':' + d.variants.length).join(' '));
