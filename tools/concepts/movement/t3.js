const E=require('./engine.js');const {P,Grid,stepNew}=E;
const mk=(g,pl)=>({g,planet:pl,rows:Array.from({length:20},(_,i)=>i===0||i===19?'#'.repeat(12):'#..........#'),w:12,h:20});
function arc(map,hold){const w=new Grid(map);let h={x:5500,y:19000,vx:0,vy:0,dx:0,dy:0,climbing:false,facing:1};const y0=h.y;let minY=h.y,land=null;
 for(let i=0;i<200;i++){h=stepNew(w,h,{x:0,y:0,jump:i===0,jumpHeld:i<hold,sprint:false});minY=Math.min(minY,h.y);if(i>2&&h.y===y0&&h.vy===0&&land===null)land=i+1;}
 return ((y0-minY)/1000).toFixed(2)+'/'+Math.round(land*1000/60)+'ms';}
for (const j of [415,420]) for (const t of [30,40,50,60]) for(const m of [0.5,0.6]){P.jump=j;P.apexThr=t;P.apexMult=m;console.log(j,t,m,'ship',arc(mk(21,false),99),'planet',arc(mk(14,true),99),'tap',arc(mk(21,false),1));}
