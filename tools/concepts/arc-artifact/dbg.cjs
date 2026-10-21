const path = require('path');
const { chromium } = require('C:/Users/Kravets/Documents/GitHub/codename-stillwake/node_modules/.pnpm/playwright-core@1.64.0/node_modules/playwright-core');
(async () => { const b = await chromium.launch({ channel: 'chrome' }); const pg = await b.newPage();
  await pg.goto(require('url').pathToFileURL(path.join(__dirname, 'site', 'test.html')).href); await pg.waitForTimeout(500);
  console.log(await pg.evaluate(() => document.getElementById('counts').textContent + ' ' + JSON.stringify(localStorage.getItem('arc-relic-picks-v1')) + [...document.querySelectorAll('.pickrow input:checked')].map((i) => i.name + '=' + i.value).join(' ') + ' | ' + document.getElementById('summary').textContent.split('\n').filter(l=>/беру|изменить|не надо/.test(l)).join(';')));
  await b.close(); })();
