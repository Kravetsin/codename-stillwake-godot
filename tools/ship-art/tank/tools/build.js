// node build.js — writes out/<look>/ with the game's file names and ../concepts.html with every picture inline.
// Nothing here touches the game's repository; it is only read.
const fs = require('fs'), path = require('path');
const { Pic, img, blit, glow, shade, loadPng } = require('./lib.js');
const { encode } = require('./png.js');
const { LOOKS } = require('./tank.js');

const ART = require('path').join(__dirname, '..', '..', '..', '..', 'game', 'art');
const ROOT = path.join(__dirname, '..'), OUT = process.argv[2] || path.join(ROOT, 'out'), IDS = Object.keys(LOOKS);
const L = n => loadPng(path.join(ART, n + '.png'));

// ---- the files, as the game names them ----
const looks = {};
for (const id of IDS) {
  const t = looks[id] = LOOKS[id](), d = path.join(OUT, id);
  fs.mkdirSync(d, { recursive: true });
  for (const f of fs.readdirSync(d)) fs.unlinkSync(path.join(d, f));
  t.base.save(path.join(d, 'machine_tank.png'));
  t.frames.forEach((f, i) => f.save(path.join(d, `tank_fill_${i}.png`)));
}

// ---- the current tank: its picture and the gauge AirView draws up its right side ----
const CUR = L('machine_tank');
function currentAt(share) {
  const im = img(32, 32); blit(im, CUR, 0, 0);
  const full = 24, level = Math.round(full * share);
  for (let y = 4; y < 4 + full; y++) for (let x = 22; x < 28; x++) im.data.set([...(y >= 4 + full - level ? [0x6f, 0xd3, 0xff] : [0x16, 0x21, 0x2d]), 255], (y * 32 + x) * 4);
  return im;
}
const SHARES = [0, 0.25, 0.5, 0.75, 1];

// a tank as the game would draw it: the base lit by the room (dimmed by `room`), the fill frame unlit over it, its Glow
function tankAt(t, frame, { room = 1, light = false } = {}) {
  let im = img(32, 32); blit(im, t.base, 0, 0);
  im = shade(im, room);
  if (light) { const l = t.lights[frame]; glow(im, l.at[0], l.at[1], 20, l.colour, l.level * 0.9); }
  blit(im, t.frames[frame], 0, 0);
  return im;
}
const half = t => Math.floor((t.frames.length - 1) / 2) + (t.frames.length === 6 ? 1 : 0);

