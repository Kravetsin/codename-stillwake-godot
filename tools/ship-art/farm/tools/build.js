// node build.js — writes out/<look>/ with the game's file names, out/debris/, and ../concepts.html with every
// picture inline. Nothing here touches the game's repository; it is only read.
const fs = require('fs'), path = require('path');
const { Pic, img, blit, savePng } = require('./lib.js');
const { encode } = require('./png.js');
const { icon, ITEMS } = require('./icons.js');
const { capsule } = require('./capsule.js');
const { current, frame, ART } = require('./compose.js');
const { soilChunk } = require('./debris.js');
const { scene, W: SW, H: SH } = require('./scene.js');

const ROOT = path.join(__dirname, '..'), OUT = process.argv[2] || path.join(ROOT, 'out'), LOOKS = ['A', 'B', 'C'];
const mk = d => fs.mkdirSync(d, { recursive: true });

// ---- the files, as the game names them ----
const caps = {};
for (const L of LOOKS) {
  const d = path.join(OUT, L); mk(path.join(d, 'items'));
  for (const it of ITEMS) icon(L, it).save(path.join(d, 'items', it + '.png'));
  const c = caps[L] = capsule(L);
  c.base.save(path.join(d, 'machine_growCapsule.png'));
  c.work.save(path.join(d, 'growCapsule_work_0.png'));
  c.plants.forEach((p, i) => p.save(path.join(d, `growCapsule_plant_${i}.png`)));
}
mk(path.join(OUT, 'debris'));
const chunks = [0, 1, 2].map(i => soilChunk(i));
chunks.forEach((p, i) => p.save(path.join(OUT, 'debris', `soilclod_0${i + 1}.png`)));

// ---- the page ----
const uri = im => 'data:image/png;base64,' + Buffer.from(encode(im.w, im.h, (im.rgba ? im.rgba() : im).data)).toString('base64');
const fileUri = f => 'data:image/png;base64,' + fs.readFileSync(f).toString('base64');
const esc = s => String(s).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
const pic = (src, w, h, k, alt) => `<img src="${src}" width="${w * k}" height="${h * k}" alt="${esc(alt)}">`;
const strip = (items, k, cls = '') => `<div class="strip ${cls}">${items.map(([src, w, h, alt, cap]) => cap ? `<figure>${pic(src, w, h, k, alt)}<figcaption>${esc(cap)}</figcaption></figure>` : pic(src, w, h, k, alt)).join('')}</div>`;

const NAME = { A: 'Вариант A', B: 'Вариант B', C: 'Вариант C' };
const SHORT = { A: 'сталь игры', B: 'из лома', C: 'лаборатория' };
const ABOUT = {
  A: 'Серые плиты и голубое стекло, как у плавильни и сборщика. Капсула — стеклянный цилиндр на плитах с решёткой и панелью, купол-колпак с розовой лампой. Иконки — простые предметы: проросший боб, горка земли, пучок листьев, пробковая колба. В капсуле горох со стручками.',
  B: 'Собрано из лома, как печь: разные плиты, ржавчина, заклёпки, деревянная чурка под ногой. Стекло стянуто железными хомутами, трещина заклеена лентой, земля в ящике из досок, лампа под колпаком прикручена к крышке, кабель идёт вниз к коробке с манометром. Иконки — мешковина и канистра. В капсуле томат с оранжевыми плодами.',
  C: 'Лабораторная капсула: светлый керамический постамент, бирюзовая полоса, стеклянный купол, дуга светодиодов под ним. Иконки — как упаковка: пакетик семян, бирюзовый горшок земли, прессованный тюк зелени, топливный картридж. В капсуле куст с красными ягодами.',
};
const ITEM_RU = { seed: ['Семена', 'seed', 'Должно читаться как «семена» при 16 px.'], soil: ['Земля', 'soil', 'Ком тёмной земли.'], biomass: ['Биомасса', 'biomass', 'Срезанная зелень, связанная в пучок.'], biofuel: ['Биотопливо', 'biofuel', 'Янтарно-зелёное топливо. Не похоже на синий кислородный баллон.'] };
const ITEM_DESC = {
  A: { seed: 'боб, расколотый ростком, и второе семя', soil: 'горка земли с комьями и камешком', biomass: 'три листа из пучка стеблей, бечёвка', biofuel: 'круглая колба с пробкой' },
  B: { seed: 'мешочек, завязанный красной бечёвкой, семя выпало', soil: 'раскрытый мешок с горкой земли', biomass: 'сноп стеблей по диагонали, красная перевязь', biofuel: 'оливковая канистра с янтарной крышкой' },
  C: { seed: 'бумажный пакетик: росток над семенем, бирюзовая полоса', soil: 'бирюзовый горшок с землёй', biomass: 'прессованный тюк зелени, две стальные ленты', biofuel: 'топливный картридж: стальные крышки, светящееся окно' },
};

