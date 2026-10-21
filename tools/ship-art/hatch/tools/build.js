// node build.js : writes out/<id>/hatch_{closed,open}_{0,2,8,10}.png for every concept and concepts.html
const fs = require('fs'), path = require('path');
const { Pic, img, blit, savePng, loadPng } = require('./lib.js');
const { encode } = require('./png.js');
const { CONCEPTS, MASKS, ART } = require('./hatch.js');

const ROOT = path.join(__dirname, '..');
const IDS = Object.keys(CONCEPTS);
const C = 16, TOP = 0;
const L = n => loadPng(ART + n + '.png');
const piece = (id, open, mask, top = TOP) => CONCEPTS[id].piece(open, mask, top);

for (const id of IDS) {
  const dir = path.join(process.argv[2] || path.join(ROOT, 'out'), id); fs.mkdirSync(dir, { recursive: true });
  for (const open of [false, true]) for (const m of MASKS) piece(id, open, m).save(path.join(dir, `hatch_${open ? 'open' : 'closed'}_${m}.png`));
}

// ---- the scene: two decks of iron, a hatch one cell wide over a ladder, one three wide with the ladder in its middle ----
const SW = 14, SH = 11, DECK = 5, ONE = 3, THREE = [8, 9, 10], LADDERS = [3, 9];
const wallIron = L('wall_iron'), backScrap = L('back_scrap'), edges = [...Array(16).keys()].map(m => (m ? L('wall-edge_iron_' + m) : null));
const ladder = L('ladder'), backPlain = L('back_iron_plain'), backGrille = L('back_iron_grille');
function cut(src, cx, cy) { const p = img(C, C); for (let y = 0; y < C; y++) for (let x = 0; x < C; x++) { const s = ((cy * C + y) * src.w + cx * C + x) * 4; p.data.set(src.data.subarray(s, s + 4), (y * C + x) * 4); } return p; }
const hashOf = cell => { let h = Math.imul((cell ^ 0x9e3779b9) >>> 0, 0x85ebca6b) >>> 0; h ^= h >>> 13; h = Math.imul(h, 0xc2b2ae35) >>> 0; return (h ^ (h >>> 16)) >>> 0; };
function layout() {
  const s = []; for (let y = 0; y < SH; y++) { s.push([]); for (let x = 0; x < SW; x++) s[y].push('.'); }
  for (let x = 0; x < SW; x++) { s[0][x] = '#'; s[SH - 1][x] = '#'; s[DECK][x] = '#'; }
  for (let y = 0; y < SH; y++) { s[y][0] = '#'; s[y][SW - 1] = '#'; }
  for (let y = 2; y <= 4; y++) s[y][SW - 1] = 'D';
  s[DECK][ONE] = 'h'; for (const x of THREE) s[DECK][x] = 'h';
  for (const x of LADDERS) for (let y = 1; y < SH - 1; y++) if (y !== DECK) s[y][x] = 'H';
  return s;
}
const MAP = layout();
const at = (x, y) => (x < 0 || y < 0 || x >= SW || y >= SH ? '#' : MAP[y][x]);
function scene(id, open, top = TOP) {
  const im = img(SW * C, SH * C, '#0d1016');
  for (let y = 0; y < SH; y++) for (let x = 0; x < SW; x++) {
    const c = at(x, y), X = x * C, Y = y * C;
    if (c !== '#') {
      // the upper deck's back wall is scrap, the lower's iron plates; behind the hatch, iron
      if (y < DECK) blit(im, cut(backScrap, x % 8, y % 4), X, Y);
      else blit(im, hashOf((y << 16) | x) % 7 === 0 && y > DECK ? backGrille : backPlain, X, Y);
    }
  }
  for (let y = 0; y < SH; y++) for (let x = 0; x < SW; x++) {
    if (at(x, y) !== '#') continue;
    const X = x * C, Y = y * C;
    blit(im, cut(wallIron, x % 4, y % 4), X, Y);
    const open = (at(x, y - 1) !== '#' ? 1 : 0) | (at(x + 1, y) !== '#' ? 2 : 0) | (at(x, y + 1) !== '#' ? 4 : 0) | (at(x - 1, y) !== '#' ? 8 : 0);
    if (open) blit(im, edges[open], X, Y);
  }
  for (let y = 0; y < SH; y++) for (let x = 0; x < SW; x++) {
    const c = at(x, y), X = x * C, Y = y * C;
    if (c === 'H') blit(im, ladder, X, Y);
    if (c === 'D') blit(im, L('door_closed_' + ((at(x, y - 1) === 'D' ? 1 : 0) | (at(x, y + 1) === 'D' ? 4 : 0))), X, Y);
    if (c === 'h') blit(im, piece(id, open, (at(x + 1, y) === 'h' ? 2 : 0) | (at(x - 1, y) === 'h' ? 8 : 0), top), X, Y);
  }
  return im;
}
function crop(im, cx, cy, cw, ch) {
  const o = img(cw * C, ch * C);
  for (let y = 0; y < ch * C; y++) { const s = ((cy * C + y) * im.w + cx * C) * 4; o.data.set(im.data.subarray(s, s + cw * C * 4), y * cw * C * 4); }
  return o;
}
const cropOne = im => crop(im, ONE - 1, DECK - 1, 3, 3), cropThree = im => crop(im, THREE[0] - 1, DECK - 1, 5, 3);