// ---- the room: back wall, pipes, a floor, a door, the vent, generator, crate and two tanks ----
const C = 16, SW = 14 * C, SH = 6 * C + 8, FLOOR_ROW = 5;
function scene(tankLeft, tankRight) {
  let im = img(SW, SH, '#0d1016');
  const back = L('back_scrap');
  for (let y = 0; y < SH; y += back.h) for (let x = 0; x < SW; x += back.w) blit(im, back, x, y);
  const wall = L('wall_iron'), floor = L('floor');
  // the ceiling and the hull under the floor: iron wall, its edges dark
  for (let x = 0; x < SW; x += 64) { blit(im, wall, x, -48); blit(im, wall, x, (FLOOR_ROW + 1) * C); }
  const pipe = (cx, cy, mask) => blit(im, L('pipe_' + mask), cx * C, cy * C);
  // the left tank's and the generator's port run along the floor to the vent up the left wall
  pipe(0, 2, 4); pipe(0, 3, 5); pipe(0, 4, 3); pipe(1, 4, 10); pipe(2, 4, 10); pipe(3, 4, 10); pipe(4, 4, 8);
  // the right tank feeds the vent beside it
  pipe(11, 2, 4); pipe(11, 3, 5); pipe(11, 4, 3); pipe(12, 4, 8);
  for (let x = 0; x < SW; x += C) blit(im, floor, x, FLOOR_ROW * C);
  // a door three cells tall, the iron wall over it
  for (let y = 0; y < C; y++) for (let x = 0; x < C; x++) { const i = ((C + y) * SW + 7 * C + x) * 4, s = ((y) * 64 + x) * 4; im.data.set(wall.data.subarray(s, s + 4), i); }
  blit(im, L('door_closed_4'), 7 * C, 2 * C); blit(im, L('door_closed_5'), 7 * C, 3 * C); blit(im, L('door_closed_1'), 7 * C, 4 * C);
  blit(im, L('machine_vent'), 0, 2 * C); blit(im, L('machine_vent'), 11 * C, 2 * C);
  blit(im, L('machine_generator'), 1 * C, 3 * C);
  blit(im, L('machine_crate'), 9 * C, 4 * C);
  // ceiling and floor edges, as the wall's edge pictures draw them
  for (let x = 0; x < SW; x++) { im.data.set([0x70, 0x77, 0x87, 255], ((C - 1) * SW + x) * 4); }
  const at = (t, cx) => blit(im, t.base || t, cx * C, 3 * C);
  if (tankLeft.cur) { blit(im, tankLeft.cur, 4 * C, 3 * C); blit(im, tankRight.cur, 12 * C, 3 * C); im = shade(im, 0.7); return { im, after: [] }; }
  at(tankLeft.t, 4); at(tankRight.t, 12);
  im = shade(im, 0.7);
  for (const [t, f, cx] of [[tankLeft.t, tankLeft.f, 4], [tankRight.t, tankRight.f, 12]]) {
    const l = t.lights[f]; glow(im, cx * C + l.at[0], 3 * C + l.at[1], 28, l.colour, l.level * 0.7);
    blit(im, t.frames[f], cx * C, 3 * C);
  }
  return { im };
}

// ---- the page ----
const uri = im => 'data:image/png;base64,' + Buffer.from(encode(im.w, im.h, (im.rgba ? im.rgba() : im).data)).toString('base64');
const esc = s => String(s).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
const pic = (src, w, h, k, alt) => `<img src="${src}" width="${w * k}" height="${h * k}" alt="${esc(alt)}">`;
const strip = (items, k, cls = '') => `<div class="strip ${cls}">${items.map(([src, w, h, alt, cap]) => cap ? `<figure>${pic(src, w, h, k, alt)}<figcaption>${esc(cap)}</figcaption></figure>` : pic(src, w, h, k, alt)).join('')}</div>`;
const hex = c => c;

