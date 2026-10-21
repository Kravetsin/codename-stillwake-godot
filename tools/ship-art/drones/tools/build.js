// node build.js — writes out/drone-<X>/ and out/bay-<X>/ with the game's file names, and ../concepts.html with every
// picture inline and the scene's runtime (page.js). Nothing here touches the game's repository; it is only read.
const fs = require('fs'), path = require('path');
const { Pic, img, blit, loadPng } = require('./lib.js');
const { encode } = require('./png.js');
const D = require('./drones.js'), B = require('./bays.js');

const ART = require('path').join(__dirname, '..', '..', '..', '..', 'game', 'art');
const ROOT = path.join(__dirname, '..'), OUT = process.argv[2] || path.join(ROOT, 'out');
const L = n => loadPng(path.join(ART, n + '.png'));
const DIDS = Object.keys(D.LOOKS), BIDS = Object.keys(B.LOOKS);

// ---- overlays from a drone's own pixels: its eye, drawn again unlit ----
function overlayOf(pic, at, w, h) { const p = new Pic(w, h); for (const [x, y] of at) p.px(x, y, pic.get(x, y)); return p; }

// ---- the files ----
function fresh(d) { fs.mkdirSync(d, { recursive: true }); for (const f of fs.readdirSync(d)) { const q = path.join(d, f); if (fs.statSync(q).isDirectory()) fs.rmSync(q, { recursive: true }); else fs.unlinkSync(q); } }
const drones = {}, bays = {};
for (const id of DIDS) {
  const r = drones[id] = D.build(id), d = path.join(OUT, 'drone-' + id);
  r.eyePic = overlayOf(r.fly[0], r.eye, D.W, D.H);
  r.dockEyePic = overlayOf(r.dock, r.dockEye, D.DW, D.DH);
  fresh(d); fs.mkdirSync(path.join(d, 'items'));
  r.fly.forEach((f, i) => f.save(path.join(d, `drone_fly_${i}.png`)));
  r.dock.save(path.join(d, 'drone_dock.png'));
  r.eyePic.save(path.join(d, 'drone_eye.png'));
  r.dockEyePic.save(path.join(d, 'drone_dock_eye.png'));
  r.work.save(path.join(d, 'drone_lamp_work.png'));
  r.low.save(path.join(d, 'drone_lamp_low.png'));
  r.icon.save(path.join(d, 'items', 'drone.png'));
  r.empty.save(path.join(d, 'items', 'drone_empty.png'));
  fs.writeFileSync(path.join(d, 'drone.json'), JSON.stringify({
    fly: { size: [D.W, D.H], anchor: D.ANCHOR, clampBelowAnchor: 5, cargo: { tether: 1, jawBar: 1, icon: 8 }, lamp: r.lamp, frames: 4 },
    dock: { size: [D.DW, D.DH], bodyCentre: [7, 6], feetRow: 10 },
    light: { work: { colour: '#7dd3fc', strength: 0.22, at: 'lamp' }, low: { colour: '#f2b84b', strength: 0.4, at: 'lamp', blinkHz: 1.5 } },
  }, null, 2));
}
const charge = B.charges(), padGlow = B.padGlow();
for (const id of BIDS) {
  const r = bays[id] = B.build(id), d = path.join(OUT, 'bay-' + id);
  fresh(d);
  r.base.save(path.join(d, 'machine_droneBay.png'));
  padGlow.save(path.join(d, 'droneBay_pad.png'));
  charge.levels.forEach((p, i) => p.save(path.join(d, `droneBay_charge_${i}.png`)));
  charge.low.save(path.join(d, 'droneBay_charge_low.png'));
  fs.writeFileSync(path.join(d, 'droneBay.json'), JSON.stringify({
    size: [32, 32], slots: 'top left, top right, bottom left, bottom right (the sim\'s slot order)',
    dock: B.PADS, padGlow: B.GLOWS, chargeLamps: B.LAMPS, beacon: r.beacon,
    light: { charging: { colour: '#7ee36b', strength: 0.22, pulseHz: 0.8 }, low: { colour: '#f2b84b', strength: 0.22 } },
  }, null, 2));
}

// ---- pictures as data ----
const uri = im => 'data:image/png;base64,' + Buffer.from(encode(im.w, im.h, (im.rgba ? im.rgba() : im).data)).toString('base64');
const esc = s => String(s).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
const IMGS = {};
const put = (name, im) => { IMGS[name] = uri(im); return name; };
put('back_scrap', L('back_scrap')); put('wall_iron', L('wall_iron')); put('ladder', L('ladder')); put('lamp', L('machine_lamp'));
for (let m = 1; m < 16; m++) put('edge_' + m, L('wall-edge_iron_' + m));
for (const m of ['furnace', 'smelter', 'crate', 'former', 'assembler', 'generator']) put(m, L('machine_' + m));
put('bay_cur', L('machine_droneBay'));
for (const i of ['scrapChunk', 'rock', 'planks']) put('icon_' + i, L('items/' + i));
const DLOOK = {};
for (const id of DIDS) {
  const r = drones[id];
  DLOOK[id] = {
    fly: r.fly.map((f, i) => put(`d${id}_fly${i}`, f)), dock: put(`d${id}_dock`, r.dock), eye: put(`d${id}_eye`, r.eyePic),
    dockEye: put(`d${id}_dockEye`, r.dockEyePic), lampWork: put(`d${id}_work`, r.work), lampLow: put(`d${id}_low`, r.low),
    anchor: D.ANCHOR, clamp: 5,
  };
}
const BLOOK = {};
for (const id of BIDS) BLOOK[id] = { base: put(`b${id}`, bays[id].base), beacon: bays[id].beacon };
put('pad_glow', padGlow); charge.levels.forEach((p, i) => put('charge_' + i, p)); put('charge_low', charge.low);