// ---- the page ----
const uri = im => 'data:image/png;base64,' + Buffer.from(encode(im.w, im.h, (im.rgba ? im.rgba() : im).data)).toString('base64');
const esc = s => String(s).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
const pic = (im, k, alt) => { const r = im.rgba ? im.rgba() : im; return `<img src="${uri(r)}" width="${r.w * k}" height="${r.h * k}" alt="${esc(alt)}">`; };
const fig = (im, k, cap, cls = '') => `<figure class="${cls}">${pic(im, k, cap)}<figcaption>${esc(cap)}</figcaption></figure>`;

const INFO = {
  A: { name: 'Дверь на боку', short: 'ближе всего к двери',
    about: 'Нынешняя дверь, повёрнутая на 90°: те же пять оранжевых тонов, рамка #503117, свет сверху слева. Окошко-иллюминатор — на левом конце, жёлто-чёрная полоса — на правом, шов посередине каждой клетки, как у двери. Открытый люк тоже повторяет открытую дверь: две тонкие направляющие, край уехавшей панели слева и защёлки справа. Игрок, видевший дверь, узнает люк сразу.' },
  B: { name: 'Стальная задвижка', short: 'полоса по краю, стрелка',
    about: 'Тёмная воронёная плита, по верхней кромке, по которой ходят, — узкая жёлто-чёрная полоса, как разметка края. Слева утопленная ручка, справа жёлтая стрелка влево: куда панель уедет. Между секциями тонкий шов. Спокойнее двери и не спорит со стенами, но на сером железе читается в основном полосой.' },
  C: { name: 'Тяжёлый люк', short: 'красный штурвал, засов',
    about: 'Толстая стальная плита с двойной фаской на концах, заклёпками и красным штурвалом — тем же красным, что у закрытого вентиля. Справа засов. Читается как «гермолюк»: штурвал сразу говорит, что это открывается. В открытом виде край уехавшей плиты толще (4 px) и на нём видна красная кромка штурвала.' },
  D: { name: 'Решётка', short: 'видно комнату насквозь',
    about: 'Оранжевая рамка двери, внутри стальные прутья через пиксель; между прутьями прозрачно, так что видно заднюю стену и свет соседней палубы. Сразу видно, что это проём, а не пол. Минус: закрытая решётка выглядит проницаемой, а люк держит воздух, как дверь; игрок может решить, что воздух сквозь неё идёт.' },
  E: { name: 'Опасная зона', short: 'вся в жёлто-чёрную полосу',
    about: 'Вся панель в диагональную жёлто-чёрную полосу, рамка тёмной стали, светлая кромка слева. Самый заметный вариант: люк виден с другого конца корабля. Полоса идёт непрерывно через стыки, так что широкий люк — одна полоса. Минус: громко; на корабле с десятью люками полос станет много, а дверь полосу носит только внизу.' },
};

