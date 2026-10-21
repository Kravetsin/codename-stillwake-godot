const fs = require('fs'), path = require('path');
const f = path.join(__dirname, 'page.html');
let h = fs.readFileSync(f, 'utf8');
const rep = (a, b) => { if (!h.includes(a)) throw new Error('missing: ' + a.slice(0, 70)); h = h.replace(a, b); };

rep('<p class="lede">Сейчас за землёй три мягких пятна и одно небо на всю планету. Здесь — четыре направления нового фона в стиле s9 (контур, 3–4 тона, приглушённый свет), живые: сцену можно двигать и смотреть параллакс прямо за нашей землёй и героем. Плюс зоны вдоль планеты, фон пещер по полосам глубины, закат как предложение, цена в Godot и форма выбора.</p>',
  '<p class="lede">Сейчас за землёй три мягких пятна и одно небо на всю планету. Планет будут сотни, поэтому фон здесь — не картина, а система: палитра, зерно и небольшая библиотека реквизита собирают небо, хребты, холмы и погоду любой планеты в стиле s9 (контур, 3–4 тона, приглушённый свет). Четыре направления — четыре способа компоновки этой системы, все живые: сцену можно двигать за нашей землёй и героем, менять тип планеты или бросить кубик. Плюс зоны внутри планеты, фон пещер по полосам глубины, закат как предложение, цена в Godot и форма выбора.</p>');
rep('<a href="#terraria">Terraria</a><a href="#directions">', '<a href="#terraria">Terraria</a><a href="#system">Система</a><a href="#directions">');
for (const [a, b] of [['03 · четыре направления, живые', '04 · четыре направления, живые'], ['04 · разнообразие по ширине', '05 · разнообразие внутри планеты'], ['05 · фон пещер по глубине', '06 · фон пещер по глубине'], ['06 · время и свет', '07 · время и свет'], ['07 · сколько это стоит', '08 · сколько это стоит'], ['08 · твой выбор', '09 · твой выбор']]) rep(a, b);

