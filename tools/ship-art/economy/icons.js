// The economy's new items, three variants each, on the part icons' canvas (outline, light from the upper left).
const { Icon, P, OUT, HI } = require('../part-icons/art.js');

// palettes beyond art.js: light, mid, shade, deep
Object.assign(P, {
  ore:   ['#77737a', '#59565c', '#403d44', '#2b292f'],
  hem:   ['#b86a52', '#8c4a38', '#663327', '#45211a'],
  rock:  ['#aaa69c', '#837f76', '#5f5c55', '#423f3a'],
  rockC: ['#9aa1a8', '#757c84', '#545a62', '#3a3f46'],
  rockW: ['#b3a690', '#8c806b', '#675d4d', '#463f35'],
  qtz:   ['#ffffff', '#e9e7f1', '#c6c3d6', '#9995b0'],
  alloy: ['#a6c2c6', '#6e8a93', '#4e6772', '#384c57'],
  hide:  ['#d3a878', '#a97c50', '#7d5734', '#553a22'],
  fat:   ['#fdf6e2', '#f0dfb6', '#d5bd8c', '#a88f62'],
});
const GLINT = '#fbfdff', MET = ['#e4e9ef', '#aab4c0', '#6b7788'], INLAY = ['#bff7ee', '#5fd6c4', '#2f8f86'];

const each = (I, fn) => { for (let y = 0; y < 16; y++) for (let x = 0; x < 16; x++) if (I.mine(x, y)) fn(x, y); };
// a fleck of metal: a bright pixel, its shade to the lower right
const fleck = (I, x, y, big) => { I.paint(x, y, MET[0]); I.paint(x + 1, y + 1, MET[2]); if (big) { I.paint(x + 1, y, MET[1]); I.paint(x, y + 1, MET[1]); } };
// a face-lit lump: faces by where a pixel sits, as rock's angular variant
const faceted = (I, pal, cut) => each(I, (x, y) => I.paint(x, y, P[pal][cut(x, y)]));

const items = [];
const item = (id, name, what) => { const it = { id, name, what, variants: [] }; items.push(it); return it; };
const v = (it, desc, draw) => { const I = new Icon(); draw(I); it.variants.push({ desc, icon: I }); };

// ===== ASTEROID ORE =====
const ore = item('asteroidOre', 'Руда астероида', 'темнее камня, с металлом в нём');
v(ore, 'Тёмный округлый ком, серебряные искры и самородок металла', I => {
  I.obj(); I.poly([[1.5, 9], [3, 4.5], [7, 2], [11.5, 3], [14.5, 7], [14, 12], [10, 14.5], [4, 14]], 'ore'); I.shadeRound(7.5, 8, 7, 'ore', [-0.5, 0.15, 0.6]);
  for (const [x, y] of [[6, 10], [7, 10], [11, 6], [4, 7]]) I.tone(x, y, 'ore', 3);
  fleck(I, 4, 5); fleck(I, 11, 10); fleck(I, 6, 12); fleck(I, 12, 4);
  I.paint(8, 6, MET[1]); I.paint(9, 6, MET[0]); I.paint(8, 7, MET[1]); I.paint(9, 7, MET[1]); I.paint(10, 7, MET[2]); I.paint(9, 8, MET[2]); I.paint(8, 8, MET[2]);
  I.paint(6, 3, GLINT);
});
v(ore, 'Угловатый тёмный обломок, жила металла через грани', I => {
  I.obj(); I.poly([[1, 11], [3, 4], [9, 1.5], [14.5, 5], [15, 11], [9, 15], [3, 14.5]], 'ore');
  faceted(I, 'ore', (x, y) => y < 4 + (x - 3) * 0.1 && x < 12 ? 0 : x < 5 && y < 12 ? 1 : x > 11 ? 2 : y > 11 ? 3 : 1);
  for (const [x, y] of [[4, 9], [5, 9], [6, 8], [7, 8], [8, 7], [9, 7], [10, 6], [11, 6], [12, 5]]) I.paint(x, y, MET[1]);
  for (const [x, y] of [[5, 9], [7, 8], [9, 7], [11, 6]]) I.paint(x, y, MET[0]);
  for (const [x, y] of [[5, 10], [7, 9], [9, 8], [11, 7]]) I.paint(x, y, MET[2]);
  fleck(I, 9, 11); fleck(I, 5, 5);
  I.paint(7, 3, GLINT);
});
v(ore, 'Пористый тёмный обломок, в кратерах блестят капли металла', I => {
  I.obj(); I.disc(8, 8.2, 6.8, 'ore'); I.erase(1, 8); I.erase(14, 4); I.shadeRound(8, 8, 7, 'ore', [-0.5, 0.15, 0.6]);
  for (const [cx, cy, rr] of [[6, 6, 1.6], [10.5, 10, 1.9], [5, 11, 1.1], [11, 5, 1.0]]) each(I, (x, y) => {
    const dx = x + .5 - cx, dy = y + .5 - cy, d = Math.hypot(dx, dy);
    if (d <= rr) I.paint(x, y, dx + dy < 0 ? P.ore[3] : P.ore[2]);
    else if (d <= rr + 0.9 && dx + dy > 0) I.paint(x, y, P.ore[0]);
  });
  fleck(I, 10, 10, true); fleck(I, 6, 6); fleck(I, 3, 4); fleck(I, 12, 7);
  I.paint(4, 3, GLINT);
});