const cur = current();
const curItems = Object.fromEntries(ITEMS.map(it => [it, fileUri(path.join(ART, 'items', it + '.png'))]));
const neighbours = ['canister', 'glass', 'planks', 'rock', 'circuit'].map(n => [fileUri(path.join(ART, 'items', n + '.png')), 16, 16, n]);

// ---- icons ----
// one grid: a row a look, a column an item; each cell the icon at x4, x1 and in an inventory slot
const iconRows = [['cur', 'Сейчас', 'заглушки', it => curItems[it], () => 'заглушка'], ...LOOKS.map(L => [L, NAME[L], SHORT[L], it => uri(icon(L, it)), it => ITEM_DESC[L][it]])];
const icons = `<section class="block igrid" id="icon-grid"><div class="ihead"><span></span>${ITEMS.map(it => `<div><b>${ITEM_RU[it][0]}</b> <code>items/${it}.png</code><p>${esc(ITEM_RU[it][2])}</p></div>`).join('')}</div>` +
  iconRows.map(([id, label, note, src, desc]) => `<div class="irow${id === 'cur' ? ' cur' : ''}"><div class="rl"><b>${esc(label)}</b><span>${esc(note)}</span></div>` +
    ITEMS.map(it => `<div class="cell"><div class="stage">${pic(src(it), 16, 16, 4, ITEM_RU[it][0])}<div class="small">${pic(src(it), 16, 16, 1, ITEM_RU[it][0])}<div class="slotbox">${pic(src(it), 16, 16, 2, ITEM_RU[it][0])}</div></div></div><span class="desc">${esc(desc(it))}</span></div>`).join('') + '</div>').join('') + '</section>';
const lineups = [['cur', 'Сейчас', ITEMS.map(it => [curItems[it], 16, 16, it])], ...LOOKS.map(L => [L, NAME[L], ITEMS.map(it => [uri(icon(L, it)), 16, 16, it])])]
  .map(([id, label, four]) => `<div class="lrow${id === 'cur' ? ' cur' : ''}"><div class="rl"><b>${esc(label)}</b><span>${id === 'cur' ? 'заглушки' : esc(SHORT[id])}</span></div><div class="stage"><span class="tag">×4 · четыре новых, затем баллон, стекло, доски, камень, плата из игры</span>${strip([...four, ...neighbours], 4, 'tight')}</div></div>`).join('');

