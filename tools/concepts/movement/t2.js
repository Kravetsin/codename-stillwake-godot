const E=require('./engine.js');const {P,MAPS,Grid,stepNow,stepNew}=E;
const tall={g:21,rows:Array.from({length:20},(_,i)=>i===0||i===19?'#'.repeat(12):'#..........#'),w:12,h:20};
const tallP={...tall,g:14,planet:true};
function arc(fn,map,hold,label){const w=new Grid(map);let h={x:5500,y:19000,vx:0,vy:0,dx:0,dy:0,climbing:false,facing:1};const y0=h.y;let minY=h.y,land=null,apex=null;
 for(let i=0;i<200;i++){const inp={x:0,y:0,jump:i===0,jumpHeld:i<hold,sprint:false};h=fn(w,h,inp);if(h.y<minY){minY=h.y;apex=i+1};if(i>2&&h.y===y0&&h.vy===0&&land===null)land=i+1;}
 console.log(label.padEnd(26),'rise',((y0-minY)/1000).toFixed(2),'apex',Math.round(apex*1000/60),'ms  air',Math.round(land*1000/60),'ms');}
for (const j of [400,405,410,420]){P.jump=j;console.log('jump',j);
arc(stepNow,tall,99,'now aboard');arc(stepNew,tall,99,'new hold');arc(stepNew,tall,1,'new tap');arc(stepNew,tall,10,'new 10');
arc(stepNow,tallP,99,'now planet');arc(stepNew,tallP,99,'new planet hold');arc(stepNew,tallP,1,'new planet tap');}