// ===== IRON ORE =====
const iron = item('ironOre', 'Железная руда', 'бурая, ржаво-красная, из дробилки');
v(iron, 'Три ржаво-бурых самородка кучкой, стальной блеск', I => {
  for (const [cx, cy, r] of [[8, 5.8, 3.4], [4.8, 10.4, 3.5], [11.2, 10.6, 3.3]]) {
    I.obj(); I.disc(cx, cy, r, 'hem'); I.shadeRound(cx, cy, r, 'hem', [-0.55, 0.15, 0.6]);
    I.paint(Math.floor(cx - 1), Math.floor(cy - 1), '#d9998a'); I.paint(Math.floor(cx + 1), Math.floor(cy), MET[1]);
  }
});
v(iron, 'Бурый обломок гематита со стальной полосой блеска', I => {
  I.obj(); I.poly([[1.5, 10], [3, 4.5], [8, 2], [13, 3.5], [14.5, 9], [12, 13.5], [6, 14.5], [2.5, 13]], 'hem');
  faceted(I, 'hem', (x, y) => y < 5 && x < 12 ? 0 : x < 4 ? 1 : x > 11 || y > 12 ? 2 : 1);
  for (const [x, y] of [[7, 13], [12, 12], [13, 11]]) I.tone(x, y, 'hem', 3);
  for (const [x, y] of [[5, 7], [6, 7], [7, 8], [8, 8], [9, 9], [10, 9]]) I.paint(x, y, MET[1]);
  I.paint(6, 7, MET[0]); I.paint(8, 8, MET[0]);
  for (const [x, y] of [[10, 5], [4, 11]]) I.paint(x, y, '#d9998a');
  I.paint(6, 3, GLINT);
});
v(iron, 'Почковидная руда: тёмно-красные пузыри с бликами', I => {
  const bumps = [[5, 6, 2.8], [10, 5, 2.9], [3.5, 10.5, 2.6], [8, 10, 3.2], [12.5, 10, 2.6]];
  for (const [cx, cy, r] of bumps) {
    I.obj(); I.disc(cx, cy, r, 'hem'); I.shadeRound(cx, cy, r, 'hem', [-0.7, 0.05, 0.55]);
    I.paint(Math.floor(cx - r * 0.45), Math.floor(cy - r * 0.45), '#e0a594');
  }
});