// ---- the room: two decks, a hatch with a ladder between them ----
const MAP = [
  '########################',
  '#......................#',
  '#......................#',
  '#......................#',
  '###########H############',
  '#..........H...........#',
  '#..........H...........#',
  '#..........H...........#',
  '#..........H...........#',
  '########################',
];
const MACHINES = [
  { art: 'bay', at: [2, 7] }, { art: 'furnace', at: [6, 7] }, { art: 'crate', at: [14, 8] }, { art: 'smelter', at: [18, 7] },
  { art: 'crate', at: [3, 3] }, { art: 'former', at: [15, 2] }, { art: 'assembler', at: [19, 2] }, { art: 'generator', at: [7, 2] },
  { art: 'lamp', at: [9, 5] }, { art: 'lamp', at: [16, 5] }, { art: 'lamp', at: [11, 1] },
];
const LIGHTS = [
  { at: [9 * 16 + 8, 5 * 16 + 10], r: 72, rgb: '255,228,186', a: 0.3 }, { at: [16 * 16 + 8, 5 * 16 + 10], r: 72, rgb: '255,228,186', a: 0.3 },
  { at: [11 * 16 + 8, 1 * 16 + 10], r: 80, rgb: '255,228,186', a: 0.3 }, { at: [6 * 16 + 18, 7 * 16 + 22], r: 34, rgb: '255,140,60', a: 0.3 },
];
// the sim's jobs: corner cells of each path, waits at a machine (holding what it holds there), a rest docked charging
const JOBS = [
  { slot: 0, offset: 0, steps: [
    { path: [[2, 7], [2, 8], [14, 8]] }, { wait: 0.7, at: [14, 8] },
    { path: [[14, 8], [7, 8]], cargo: 'scrapChunk' }, { wait: 0.7, at: [7, 8], cargo: 'scrapChunk' },
    { path: [[7, 8], [2, 8], [2, 7]] }, { rest: 3.2, from: 2.2, to: 4 },
  ] },
  { slot: 2, offset: 3.1, steps: [
    { path: [[3, 8], [11, 8], [11, 3], [4, 3]] }, { wait: 0.7, at: [4, 3] },
    { path: [[4, 3], [11, 3], [11, 8], [18, 8]], cargo: 'rock' }, { wait: 0.7, at: [18, 8], cargo: 'rock' },
    { path: [[18, 8], [3, 8]], low: true }, { rest: 4.2, from: 0.6, to: 4 },
  ] },
];
const DATA = {
  img: IMGS, drones: DLOOK, bays: BLOOK, map: MAP, machines: MACHINES, lights: LIGHTS, jobs: JOBS, bayAt: [2, 7],
  pads: B.PADS, glows: B.GLOWS, lamps: B.LAMPS, dockCentre: [7, 6], curPads: [[5, 15], [19, 15], [5, 27], [19, 27]],
  parked: [{ slot: 1, charge: 4 }],
};