rep('<section id="directions">', `<section id="system">
  <p class="eyebrow">03 · сотни планет</p>
  <h2>Система, а не картина</h2>
  <p class="read">Фон любой планеты собирается из трёх вещей. <b>Данные</b> — палитры и числа в <code>Content</code>: их пишут, а не рисуют. <b>Зерно</b> планеты — откуда шум хребтов, где стоит реквизит, где границы зон, какого размера гигант и сколько лун. <b>Библиотека реквизита</b> — единственный арт: силуэты в индексных тонах, которые красит палитра. Всё на этой странице, кроме земли и героя, собрано именно так. Направления A–D ниже — способы компоновки одной системы: сколько слоёв, их высота и параллакс, какие события в небе.</p>
  <div class="gallery" id="gallery" aria-label="Шесть типов планет, одна система"></div>
  <p class="small">Шесть типов из одной системы, направление A, у спавна. Клик открывает планету в живой сцене; там же кнопка «Случайная планета» — новое зерно и палитра из набора.</p>
  <div class="cols">
    <div style="display:grid;gap:10px">
      <h3>Слой из шума и палитры</h3>
      <ul class="plain">
        <li><b>Небо</b> — 5 тонов градиента и дымка из палитры неба; гигант (полосы — палитра, размер, наклон, кольцо — зерно), 0–3 луны, солнце.</li>
        <li><b>Дальние, средние, ближние силуэты</b> — 1D-шум с параметрами слоя: доля камеры, высота, ячейка шума (шероховатость), форма профиля (округлые, столовые, зубчатые, конусы вулканов), дымка, контур. Тоны слоя — 4 роли (свет, основа, тень, линия) из палитры биома, смешанные с дымкой по глубине.</li>
        <li><b>Реквизит</b> — из библиотеки, по весам биома и по зерну: акации, голые деревья, останцы, кристаллы, грибы, гигантская флора, шпили, арки, парящие скалы.</li>
        <li><b>Погода и жизнь</b> — малый набор эффектов: облака (кучевые, перистые, пепел), пылевые полосы, сияние, мошки, дым кратеров, «птицы».</li>
      </ul>
    </div>
    <div style="display:grid;gap:10px">
      <h3>Индексные тона: один реквизит, восемь биомов</h3>
      <div class="recolour" id="recolour" aria-label="Акация из игры, перекрашенная палитрами восьми биомов"></div>
      <p class="small">Это настоящая <code>acacia_0.png</code> из игры. Листва разложена по яркости на 5 индексов и заново окрашена рампой биома, ствол — множителем грунта, свечение семян — цветом свечения биома. В игре это шейдер: картинка хранит индекс тона, палитра — цвета.</p>
    </div>
  </div>
  <div class="cols">
    <div style="display:grid;gap:8px">
      <h3>Что данные</h3>
      <div class="tablebox"><table>
        <thead><tr><th>Что</th><th>Где</th><th>Пример</th></tr></thead>
        <tbody>
          <tr><td>Палитра неба</td><td>на тип</td><td>5 тонов + дымка; 6 кураторских: бирюза, нефрит, охра, фиолет, лёд, ржавчина</td></tr>
          <tr><td>Палитра биома</td><td>на биом</td><td>дальний, ближний, свет, слои пород; мох 5 тонов; листва 5; свечения 1–3</td></tr>
          <tr><td>Веса реквизита</td><td>на биом</td><td>саванна: акация 6, флора 1; бэдлендс: останец 5, акация 1, голое 1</td></tr>
          <tr><td>Форма хребтов</td><td>на биом</td><td>округлые / столовые / зубчатые / конусы</td></tr>
          <tr><td>Параметры слоёв</td><td>на направление</td><td>доля камеры, высота, ячейка шума, дымка, контур, число реквизита</td></tr>
          <tr><td>Погода</td><td>на тип</td><td>низкие облака: кучевые / пыль / пепел; сияние; цвет мошек</td></tr>
          <tr><td>Набор биомов</td><td>на тип</td><td>3 зоны: левая, у спавна, правая; пул соседей для случайной</td></tr>
          <tr><td>Зерно</td><td>на планету</td><td>шум, места реквизита, границы зон, гигант, луны, солнце</td></tr>
        </tbody>
      </table></div>
      <p class="small">Генерация — целочисленный хэш от зерна, как у <code>Terrain</code>: все пиры видят одинаковый фон, хотя он только клиентский.</p>
    </div>
    <div style="display:grid;gap:8px">
      <h3>Что арт: библиотека реквизита</h3>
      <div class="tablebox"><table>
        <thead><tr><th>Набор</th><th>Силуэты</th><th class="num">≈ картинок</th></tr></thead>
        <tbody>
          <tr><td>Общий, для всех</td><td>шпиль руин (3 размера), арка (3), парящая скала (4), останец (4), голое дерево (3), кучевые облака (6), перистые полосы (2). Гигант, луны, шум хребтов — код</td><td class="num">25</td></tr>
          <tr><td>Саванна</td><td>акация (4 формы × 3 размера), куст (3), гигантский колокольчик (3)</td><td class="num">18</td></tr>
          <tr><td>Бэдлендс</td><td>сухая акация (2), столб-кактус (3), каменный мост (1); останцы — общие</td><td class="num">6</td></tr>
          <tr><td>Кристальный</td><td>кластер (5), одиночный шпиль (3), кристальное дерево (2)</td><td class="num">10</td></tr>
          <tr><td>Ледяной</td><td>ледяной шпиль (4), торос (3); замёрзшее дерево — общее голое</td><td class="num">7</td></tr>
          <tr><td>Вулканический</td><td>базальтовый столб (4); конусы — профиль, дым — эффект</td><td class="num">4</td></tr>
          <tr><td>Грибной</td><td>гигантский гриб (5 форм × 2 размера), гриб-полка (2), колокольчик-нить (3)</td><td class="num">15</td></tr>
          <tr><td><b>Всего</b></td><td>на шесть типов; каждый новый тип — 4–18 своих</td><td class="num"><b>≈ 85</b></td></tr>
        </tbody>
      </table></div>
      <p class="small">Размеры — потому что пиксель-арт не масштабируют: дальний 12–24 px, средний 24–48, ближний 48–80. Силуэты в 4 серых тонах плюс маска свечения. На странице реквизит рисует код (как экспорт 0079) — это тоже путь: тогда «арт» — это код рисования, а не картинки.</p>
    </div>
  </div>
</section>

<section id="directions">`);