// ===== GRAVEL =====
const gravel = item('gravel', 'Щебень', 'горсть острых серых камешков');
const pebble = (I, cx, cy, r, pal, k) => {
  I.obj(); I.poly([[cx - r, cy + r * 0.3], [cx - r * 0.4, cy - r], [cx + r * 0.7, cy - r * 0.8], [cx + r, cy + r * 0.2], [cx + r * 0.3, cy + r], [cx - r * 0.7, cy + r * 0.9]].map(([x, y]) => [x + (k % 2) * 0.3, y]), pal);
  I.shade(pal);
};
v(gravel, 'Горка мелких острых камешков, серые разных оттенков', I => {
  const ps = [[8, 6, 2.2, 'rock'], [5, 8, 2.1, 'rockC'], [11, 8, 2.2, 'rock'], [3, 11.5, 2.1, 'rock'], [7.5, 10, 2.3, 'rockC'], [12.5, 11.5, 2.2, 'rockC'], [5.5, 13, 2.1, 'rockC'], [10, 13, 2.2, 'rock']];
  ps.forEach(([x, y, r, p], k) => pebble(I, x, y, r, p, k));
});
v(gravel, 'Четыре крупных колотых камня врассыпную', I => {
  const ps = [[5, 5, 3, 'rockC'], [11.5, 6, 2.6, 'rock'], [4.5, 11.5, 2.7, 'rock'], [10.5, 11.5, 3.1, 'rockC']];
  ps.forEach(([x, y, r, p], k) => { pebble(I, x, y, r, p, k); I.paint(Math.floor(x - r * 0.3), Math.floor(y - r * 0.5), GLINT); });
});
v(gravel, 'Плоская россыпь: тёплый и холодный серый, пара тёмных', I => {
  const ps = [[4, 8.5, 2, 'rockW'], [8, 7.5, 2.1, 'rockC'], [12, 8.5, 2, 'rock'], [2.5, 12, 2, 'rockC'], [6, 11.5, 2.1, 'rock'], [10, 11.5, 2.2, 'rockW'], [13.5, 12, 1.9, 'ore'], [7.5, 13.5, 1.8, 'ore']];
  ps.forEach(([x, y, r, p], k) => pebble(I, x, y, r, p, k));
});

// ===== QUARTZ =====
const qtz = item('quartz', 'Кварц', 'молочно-белые шестигранные кристаллы');
// a prism standing from its foot (x, y), w wide, h tall to its point, leaning `lean` px over its height
const prism = (I, x, y, w, h, lean = 0) => {
  I.obj(); const t = y - h, s = (w - 1) / 2;
  I.poly([[x, y], [x + lean * 0.8, t + 2.5], [x + s + lean, t], [x + w + lean, t + 2.5], [x + w, y]], 'qtz');
  each(I, (px, py) => { const mid = x + s + lean * (y - py) / h; I.paint(px, py, px + 0.5 < mid - 0.3 ? P.qtz[0] : px + 0.5 < mid + 0.8 ? P.qtz[1] : P.qtz[2]); });
};
v(qtz, 'Друза: три шестигранных столбика на сером камне', I => {
  prism(I, 2, 13, 3, 7, -1); prism(I, 10, 13, 3, 8, 1.5); prism(I, 5.5, 13, 4, 11, 0);
  I.obj(); I.poly([[1, 15], [2, 12], [6, 11.5], [11, 11.5], [14.5, 12.5], [15, 15]], 'rockC'); I.shade('rockC');
  I.paint(6, 4, GLINT);
});
v(qtz, 'Один длинный кристалл лёжа, с острым концом', I => {
  I.obj(); I.poly([[1.5, 11], [10, 3.5], [13, 2.5], [14.5, 4.5], [13, 7], [4.5, 14.5], [2, 13.5]], 'qtz');
  each(I, (x, y) => { const s = (x + 0.5) * 0.66 + (y + 0.5) * 0.75; I.paint(x, y, s < 9.8 ? P.qtz[0] : s < 11.2 ? P.qtz[1] : s < 12.4 ? P.qtz[2] : P.qtz[3]); });
  for (const [x, y] of [[12, 3], [13, 4]]) I.paint(x, y, P.qtz[0]);
  I.paint(4, 10, GLINT); I.paint(5, 9, GLINT);
});
v(qtz, 'Молочный кусок жильного кварца, сколы и серые прожилки', I => {
  I.obj(); I.poly([[1.5, 11], [3, 5], [7, 1.5], [11, 3], [14.5, 6.5], [13.5, 12], [7, 14.5]], 'qtz');
  faceted(I, 'qtz', (x, y) => y < 6 - (x - 3) * 0.2 + (x > 7 ? (x - 7) * 0.6 : 0) ? 0 : x + y < 12 ? 1 : x > 10 ? 3 : 2);
  for (const [x, y] of [[6, 9], [7, 10], [8, 10], [9, 11], [11, 6], [12, 7]]) I.paint(x, y, P.qtz[3]);
  for (const [x, y] of [[9, 13], [12, 12]]) I.paint(x, y, '#8a87a3');
  I.paint(6, 4, GLINT); I.paint(7, 4, GLINT);
});

