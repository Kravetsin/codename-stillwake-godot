// Writes every wall's candidates into outDir (<material>/<variant>/: the files at the names game/art holds) and
// outDir/pick.html to choose from: each candidate as a swatch, and in a small room inside an iron hull with a scrap
// bulkhead, laid out as the tileset lays it (sheets cut over the world a cell at a time, the 16 px edges halved onto the
// sides open to air, iron's back a 16 px panel per 2x2 cells, a grille in about one of seven).
// node tools/ship-art/walls/build.js [outDir, default walls/out]; pick.js copies the picked ones (picks.json).
const fs = require('fs'), path = require('path');
const { read, encode } = require('../part-icons/png.js');
const { Sheet } = require('./lib.js');
const { MATERIALS, paint } = require('./paint.js');
const ART = path.join(__dirname, '..', '..', '..', 'game', 'art');
const OUT = process.argv[2] || path.join(__dirname, 'out');
const CELL = 8;

const b64 = (buf) => 'data:image/png;base64,' + buf.toString('base64');
const png = (im) => encode(im.w, im.h, im.data);
const write = (file, im) => { fs.mkdirSync(path.dirname(file), { recursive: true }); const buf = png(im); fs.writeFileSync(file, buf); return b64(buf); };
const art = (name) => read(path.join(ART, name + '.png'));

// The room: an iron hull two cells thick, a scrap bulkhead, a lining inside the hull (armour out, seal in).
// I iron, S scrap, M the lining; '.' a room on the lining's back wall, ',' on scrap's; ' ' space.
const MAP = [
  '                                            ',
  '  IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII     ',
  '  IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII    ',
  '  IIMMMMMMMMMMMMMMMMMMMMMMMMMMSSSSSSSSSSII  ',
  '  IIM.............M..........S,,,,,,,,,SII  ',
  '  IIM.............M..........S,,,,,,,,,SII  ',
  '  IIM.............M..........S,,,,,,,,,SII  ',
  '  IIM.............M..........S,,,,,,,,,SII  ',
  '  IIM.............M..........S,,,,,,,,,SII  ',
  '  IIMMMMMMMMM.....M..........S,,,,,,,,,SII  ',
  '  IIM.............M..........S,,,,,,,,,SII  ',
  '  IIM.........................,,,,,,,,,SII  ',
  '  IIM.........................,,,,,,,,,SII  ',
  '  IIM.........................,,,,,,,,,SII  ',
  '  IIM.............MMM........S,,,,,,,,,SII  ',
  '  IIMMMMMMMMMMMMMMMMMMMMMMMMMMSSSSSSSSSSII  ',
  '  IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII   ',
  '   IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII    ',
  '                                            ',
];
const PLACED = [['machine_workbench', 6, 10], ['machine_tank', 23, 10], ['machine_tank', 32, 10]];
const grilleAt = (bx, by) => ((bx * 73856093) ^ (by * 19349663)) >>> 0;

/** The room drawn with `looks`: I, S and M each a candidate's pictures (paint()), '.' on M's back wall, ',' on S's. */
function room(looks) {
  const cw = MAP[0].length, ch = MAP.length, s = new Sheet(cw * CELL, ch * CELL), stars = new Set();
  for (let k = 0; k < 40; k++) stars.add(((k * 53 + (k * k) % 17) % (ch * CELL)) * cw * CELL + (k * 97) % (cw * CELL));
  const at = (x, y) => (MAP[y] && MAP[y][x]) || ' ';
  const wall = (c) => 'IMS'.includes(c);
  const blit = (im, sx, sy, dx, dy, w, h) => {
    for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) {
      const o = ((sy + y) * im.w + sx + x) * 4, a = im.data[o + 3] / 255;
      if (!a) continue;
      const under = s.get(dx + x, dy + y);
      s.set(dx + x, dy + y, [0, 1, 2].map((i) => Math.round(im.data[o + i] * a + under[i] * (1 - a))));
    }
  };
  s.each((x, y) => (stars.has(y * s.w + x) ? [150, 160, 180] : [11, 14, 20]));
  for (let y = 0; y < ch; y++) for (let x = 0; x < cw; x++) {
    const c = at(x, y), px = x * CELL, py = y * CELL;
    if (c === '.' || c === ',') {
      const l = looks[c === '.' ? 'M' : 'S'];
      // A back wall: a sheet cut over the world, or iron's 16 px panels, a grille where a hash of the 2x2 block says.
      const sheet = l.back || (grilleAt(x >> 1, y >> 1) % 7 ? l.tiles.plain : l.tiles.grille);
      blit(sheet, px % sheet.w, py % sheet.h, px, py, CELL, CELL);
    }
    if (!wall(c)) continue;
    const l = looks[c];
    blit(l.wall, px % l.wall.w, py % l.wall.h, px, py, CELL, CELL);
    const open = (wall(at(x, y - 1)) ? 0 : 1) | (wall(at(x + 1, y)) ? 0 : 2) | (wall(at(x, y + 1)) ? 0 : 4) | (wall(at(x - 1, y)) ? 0 : 8);
    if (!open) continue;
    // The 16 px edge halved, nearest: the even pixels of each row and column, as Godot's resize keeps them.
    const e = l.edges[open], half = { w: 8, h: 8, data: Buffer.alloc(256) };
    for (let j = 0; j < 8; j++) for (let i = 0; i < 8; i++) e.data.copy(half.data, (j * 8 + i) * 4, (j * 2 * 16 + i * 2) * 4, (j * 2 * 16 + i * 2) * 4 + 4);
    blit(half, 0, 0, px, py, 8, 8);
  }
  for (const [name, x, y] of PLACED) { const im = art(name); blit(im, 0, 0, x * CELL, (y + 4) * CELL - im.h + CELL, im.w, im.h); }
  return { w: s.w, h: s.h, data: s.rgba() };
}