function concept(id) {
  const I = INFO[id];
  const closed = scene(id, false), opened = scene(id, true);
  const files = [false, true].flatMap(open => MASKS.map(m => [piece(id, open, m), `${open ? 'open' : 'closed'}_${m}`]));
  return `<section class="kind" id="c${id}">
  <div class="khead"><h3><span class="cid">${id}</span>${esc(I.name)}</h3><span class="dim">${esc(I.short)} · out/${id}/</span></div>
  <p class="about">${esc(I.about)}</p>
  <div class="crow"><div class="rl"><b>×2, как в игре</b><span>зум 2 · без освещения</span></div>
    <div class="stage scenes">${fig(closed, 2, 'закрыт: люк в 1 клетку слева, в 3 справа')}${fig(opened, 2, 'открыт')}</div></div>
  <div class="crow"><div class="rl"><b>×6, крупно</b><span>кусок той же сцены</span></div>
    <div class="stage strip">${fig(cropOne(closed), 6, '1 клетка · закрыт')}${fig(cropOne(opened), 6, '1 клетка · открыт')}${fig(cropThree(closed), 6, '3 клетки · закрыт')}${fig(cropThree(opened), 6, '3 клетки · открыт')}</div></div>
  <div class="crow"><div class="rl"><b>Файлы</b><span>×5 · клетка = прозрачно</span></div>
    <div class="stage chk strip">${files.map(([p, n]) => fig(p, 5, n)).join('')}</div></div>
</section>`;
}

// the door as it is, for reference, and the placement comparison
const doorPieces = [['door_closed_4', 'верх'], ['door_closed_5', 'середина'], ['door_closed_1', 'низ'], ['door_open_4', 'открыта, верх'], ['door_open_1', 'открыта, низ']];
const flushA = cropOne(scene('A', false, 0)), centredA = cropOne(scene('A', false, 3));
const flushAo = cropOne(scene('A', true, 0)), centredAo = cropOne(scene('A', true, 3));

const radio = (group, name, opts) => `<fieldset class="pickrow" data-group="${esc(group)}"><legend>${esc(group)}</legend>${opts.map(([v, label], i) => `<label><input type="radio" id="${name}-${i}" name="${name}" value="${esc(v)}"${i === 0 ? ' checked' : ''}> ${esc(label)}</label>`).join('')}</fieldset>`;
const picks = radio('Люк', 'pick-hatch', [['', 'не решил'], ...IDS.map(id => [id + ' ' + INFO[id].name.toLowerCase(), id + ' — ' + INFO[id].name.toLowerCase()]), ['ни один', 'ни один']]) +
  radio('Положение', 'pick-place', [['', 'как нарисовано (заподлицо)'], ['по центру клетки, как дверь', 'по центру клетки, как дверь']]);

