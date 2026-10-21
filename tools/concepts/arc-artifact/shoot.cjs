// node shoot.cjs [width]: screenshots of the built page (wrapped as the artifact skeleton would) over a few frames
const path = require('path'), fs = require('fs');
const { chromium } = require('C:/Users/Kravets/Documents/GitHub/codename-stillwake/node_modules/.pnpm/playwright-core@1.64.0/node_modules/playwright-core');
const W = +(process.argv[2] || 1280), OUT = path.join(__dirname, 'shots'); fs.mkdirSync(OUT, { recursive: true });
const src = fs.readFileSync(path.join(__dirname, 'site', 'arc-relic.html'), 'utf8');
fs.writeFileSync(path.join(__dirname, 'site', 'test.html'), '<!doctype html><html><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1,viewport-fit=cover"><style>body{margin:0}[hidden]{display:none!important}img{max-width:100%}</style></head><body>' + src + '</body></html>');
(async () => {
  const b = await chromium.launch({ channel: 'chrome' });
  const pg = await b.newPage({ viewport: { width: W, height: 900 }, deviceScaleFactor: 1 });
  const errs = []; pg.on('pageerror', (e) => errs.push(e.message)); pg.on('console', (m) => { if (m.type() === 'error') errs.push(m.text()); });
  await pg.goto(require('url').pathToFileURL(path.join(__dirname, 'site', 'test.html')).href);
  await pg.waitForTimeout(1200);
  console.log('scrollWidth', await pg.evaluate(() => [document.documentElement.scrollWidth, window.innerWidth]), 'errors', errs);
  await pg.screenshot({ path: path.join(OUT, `full-${W}.png`), fullPage: true });
  if (W >= 1000) {
    await pg.evaluate(() => window.__arc.setMotion(false));
    const shoot = async (sel, name, n, every) => { const e = await pg.$(sel); await e.scrollIntoViewIfNeeded(); await pg.waitForTimeout(150);
      for (let f = 0; f < n; f++) { await pg.evaluate((k) => window.__arc.step(k), every); await e.screenshot({ path: path.join(OUT, `${name}-f${f}.png`) }); } };
    await pg.evaluate(() => window.__arc.layers.forEach((L) => { L.vis = true; }));
    for (const id of ['shard', 'ring', 'core']) await shoot(`#card-${id} .views`, 'card-' + id, 4, 7);
    await shoot('#v-spire-v1 .frame', 'spire-v1', 4, 9); await shoot('#v-spire-v2 .frame', 'spire-v2', 3, 9); await shoot('#v-spire-v3 .frame', 'spire-v3', 3, 9);
    await shoot('#v-home-niche .frame', 'home-niche', 3, 9); await shoot('#v-home-lamp .frame', 'home-lamp', 3, 9);
  }
  await b.close();
})();