// ---- capsule ----
const STAGE = ['росток', 'малое', 'листва', 'урожай'];
function capRow(id, label, note, c) {
  const off = [[uri(frame(c, -1, false, { room: 1 })), 32, 48, 'пустая', 'пусто'], ...[0, 1, 2, 3].map(s => [uri(frame(c, s, false, { room: 1 })), 32, 48, STAGE[s], STAGE[s]])];
  const on = [0, 1, 2, 3].map(s => [uri(frame(c, s, true, { room: 0.8 })), 32, 48, STAGE[s] + ', лампа горит', STAGE[s]]);
  return `<div class="crow${id === 'cur' ? ' cur' : ''}"><div class="rl"><b>${esc(label)}</b><span>${esc(note)}</span></div>
  <div class="stage x1"><span class="tag">×1 · лампа выкл / вкл</span>${strip(off.map(a => a.slice(0, 4)), 1)}${strip(on.map(a => a.slice(0, 4)), 1)}</div>
  <div class="stage"><span class="tag">×4 · лампа выключена (пауза, нет энергии или пусто)</span>${strip(off, 4, 'cap')}</div>
  <div class="stage lit"><span class="tag">×4 · лампа горит — растёт; розовый свет в игре даёт Glow, здесь он примерный</span>${strip(on, 4, 'cap')}</div></div>`;
}
const capsules = capRow('cur', 'Сейчас', 'заглушка', cur) + LOOKS.map(L => capRow(L, NAME[L], SHORT[L], caps[L])).join('');
const layers = LOOKS.map(L => {
  const c = caps[L];
  return `<div class="lrow"><div class="rl"><b>${NAME[L]}</b><span>Glow: (${c.light[0]}, ${c.light[1]})</span></div><div class="stage chk"><span class="tag">×3 · файлы как есть</span>${strip([
    [uri(c.base), 32, 48, 'machine_growCapsule', 'machine_growCapsule'], [uri(c.work), 32, 48, 'work_0', 'growCapsule_work_0'],
    ...c.plants.map((p, i) => [uri(p), 32, 48, 'plant_' + i, 'growCapsule_plant_' + i])], 3, 'cap')}</div></div>`;
}).join('');
const scenes = [['cur', 'Сейчас', cur], ...LOOKS.map(L => [L, NAME[L], caps[L]])].map(([id, label, c]) =>
  `<figure class="scene">${pic(uri(scene(c)), SW, SH, 2, 'Комната корабля: печь, плавильня и две капсулы, ' + label)}<figcaption>${esc(label)} · ×2 · печь и плавильня из игры; капсула с листвой без света и капсула с урожаем, лампа горит</figcaption></figure>`).join('');

// ---- soil clod ----
const D = path.join(ART, 'debris');
const refs = [['asteroid_01', 32, 32], ['smallrock_01', 24, 24], ['crate_01', 18, 16], ['pod_01', 16, 18]].map(([n, w, h]) => [fileUri(path.join(D, n + '.png')), w, h, n]);
const CHUNK_RU = ['круглый ком, росток сверху, корни из рваного низа', 'ком в обломке бирюзового лотка, растение побольше со стручком', 'длинный ком, растение повёрнуто вбок, в него воткнута полоска стали'];
const chunkSrc = chunks.map(p => uri(p));
const clods = `<div class="crow"><div class="rl"><b>Ком земли</b><span>32×32 · <code>soilclod_0N.png</code></span></div>
  <div class="stage x1"><span class="tag">×1 · рядом астероид, камешек, ящик, капсула снабжения</span>${strip([...chunkSrc.map((s, i) => [s, 32, 32, 'вариант ' + (i + 1)]), ...refs], 1)}</div>
  <div class="stage"><span class="tag">×4</span>${strip(chunkSrc.map((s, i) => [s, 32, 32, 'вариант ' + (i + 1), (i + 1) + ': ' + CHUNK_RU[i]]), 4, 'cap')}</div></div>`;

// ---- picks ----
const radios = (group, opts) => `<fieldset class="pickrow" data-group="${group}"><legend>${esc(group)}:</legend>${opts.map(([v, label], i) => `<label><input type="radio" name="pick-${esc(group)}" value="${esc(v)}"${i === 0 ? ' checked' : ''}> ${esc(label)}</label>`).join('')}</fieldset>`;
const looksOpts = [['', 'не решил'], ['оставить как есть', 'оставить'], ['A', 'A'], ['B', 'B'], ['C', 'C']];
const picks = radios('Иконки', looksOpts) + radios('Капсула', looksOpts) +
  radios('Ком земли', [['', 'не решил'], ['вариант 1', '1'], ['вариант 2', '2'], ['вариант 3', '3'], ['все три', 'все три'], ['не нужен', 'не нужен']]);

const styleCards = LOOKS.map(L => `<li><b>${NAME[L]}</b><span class="en">${esc(SHORT[L])}</span><p>${esc(ABOUT[L])}</p></li>`).join('');