const html = `<title>Stillwake Hatch Concepts</title>
<meta name="claude:viewport-fit-sides" content="cover">
<style>
/* layout: one column of concept benches, each a row-labelled grid of stages; a pick bar at the end */
:root {
  --page: #0b0e14; --bench: #111823; --line: #253042; --ink: #e3e9f0; --s300: #cbd5e1; --s400: #94a3b8; --s500: #64748b;
  --door: #c97a3a; --door-lit: #d59765; --stage: #10151d;
  --mono: Consolas, Menlo, ui-monospace, monospace; --ui: "Segoe UI", system-ui, sans-serif;
  color-scheme: dark;
}
* { box-sizing: border-box; }
body { margin: 0; background: var(--page); color: var(--ink); font: 15px/1.5 var(--ui); padding-block: 28px 64px; padding-left: max(16px, env(safe-area-inset-left, 0px)); padding-right: max(16px, env(safe-area-inset-right, 0px)); overflow-x: hidden; }
.wrap { max-width: 1240px; margin: 0 auto; display: grid; gap: 18px; }
.eyebrow { font: 600 12px var(--mono); letter-spacing: .12em; text-transform: uppercase; color: var(--door-lit); }
h1 { font: 600 30px/1.15 var(--ui); margin: 4px 0 8px; text-wrap: balance; }
header p { margin: 0 0 8px; color: var(--s300); max-width: 92ch; }
.bar { position: sticky; top: env(safe-area-inset-top, 0px); z-index: 5; display: flex; flex-wrap: wrap; gap: 6px 14px; background: rgba(11, 14, 20, .94); border-bottom: 1px solid var(--line); padding: 10px 0; font-size: 13px; }
.bar a { color: var(--s400); text-decoration: none; } .bar a:hover, .bar a:focus-visible { color: var(--ink); }
h2.group { font: 600 22px var(--ui); margin: 14px 0 0; padding-top: 8px; border-top: 1px solid var(--line); scroll-margin-top: 56px; }
.lead { margin: 0; color: var(--s300); font-size: 14px; max-width: 100ch; }
.kind, .block { background: var(--bench); border: 1px solid var(--line); border-radius: 8px; padding: 14px; display: grid; gap: 10px; scroll-margin-top: 56px; min-width: 0; }
.khead { display: flex; flex-wrap: wrap; gap: 4px 14px; align-items: baseline; }
.khead h3 { margin: 0; font: 600 18px var(--ui); display: flex; gap: 10px; align-items: baseline; }
.cid { font: 700 13px var(--mono); color: var(--page); background: var(--door); border-radius: 3px; padding: 1px 7px; }
.dim { color: var(--s500); font: 12px var(--mono); } code { font: 12px var(--mono); color: var(--s300); }
.about { margin: 0; color: var(--s300); font-size: 14px; max-width: 110ch; }
.crow { display: grid; grid-template-columns: 150px minmax(0, 1fr); gap: 6px 10px; padding: 7px 0; border-top: 1px solid rgba(37, 48, 66, .6); min-width: 0; }
.rl { display: grid; align-content: center; gap: 2px; padding: 0 4px; } .rl b { font: 600 14px var(--ui); } .rl span { font: 11px var(--mono); color: var(--s500); }
.stage { border-radius: 4px; padding: 12px; background: var(--stage); min-width: 0; overflow-x: auto; display: flex; flex-wrap: wrap; gap: 12px 16px; align-items: flex-end; }
.stage.chk { background-color: #1a2029; background-image: linear-gradient(45deg, #141a22 25%, transparent 25%, transparent 75%, #141a22 75%), linear-gradient(45deg, #141a22 25%, transparent 25%, transparent 75%, #141a22 75%); background-size: 10px 10px; background-position: 0 0, 5px 5px; }
.stage img { image-rendering: pixelated; display: block; max-width: 100%; height: auto; }
.stage figure { margin: 0; display: grid; gap: 4px; justify-items: start; max-width: 100%; min-width: 0; }
.stage figcaption { font: 11px var(--mono); color: var(--s400); }
.notes { background: var(--bench); border: 1px solid var(--line); border-radius: 8px; padding: 14px 18px; color: var(--s300); font-size: 14px; }
.notes ul { margin: 6px 0; padding-left: 22px; } .notes li { margin: 4px 0; max-width: 105ch; }
.answer { background: var(--bench); border: 1px solid var(--door); border-radius: 8px; padding: 16px; display: grid; gap: 12px; scroll-margin-top: 56px; }
.answer h2 { margin: 0; font: 600 20px var(--ui); }
.answer p { margin: 0; color: var(--s300); max-width: 95ch; }
.pickrow { border: 0; margin: 0; padding: 0; display: flex; flex-wrap: wrap; gap: 4px 16px; font-size: 14px; color: var(--s300); }
.pickrow legend { float: left; margin-right: 12px; min-width: 96px; color: var(--ink); font-weight: 600; }
.pickrow label { display: inline-flex; gap: 6px; align-items: center; cursor: pointer; }
.pickrow input { accent-color: var(--door); margin: 0; }
.answer label.ta { display: grid; gap: 6px; font-weight: 600; font-size: 14px; }
textarea { font: 14px/1.45 var(--ui); color: var(--ink); background: var(--page); border: 1px solid var(--line); border-radius: 6px; padding: 8px 10px; min-height: 64px; resize: vertical; width: 100%; }
textarea:focus-visible, button:focus-visible, input:focus-visible { outline: 2px solid var(--door-lit); outline-offset: 2px; }
.pick { display: flex; align-items: stretch; border: 1px solid var(--line); border-radius: 6px; overflow: hidden; max-width: 100%; }
.pick code { font: 14px var(--mono); padding: 9px 12px; background: var(--page); color: var(--ink); user-select: all; flex: 1; min-width: 0; overflow-wrap: anywhere; white-space: pre-wrap; }
.pick button { font: 600 13px var(--ui); border: 0; background: var(--door); color: #1b0f05; padding: 0 16px; cursor: pointer; }
@media (max-width: 760px) { .crow { grid-template-columns: minmax(0, 1fr); } .rl { grid-auto-flow: column; justify-content: start; gap: 8px; } }
</style>
<div class="wrap">
<header>
  <div class="eyebrow">Stillwake · люк · концепт-арт</div>
  <h1>Люк в палубе</h1>
  <p>Люк — дверь, положенная в пол между палубами. Клетка та же, 16×16, пиксель картинки — пиксель мира; панель толщиной 10 px, как дверь шириной. Куски по стыкам слева и справа, по маскам игры (<code>Right 2</code>, <code>Left 8</code>): <code>0</code> один, <code>2</code> левый конец, <code>10</code> середина, <code>8</code> правый конец — для закрытого и открытого. Открытый люк — панель уехала влево в раму, клетка свободна для лестницы.</p>
  <p>Панель нарисована <b>заподлицо с полом</b> (строки 0–9 клетки), а не по центру, как дверь (3–12): на закрытом люке стоят, и ноги героя должны касаться его верха. Сравнение — в конце, там же выбор.</p>
</header>
<nav class="bar"><a href="#door">Дверь сейчас</a>${IDS.map(id => `<a href="#c${id}">${id} — ${esc(INFO[id].name.toLowerCase())}</a>`).join('')}<a href="#place">Положение</a><a href="#answer">Ответ</a></nav>

<h2 class="group" id="door">Дверь сейчас</h2>
<section class="block">
  <div class="crow"><div class="rl"><b>Куски двери</b><span>×5 · door_*.png</span></div>
    <div class="stage chk">${doorPieces.map(([n, cap]) => fig(L(n), 5, cap)).join('')}</div></div>
  <p class="about">Дверь в три клетки: рамка и окошко сверху, шов в каждой клетке, жёлто-чёрная полоса внизу. Открытая — две направляющие и край панели, уехавшей в потолок. В сценах ниже она стоит в правой стене верхней палубы для сравнения.</p>
</section>

<h2 class="group" id="concepts">Варианты</h2>
<p class="lead">В каждом: сцена из плиток игры (железные стены и их кромки, задние стены, лестница, дверь) в ×2, крупные куски той же сцены в ×6 и файлы по отдельности. Сцена без освещения: в игре комнаты темнее.</p>
${IDS.map(concept).join('\n')}

<h2 class="group" id="place">Положение панели</h2>
<section class="block">
  <div class="crow"><div class="rl"><b>Заподлицо</b><span>строки 0–9 · так нарисовано</span></div>
    <div class="stage">${fig(flushA, 6, 'A · закрыт')}${fig(flushAo, 6, 'A · открыт')}</div></div>
  <div class="crow"><div class="rl"><b>По центру</b><span>строки 3–12 · как дверь</span></div>
    <div class="stage">${fig(centredA, 6, 'A · закрыт')}${fig(centredAo, 6, 'A · открыт')}</div></div>
  <p class="about">Пол — верх клетки стены, по нему герой и ходит. По центру люк выходит утопленным на 3 px: герой на закрытом люке висит над ним в воздухе, а снизу под люком видна полоска задней стены. Заподлицо верх люка продолжает пол, а пустота под панелью видна только снизу, как проём рамы. Любой вариант перерисовывается в любое положение одним числом (<code>top</code> в <code>tools/hatch.js</code>).</p>
</section>

<section class="notes">
  <ul>
    <li><b>Файлы.</b> <code>hatch-art/out/&lt;вариант&gt;/hatch_closed_{0,2,8,10}.png</code> и <code>hatch_open_*</code>. Маска как у двери, только по горизонтали: <code>linked &amp; (Left | Right)</code>. Если игре нужны все 16 файлов, как у двери (<code>door_*_0..15</code>), остальные копируются по <code>mask &amp; 10</code>.</li>
    <li><b>Свет.</b> <code>LightView</code> берёт полосу двери из непрозрачных столбцов <code>door_closed_0</code>; для люка это строки <code>hatch_closed_0</code>: 0–9 заподлицо или 3–12 по центру.</li>
    <li><b>Чего не рисуем.</b> Лестницу внутри клетки люка: в клетке одна структура, лестница кончается у люка и продолжается за ним. Анимацию открытия и лампочку состояния: дверь их не имеет, и открытый люк и так виден по пустой клетке.</li>
  </ul>
</section>

<section class="answer" id="answer">
  <h2>Как ответить</h2>
  <p>Выберите — строка соберётся сама. Правки допишите в поле: «C, но штурвал меньше», «A с решёткой из D», «E только на концах».</p>
  ${picks}
  <label class="ta" for="notes">Правки<textarea id="notes" placeholder="Например: B, но стрелку убрать"></textarea></label>
  <div class="pick"><code id="line">Ничего не выбрано</code><button type="button" id="copy">Копировать</button></div>
</section>
</div>
<script>
(function () {
  var line = document.getElementById('line'), notes = document.getElementById('notes');
  function build() {
    var parts = [];
    document.querySelectorAll('.pickrow').forEach(function (f) {
      var c = f.querySelector('input:checked');
      if (c && c.value) parts.push(f.getAttribute('data-group') + ': ' + c.value);
    });
    var n = notes.value.trim();
    if (n) parts.push('Правки: ' + n);
    line.textContent = parts.length ? 'Люк · ' + parts.join(' · ') : 'Ничего не выбрано';
  }
  document.addEventListener('change', function (e) { if (e.target.matches('.pickrow input')) build(); });
  notes.addEventListener('input', build);
  build();
  document.getElementById('copy').addEventListener('click', function () {
    var b = this, text = line.textContent;
    var done = function () { b.textContent = 'Скопировано'; setTimeout(function () { b.textContent = 'Копировать'; }, 1400); };
    var fail = function () { var r = document.createRange(); r.selectNodeContents(line); var s = window.getSelection(); s.removeAllRanges(); s.addRange(r); b.textContent = 'Выделено — Ctrl+C'; };
    try { navigator.clipboard.writeText(text).then(done, fail); } catch (e) { fail(); }
  });
})();
</script>
`;
fs.writeFileSync(path.join(ROOT, 'concepts.html'), html);
fs.mkdirSync(path.join(ROOT, 'tmp'), { recursive: true });
for (const id of IDS) {
  const both = img(SW * C * 2 + 8, SH * C, '#000');
  blit(both, scene(id, false), 0, 0); blit(both, scene(id, true), SW * C + 8, 0);
  savePng(path.join(ROOT, 'tmp', 'scene_' + id + '.png'), both);
}
console.log('concepts.html', (Buffer.byteLength(html) / 1024).toFixed(1), 'KB');
