// node tools/pick-kit/build.js <page.js> <out-dir> [--break <name>]
// Writes <out-dir>/index.html, one self-contained page: the kit, the bag's item pictures and the page file. Then runs
// the page's stub check; --break applies one of the page's breaks first, to see its check fail (README.md).
const fs = require('fs'), path = require('path');
const [pageArg, outArg] = process.argv.slice(2).filter(a => !a.startsWith('--'));
const bi = process.argv.indexOf('--break'), brk = bi > 0 ? process.argv[bi + 1] : null;
if (!pageArg || !outArg) { console.log('usage: node tools/pick-kit/build.js <page.js> <out-dir> [--break <name>]'); process.exit(2); }
const page = require(path.resolve(pageArg));
const kit = fs.readFileSync(path.join(__dirname, 'kit.js'), 'utf8');
const items = path.join(__dirname, '..', '..', 'game', 'art', 'hd', 'items');

const itemLine = kit.split('\n').find(l => l.startsWith('const BAG_ITEMS'));
const icons = {};
for (const [, id] of itemLine.matchAll(/'(\w+)'/g)) icons[id] = 'data:image/png;base64,' + fs.readFileSync(path.join(items, id + '.png')).toString('base64');

const span = t => `<span class="seg" id="${t.id}">${t.label ? `<span class="lbl">${t.label}</span>` : ''}</span>`;
const bar = ts => ts.length ? `<div class="bar">\n  ${ts.map(span).join('\n  ')}\n</div>\n` : '';
const own = n => page.toggles.filter(t => (t.bar || 2) === n);
const fn = page.page.toString(), expr = /^(async\s+)?(function\b|\(|[\w$]+\s*=>)/.test(fn) ? fn : 'function ' + fn;
const data = { toggles: page.toggles, pick: page.pick };
const script = `(() => {\n'use strict';\nconst ICON_SRC = ${JSON.stringify(icons)};\nconst PAGE = ${JSON.stringify(data)};\n${kit}\nstartPage((${expr})());\n})();\n`;
const html = `<title>${page.title}</title>
<meta name="claude:viewport-fit-sides" content="cover">
<style>
:root{color-scheme:dark;--bg:#0c0e12;--panel:#151920;--panel2:#1d222b;--text:#e3e0d8;--mute:#9a968c;--line:#2d333d;--accent:#8cc4ff;--pick:#7fe0c4;--stage:#05070b}
*{box-sizing:border-box}
html,body{margin:0;background:var(--bg);color:var(--text);overflow-x:hidden}
body{font:15px/1.45 system-ui,-apple-system,"Segoe UI",Roboto,sans-serif;padding:20px max(16px,env(safe-area-inset-right,0px)) 48px max(16px,env(safe-area-inset-left,0px))}
main{max-width:1320px;margin:0 auto}
h1{font-size:22px;font-weight:650;margin:0 0 6px}
.lead{color:var(--mute);margin:0 0 6px;max-width:88ch}
.lead b{color:var(--text);font-weight:600}
.miss{color:var(--mute);font-size:13px;margin:0 0 10px;max-width:88ch;border-left:2px solid var(--line);padding-left:10px}
.bar{display:flex;flex-wrap:wrap;gap:8px 14px;align-items:center;margin:12px 0 6px}
.bar+.bar{margin-top:0}
.seg{display:flex;flex-wrap:wrap;gap:6px;align-items:center}
.seg .lbl{font-size:13px;color:var(--mute);margin-right:2px}
#status{color:var(--mute);font-size:14px;margin:10px 0 12px;min-height:1.4em}
button{font:inherit;font-size:13px;color:var(--text);background:var(--panel2);border:1px solid var(--line);border-radius:6px;padding:4px 10px;cursor:pointer}
button:hover{border-color:var(--mute)}
button.on{border-color:var(--accent);color:var(--accent)}
.grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(min(100%,560px),1fr));gap:16px}
.cand{background:var(--panel);border:1px solid var(--line);border-radius:10px;padding:12px;display:flex;flex-direction:column;gap:9px;min-width:0}
.cand.wide{grid-column:1/-1}
.cand.chosen{border-color:var(--pick);box-shadow:0 0 0 1px var(--pick) inset}
.head{display:flex;justify-content:space-between;align-items:baseline;gap:8px}
.name{font-weight:650;font-size:17px}
.tag{font-size:12px;color:var(--mute);font-family:ui-monospace,Consolas,monospace}
.idea{margin:0;color:var(--mute);font-size:14px}
.idea.time{font-size:13px;color:#c9b27a}
canvas.view{display:block;width:100%;height:auto;aspect-ratio:1000/540;border-radius:6px;background:var(--stage);touch-action:none}
.cand.wide canvas.view{max-width:1000px}
.acts{display:flex;flex-wrap:wrap;gap:8px;align-items:center}
.pick{font-size:14px;padding:6px 14px}
.cand.chosen .pick{border-color:var(--pick);color:var(--pick)}
.foot{margin-top:22px;display:flex;flex-wrap:wrap;gap:10px;align-items:center}
#fallback{width:100%;max-width:560px;font:13px ui-monospace,Consolas,monospace;background:var(--panel2);color:var(--text);border:1px solid var(--line);border-radius:6px;padding:6px}
</style>
<main>
<h1>${page.heading}</h1>
${page.lead.trim()}
${bar([{ id: 'openMode' }, ...own(1), { id: 'heroMode' }, { id: 'speedMode' }, { id: 'playMode' }])}${bar(own(2))}<p id="status">Загрузка…</p>
<div class="grid" id="cands"></div>
<div class="foot">
  <button id="copy" type="button">Скопировать выбор</button>
  <textarea id="fallback" rows="2" hidden readonly></textarea>
</div>
</main>
<script>
${script}</script>
`;
const out = path.join(path.resolve(outArg), 'index.html');
fs.mkdirSync(path.dirname(out), { recursive: true });
fs.writeFileSync(out, html);
const external = /https?:\/\//.test(html);
console.log(`${out}: ${Buffer.byteLength(html)} bytes${external ? ', AN EXTERNAL URL (the frame blocks other hosts)' : ''}`);

let checked = script;
if (brk) {
  const swaps = (page.breaks || {})[brk];
  if (!swaps) { console.log('no break', brk, 'in', Object.keys(page.breaks || {}).join(', ')); process.exit(2); }
  for (const [a, b] of swaps) { if (!checked.includes(a)) { console.log('break site not found:', a); process.exit(2); } checked = checked.replace(a, b); }
  console.log('BROKEN:', brk);
}
const ok = require('./check.js').run(checked, page) && !external;
console.log(ok ? 'CHECK OK' : 'CHECK FAILED');
process.exitCode = ok ? 0 : 1;
