const fs = require('fs'), path = require('path');
const { draw, recolor, P } = require('./engine2');
// node tools/planet-art/icons/build.js [outDir] [items1.js items2.js ...]: every candidate as <outDir>/<id>/<id>_<key>.png;
// pick.js copies the picked ones into game/art/items.
const GAME_ART = path.join(__dirname, '..', '..', '..', 'game', 'art');
const ART = path.join(GAME_ART, 'items') + '/';
const OUT = process.argv[2] || path.join(__dirname, 'out');
const files = process.argv.slice(3).length ? process.argv.slice(3) : ['items1.js'];
const all = [];
for (const f of files) { delete require.cache[require.resolve('./' + f)]; all.push(...require('./' + f)); }
const manifest = {};
function composite(names){const base=P.decode(GAME_ART+'/'+names[0]+'.png');for(const n of names.slice(1)){const o=P.decode(GAME_ART+'/'+n+'.png');for(let i=0;i<256;i++){const a=o.data[i*4+3]/255;if(!a)continue;for(let k=0;k<3;k++)base.data[i*4+k]=Math.round(o.data[i*4+k]*a+base.data[i*4+k]*(1-a));base.data[i*4+3]=Math.max(base.data[i*4+3],o.data[i*4+3]);}}return base;}
for (const it of all) {
  fs.mkdirSync(path.join(OUT, it.id), { recursive: true });
  manifest[it.id] = {};
  for (const c of it.cands) {
    let img;
    try { img = c.composite ? composite(c.composite) : c.recolor ? recolor(ART + c.recolor.file + '.png', c.recolor.map) : draw(c.rows, c.legend); }
    catch (e) { console.log('ERR', it.id, c.key, e.message); continue; }
    const file = `${it.id}/${it.id}_${c.key}.png`;
    P.encode(img, path.join(OUT, file));
    c.file = file; c.b64 = fs.readFileSync(path.join(OUT, file)).toString('base64');
    manifest[it.id][c.key] = file;
  }
}
module.exports = { all, manifest, OUT };
if (require.main === module) {
  let h = '<body style="background:#0f172a;color:#cbd5e1;font:11px Consolas,monospace;margin:8px">';
  for (const it of all) {
    const cur = fs.existsSync(ART + it.id + '.png') ? fs.readFileSync(ART + it.id + '.png').toString('base64') : null;
    h += `<div style="display:flex;gap:14px;align-items:flex-end;margin-bottom:10px"><div style="width:90px">${it.id}</div>`;
    const cell = (b64, label) => `<div style="text-align:center"><div style="background:#1d2739;border:1px solid #475569;border-radius:3px;width:96px;height:96px;display:flex;align-items:center;justify-content:center"><img src="data:image/png;base64,${b64}" style="width:96px;image-rendering:pixelated"></div><div style="display:flex;gap:6px;justify-content:center;align-items:center;margin-top:3px"><div style="background:#1d2739;border:1px solid #475569;border-radius:3px;width:38px;height:38px;display:flex;align-items:center;justify-content:center"><img src="data:image/png;base64,${b64}" style="width:32px;image-rendering:pixelated"></div><img src="data:image/png;base64,${b64}" style="width:16px;image-rendering:pixelated"></div>${label}</div>`;
    if (cur) h += cell(cur, 'сейчас');
    for (const c of it.cands) if (c.b64) h += cell(c.b64, c.key + ' ' + c.name);
    h += '</div>';
  }
  fs.writeFileSync(path.join(OUT, 'preview.html'), h);
}