const INFO = {
  A: {
    name: 'Вариант A', short: 'смотровое стекло', frames: 6,
    about: 'Стальной сосуд на ножках, как плавильня: купол с медным вентилем, два шва с заклёпками, патрубок порта слева внизу. Спереди — смотровое стекло 6×10 в тёмной раме с медными болтами и рисками сбоку. Внутри поднимается бледно-голубой жидкий кислород: поверхность светлее, пузырьки, блик стекла поверх. Стекло с подсветкой, поэтому жидкость светится и чуть освещает комнату. Пустой бак: тёмное стекло и красная лампочка над ним справа.',
    steps: ['пусто', '1/5', '2/5', '3/5', '4/5', 'полный'],
  },
  B: {
    name: 'Вариант B', short: 'баллоны и лампы', frames: 5,
    about: 'Два баллона в стальной раме, плечи окрашены в старый голубой цвет машин (кислородная окраска), горловины соединены трубкой через стойку. Между баллонами стойка с колонкой из четырёх ламп, как у зарядной станции. Одна лампа — один баллон кислорода (2250 из 9000): тёмная лампа значит, что ещё один баллон влезет. Все четыре горят — баллон уже не влезет (ровно тогда сим отказывает с atmosphere.tankFull). Пустой бак: нижняя лампа красная.',
    steps: ['пусто', '1 баллон', '2', '3', 'полный'],
  },
  C: {
    name: 'Вариант C', short: 'из лома, манометр', frames: 5,
    about: 'Бак собран из лома, как печь: три разных листа на сварке, вмятина, гофрированная заплата с ржавыми потёками, разные подставки — стальная и ржавая, предохранительный клапан и вентиль на куполе. Спереди большой манометр в медном ободе. Циферблат с подсветкой: пустой бак — красный, четверть — янтарный, от половины — голубой. Стрелка идёт слева внизу (пусто) вверх и вправо вниз (полный). Шаги те же, что у B — по баллону.',
    steps: ['пусто', '¼', '½', '¾', 'полный'],
  },
};
const LIGHT_RU = {
  A: 'Кадр 0: красный #e03030 в (22, 10), 0.25. Кадры 1–5: голубой #6fd3ff в (16, 16) — середина стекла; сила 0.18 / 0.24 / 0.30 / 0.36 / 0.42.',
  B: 'Кадр 0: красный #e03030 в (16, 21) — нижняя лампа, 0.25. Кадры 1–4: голубой #6fd3ff в (16, 15) — середина колонки; сила 0.16 / 0.22 / 0.28 / 0.34.',
  C: 'Все кадры в (12.5, 10.5) — центр циферблата, цвет циферблата: 0 — #e03030, 0.25; 1 — #f2b84b, 0.22; 2–4 — #6fd3ff, 0.24 / 0.30 / 0.36.',
};
const curRow = `<div class="crow cur"><div class="rl"><b>Сейчас</b><span>бирюзовый цилиндр + полоска AirView</span></div>
  <div class="stage x1"><span class="tag">×1 · пусто · ¼ · ½ · ¾ · полный</span>${strip(SHARES.map(s => [uri(currentAt(s)), 32, 32, 'сейчас ' + s]), 1)}</div>
  <div class="stage"><span class="tag">×4 · полоска справа — отдельный индикатор, нарисованный кодом поверх картинки</span>${strip(SHARES.map((s, i) => [uri(currentAt(s)), 32, 32, 'сейчас ' + s, ['пусто', '¼', '½', '¾', 'полный'][i]]), 4, 'cap')}</div></div>`;

function variant(id) {
  const t = looks[id], I = INFO[id], n = t.frames.length, mid = half(t);
  const lit = t.frames.map((_, i) => [uri(tankAt(t, i)), 32, 32, I.name + ' ' + I.steps[i], I.steps[i]]);
  const dark = t.frames.map((_, i) => [uri(tankAt(t, i, { room: 0.42, light: true })), 32, 32, I.name + ' в темноте ' + I.steps[i], I.steps[i]]);
  const files = [[uri(t.base), 32, 32, 'machine_tank', 'machine_tank'], ...t.frames.map((f, i) => [uri(f), 32, 32, 'tank_fill_' + i, 'tank_fill_' + i])];
  const sideBySide = [[uri(currentAt(0.5)), 32, 32, 'сейчас, половина', 'сейчас'], [uri(tankAt(t, mid)), 32, 32, I.name + ', половина', I.short]];
  const sc = scene({ t, f: n - 1 }, { t, f: 0 }).im;
  return `<section class="kind" id="v${id}">
  <div class="khead"><h3>${I.name} — ${esc(I.short)}</h3><span class="dim">32×32 · ${n} кадров заполнения · out/${id}/</span></div>
  <p class="about">${esc(I.about)}</p>
  <div class="crow"><div class="rl"><b>Рядом с нынешним</b><span>оба наполовину</span></div>
    <div class="stage x1 pair"><span class="tag">×1</span>${strip(sideBySide.map(a => a.slice(0, 4)), 1)}</div>
    <div class="stage"><span class="tag">×4</span>${strip(sideBySide, 4, 'cap')}</div></div>
  <div class="crow"><div class="rl"><b>Кадры заполнения</b><span>освещённая комната</span></div>
    <div class="stage x1"><span class="tag">×1</span>${strip(lit.map(a => a.slice(0, 4)), 1)}</div>
    <div class="stage"><span class="tag">×4 · база освещена, кадр поверх без освещения</span>${strip(lit, 4, 'cap')}</div></div>
  <div class="crow"><div class="rl"><b>В тёмной комнате</b><span>свет — Glow, примерно</span></div>
    <div class="stage lit"><span class="tag">×4 · база притушена, кадр светится, вокруг его Glow</span>${strip(dark, 4, 'cap')}</div></div>
  <div class="crow"><div class="rl"><b>Файлы</b><span>как их грузит игра</span></div>
    <div class="stage chk"><span class="tag">×3 · на клетке — прозрачность</span>${strip(files, 3, 'cap')}</div></div>
  <p class="about light"><b>Свет.</b> ${esc(LIGHT_RU[id])}</p>
  <figure class="scene">${pic(uri(sc), SW, SH, 2, 'Комната: вентиляция, генератор, ' + I.name + ' полный, дверь, ящик, ' + I.name + ' пустой')}<figcaption>${I.name} · ×2 · вентиляция и генератор кислорода, бак полный, дверь, ящик, бак пустой; комната притушена, свет примерный</figcaption></figure>
</section>`;
}
const curScene = scene({ cur: currentAt(1) }, { cur: currentAt(0) }).im;

