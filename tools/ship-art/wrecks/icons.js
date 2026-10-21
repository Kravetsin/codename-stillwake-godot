// Item icons (16x16) for the wreck kinds, area-sampled down from their 1x pictures to fit 14 px, centred.
// node tools/ship-art/wrecks/icons.js [fit] [rim]: reads wrecks/out, writes wrecks/icons and wrecks/icons-sheet.png.
const {readPng,png,sheet}=require('./pngio');const fs=require('fs'),path=require('path');
const OUTD=process.env.WRECKS_OUT||path.join(__dirname,'out'),ICD=process.env.WRECKS_ICONS||path.join(__dirname,'icons');fs.mkdirSync(ICD,{recursive:true});
const ids=['hullSection','cargoContainer','machineBlock','cableRack','tankCluster','shieldSlab'];
const fit=+(process.argv[2]||14);
function shrink(p){
  const s=fit/Math.max(p.w,p.h), W=Math.round(p.w*s), H=Math.round(p.h*s);
  const out=Buffer.alloc(16*16*4), ox=Math.floor((16-W)/2), oy=Math.floor((16-H)/2)+ 0;
  for(let y=0;y<H;y++)for(let x=0;x<W;x++){
    const x0=x/s,x1=(x+1)/s,y0=y/s,y1=(y+1)/s;let r=0,g=0,b=0,a=0,area=0;
    for(let sy=Math.floor(y0);sy<Math.ceil(y1);sy++)for(let sx=Math.floor(x0);sx<Math.ceil(x1);sx++){
      const wx=Math.min(x1,sx+1)-Math.max(x0,sx),wy=Math.min(y1,sy+1)-Math.max(y0,sy),w=wx*wy;area+=w;
      const i=(sy*p.w+sx)*4,al=p.px[i+3]/255;r+=p.px[i]*al*w;g+=p.px[i+1]*al*w;b+=p.px[i+2]*al*w;a+=al*w;}
    if(a/area<0.5)continue;const o=((y+oy)*16+x+ox)*4;out[o]=r/a;out[o+1]=g/a;out[o+2]=b/a;out[o+3]=255;}
  // A dark rim where an opaque pixel meets a clear one outside, as the hand-drawn icons have.
  const rim=Buffer.from(out);
  for(let y=0;y<16;y++)for(let x=0;x<16;x++){const i=(y*16+x)*4;if(out[i+3])continue;
    const n=[[1,0],[-1,0],[0,1],[0,-1]].some(([dx,dy])=>{const X=x+dx,Y=y+dy;return X>=0&&Y>=0&&X<16&&Y<16&&out[(Y*16+X)*4+3];});
    if(n){rim[i]=14;rim[i+1]=18;rim[i+2]=26;rim[i+3]=255;}}
  return {w:16,h:16,px:process.argv[3]==='rim'?rim:out};
}
const made=ids.map(id=>{const p=shrink(readPng(path.join(OUTD,'wreck_'+id+'.png')));fs.writeFileSync(path.join(ICD,id+'.png'),png(16,16,p.px));return p;});
const real=readPng(path.join(__dirname,'..','..','..','game','art','items','hullSection.png'));
fs.writeFileSync(path.join(ICD,'..','icons-sheet.png'),sheet([[real,...made],[readPng(path.join(OUTD,'item_circuit.png'))]],8));
