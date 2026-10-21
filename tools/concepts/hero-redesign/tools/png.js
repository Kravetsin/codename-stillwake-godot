const fs=require('fs'),zlib=require('zlib');
function decode(file){const b=fs.readFileSync(file);let p=8,w,h,ct,bd,idat=[],pal=null,trns=null;
while(p<b.length){const len=b.readUInt32BE(p),type=b.toString('ascii',p+4,p+8),d=b.slice(p+8,p+8+len);p+=12+len;
if(type==='IHDR'){w=d.readUInt32BE(0);h=d.readUInt32BE(4);bd=d[8];ct=d[9];}else if(type==='IDAT')idat.push(d);else if(type==='PLTE')pal=d;else if(type==='tRNS')trns=d;}
const raw=zlib.inflateSync(Buffer.concat(idat));const ch={0:1,2:3,3:1,4:2,6:4}[ct];const bpp=Math.max(1,ch*bd/8);const stride=Math.ceil(w*ch*bd/8);
const out=Buffer.alloc(w*h*4);let prev=Buffer.alloc(stride);let q=0;
for(let y=0;y<h;y++){const f=raw[q++];const line=Buffer.from(raw.slice(q,q+stride));q+=stride;
for(let i=0;i<stride;i++){const a=i>=bpp?line[i-bpp]:0,up=prev[i],c=i>=bpp?prev[i-bpp]:0;let v=line[i];
if(f===1)v+=a;else if(f===2)v+=up;else if(f===3)v+=(a+up)>>1;else if(f===4){const pp=a+up-c,pa=Math.abs(pp-a),pb=Math.abs(pp-up),pc=Math.abs(pp-c);v+=(pa<=pb&&pa<=pc)?a:(pb<=pc?up:c);}line[i]=v&255;}
for(let x=0;x<w;x++){let r,g,bb,al=255;
if(ct===6){r=line[x*4];g=line[x*4+1];bb=line[x*4+2];al=line[x*4+3];}else if(ct===2){r=line[x*3];g=line[x*3+1];bb=line[x*3+2];}
else if(ct===3){let idx;if(bd===8)idx=line[x];else{const per=8/bd;idx=(line[Math.floor(x/per)]>>(8-bd*(x%per+1)))&((1<<bd)-1);}r=pal[idx*3];g=pal[idx*3+1];bb=pal[idx*3+2];al=trns&&idx<trns.length?trns[idx]:255;}
else if(ct===4){r=g=bb=line[x*2];al=line[x*2+1];}else{r=g=bb=line[x];}
out.set([r,g,bb,al],(y*w+x)*4);}prev=line;}
return {w,h,data:out};}
module.exports={decode};
if(require.main===module){for(const f of process.argv.slice(2)){const im=decode(f);const cols={};let s='';
for(let y=0;y<im.h;y++){for(let x=0;x<im.w;x++){const i=(y*im.w+x)*4;if(!im.data[i+3]){s+='.';continue;}const k=[0,1,2].map(j=>im.data[i+j].toString(16).padStart(2,'0')).join('')+(im.data[i+3]<255?'/'+im.data[i+3]:'');if(!(k in cols))cols[k]=String.fromCharCode(Object.keys(cols).length<26?65+Object.keys(cols).length:97+Object.keys(cols).length-26);s+=cols[k];}s+='\n';}
console.log(f.split(/[\/]/).pop(),im.w+'x'+im.h);console.log(Object.entries(cols).map(([k,v])=>v+'='+k).join(' '));console.log(s);}}
function crc32(buf){let c,crc=0xffffffff;for(let n=0;n<buf.length;n++){c=(crc^buf[n])&0xff;for(let k=0;k<8;k++)c=c&1?0xedb88320^(c>>>1):c>>>1;crc=(crc>>>8)^c;}return (crc^0xffffffff)>>>0;}
function encode(im){const {w,h,data}=im;const raw=Buffer.alloc((w*4+1)*h);for(let y=0;y<h;y++){raw[y*(w*4+1)]=0;data.copy(raw,y*(w*4+1)+1,y*w*4,(y+1)*w*4);}
const chunk=(t,d)=>{const l=Buffer.alloc(4);l.writeUInt32BE(d.length);const td=Buffer.concat([Buffer.from(t),d]);const c=Buffer.alloc(4);c.writeUInt32BE(crc32(td));return Buffer.concat([l,td,c]);};
const ih=Buffer.alloc(13);ih.writeUInt32BE(w,0);ih.writeUInt32BE(h,4);ih[8]=8;ih[9]=6;ih[10]=0;ih[11]=0;ih[12]=0;
return Buffer.concat([Buffer.from([137,80,78,71,13,10,26,10]),chunk('IHDR',ih),chunk('IDAT',zlib.deflateSync(raw)),chunk('IEND',Buffer.alloc(0))]);}
function crop(im,x0,y0,w,h,s=1){const out={w:w*s,h:h*s,data:Buffer.alloc(w*h*s*s*4)};for(let y=0;y<h*s;y++)for(let x=0;x<w*s;x++){const sx=x0+Math.floor(x/s),sy=y0+Math.floor(y/s);if(sx<0||sy<0||sx>=im.w||sy>=im.h)continue;im.data.copy(out.data,(y*w*s+x)*4,(sy*im.w+sx)*4,(sy*im.w+sx)*4+4);}return out;}
module.exports.encode=encode;module.exports.crop=crop;