// ---- still pictures for the variant rows ----
const pic = (src, w, h, k, alt) => `<img src="${src}" width="${w * k}" height="${h * k}" alt="${esc(alt)}">`;
const fig = (src, w, h, k, alt, cap) => `<figure>${pic(src, w, h, k, alt)}${cap ? `<figcaption>${esc(cap)}</figcaption>` : ''}</figure>`;
const strip = (items, cls = '') => `<div class="strip ${cls}">${items.join('')}</div>`;
// the current drone as LogisticsView draws it, one frame, in an 18x12 box like the new ones
function currentDrone(cargo) {
  const im = img(18, cargo ? 22 : 12), c = [9, 6];
  const r = (x, y, w, h, col) => { const rgb = [1, 3, 5].map(i => parseInt(col.slice(i, i + 2), 16)); for (let yy = 0; yy < h; yy++) for (let xx = 0; xx < w; xx++) { const X = c[0] + x + xx, Y = c[1] + y + yy; if (X >= 0 && Y >= 0 && X < im.w && Y < im.h) im.data.set([...rgb, 255], (Y * im.w + X) * 4); } };
  r(-5, -3, 10, 5, '#1a1e25'); r(-4, -2, 8, 3, '#cbd5e1'); r(-4, 0, 8, 1, '#8fa0b2'); r(1, -1, 2, 1, '#60a5fa');
  for (const s of [-1, 1]) { r(s * 5, -4, 1, 2, '#8fa0b2'); r(s * 5 - 3, -5, 7, 1, '#e2e8f0'); }
  if (cargo) { const ic = L('items/' + cargo); for (let y = 0; y < 8; y++) for (let x = 0; x < 8; x++) { const s = ((y * 2) * 16 + x * 2) * 4; if (ic.data[s + 3] > 127) im.data.set(ic.data.subarray(s, s + 4), ((c[1] + 3 + y) * im.w + c[0] - 4 + x) * 4); } }
  return im;
}
// a proposed drone with its stack on the tether, still, for the x1/x4 rows
function withCargo(id, cargo) {
  const r = drones[id], im = img(18, 22); blit(im, r.fly[0], 0, 0); blit(im, r.eyePic, 0, 0); blit(im, r.work, 0, 0);
  const ic = L('items/' + cargo), cx = 9, top = D.ANCHOR[1] + 5 + 2;
  im.data.set([0x7f, 0x84, 0x8f, 255], ((top - 2) * im.w + cx - 1) * 4);

  for (let y = 0; y < 8; y++) for (let x = 0; x < 8; x++) { const s = ((y * 2) * 16 + x * 2) * 4; if (ic.data[s + 3] > 127) im.data.set(ic.data.subarray(s, s + 4), ((top + y) * im.w + cx - 4 + x) * 4); }
  for (let x = cx - 3; x < cx + 3; x++) im.data.set([0x8f, 0x99, 0xad, 255], ((top - 1) * im.w + x) * 4);
  im.data.set([0x47, 0x4c, 0x57, 255], (top * im.w + cx - 3) * 4); im.data.set([0x47, 0x4c, 0x57, 255], (top * im.w + cx + 2) * 4);
  return im;
}
// a bay with drones docked: which slots hold one, at what charge (lamps 0..4, -1 low)
function bayWith(id, slots, droneId) {
  const base = id === 'cur' ? L('machine_droneBay') : bays[id].base, im = img(32, 32); blit(im, base, 0, 0);
  if (id === 'cur') {
    slots.forEach((s, i) => { if (s === null) return; const [x, y] = DATA.curPads[i]; for (let yy = 0; yy < 2; yy++) for (let xx = 0; xx < 8; xx++) im.data.set([0x7d, 0xd3, 0xfc, 255], ((y + yy) * 32 + x + xx) * 4); });
    return im;
  }
  const r = drones[droneId];
  slots.forEach((s, i) => {
    if (s === null) return;
    const [px, py] = B.PADS[i], [gx, gy] = B.GLOWS[i], [lx, ly] = B.LAMPS[i];
    blit(im, r.dock, px, py); blit(im, r.dockEyePic, px, py);
    if (s < 4) blit(im, padGlow, gx, gy);
    blit(im, s < 0 ? charge.low : charge.levels[s], lx, ly);
  });
  return im;
}

const INFO_D = {
  A: { short: 'стальной квадрик', about: 'Ближе всех к нынешнему, но объёмный: стальная капсула с бликом сверху и тенью снизу, шов с заклёпками, тёмный визор с голубым зрачком спереди. Два мотора-стакана на коротких штангах, над ними лопасти. В доке штанги складываются вниз и мотор становится ногой, лопасти лежат на спине — так два дрона помещаются на полке.' },
  B: { short: 'шмель', about: 'Круглый корпус в оранжевом цвете экипажа (как скафандр героя), тёмный пояс, большой глаз. Вместо открытых лопастей — два кольцевых вентилятора по бокам: внутри мелькают лопатки, над ними дрожит воздух. Сразу читается как «наш», не теряется на тёмной стене. В доке кольца поворачиваются вверх, как сложенные крылья.' },
  C: { short: 'грузчик', about: 'Коробчатый корпус в предупреждающем жёлтом, полосатое брюхо, стальная лыжа, окно спереди. Один широкий винт на мачте, как у вертолёта: самый заметный «пропеллер» из трёх. Хвостовая балка с лампой на конце. В доке хвост складывается вверх, лопасть ложится вдоль крыши, внизу лыжа.' },
};
const INFO_B = {
  A: { short: 'стеллаж', about: 'Стальной стеллаж: три стойки, две полки, сзади тёмная решётка. Сверху балка с жёлто-чёрной полосой. На каждой полке два пятачка с медными контактами; на свободном месте видна вилка зарядки на задней стенке — место ждёт дрона. На кромке полки под каждым местом четыре лампочки заряда.' },
  B: { short: 'ангар', about: 'Стальной шкаф с оранжевой окантовкой цвета экипажа и четырьмя глубокими нишами. Над каждой нишей поднятая гофрированная шторка, левая стенка ниши освещена, правая в тени, потолок тёмный — видна глубина. Дно ниши — пятачок, лампочки заряда на кромке.' },
  C: { short: 'насест', about: 'Мачта с кабелем, по бокам две консоли на подкосах, внизу широкое основание. Дроны садятся на концы консолей и на основание, как птицы на жёрдочку. Самый открытый силуэт: сквозь него видна стена. На верхушке мачты голубой маячок, горит, пока станция под питанием.' },
};

