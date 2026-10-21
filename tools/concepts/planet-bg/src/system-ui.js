// ---------- the system on the page: passport, gallery of types, one prop recoloured ----------
function passport(el, P) {
  if (!el) return;
  const sw = (c) => `<span class="swatch" style="background:${typeof c === 'string' ? c : css(c)}"></span>`;
  el.innerHTML = `<div class="pphead"><b>${P.name}</b><span class="small mono">тип: ${P.typeName.toLowerCase()} · зерно ${P.seed}</span></div>
    <div class="pp"><span class="k">небо</span>${SKIES[P.sky].ramp.map(sw).join('')}<span class="k">гигант</span>${P.giantBands.map(sw).join('')}<span class="small">× ${P.gscale.toFixed(2)}, ${P.ring ? 'с кольцом' : 'без кольца'}</span></div>
    <div class="pp">${P.biomes.map((b, i) => `<span class="k">${['зона 1', 'зона 2 · спавн', 'зона 3'][i]}: ${BIOMES[b].name}</span>${[BIOMES[b].far, BIOMES[b].near, BIOMES[b].rim, ...BIOMES[b].glow].map(sw).join('')}`).join('')}</div>
    <div class="pp small"><span class="k">границы</span>${P.borders[0]} и ${P.borders[1]}<span class="k">погода</span>${{ cumulus: 'кучевые', dust: 'пыль', ash: 'пепел' }[P.weather.low]}${P.weather.aurora ? ', сияние' : ''}<span class="k">мошки</span>${P.weather.motes.map(sw).join('')}</div>`;
}

function setupGallery(main) {
  const box = document.getElementById('gallery'); if (!box) return;
  const host = document.createElement('div'); host.innerHTML = '<canvas class="stage"></canvas>';
  const tv = new SurfaceView(host); tv.tFix = 12;
  const items = TYPE_ORDER.map((k) => {
    const P = PRESETS[k], b = document.createElement('button'); b.type = 'button'; b.className = 'thumb'; b.dataset.type = k;
    b.innerHTML = `<canvas width="480" height="270" aria-hidden="true"></canvas><span class="tcap"><b>${P.typeName}</b><small>${P.biomes.map((z) => BIOMES[z].name).join(' · ')}</small></span>`;
    b.addEventListener('click', () => {
      if (main.s.dir === 'now') document.querySelector('#viewer [data-g=dir] [data-v=A]').click();
      main.setPlanet(P); main.el.scrollIntoView({ behavior: reduce ? 'auto' : 'smooth', block: 'start' });
    });
    box.appendChild(b); return { P, cv: b.querySelector('canvas') };
  });
  let i = 0;
  const next = () => {
    if (i >= items.length) return;
    const it = items[i++]; tv.planet = it.P; tv.s.dir = 'A'; tv.hero.x = SPAWN * CELL + 8; tv.look = -110; tv.draw();
    const c = it.cv.getContext('2d'); c.imageSmoothingEnabled = true; c.drawImage(tv.cv, 0, 0, 960, 540, 0, 0, 480, 270);
    setTimeout(next, 150);
  };
  setTimeout(next, 500);
}

function setupRecolour() {
  const box = document.getElementById('recolour'); if (!box) return;
  for (const bk of Object.keys(BIOMES)) {
    const f = document.createElement('figure'), cv = document.createElement('canvas'), im = IMG.acacia_0; cv.width = im.width; cv.height = im.height;
    const c = cv.getContext('2d'); c.drawImage(recolour('acacia_0', bk), 0, 0); c.globalCompositeOperation = 'lighter'; c.drawImage(recolour('acacia_0_glow', bk), 0, 0);
    f.appendChild(cv); const cap = document.createElement('figcaption'); cap.textContent = BIOMES[bk].name; f.appendChild(cap); box.appendChild(f);
  }
}

function setupPlanetControls(v, root) {
  const pseg = root.querySelector('[data-g=planet]');
  const setPlanet = (P) => {
    v.planet = P;
    seg(pseg, PRESETS[P.type] === P ? P.type : '', (k) => setPlanet(PRESETS[k]));
    passport(root.querySelector('.passport'), P);
    document.querySelectorAll('.thumb').forEach((t) => t.classList.toggle('on', PRESETS[t.dataset.type] === P));
  };
  v.setPlanet = setPlanet; setPlanet(v.planet);
  root.querySelector('[data-roll]').addEventListener('click', () => { if (v.s.dir === 'now') root.querySelector('[data-g=dir] [data-v=A]').click(); setPlanet(randomPlanet()); });
  v.onMini = (P) => {
    const L = root.querySelector('.minilegend');
    L.innerHTML = P ? `<span>0 · ${BIOMES[P.biomes[0]].name}</span><span>${P.borders[0]}</span><span>${BIOMES[P.biomes[1]].name} · спавн 1024</span><span>${P.borders[1]}</span><span>${BIOMES[P.biomes[2]].name} · 2048</span>` : '<span>0</span><span>одна картинка на всю планету</span><span>2048</span>';
  };
}