/** A back wall for the swatch: its sheet, or iron's panels laid 8 by 4, a grille here and there. */
function backSample(p) {
  if (p.back) return p.back;
  const s = new Sheet(128, 64);
  s.each((x, y) => {
    const im = grilleAt(x >> 4, y >> 4) % 7 ? p.tiles.plain : p.tiles.grille, o = ((y % 16) * 16 + (x % 16)) * 4;
    return [im.data[o], im.data[o + 1], im.data[o + 2]];
  });
  return { w: 128, h: 64, data: s.rgba() };
}

function build() {
  const kept = { scrap: paint(MATERIALS[0], MATERIALS[0].variants[0]), iron: paint(MATERIALS[1], MATERIALS[1].variants[0]) };
  return MATERIALS.map((m) => ({
    ...m,
    variants: m.variants.map((v) => {
      const p = paint(m, v), dir = path.join(OUT, m.id, v.id);
      for (const [name, im] of Object.entries(p.files)) write(path.join(dir, name + '.png'), im);
      // Armour stands in its own place in the room and lines it too; a seal lines the room inside today's armour.
      const looks = m.id === 'scrap' ? { I: kept.iron, S: p, M: p } : m.id === 'iron' ? { I: p, S: kept.scrap, M: p } : { I: kept.iron, S: kept.scrap, M: p };
      return {
        id: v.id, name: v.name, what: v.what,
        wall: b64(png(p.wall)), back: b64(png(backSample(p))), edge: b64(png(p.edges[15])),
        room: write(path.join(OUT, 'rooms', `${m.id}-${v.id}.png`), room(looks)),
      };
    }),
  }));
}

function page(ms) {
  const card = (m, v) => `
  <label class="card">
    <input type="radio" name="${m.id}" value="${v.id}">
    <div class="title"><b>${v.name}</b> <code>${v.id}</code></div>
    <div class="what">${v.what}</div>
    <img class="room" src="${v.room}" alt="">
    <div class="sw">
      <figure><img class="z3" src="${v.wall}"><figcaption>стена</figcaption></figure>
      <figure><img class="z3" src="${v.back}"><figcaption>задняя стена</figcaption></figure>
      <figure><img class="z3 e" src="${v.edge}"><figcaption>кромка</figcaption></figure>
    </div>
  </label>`;
  return `<!doctype html>
<html lang="ru"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>Стены корабля</title>
<style>
:root{--bg:#0b0e14;--panel:#111823;--card:#0e141d;--line:#334155;--amber:#fcd34d;--text:#d6dde8;--dim:#7d8aa0}
*{box-sizing:border-box}
body{margin:0;background:var(--bg);color:var(--text);font:14px/1.4 system-ui,"Segoe UI",sans-serif}
header{position:sticky;top:0;z-index:5;background:rgba(11,14,20,.97);border-bottom:1px solid var(--line);padding:12px 16px}
h1{font-size:16px;margin:0 0 4px;font-weight:600}
h2{font-size:15px;margin:24px 16px 4px}
.sub{color:var(--dim);font-size:12px;margin:0 16px 8px}
header .sub{margin:0 0 8px}
.tally code{display:block;background:var(--panel);border:1px solid var(--line);border-radius:4px;padding:6px 8px;font:12px ui-monospace,Consolas,monospace;color:var(--amber);overflow-wrap:anywhere}
.row{display:flex;gap:12px;flex-wrap:wrap;padding:0 16px}
.card{background:var(--card);border:2px solid var(--line);border-radius:6px;padding:10px;cursor:pointer;flex:1 1 360px;max-width:1080px}
.card:has(input:checked){border-color:var(--amber)}
.card input{float:right}
.what{color:var(--dim);font-size:12px;margin:2px 0 8px}
img{image-rendering:pixelated;display:block}
.room{width:100%;height:auto}
.sw{display:flex;gap:12px;flex-wrap:wrap;margin-top:8px}
figure{margin:0}figcaption{color:var(--dim);font-size:11px}
.z3{width:256px;height:auto}.z3.e{width:64px}
</style></head><body>
<header><h1>Стены корабля: броня (лом, железо) и герметик (стекло, слюда, шкура)</h1>
<p class="sub">Броня снаружи держит метеоры и течёт; герметик внутри держит воздух и бьётся. Комната: корпус из железа в две клетки, переборка из лома, изнутри обшито. Кандидат брони стоит на своём месте и в обшивке, герметика — в обшивке. Масштаб 1:1 с игрой, без света.</p>
<div class="tally"><code id="tally">выберите по одному в каждом</code></div></header>
${ms.map((m) => `<h2>${m.name} <code>${m.id}</code></h2><p class="sub">${m.what}</p><div class="row">${m.variants.map((v) => card(m, v)).join('')}</div>`).join('\n')}
<script>
const ids=${JSON.stringify(ms.map((m) => m.id))};
const tally=()=>{const p=Object.fromEntries(ids.map(i=>[i,(document.querySelector('input[name='+i+']:checked')||{}).value||'?']));document.getElementById('tally').textContent=JSON.stringify(p);};
document.addEventListener('change',tally);tally();
</script></body></html>`;
}

const ms = build();
fs.writeFileSync(path.join(OUT, 'pick.html'), page(ms));
console.log('wrote', ms.reduce((a, m) => a + m.variants.length, 0), 'candidates;', path.join(OUT, 'pick.html'));
