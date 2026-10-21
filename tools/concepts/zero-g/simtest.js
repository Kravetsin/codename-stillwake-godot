const fs=require('fs'),vm=require('vm');
const strip=s=>s.replace(/if \(typeof module[\s\S]*$/,'');
const src=strip(fs.readFileSync('engine.js','utf8'))+strip(fs.readFileSync('zphys.js','utf8'))+'\n;globalThis.out={STATES,keysAt,stepZ,Grid,MAPS,U,HW,HH,clear};';
const ctx={Math,console};vm.createContext(ctx);vm.runInContext(src,ctx);
const {STATES,keysAt,stepZ,Grid,MAPS,U,HW,HH,clear}=ctx.out;
for(const s of STATES){
  const g=new Grid(MAPS['z_'+s.map]);
  let h={x:Math.round(s.at[0]*U),y:Math.round(s.at[1]*U),vx:0,vy:0,dx:0,dy:0,facing:s.face};
  const ok=clear(g,h.x,h.y);
  let line=[];let seg=0,acc=0;
  for(let t=0;t<s.len;t++){const k=keysAt(s,t);h=stepZ(g,h,k,false);
    if(h.bumped)line.push(`t${t} BUMP ${h.bumped.speed.toFixed(0)}`);
    if(h.pushed)line.push(`t${t} push`);
    if(t===acc+s.tl[seg][0]-1){line.push(`t${t}:(${(h.x/U).toFixed(2)},${(h.y/U).toFixed(2)}) v${Math.hypot(h.vx,h.vy).toFixed(0)}`);acc+=s.tl[seg][0];seg++;}
  }
  console.log(s.id,'clear',ok,'|',line.join(' | '));
}