// ===== ALLOY PLATE =====
const alloy = item('alloyPlate', 'Сплавная пластина', 'из лома сплава руин, холодный сине-зелёный');
v(alloy, 'Квадратная пластина со срезанным углом и гравировкой по краю', I => {
  I.obj(); I.poly([[2, 3], [11, 3], [14, 6], [14, 13], [2, 13]], 'alloy'); I.shade('alloy');
  for (let x = 4; x <= 11; x++) { I.paint(x, 5, P.alloy[3]); I.paint(x, 11, P.alloy[0]); }
  for (let y = 6; y <= 10; y++) { I.paint(4, y, P.alloy[3]); I.paint(12, y, P.alloy[0]); }
  I.paint(12, 7, P.alloy[2]); I.paint(11, 6, P.alloy[3]);
  for (let x = 6; x <= 10; x++) I.paint(x, 8, INLAY[2]);
  I.paint(7, 8, INLAY[1]); I.paint(3, 4, HI);
});
v(alloy, 'Стопка из двух тонких пластин, вид сверху-спереди', I => {
  const slab = (dy) => {
    I.obj(); I.poly([[1, 9 + dy], [5, 5 + dy], [15, 5 + dy], [11, 9 + dy]], 'alloy');
    each(I, (x, y) => I.paint(x, y, y === 5 + dy ? P.alloy[0] : P.alloy[1]));
    I.rect(1, 10 + dy, 10, 10 + dy, 'alloy'); for (let x = 1; x <= 10; x++) I.paint(x, 10 + dy, P.alloy[2]);
    I.rect(11, 9 + dy, 11, 10 + dy, 'alloy'); I.paint(11, 9 + dy, P.alloy[3]); I.paint(11, 10 + dy, P.alloy[3]);
    for (let k = 0; k <= 2; k++) I.paint(6 + k * 2 - (k ? 0 : 0), 7 + dy, P.alloy[0]);
  };
  slab(3); slab(-1);
  for (let x = 5; x <= 9; x++) I.paint(x + 1, 6, INLAY[1]);
  I.paint(6, 5, HI);
});
v(alloy, 'Пластина с сотовым узором и бирюзовой вставкой', I => {
  I.obj(); I.rect(2, 3, 13, 13, 'alloy'); I.erase(2, 3); I.erase(13, 13); I.shade('alloy');
  for (let y = 5; y <= 11; y += 3) for (let x = 4 + ((y - 5) / 3 % 2) * 2; x <= 11; x += 4) { I.paint(x, y, P.alloy[2]); I.paint(x + 1, y, P.alloy[2]); I.paint(x, y + 1, P.alloy[3]); }
  for (let y = 4; y <= 12; y++) I.paint(9, y, y % 2 ? INLAY[1] : INLAY[2]);
  I.paint(3, 4, HI);
});