function droneSection(id) {
  const r = drones[id], I = INFO_D[id];
  const cmp = [
    fig(uri(currentDrone('scrapChunk')), 18, 22, 4, 'сейчас', 'сейчас'),
    fig(uri(withCargo(id, 'scrapChunk')), 18, 22, 4, D.LOOKS[id].name, I.short),
  ];
  const cmp1 = [pic(uri(currentDrone('scrapChunk')), 18, 22, 1, 'сейчас'), pic(uri(withCargo(id, 'scrapChunk')), 18, 22, 1, I.short)];
  const frames = [...r.fly.map((f, i) => fig(uri(f), D.W, D.H, 4, 'кадр ' + i, 'drone_fly_' + i)), fig(uri(r.dock), D.DW, D.DH, 4, 'док', 'drone_dock')];
  const frames1 = [...r.fly.map((f, i) => pic(uri(f), D.W, D.H, 1, 'кадр ' + i)), pic(uri(r.dock), D.DW, D.DH, 1, 'док')];
  const lit = (over) => { const im = img(D.W, D.H); blit(im, r.fly[0], 0, 0); blit(im, r.eyePic, 0, 0); if (over) blit(im, over, 0, 0); return im; };
  const lights = [fig(uri(lit(r.work)), D.W, D.H, 4, 'работает', 'работает'), fig(uri(lit(r.low)), D.W, D.H, 4, 'мало заряда', 'мало заряда'),
    fig(uri(r.icon), 16, 16, 4, 'иконка', 'items/drone'), fig(uri(r.empty), 16, 16, 4, 'иконка пустого', 'items/drone_empty')];
  const files = [fig(uri(r.eyePic), D.W, D.H, 3, 'глаз', 'drone_eye'), fig(uri(r.work), D.W, D.H, 3, 'лампа', 'drone_lamp_work'), fig(uri(r.low), D.W, D.H, 3, 'лампа низкий', 'drone_lamp_low'), fig(uri(r.dockEyePic), D.DW, D.DH, 3, 'глаз в доке', 'drone_dock_eye')];
  const lampAt = r.lamp.map(p => `(${p[0]}, ${p[1]})`).join(', ');
  return `<section class="kind" id="d${id}">
  <div class="khead"><h3>Дрон ${id} — ${esc(I.short)}</h3><span class="dim">полёт 18×12 · 4 кадра винтов · док 14×12 · out/drone-${id}/</span></div>
  <p class="about">${esc(I.about)}</p>
  <div class="crow"><div class="rl"><b>Рядом с нынешним</b><span>оба с грузом</span></div>
    <div class="stage x1"><span class="tag">×1</span>${strip(cmp1)}</div>
    <div class="stage"><span class="tag">×4 · груз на подвесе, ниже лампа и глаз горят</span>${strip(cmp, 'cap')}</div></div>
  <div class="crow"><div class="rl"><b>Кадры</b><span>винты и док</span></div>
    <div class="stage x1"><span class="tag">×1</span>${strip(frames1)}</div>
    <div class="stage"><span class="tag">×4 · 4 кадра винтов, затем поза в доке</span>${strip(frames, 'cap')}</div></div>
  <div class="crow"><div class="rl"><b>Свет и иконки</b><span>лампа, глаз, предмет</span></div>
    <div class="stage"><span class="tag">×4</span>${strip(lights, 'cap')}</div>
    <div class="stage chk"><span class="tag">×3 · накладки без освещения</span>${strip(files, 'cap')}</div></div>
  <div class="crow"><div class="rl"><b>В движении</b><span>×4, по ползункам</span></div>
    <div class="stage mini-stage"><span class="tag">×4 · стоит, летит 3 клетки, тормозит, обратно; красная линия — центр клетки, по которому летит сим</span><canvas class="mini" data-look="${id}" width="384" height="160"></canvas></div></div>
  <p class="about light"><b>Свет.</b> Лампа статуса в ${lampAt} (пиксели картинки полёта): голубая <code>#7dd3fc</code>, пока дрон летит по делу, Glow ×0.22, радиус около клетки; янтарная <code>#f2b84b</code>, мигает 1.5 Гц, когда он летит домой с низким зарядом, Glow ×0.4. Глаз рисуется ещё раз без освещения (<code>drone_eye</code>), чтобы читался в тёмной комнате, но своего света не даёт. Без заряда — ни лампы, ни глаза (как <code>drone_empty</code>).</p>
</section>`;
}