const radios = (group, opts) => `<fieldset class="pickrow" data-group="${group}"><legend>${esc(group)}:</legend>${opts.map(([v, label], i) => `<label><input type="radio" name="pick-${esc(group)}" value="${esc(v)}"${i === 0 ? ' checked' : ''}> ${esc(label)}</label>`).join('')}</fieldset>`;
const picks = radios('Бак', [['', 'не решил'], ['A', 'A — стекло'], ['B', 'B — лампы'], ['C', 'C — манометр'], ['ни один', 'ни один']]) +
  radios('Шаги', [['', 'как в варианте'], ['по баллону (5 кадров)', 'по баллону, 5 кадров'], ['6 кадров', '6 кадров']]) +
  radios('Пустой', [['', 'как в варианте'], ['красный', 'красным'], ['просто тёмный', 'просто тёмным']]);

const html = `<!doctype html>
<html lang="ru">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>Oxygen Tank Concepts</title>
<meta name="description" content="Stillwake: кислородный бак 2×2, три варианта, показывающих запас в самой картинке — смотровое стекло, колонка ламп, манометр.">
<style>
:root {
  --page: #0b0e14; --bench: #111823; --line: #253042; --ink: #e3e9f0; --muted: #8b99aa; --s300: #cbd5e1; --s400: #94a3b8; --s500: #64748b;
  --amber: #f2b84b; --o2: #6fd3ff; --stage: #10151d; --mono: Consolas, Menlo, ui-monospace, monospace; --ui: "Segoe UI", system-ui, sans-serif; color-scheme: dark;
}
* { box-sizing: border-box; }
html { background: var(--page); }
body { margin: 0; background: var(--page); color: var(--ink); font: 15px/1.5 var(--ui); padding: 28px 16px 64px; overflow-x: hidden; }
.wrap { max-width: 1240px; margin: 0 auto; display: grid; gap: 18px; }
.eyebrow { font: 600 12px var(--mono); letter-spacing: .12em; text-transform: uppercase; color: var(--o2); }
h1 { font: 600 30px/1.15 var(--ui); margin: 4px 0 8px; }
header p { margin: 0 0 8px; color: var(--muted); max-width: 92ch; }
.bar { position: sticky; top: 0; z-index: 5; display: flex; flex-wrap: wrap; gap: 6px 14px; background: rgba(11, 14, 20, .94); border-bottom: 1px solid var(--line); padding: 10px 0; backdrop-filter: blur(4px); font-size: 13px; }
.bar a { color: var(--s400); text-decoration: none; } .bar a:hover { color: var(--ink); }
h2.group { font: 600 22px var(--ui); margin: 14px 0 0; padding-top: 8px; border-top: 1px solid var(--line); scroll-margin-top: 56px; }
.lead { margin: 0; color: var(--s300); font-size: 14px; max-width: 100ch; }
.kind, .block { background: var(--bench); border: 1px solid var(--line); border-radius: 8px; padding: 14px; display: grid; gap: 10px; scroll-margin-top: 56px; min-width: 0; }
.khead { display: flex; flex-wrap: wrap; gap: 4px 14px; align-items: baseline; }
.khead h3 { margin: 0; font: 600 18px var(--ui); }
.dim { color: var(--s500); font: 12px var(--mono); } code { font: 12px var(--mono); color: var(--s300); }
.about { margin: 0; color: var(--s300); font-size: 14px; max-width: 110ch; }
.about.light { font-size: 13px; color: var(--s400); }
.crow { display: grid; grid-template-columns: 150px minmax(0, 1fr); gap: 6px; padding: 7px 0; border-top: 1px solid rgba(37, 48, 66, .6); min-width: 0; }
.crow > .stage { grid-column: 2; }
.cur .rl b { color: var(--s400); }
.rl { display: grid; align-content: center; gap: 2px; padding: 0 4px; grid-row: span 2; } .rl b { font: 600 14px var(--ui); } .rl span { font: 11px var(--mono); color: var(--s500); }
.stage { position: relative; border-radius: 4px; padding: 18px 12px 10px; background: var(--stage); min-width: 0; overflow-x: auto; }
.stage.lit { background: #07090d; }
.stage.chk { background-color: #1a2029; background-image: linear-gradient(45deg, #141a22 25%, transparent 25%, transparent 75%, #141a22 75%), linear-gradient(45deg, #141a22 25%, transparent 25%, transparent 75%, #141a22 75%); background-size: 12px 12px; background-position: 0 0, 6px 6px; }
.tag { position: absolute; top: 3px; left: 8px; font: 10px var(--mono); color: var(--s500); white-space: nowrap; }
.strip { display: flex; flex-wrap: wrap; gap: 10px 14px; align-items: flex-end; }
.strip img { image-rendering: pixelated; display: block; max-width: 100%; height: auto; }
.strip figure { margin: 0; display: grid; gap: 4px; justify-items: center; max-width: 100%; }
.strip figcaption { font: 11px var(--mono); color: var(--s400); max-width: 128px; text-align: center; }
.scene { margin: 0; background: #000; border-radius: 6px; overflow: hidden; border: 1px solid var(--line); max-width: ${SW * 2 + 2}px; }
.scene img { display: block; width: 100%; height: auto; image-rendering: pixelated; }
.scene figcaption { font: 12px var(--mono); color: var(--s400); padding: 6px 12px 8px; }
.notes { background: var(--bench); border: 1px solid var(--line); border-radius: 8px; padding: 14px 18px; color: var(--s300); font-size: 14px; }
.notes ol, .notes ul { margin: 6px 0; padding-left: 22px; } .notes li { margin: 4px 0; } .notes h3 { margin: 4px 0; font: 600 16px var(--ui); color: var(--ink); }
.answer { background: var(--bench); border: 1px solid var(--line); border-radius: 8px; padding: 16px; display: grid; gap: 12px; }
.answer h2 { margin: 0; font: 600 20px var(--ui); }
.answer p { margin: 0; color: var(--s300); max-width: 95ch; }
.pickrow { border: 0; margin: 0; padding: 0; display: flex; flex-wrap: wrap; gap: 4px 16px; font-size: 14px; color: var(--s300); }
.pickrow legend { float: left; margin-right: 12px; min-width: 90px; color: var(--ink); font-weight: 600; }
.pickrow label { display: inline-flex; gap: 5px; align-items: center; cursor: pointer; }
.pickrow input { accent-color: var(--o2); margin: 0; }
.pick { display: flex; align-items: stretch; border: 1px solid var(--line); border-radius: 6px; overflow: hidden; max-width: 100%; }
.pick code { font: 14px var(--mono); padding: 9px 12px; background: var(--page); color: var(--ink); user-select: all; flex: 1; min-width: 0; overflow-wrap: anywhere; }
.pick button { font: 600 13px var(--ui); border: 0; background: var(--o2); color: #06202c; padding: 0 16px; cursor: pointer; }
.note { font-size: 13px; color: var(--s400); }
@media (max-width: 760px) {
  .crow { grid-template-columns: minmax(0, 1fr); } .crow > .stage { grid-column: 1; }
  .rl { grid-auto-flow: column; justify-content: start; gap: 8px; grid-row: auto; }
}
@media (max-width: 560px) { .tag { white-space: normal; right: 8px; line-height: 1.2; } .stage { padding-top: 30px; } }
</style>
</head>
<body>
<div class="wrap">
<header>
  <div class="eyebrow">Stillwake · кислородный бак · концепт-арт</div>
  <h1>Кислородный бак</h1>
  <p>Нынешний бак — гладкий бирюзовый цилиндр, а запас показывает отдельная голубая полоска, которую <code>AirView</code> рисует кодом поверх картинки. Здесь три варианта, где запас виден в самой машине — как батарея в отсеке зарядной станции. Размер ровно 2×2 клетки (32×32), пиксель картинки — пиксель мира, свет сверху слева, контур <code>#1a1e25</code>, сталь и медь из плавильни, Формера, сборщика и печи. Патрубок слева внизу стоит там, где порт бака (клетка (0, 1)) и где через неё идёт труба.</p>
  <p>Устроено как у станций: <code>machine_tank.png</code> — бак пустой и тёмный, освещается комнатой; поверх без освещения — один кадр <code>tank_fill_N.png</code> (32×32, только то, что светится). Кадр 0 — «пусто» (красное), последний — «полный». Кадр светится и даёт свой <code>Glow</code>; точки и цвета света подписаны у каждого варианта.</p>
</header>
<nav class="bar"><a href="#current">Сейчас</a><a href="#vA">A — стекло</a><a href="#vB">B — лампы</a><a href="#vC">C — манометр</a><a href="#code">Что меняется в коде</a><a href="#answer">Ответ</a></nav>

<h2 class="group" id="current">Сейчас</h2>
<section class="block">${curRow}
<figure class="scene">${pic(uri(curScene), SW, SH, 2, 'Комната с нынешними баками')}<figcaption>Сейчас · ×2 · вентиляция и генератор кислорода, бак полный, дверь, ящик, бак пустой</figcaption></figure></section>

<h2 class="group" id="variants">Варианты</h2>
<p class="lead">У каждого: рядом с нынешним баком (×1 и ×4), все кадры заполнения от пустого до полного в светлой и в тёмной комнате, файлы по отдельности и кусок комнаты в ×2 из плиток игры.</p>
${IDS.map(variant).join('\n')}

<h2 class="group" id="code">Что меняется в коде</h2>
<section class="notes">
<ol>
  <li><b>Файлы.</b> Выбранный набор из <code>tank-art/out/&lt;вариант&gt;/</code> копируется в <code>game/art/</code>: <code>machine_tank.png</code> заменяет нынешний (его по-прежнему рисует <code>GridView</code> на слое машин, освещённым), плюс <code>tank_fill_0..4.png</code> (B, C) или <code>tank_fill_0..5.png</code> (A). Godot сам создаст <code>.import</code>.</li>
  <li><b>Откуда доля.</b> Тот же запрос, что сейчас у полоски: <code>m.Atmosphere.OxygenIn(tank.Id)</code> из 9000 (<code>Content.Air.TankCapacity</code>). Кадр — чистая функция, например <code>TankFill.Frame(int oxygen)</code> рядом с <code>game/shared/logic/Charges.cs</code>, с тестом в <code>game.tests</code>:
    <ul>
      <li>B и C (5 кадров, по баллону): <code>oxygen == 0 ? 0 : (oxygen + Air.Canister - 1) / Air.Canister</code> → 1…4. Все четыре шага ⇔ <code>oxygen &gt; 6750</code> ⇔ баллон не влезет (<code>atmosphere.tankFull</code>). Тест: 0→0, 1→1, 2250→1, 2251→2, 6750→3, 6751→4, 9000→4.</li>
      <li>A (6 кадров, по пятым): <code>oxygen == 0 ? 0 : (oxygen * 5 + Air.TankCapacity - 1) / Air.TankCapacity</code> → 1…5.</li>
      <li>Любой остаток показывает хотя бы нижний шаг; «пусто» — только ровно 0, как и в <code>PipesOverlay</code> (бак кормит трубы при <code>OxygenIn &gt; 0</code>).</li>
    </ul></li>
  <li><b>Вид.</b> Новый узел <code>game/features/atmosphere/TankLooks.cs</code> по образцу <code>ChargerLights</code>: раз в 250 мс (как <code>AirView.Survey</code>) обходит <code>grid.MachinesOf("tank")</code>, на каждый бак — <code>Sprite2D</code> с <code>CanvasItemMaterial.Unshaded</code>, <code>ZIndex = Layers.Screens</code>, текстура <code>PixelArt.Picture($"tank_fill_{frame}")</code>, и <code>Glow</code> в точке кадра с цветом × сила. Ушедшие баки — спрайт и Glow удалить. Кадр меняется только при чтении, не каждый кадр.</li>
  <li><b>Убрать полоску.</b> Из <code>AirView</code>: <code>_gauges</code>, <code>_tanks</code>, <code>GaugeWidth</code>, цвета <code>Track</code> и <code>Oxygen</code>, кусок <code>Survey</code> про баки; <code>Layers.Gauges</code> больше никто не использует. Обновить <code>features/atmosphere/README.md</code> (таблица и гарантии) и summary-комментарий <code>AirView</code>.</li>
  <li><b>Чего не делаем.</b> Не анимируем (пузырьки, мигание) — бак меняется медленно, а движение на экране мы бережём для того, что требует внимания. Не рисуем, кормит ли бак трубы прямо сейчас — это показывает оверлей P.</li>
</ol>
</section>

<section class="answer" id="answer">
  <h2>Как ответить</h2>
  <p>Выберите — строка соберётся сама. Можно дописать правки: «A, но стекло шире», «B с манометром из C», «C без заплаты».</p>
  ${picks}
  <div class="pick"><code id="line">Ничего не выбрано</code><button type="button" id="copy">Копировать</button></div>
  <p class="note">Пример: Бак: B · Пустой: красный</p>
</section>
</div>
<script>
(function () {
  function build() {
    var parts = [];
    document.querySelectorAll('.pickrow').forEach(function (f) {
      var c = f.querySelector('input:checked');
      if (c && c.value) parts.push(f.getAttribute('data-group') + ': ' + c.value);
    });
    document.getElementById('line').textContent = parts.length ? parts.join(' · ') : 'Ничего не выбрано';
  }
  document.addEventListener('change', function (e) { if (e.target.matches('.pickrow input')) build(); });
  build();
  document.getElementById('copy').addEventListener('click', function () {
    var b = this, text = document.getElementById('line').textContent;
    var done = function () { b.textContent = 'Скопировано'; setTimeout(function () { b.textContent = 'Копировать'; }, 1400); };
    if (navigator.clipboard && navigator.clipboard.writeText) navigator.clipboard.writeText(text).then(done, done); else done();
  });
})();
</script>
</body>
</html>
`;
fs.writeFileSync(path.join(ROOT, 'concepts.html'), html);
// the scenes as files too, for a look without the page
fs.mkdirSync(path.join(ROOT, 'tmp'), { recursive: true });
const { savePng } = require('./lib.js');
for (const id of IDS) savePng(path.join(ROOT, 'tmp', 'scene_' + id + '.png'), scene({ t: looks[id], f: looks[id].frames.length - 1 }, { t: looks[id], f: 0 }).im);
savePng(path.join(ROOT, 'tmp', 'scene_cur.png'), curScene);
console.log('concepts.html', (Buffer.byteLength(html) / 1024).toFixed(1), 'KB');