// ===== HIDE =====
const hide = item('hide', 'Шкура', 'со травоядного, мехом наружу');
v(hide, 'Распластанная шкура с лапами, хребет и шерсть полосками', I => {
  I.obj(); I.poly([[0.5, 1.5], [4, 3.5], [6, 5], [10, 5], [12, 3.5], [15.5, 1.5], [12.5, 6.5], [12, 8.5], [12, 10.5], [15, 14.5], [11, 13], [9, 12.5], [8, 15], [7, 12.5], [5, 13], [1, 14.5], [4, 10.5], [4, 8.5], [3.5, 6.5]], 'hide');
  I.shade('hide');
  for (let y = 5; y <= 12; y++) { I.tone(7, y, 'hide', 2); I.tone(8, y, 'hide', 3); }
  for (const [x, y] of [[5, 7], [10, 7], [5, 10], [10, 10], [6, 8], [9, 9]]) I.tone(x, y, 'hide', 2);
});
v(hide, 'Скатанная шкура, перевязанная двумя шнурами', I => {
  I.obj(); I.rect(2, 5, 11, 11, 'hide'); I.erase(2, 5); I.erase(2, 11);
  each(I, (x, y) => I.tone(x, y, 'hide', y <= 5 ? 0 : y <= 7 ? 1 : y <= 9 ? 2 : 3));
  I.obj(); I.ellipse(12, 8, 2.6, 3.6, 'hide');
  each(I, (x, y) => I.tone(x, y, 'hide', 0));
  for (const [x, y] of [[12, 8], [11, 7], [12, 6], [13, 7], [13, 9], [11, 9]]) I.tone(x, y, 'hide', 2);
  I.cur = 1;
  for (const x of [5, 9]) for (let y = 5; y <= 11; y++) I.paint(x, y, y % 2 ? P.rope[1] : P.rope[2], true);
});
v(hide, 'Сложенная шкура: угол отвёрнут светлой изнанкой, мех по краю', I => {
  I.obj(); I.poly([[2, 3], [13, 3], [13, 9], [8, 14], [2, 14]], 'hide'); I.shade('hide');
  for (let y = 4; y <= 13; y += 2) { I.tone(2, y, 'hide', 0); }
  for (const [x, y] of [[5, 6], [8, 5], [10, 7], [4, 10], [7, 9], [5, 12]]) I.tone(x, y, 'hide', 2);
  I.obj(); I.poly([[8, 14], [13, 9], [14, 14]], 'fat');
  each(I, (x, y) => I.paint(x, y, x + y > 25 ? P.fat[2] : P.fat[1]));
});

// ===== FAT =====
const fat = item('fat', 'Жир', 'белый, топится в биотопливо');
v(fat, 'Брусок сала: верхняя грань светлее, блик', I => {
  I.obj(); I.poly([[2, 7], [5, 4], [14, 4], [11, 7]], 'fat'); each(I, (x, y) => I.paint(x, y, P.fat[0]));
  I.obj(); I.rect(2, 7, 10, 13, 'fat'); each(I, (x, y) => I.paint(x, y, y >= 12 ? P.fat[2] : P.fat[1]));
  I.obj(); I.poly([[11, 7], [14, 4], [14, 10], [11, 13.9]], 'fat'); each(I, (x, y) => I.paint(x, y, P.fat[2]));
  I.cur = 0;
  I.paint(3, 9, GLINT, true); I.paint(4, 9, P.fat[0], true);
  for (const x of [5, 6, 7, 8]) I.paint(x, 11, '#e9c3b0', true);
});
v(fat, 'Глянцевый ком жира с розовой прожилкой', I => {
  I.obj(); I.poly([[1.5, 10], [3, 5.5], [7, 3], [12, 4], [14.5, 8], [13.5, 12.5], [9, 14.5], [4, 13.5]], 'fat');
  I.shadeRound(8, 8.5, 7, 'fat', [-0.55, 0.2, 0.65]);
  for (const [x, y] of [[4, 10], [5, 10], [6, 11], [7, 11], [8, 11], [9, 12], [10, 12]]) I.paint(x, y, '#e4aa98');
  I.paint(5, 5, GLINT); I.paint(6, 5, GLINT); I.paint(5, 6, GLINT);
});
v(fat, 'Шмат сала со шкуркой и розовой прослойкой', I => {
  I.obj(); I.poly([[1, 13], [1, 7], [4, 4], [15, 4], [15, 10], [12, 13]], 'fat');
  each(I, (x, y) => I.paint(x, y, y <= 4 ? '#b07a4a' : y <= 5 ? '#d39c66' : x > 11 && x + y > 18 ? P.fat[2] : y === 9 || y === 10 && x < 6 ? '#e4aa98' : y > 11 ? P.fat[2] : P.fat[1]));
  for (const [x, y] of [[4, 4], [1, 7]]) I.paint(x, y, '#d39c66');
  I.paint(3, 7, GLINT); I.paint(4, 7, P.fat[0]);
});

module.exports = { items };
