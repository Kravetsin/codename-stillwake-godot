const fs=require('fs'),vm=require('vm');
const src=fs.readFileSync('engine.js','utf8').replace(/if \(typeof module[\s\S]*$/,'')+fs.readFileSync('cards.js','utf8').replace(/if \(typeof module[\s\S]*$/,'')+`
;globalThis.out=[];
for (const c of CARDS){ for (const mode of ['now','new']){
  const grid=new Grid(MAPS[c.map]); let h={x:c.spawn[0]*U+500,y:c.spawn[1]*U,vx:0,vy:0,dx:0,dy:0,climbing:false,facing:1};
  const r={grid}; const m={}; const evs=[]; let minY=h.y, maxX=h.x, minX=h.x;
  if(!clear(grid,h.x,h.y)) evs.push('SPAWN IN WALL');
  for(let t=0;t<c.loop;t++){ const inp=SCRIPTS[c.script](h,t,m,r); h=(mode==='new'?stepNew:stepNow)(grid,h,inp); if(h.ev) evs.push(t+':'+h.ev); if(h.topped) evs.push(t+':top');
    if(t%30===0) evs.push(t+'@'+(h.x/1000).toFixed(1)+','+(h.y/1000).toFixed(1)+(h.climbing?'L':''));
    if(!clear(grid,h.x,h.y)) {evs.push('IN WALL '+t);break;} }
  out.push(c.id+' '+mode+': '+evs.join(' ')+' mem='+JSON.stringify(m));
}}`;
vm.runInNewContext(src,globalThis);console.log(out.join('\n'));