function baySection(id) {
  const I = INFO_B[id], d0 = 'A';
  const states = [[[null, null, null, null], 'пусто'], [[4, null, -1, null], 'два дрона: полный и разряженный'], [[4, 2, 3, 4], 'все четыре заряжаются']];
  const cur = [[null, null, null, null], [0, null, 0, null], [0, 0, 0, 0]];
  const row = (k, list) => list.map(([s, cap], i) => fig(uri(bayWith(id, s, d0)), 32, 32, k, cap, k > 1 ? cap : '')).join('');
  const cmp = [fig(uri(bayWith('cur', cur[1], d0)), 32, 32, 4, 'сейчас', 'сейчас'), fig(uri(bayWith(id, states[1][0], d0)), 32, 32, 4, I.short, I.short)];
  const others = DIDS.filter(x => x !== d0).map(did => fig(uri(bayWith(id, [4, 3, -1, null], did)), 32, 32, 4, 'с дроном ' + did, 'с дроном ' + did));
  return `<section class="kind" id="b${id}">
  <div class="khead"><h3>Станция ${id} — ${esc(I.short)}</h3><span class="dim">32×32 (2×2 клетки) · 4 места · out/bay-${id}/</span></div>
  <p class="about">${esc(I.about)}</p>
  <div class="crow"><div class="rl"><b>Рядом с нынешней</b><span>по два дрона</span></div>
    <div class="stage x1"><span class="tag">×1</span>${strip([pic(uri(bayWith('cur', cur[1], d0)), 32, 32, 1, 'сейчас'), pic(uri(bayWith(id, states[1][0], d0)), 32, 32, 1, I.short)])}</div>
    <div class="stage"><span class="tag">×4</span>${strip(cmp, 'cap')}</div></div>
  <div class="crow"><div class="rl"><b>Заполнение</b><span>дрон A</span></div>
    <div class="stage x1"><span class="tag">×1</span>${strip(states.map(([s, cap]) => pic(uri(bayWith(id, s, d0)), 32, 32, 1, cap)))}</div>
    <div class="stage"><span class="tag">×4 · зелёные лампочки — заряд по четвертям, янтарная — меньше четверти; пятачок под заряжающимся дроном светится</span>${strip([row(4, states)], 'cap')}</div></div>
  <div class="crow"><div class="rl"><b>С другими дронами</b><span>B и C</span></div>
    <div class="stage"><span class="tag">×4 · места одни и те же для всех дронов</span>${strip(others, 'cap')}</div></div>
</section>`;
}

const fileTable = `<div class="files-wrap"><table class="files"><thead><tr><th>Файл</th><th>Размер</th><th>Что это</th></tr></thead><tbody>
<tr><td><code>drone_fly_0..3.png</code></td><td>18×12</td><td>дрон в полёте, 4 кадра винтов; центр корпуса (9, 6) — точка, которую двигает клиент; зажим груза на 5 px ниже</td></tr>
<tr><td><code>drone_dock.png</code></td><td>14×12</td><td>дрон в доке, сложенный; ставится левым верхним углом в точку места</td></tr>
<tr><td><code>drone_eye.png</code>, <code>drone_dock_eye.png</code></td><td>18×12, 14×12</td><td>глаз, поверх без освещения</td></tr>
<tr><td><code>drone_lamp_work.png</code>, <code>drone_lamp_low.png</code></td><td>18×12</td><td>лампа статуса, голубая и янтарная, поверх без освещения, поворачивается с корпусом</td></tr>
<tr><td><code>items/drone.png</code>, <code>items/drone_empty.png</code></td><td>16×16</td><td>иконка предмета (сложенный дрон) и разряженного, с тёмным глазом</td></tr>
<tr><td><code>machine_droneBay.png</code></td><td>32×32</td><td>станция с пустыми местами</td></tr>
<tr><td><code>droneBay_pad.png</code></td><td>8×1</td><td>светящийся пятачок под заряжающимся дроном, без освещения</td></tr>
<tr><td><code>droneBay_charge_0..4.png</code>, <code>droneBay_charge_low.png</code></td><td>11×1</td><td>4 лампочки заряда на кромке полки: сколько горит зелёным; low — одна янтарная</td></tr>
<tr><td><code>drone.json</code>, <code>droneBay.json</code></td><td></td><td>точки: якорь, зажим, лампа; места, пятачки, лампочки — одни для всех трёх станций</td></tr>
</tbody></table></div>`;

const radios = (group, name, opts) => `<fieldset class="pickrow" data-group="${esc(group)}"><legend>${esc(group)}:</legend>${opts.map(([v, label, say], i) => `<label><input type="radio" name="${name}" value="${esc(v)}"${say ? ` data-say="${esc(say)}"` : ''}${i === 0 ? ' checked' : ''}> ${esc(label)}</label>`).join('')}</fieldset>`;
const picks = radios('Дрон', 'pick-drone', [['', 'не решил'], ['A', 'A — квадрик'], ['B', 'B — шмель'], ['C', 'C — грузчик'], ['now', 'как сейчас', 'как сейчас']]) +
  radios('Станция', 'pick-bay', [['', 'не решил'], ['A', 'A — стеллаж'], ['B', 'B — ангар'], ['C', 'C — насест'], ['now', 'как сейчас', 'как сейчас']]) +
  radios('Движение', 'pick-motion', [['', 'не решил'], ['mock', 'как в макете', 'как в макете'], ['calm', 'спокойнее', 'спокойнее'], ['lively', 'живее', 'живее'], ['sliders', 'по ползункам'], ['none', 'без изменений', 'без изменений']]);