rep('<div class="tgroup"><span class="lbl">Фон</span>', `<div class="tgroup"><span class="lbl">Планета</span><div class="seg" data-g="planet"><button type="button" data-v="savanna">Саванна</button><button type="button" data-v="badlands">Бэдлендс</button><button type="button" data-v="crystal">Кристалл</button><button type="button" data-v="ice">Лёд</button><button type="button" data-v="volcanic">Вулкан</button><button type="button" data-v="fungal">Грибы</button></div><button type="button" class="primary roll" data-roll>Случайная планета</button></div>
    <div class="tgroup"><span class="lbl">Фон</span>`);
rep('<p class="dirnote"></p>', '<p class="dirnote"></p>\n  <div class="passport" aria-live="polite"></div>');

rep('/* pick form */', `/* the system */
.gallery { display: grid; grid-template-columns: repeat(auto-fill, minmax(min(100%, 250px), 1fr)); gap: 10px; }
.thumb { display: grid; gap: 0; padding: 0; border: 1px solid var(--line); border-radius: var(--r); overflow: hidden; background: var(--panel); color: var(--fg); cursor: pointer; text-align: left; font: inherit; }
.thumb canvas { width: 100%; aspect-ratio: 16 / 9; display: block; background: #000; }
.thumb .tcap { display: grid; padding: 7px 10px 9px; gap: 1px; }
.thumb .tcap small { font-size: 0.78rem; }
.thumb:hover, .thumb.on { border-color: var(--teal); }
.recolour { display: flex; flex-wrap: wrap; gap: 6px; background: linear-gradient(#93bfdc, #e8e2c8); border-radius: var(--r); padding: 8px; border: 1px solid var(--line); }
.recolour figure { display: grid; justify-items: center; gap: 2px; width: calc(25% - 6px); min-width: 70px; }
.recolour canvas { width: 100%; max-width: 92px; image-rendering: pixelated; }
.recolour figcaption { font: 0.7rem var(--mono); color: #22302c; text-align: center; }
.passport { display: grid; gap: 4px; background: var(--panel); border: 1px solid var(--line); border-radius: var(--r); padding: 8px 12px; font-size: 0.86rem; }
.passport .pphead { display: flex; flex-wrap: wrap; gap: 4px 12px; align-items: baseline; }
.passport .pp { display: flex; flex-wrap: wrap; align-items: center; gap: 3px; }
.passport .k { color: var(--muted); margin: 0 4px 0 10px; font-size: 0.8rem; }
.passport .pp .k:first-child { margin-left: 0; }
.passport .swatch { margin-right: 0; width: 12px; height: 12px; }
.roll { padding: 6px 12px; }

/* pick form */`);

rep('<tr><td>CPU</td>', '<tr><td>Сборка при высадке</td><td>картинки готовы</td><td>слой 2048 × 150–300 из шума и реквизита: в браузере здесь ≈ {{BUILD_MS}} мс на слой; в C# на потоке — того же порядка, ≈ 15 слоёв на планету ≈ 0.5–1 с на фоне, пока идёт высадка; можно хранить готовые PNG в папке сохранения</td></tr>\n      <tr><td>CPU</td>');

fs.writeFileSync(f, h); console.log('page patched');
