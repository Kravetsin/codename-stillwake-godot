'use strict';
// Inlines the renders of out/ into template.html: node build.js <out.html>
const fs = require('fs'), path = require('path');
const out = path.join(__dirname, 'out');
const uri = f => 'data:image/png;base64,' + fs.readFileSync(path.join(out, f)).toString('base64');
const size = f => { const b = fs.readFileSync(path.join(out, f)); return [b.readUInt32BE(16), b.readUInt32BE(20)]; };

const STYLES = [
  ['a_chibi', 'A', 'Чиби, как Terraria', {
    'Голова': 'крупная, 15×15 px (шлем сейчас 13×12)',
    'Контур': 'почти чёрный, вокруг всего',
    'Тени': 'плоская заливка и одна тень',
    'Лицо': 'глаз в два пикселя, белок и зрачок; у второй румянец и ресницы',
  }, 'Читается на любом зуме, самый дружелюбный. Ближе всего к Terraria.', 'Шлем придётся перерисовать крупнее. Лица отличаются в основном причёской и цветом.'],
  ['b_detailed', 'B', 'Подробный, как Starbound', {
    'Голова': 'как шлем, 14×14 px, поворот на три четверти',
    'Контур': 'тёмный оттенок своего цвета, как у нынешних скафандров',
    'Тени': 'четыре тона, волосы прядями',
    'Лицо': 'два глаза, брови, нос, рот; на комбинезоне молния, карманы, ремень',
  }, 'Лица самые разные. По технике совпадает с тем, как уже нарисован скафандр.', 'Каждое новое лицо и причёска стоят больше пикселей и времени.'],
  ['c_flat', 'C', 'Плоский, без контура', {
    'Голова': '12×13 px, профиль держится силуэтом',
    'Контур': 'нет',
    'Тени': 'два тона на цвет',
    'Лицо': 'глаз в один пиксель; светлая полоса через грудь и рукава',
  }, 'Быстрее всего рисовать. Цвет комбинезона виден издалека.', 'Тёмные волосы и кожа сливаются с тёмной палубой, рука сливается с телом. Не похож на остальную графику игры.'],
  ['d_retro', 'D', 'Ретро, малая голова', {
    'Голова': 'маленькая, 10×10 px, взрослые пропорции',
    'Контур': 'чёрный, вокруг всего',
    'Тени': 'два тона на цвет',
    'Лицо': 'только глаз и бровь; героев различают причёска и борода',
  }, 'Герой выглядит взрослым инженером, а не ребёнком.', 'На лицо почти нет места. Под нынешним шлемом голова будет болтаться, шлем придётся уменьшать.'],
  ['e_soft', 'E', 'Мягкий свет', {
    'Голова': 'профиль 15×15 px, две формы: квадратная челюсть и мягкая с маленьким подбородком. Причёски у второй пары переставлены: пол читается по форме головы',
    'Контур': 'только с теневой стороны, освещённый перед открыт',
    'Тени': 'четыре тона и холодный ободок света по спине',
    'Лицо': 'мужское: густая бровь, сжатый рот; женское: ресницы вперёд, румянец, губы цветом',
  }, 'Самый объёмный. Хорошо ляжет на 2D-свет Godot.', 'Ободок нарисован прямо в картинке и будет светиться и в тёмной каюте. Лучше отдать его шейдеру света.'],
];

const plates = STYLES.map(([id, letter, name, traits, plus, minus]) => {
  const [lw, lh] = size(id + '_line.png'), [hw, hh] = size(id + '_heads.png');
  const t = Object.entries(traits).map(([k, v]) => `<li><span class="k">${k}</span><span>${v}</span></li>`).join('');
  return `<section class="plate" id="${letter.toLowerCase()}">
    <div class="plate-head"><span class="letter">${letter}</span><h2>${name}</h2></div>
    <div class="art">
      <figure><img class="px" src="${uri(id + '_line.png')}" alt="${name}: герой в скафандре и два героя без него" width="${lw}" height="${lh}"><figcaption>на палубе, ×6</figcaption></figure>
      <figure><img class="px" src="${uri(id + '_heads.png')}" alt="${name}: две головы крупно" width="${hw}" height="${hh}"><figcaption>головы, ×10</figcaption></figure>
    </div>
    <ul class="traits">${t}</ul>
    <div class="verdict"><p class="plus"><span class="k">Плюс</span><br>${plus}</p><p class="minus"><span class="k">Минус</span><br>${minus}</p></div>
  </section>`;
}).join('\n\n  ');

const [aw, ah] = size('all_x3.png');
const put = (h, key, file) => h.replace('{{' + key + '}}', uri(file)).replace('{{' + key + '_w}}', size(file)[0]).replace('{{' + key + '_h}}', size(file)[1]);
let html = fs.readFileSync(path.join(__dirname, 'template.html'), 'utf8')
  .replace('{{plates}}', plates)
  .replace('{{all_x3}}', uri('all_x3.png')).replace('{{all_x3_w}}', aw).replace('{{all_x3_h}}', ah)
  .replace('{{fg_line}}', uri('fg_line.png')).replace('{{fg_line_w}}', size('fg_line.png')[0]).replace('{{fg_line_h}}', size('fg_line.png')[1])
  .replace('{{fg_heads}}', uri('fg_heads.png')).replace('{{fg_heads_w}}', size('fg_heads.png')[0]).replace('{{fg_heads_h}}', size('fg_heads.png')[1]);
for (const [k, f] of [['set_line', 'f_one1x_line.png'], ['cat_hairs', 'cat_hairs.png'], ['cat_faces', 'cat_faces.png'], ['cat_colours', 'cat_colours.png']]) html = put(html, k, f);
fs.writeFileSync(process.argv[2], html);
console.log('wrote', process.argv[2], html.length);
