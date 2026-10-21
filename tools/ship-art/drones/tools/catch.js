// node catch.js dir : saves each POSTed data: URL as dir/<path>.png, for checking the page's canvas by eye
const http = require('http'), fs = require('fs'), path = require('path'); const dir = process.argv[2];
http.createServer((q, r) => { let b = ''; q.on('data', d => b += d); q.on('end', () => {
  if (q.method === 'POST') fs.writeFileSync(path.join(dir, path.basename(q.url) + '.png'), Buffer.from(b.split(',')[1], 'base64'));
  r.writeHead(200, { 'Access-Control-Allow-Origin': '*' }); r.end('ok'); }); }).listen(8766, '127.0.0.1');