const html = `<!doctype html>
<html lang="ru">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>Farm Art Concepts</title>
<meta name="description" content="Stillwake: иконки семян, земли, биомассы и биотоплива, капсула для выращивания в четырёх стадиях и ком земли с ростком — три варианта рядом с заглушками.">
<style>
:root {
  --page: #0b0e14; --bench: #111823; --line: #253042; --ink: #e3e9f0; --muted: #8b99aa; --s300: #cbd5e1; --s400: #94a3b8; --s500: #64748b;
  --amber: #f2b84b; --pink: #ff7ad1; --stage: #10151d; --mono: Consolas, Menlo, ui-monospace, monospace; --ui: "Segoe UI", system-ui, sans-serif; color-scheme: dark;
}
* { box-sizing: border-box; }
html { background: var(--page); }
body { margin: 0; background: var(--page); color: var(--ink); font: 15px/1.5 var(--ui); padding: 28px 16px 64px; overflow-x: hidden; }
.wrap { max-width: 1240px; margin: 0 auto; display: grid; gap: 18px; }
.eyebrow { font: 600 12px var(--mono); letter-spacing: .12em; text-transform: uppercase; color: var(--amber); }
h1 { font: 600 30px/1.15 var(--ui); margin: 4px 0 8px; }
header p { margin: 0 0 8px; color: var(--muted); max-width: 92ch; }
.rules { list-style: none; padding: 0; margin: 12px 0 0; display: grid; grid-template-columns: repeat(auto-fit, minmax(260px, 1fr)); gap: 8px; }
.rules li { background: var(--bench); border: 1px solid var(--line); border-radius: 6px; padding: 10px 12px; font-size: 13px; color: var(--s300); }
.rules b { font: 600 14px var(--ui); color: var(--ink); margin-right: 8px; } .rules .en { font: 11px var(--mono); color: var(--s500); }
.rules p { margin: 6px 0 0; }
.bar { position: sticky; top: 0; z-index: 5; display: flex; flex-wrap: wrap; gap: 6px 14px; background: rgba(11, 14, 20, .94); border-bottom: 1px solid var(--line); padding: 10px 0; backdrop-filter: blur(4px); font-size: 13px; }
.bar a { color: var(--s400); text-decoration: none; } .bar a:hover { color: var(--ink); }
h2.group { font: 600 22px var(--ui); margin: 14px 0 0; padding-top: 8px; border-top: 1px solid var(--line); scroll-margin-top: 56px; }
.lead { margin: 0; color: var(--s300); font-size: 14px; max-width: 100ch; }
.kind, .block { background: var(--bench); border: 1px solid var(--line); border-radius: 8px; padding: 14px; display: grid; gap: 10px; scroll-margin-top: 56px; min-width: 0; }
.khead { display: flex; flex-wrap: wrap; gap: 4px 14px; align-items: baseline; }
.khead h3 { margin: 0; font: 600 18px var(--ui); }
.dim { color: var(--s500); font: 12px var(--mono); } code { font: 12px var(--mono); color: var(--s300); }
.about { margin: 0; color: var(--s300); font-size: 14px; }
.irow, .crow, .lrow { display: grid; gap: 6px; padding: 7px 0; border-top: 1px solid rgba(37, 48, 66, .6); min-width: 0; }
.irows > :first-child, .block > .crow:first-of-type, .block > .lrow:first-of-type { border-top: 0; }
.igrid .ihead, .igrid .irow { display: grid; grid-template-columns: 130px repeat(4, minmax(0, 1fr)); gap: 10px; align-items: stretch; }
.ihead { font-size: 13px; color: var(--s300); } .ihead p { margin: 2px 0 0; font-size: 12px; color: var(--s500); }
.cell { display: grid; gap: 4px; align-content: start; min-width: 0; }
.cell .stage { display: flex; align-items: center; gap: 14px; padding: 10px 12px; }
.cell img { image-rendering: pixelated; display: block; }
.small { display: grid; gap: 8px; justify-items: center; }
.desc { font: 11px var(--mono); color: var(--s500); }
.crow, .lrow { grid-template-columns: 150px minmax(0, 1fr); }
.crow > .stage:not(.x1), .crow > .x1 { grid-column: 2; }
.cur { opacity: .9; } .cur .rl b { color: var(--s400); }
.rl { display: grid; align-content: center; gap: 2px; padding: 0 4px; } .rl b { font: 600 14px var(--ui); } .rl span { font: 11px var(--mono); color: var(--s500); }
.stage { position: relative; border-radius: 4px; padding: 18px 12px 10px; background: var(--stage); min-width: 0; overflow-x: auto; }
.stage.lit { background: #0c0f15; }
.stage.chk { background-color: #1a2029; background-image: linear-gradient(45deg, #141a22 25%, transparent 25%, transparent 75%, #141a22 75%), linear-gradient(45deg, #141a22 25%, transparent 25%, transparent 75%, #141a22 75%); background-size: 12px 12px; background-position: 0 0, 6px 6px; }
.tag { position: absolute; top: 3px; left: 8px; font: 10px var(--mono); color: var(--s500); white-space: nowrap; }
.strip { display: flex; flex-wrap: wrap; gap: 10px 14px; align-items: flex-end; }
.strip + .strip { margin-top: 8px; }
.strip.tight { gap: 8px; }
.strip img { image-rendering: pixelated; display: block; max-width: 100%; height: auto; }
.strip figure { margin: 0; display: grid; gap: 4px; justify-items: center; max-width: 100%; }
.strip figcaption { font: 11px var(--mono); color: var(--s400); max-width: 128px; text-align: center; }
.slotbox { width: 44px; height: 44px; display: grid; place-items: center; background: #1b2230; border: 2px solid #2c3648; border-radius: 4px; }
.scenes { display: grid; gap: 12px; grid-template-columns: repeat(auto-fit, minmax(min(100%, 520px), 1fr)); }
.scene { margin: 0; background: #000; border-radius: 6px; overflow: hidden; border: 1px solid var(--line); }
.scene img { display: block; width: 100%; height: auto; image-rendering: pixelated; }
.scene figcaption { font: 12px var(--mono); color: var(--s400); padding: 6px 12px 8px; }
.answer { background: var(--bench); border: 1px solid var(--line); border-radius: 8px; padding: 16px; display: grid; gap: 12px; }
.answer h2 { margin: 0; font: 600 20px var(--ui); }
.answer p { margin: 0; color: var(--s300); max-width: 95ch; }
.pickrow { border: 0; margin: 0; padding: 0; display: flex; flex-wrap: wrap; gap: 4px 16px; font-size: 14px; color: var(--s300); }
.pickrow legend { float: left; margin-right: 12px; min-width: 90px; color: var(--ink); font-weight: 600; }
.pickrow label { display: inline-flex; gap: 5px; align-items: center; cursor: pointer; }
.pickrow input { accent-color: var(--amber); margin: 0; }
.pick { display: flex; align-items: stretch; border: 1px solid var(--line); border-radius: 6px; overflow: hidden; max-width: 100%; }
.pick code { font: 14px var(--mono); padding: 9px 12px; background: var(--page); color: var(--ink); user-select: all; flex: 1; min-width: 0; overflow-wrap: anywhere; }
.pick button { font: 600 13px var(--ui); border: 0; background: var(--amber); color: #1a1405; padding: 0 16px; cursor: pointer; }
.note { font-size: 13px; color: var(--s400); }
@media (max-width: 760px) {
  .igrid .ihead { display: none; } .igrid .irow { grid-template-columns: repeat(2, minmax(0, 1fr)); } .igrid .irow .rl { grid-column: 1 / -1; }
  .crow, .lrow { grid-template-columns: minmax(0, 1fr); } .crow > .stage:not(.x1), .crow > .x1 { grid-column: 1; }
  .rl { grid-auto-flow: column; justify-content: start; gap: 8px; }
}
@media (max-width: 560px) { .igrid .irow { grid-template-columns: minmax(0, 1fr); } .tag { white-space: normal; right: 8px; line-height: 1.2; } .stage { padding-top: 30px; } .cell .stage { padding-top: 10px; } }
</style>
</head>
<body>
<div class="wrap">
<header>
  <div class="eyebrow">Stillwake · ферма · концепт-арт</div>
  <h1>Ферма</h1>
  <p>Картинки вместо заглушек фермы (ADR 0060): четыре иконки предметов, капсула для выращивания 2×3 с четырьмя стадиями растения и лампой, и на будущее — новый обломок, ком земли с ростком. Три варианта стиля рядом с нынешними заглушками, ×1 и ×4. Пиксель картинки — пиксель мира, свет сверху слева, тёмный контур, палитра из готовой графики игры.</p>
  <p>Файлы лежат в <code>farm-art/out/&lt;вариант&gt;/</code> под именами игры (<code>items/seed.png</code>, <code>machine_growCapsule.png</code>, <code>growCapsule_work_0.png</code>, <code>growCapsule_plant_0..3.png</code>) — выбранный набор копируется в <code>game/art/</code> как есть. Капсула собирается так же, как сейчас в <code>StationView</code>: машина, поверх растение на весь размер (блики стекла повторены поверх листвы), поверх — горящая лампа без освещения. Меняется только точка света лампы: она подписана у каждого варианта.</p>
  <ul class="rules">${styleCards}</ul>
</header>
<nav class="bar"><a href="#icons">Иконки</a><a href="#lineup">Иконки рядом с игрой</a><a href="#capsule">Капсула</a><a href="#layers">Файлы капсулы</a><a href="#context">В контексте</a><a href="#clod">Ком земли</a><a href="#answer">Ответ</a></nav>

<h2 class="group" id="icons">Иконки</h2>
<p class="lead">Строка — вариант, столбец — предмет: ×4, ×1 и в слоте инвентаря.</p>
${icons}
<section class="block" id="lineup"><div class="khead"><h3>Рядом с иконками игры</h3><span class="dim">выдерживается ли стиль в ряду</span></div>${lineups}</section>

<h2 class="group" id="capsule">Капсула для выращивания</h2>
<p class="lead">32×48. Пустая и четыре стадии — росток, малое, листва, урожай (на урожае видны плоды) — с выключенной лампой; затем те же стадии с горящей лампой. Растение на паузе (нет энергии) выглядит как ряд «выключено».</p>
<section class="block">${capsules}</section>
<section class="block" id="layers"><div class="khead"><h3>Файлы капсулы по отдельности</h3><span class="dim">машина · лампа (рисуется без освещения) · растение 0..3, на клетчатом фоне — прозрачность</span></div>
<p class="about">В кадр лампы входит и розовый огонёк на панели — горит, пока капсула растит. Точку света лампы (<code>Lights</code> в <code>StationView.Moving["growCapsule"]</code>) нужно поставить ту, что подписана слева; сейчас там (16, 9).</p>${layers}</section>

<h2 class="group" id="context">В контексте</h2>
<p class="lead">Кусок комнаты в ×2 из плиток игры: печь и плавильня, капсула с листвой без света и капсула с урожаем под горящей лампой. Свет игры здесь примерный: комната притушена, розовое пятно нарисовано мягким кругом.</p>
<div class="scenes">${scenes}</div>

<h2 class="group" id="clod">Ком земли с ростком — новый обломок, на потом</h2>
<p class="lead">Ком земли, вырванный с фермы чужого корабля, дрейфует с растением. 32×32, три картинки в одном стиле, контур и свет как у обломков игры. В игре такого вида пока нет — это только концепт.</p>
<section class="block">${clods}</section>

<section class="answer" id="answer">
  <h2>Как ответить</h2>
  <p>Выберите по группе — строка соберётся сама. Можно смешать или дописать, что поправить: «иконки из B, но биотопливо из A», «плоды крупнее», «стекло без хомутов».</p>
  ${picks}
  <div class="pick"><code id="line">Ничего не выбрано</code><button type="button" id="copy">Копировать</button></div>
  <p class="note">Пример: Иконки: B · Капсула: A · Ком земли: вариант 2</p>
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
console.log('concepts.html', (Buffer.byteLength(html) / 1024).toFixed(1), 'KB');