const slider = (key, label, min, max, step, unit) => `<label class="sl"><span>${esc(label)}</span><input type="range" data-p="${key}" min="${min}" max="${max}" step="${step}"><output data-out="${key}"></output><i>${esc(unit)}</i></label>`;
const sceneRadio = (name, opts) => opts.map(([v, label], i) => `<label><input type="radio" name="${name}" value="${v}"${i === 0 ? ' checked' : ''}> ${esc(label)}</label>`).join('');

const css = fs.readFileSync(path.join(__dirname, 'page.css'), 'utf8');
const runtime = fs.readFileSync(path.join(__dirname, 'page.js'), 'utf8');
const SW = MAP[0].length * 16 * 2, SH = MAP.length * 16 * 2;

const html = `<!doctype html>
<html lang="ru">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>Drone Look Concepts</title>
<meta name="description" content="Stillwake: дроны и станция дронов — три вида дрона, три вида станции, груз на подвесе, подъём над полом и модель движения с покачиванием и наклоном.">
<style>
${css}
.scenebox { max-width: ${SW + 2}px; }
</style>
</head>
<body>
<div class="wrap">
<header>
  <div class="eyebrow">Stillwake · дроны и станция · концепт-арт и движение</div>
  <h1>Дроны и станция дронов</h1>
  <p>Что не так сейчас: дрон плоский (пять прямоугольников кодом), станция — четыре тёмные клетки, дроны летят ровно, как по рельсам, а груз висит так низко, что залезает в пол и в машины. Здесь три вида дрона и три вида станции в пиксель-арте игры (контур <code>#1a1e25</code>, сталь печи и плавильни, свет сверху слева), груз на коротком подвесе, полёт на полклетки (8 px) выше центра клетки и модель движения: покачивание, наклон в разгон и торможение, мягкие повороты, небольшой перелёт на остановке, посадка на пятачок.</p>
  <p>Всё движение — косметика клиента поверх сглаженной позиции из сима (<code>Served.Smoothed</code>). Сим остаётся целочисленным и поклеточным, ничего в нём не меняется.</p>
</header>
<nav class="bar"><a href="#scene">Сцена</a><a href="#drones">Дроны</a><a href="#bays">Станция</a><a href="#motion">Движение</a><a href="#code">Что меняется в коде</a><a href="#answer">Ответ</a></nav>

<h2 class="group" id="scene">Сцена · ×2</h2>
<section class="block">
  <p class="lead">Комната на две палубы из плиток игры, между ними люк с лестницей. Два дрона работают по-настоящему поклеточно: первый носит лом из ящика в печь, второй поднимается через люк к ящику наверху, несёт камень в плавильню и возвращается разряженным (янтарная лампа). Третий стоит в доке полный, четвёртое место пустое. Переключите «как сейчас» и «предложение», включите сетку — красная точка показывает, где дрон по симу.</p>
  <div class="controls">
    <fieldset class="seg"><legend>Вид</legend>${sceneRadio('view', [['new', 'предложение'], ['now', 'как сейчас']])}</fieldset>
    <fieldset class="seg only-new"><legend>Дрон</legend>${sceneRadio('scene-drone', [['A', 'A'], ['B', 'B'], ['C', 'C']])}</fieldset>
    <fieldset class="seg only-new"><legend>Станция</legend>${sceneRadio('scene-bay', [['A', 'A'], ['B', 'B'], ['C', 'C']])}</fieldset>
    <fieldset class="seg"><legend>Скорость</legend>${sceneRadio('slow', [['1', '×1'], ['0.5', '×½'], ['0.25', '×¼']])}</fieldset>
    <label class="chk1"><input type="checkbox" id="grid"> сетка и позиция сима</label>
  </div>
  <div class="scenebox"><canvas id="room" aria-label="Анимированная сцена: дроны летают по комнате и садятся в станцию"></canvas></div>
  <div class="sliders only-new">
    ${slider('lift', 'Подъём над центром клетки', 0, 10, 0.5, 'px')}
    ${slider('bobAmp', 'Покачивание, амплитуда', 0, 3, 0.1, 'px')}
    ${slider('bobPeriod', 'Покачивание, период', 0.6, 3, 0.1, 'с')}
    ${slider('tiltMax', 'Наклон, максимум', 0, 25, 1, '°')}
    ${slider('tiltCruise', 'Наклон в ровном полёте', 0, 15, 0.5, '°')}
    ${slider('springHz', 'Пружина, частота', 1, 6, 0.1, 'Гц')}
    ${slider('damping', 'Пружина, затухание', 0.3, 1.2, 0.05, '')}
    ${slider('swing', 'Раскачка груза', 0, 40, 1, '°')}
    <button type="button" id="reset">Как в макете</button>
  </div>
</section>

<h2 class="group" id="drones">Дроны</h2>
<p class="lead">Картинка полёта 18×12: корпус около 14 px, с лопастями до 18. При зуме 2 это 36 px на экране — чуть шире нынешнего (14 px с лопастями). В доке дрон складывается до 12 px, чтобы два стояли на одной полке 32-пиксельной станции. Направо смотрит сама картинка, налево — зеркально.</p>
${DIDS.map(droneSection).join('\n')}

<h2 class="group" id="bays">Станция</h2>
<p class="lead">2×2 клетки, как сейчас: две полки по два места. Пристыкованный дрон рисуется на самой станции, как батарея у зарядной станции, а не абстрактной клеткой. Места одни для всех трёх станций и всех трёх дронов. Пустое место видно сразу: пятачок с медными контактами и тёмные лампочки.</p>
<section class="kind cur" id="bcur"><div class="khead"><h3>Сейчас</h3><span class="dim">machine_droneBay.png + голубые полоски кодом</span></div>
  <div class="crow"><div class="rl"><b>Нынешняя</b><span>пусто · два · четыре</span></div>
  <div class="stage"><span class="tag">×4</span>${strip([[null, null, null, null], [0, null, 0, null], [0, 0, 0, 0]].map((s, i) => fig(uri(bayWith('cur', s, 'A')), 32, 32, 4, 'сейчас', ['пусто', 'два дрона', 'четыре'][i])), 'cap')}</div></div></section>
${BIDS.map(baySection).join('\n')}

<h2 class="group" id="motion">Движение</h2>
<section class="notes">
<h3>Модель (в макете, параметры — ползунки выше)</h3>
<ol>
  <li><b>Цель.</b> Позиция из <code>Served.Smoothed</code> в пикселях, поднятая на <b>8 px</b> (полклетки). Под глухой клеткой сверху (коридор в одну клетку, потолок) — на 2 px, чтобы винты не лезли в потолок. Под зажимом 1 px троса и планка захвата, ниже иконка 8×8; её низ — на 1 px выше пола клетки. При 0.3–0.4 клетки (5–6 px) груз всё ещё задевает пол на 1–2 px, при 7 касается его; поэтому полклетки. Винты при этом заходят в клетку выше на 5 px — в открытой комнате это не видно.</li>
  <li><b>Пружина с подачей скорости.</b> Нарисованная точка догоняет цель: <code>a = ω²·(цель − x) + 2ζω·(v_цели − v)</code>, <code>ω = 2π·2.5 Гц</code>, <code>ζ = 0.65</code>. В ровном полёте отставания нет (скорость цели подаётся), на старте дрон плавно разгоняется, на остановке перелетает примерно на 2 px и возвращается, на углу пути описывает дугу. Шаг интегрирования не больше 1/240 с.</li>
  <li><b>Наклон.</b> <code>угол = vx/64·7° + ax·0.012°</code>, не больше <b>±14°</b>, догоняет за τ = 0.08 с. В ровном полёте 7° носом вперёд, на разгоне до 14°, на торможении — назад. На вертикальных отрезках ровно. Поворот — <code>DrawSetTransform</code> вокруг центра корпуса, как поворачиваются части героя; заранее нарисованные кадры наклона не нужны (рекомендую поворот: плавный, ни одного лишнего файла).</li>
  <li><b>Направление.</b> Картинка зеркалится, когда |vx| &gt; 6 px/с; на вертикали держит прежнее.</li>
  <li><b>Покачивание.</b> <code>y += 1 px·sin(2π·t/1.4 с + фаза дрона)</code>, в полёте вдвое меньше, у каждого дрона своя фаза.</li>
  <li><b>Груз.</b> Маятник: <code>φ'' = −ω_c²φ − 2·0.3·ω_c·φ' − k·ax</code>, <code>ω_c = 2π·1.4 Гц</code>, отклонение около 18° на разгоне, не больше 34°. Иконка не поворачивается, только смещается — пиксели остаются ровными. Зажим поворачивается вместе с корпусом.</li>
  <li><b>Винты.</b> 4 кадра, 20 кадров/с в полёте; при взлёте и посадке частота плавно растёт и падает (τ = 0.12 с).</li>
  <li><b>Взлёт.</b> Новый дрон в списке сима начинается с пятачка своего места: 0.22 с раскрутка на месте, потом пружина сама поднимает его к цели.</li>
  <li><b>Посадка.</b> Дрон пропал из <code>Drones</code>, а в станции прибавился дрон: клиент ещё 1 с рисует его сам — 0.55 с встаёт перед своим местом в 4 px над пятачком (станция видна спереди, дрон входит в полку «от зрителя», не сверху через полку), 0.45 с опускается, винты останавливаются; потом его рисует станция. Пока идёт посадка, место рисуется пустым.</li>
</ol>
<h3>Чего не делаем</h3>
<ul>
  <li>Сим не трогаем: ни высоты, ни наклона, ни новых команд. Пути по-прежнему из прямых отрезков; дуги на углах только на экране, и при ζ ниже 0.5 дрон на углу может выйти за клетку на 2–3 px.</li>
  <li>Дрон не облетает других дронов и героя — все рисуются поверх, как сейчас.</li>
  <li>Груз не поворачиваем: повёрнутые 8×8 пикселей выглядят грязно.</li>
</ul>
</section>

<h2 class="group" id="code">Что меняется в коде</h2>
<section class="notes">
<ol>
  <li><b>Арт.</b> Из <code>drone-art/out/drone-&lt;X&gt;/</code> в <code>game/art/</code>: <code>drone_fly_0..3</code>, <code>drone_dock</code>, <code>drone_eye</code>, <code>drone_dock_eye</code>, <code>drone_lamp_work</code>, <code>drone_lamp_low</code>; <code>items/drone.png</code> и <code>items/drone_empty.png</code> заменяют нынешние (их видно в слотах станции и в инвентаре). Из <code>out/bay-&lt;X&gt;/</code>: <code>machine_droneBay.png</code> (заменяет), <code>droneBay_pad</code>, <code>droneBay_charge_0..4</code>, <code>droneBay_charge_low</code>.</li>
  <li><b><code>game/features/logistics/LogisticsView.cs</code></b> — основное:
    <ul>
      <li><code>DrawDrone</code> с прямоугольниками и <code>Body/Shade/Rotor/Lens/Outline</code> уходит; дрон — <code>DrawTextureRect</code> c <code>PixelArt.Lit("drone_fly_N")</code> (карта нормалей даёт объём под лампами) внутри <code>DrawSetTransform(точка, наклон, (face, 1))</code>; глаз и лампа — во втором узле без освещения (как <code>_beacons</code> сейчас) с той же трансформацией.</li>
      <li>Новый класс состояния на дрон по <code>Drone.Id</code> (<code>Dictionary&lt;int, Flier&gt;</code>): x, y, vx, vy, наклон, сторона, маятник, обороты, фаза; шаг каждый <code>_Process</code> по <code>delta</code>. Удаляется после посадки.</li>
      <li>Посадка: в <code>Read()</code> сравнить занятые места с прошлым чтением; дрон, пропавший из <code>Drones</code>, ведётся к месту, которое стало занятым (ближайшее из новых). Пока он садится, <code>_pads</code> это место не рисует.</li>
      <li><code>Pads</code> (4 прямоугольника) заменяется таблицей из <code>droneBay.json</code>: точка дрона, пятачок, лампочки. Станция рисует: <code>drone_dock</code> освещённым (слой машин), <code>droneBay_pad</code> и <code>droneBay_charge_N</code> без освещения, <code>Glow</code> зелёный ×0.22 с пульсом 0.8 Гц у заряжающегося (как у <code>ChargerLights</code>). Заряд — <code>Charges.Share</code> слота: 4 лампочки по четвертям, меньше четверти — <code>charge_low</code>.</li>
      <li>Груз: <code>DrawTextureRect(ItemIcons.Icon(..))</code> 8×8 на конце подвеса от зажима; подвес — линия 1 px <code>#22262c</code>.</li>
    </ul></li>
  <li><b>Чистая логика без Godot</b> — рядом с <code>Served.Smoothed</code> в <code>Served.cs</code> или новым <code>Flight.cs</code> в той же папке: шаг пружины, наклон, маятник, подъём от клетки сверху (<code>IGridQueries.StructureAt</code> / проходимость); с тестами в <code>game.tests</code> (например: ровный полёт без отставания; после остановки вернулся в цель за 1 с; угол не больше максимума; подъём 2 под стеной).</li>
  <li><b>Константы</b> — в начале <code>LogisticsView</code> / нового файла: подъём 8, качание 1 px / 1.4 с, наклон 14° / 7°, пружина 2.5 Гц / 0.65, груз 18°. Это косметика клиента, не <code>Content</code>.</li>
  <li><b><code>features/logistics/README.md</code></b> — переписать «Drones in flight» и «The bay» в гарантиях; тестбед не меняется.</li>
  <li><b>Не меняется:</b> <code>sim/Logistics/*</code>, <code>Content/Logistics.cs</code>, рамка станции в <code>InventoryPanel</code> (только новая иконка в слотах).</li>
</ol>
${fileTable}
</section>

<section class="answer" id="answer">
  <h2>Как ответить</h2>
  <p>Выберите — строка соберётся сама. Ползунки в сцене переключают «Движение» на «по ползункам» и вписывают значения. Можно дописать правки: «B, но глаз меньше», «стеллаж без полосы».</p>
  ${picks}
  <div class="pick"><code id="line">Ничего не выбрано</code><button type="button" id="copy">Копировать</button></div>
  <p class="note">Пример: Дрон: B · Станция: A · Движение: как в макете</p>
</section>
</div>
<script>
const DATA = ${JSON.stringify(DATA)};
${runtime}
</script>
</body>
</html>
`;
fs.writeFileSync(path.join(ROOT, 'concepts.html'), html);
console.log('concepts.html', (Buffer.byteLength(html) / 1024).toFixed(1), 'KB');
