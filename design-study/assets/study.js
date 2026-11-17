(function(){
const $=(s,r=document)=>r.querySelector(s),$$=(s,r=document)=>[...r.querySelectorAll(s)];
const ls={get(k){try{return localStorage.getItem(k)}catch(e){return null}},set(k,v){try{localStorage.setItem(k,v)}catch(e){}}};
// theme
const t=ls.get('lmsd-theme');if(t)document.documentElement.dataset.theme=t;
const tb=$('#themeBtn');if(tb)tb.onclick=()=>{const d=document.documentElement;const n=(d.dataset.theme==='dark')?'light':'dark';d.dataset.theme=n;ls.set('lmsd-theme',n)};
const mb=$('#menuBtn');if(mb)mb.onclick=()=>$('nav.side').classList.toggle('open');
// copy buttons
$$('pre').forEach(p=>{const b=document.createElement('button');b.className='cp';b.textContent='Copy';b.onclick=()=>{const txt=p.querySelector('code').innerText;(navigator.clipboard?navigator.clipboard.writeText(txt):Promise.reject()).then(()=>{b.textContent='Copied';setTimeout(()=>b.textContent='Copy',1200)}).catch(()=>{const r=document.createRange();r.selectNodeContents(p.querySelector('code'));const s=getSelection();s.removeAllRanges();s.addRange(r);b.textContent='Selected'})};p.appendChild(b)});
// reveal
const io='IntersectionObserver' in window?new IntersectionObserver(es=>es.forEach(e=>{if(e.isIntersecting){e.target.classList.add('in');io.unobserve(e.target)}}),{threshold:0}):null;
$$('.reveal').forEach(e=>io?io.observe(e):e.classList.add('in'));
// typing terminals
$$('.term[data-lines]').forEach(el=>{const lines=JSON.parse(el.dataset.lines);let started=false;const run=()=>{if(started)return;started=true;let i=0,j=0;el.innerHTML='<span class="cur"> </span>';let out='';(function tick(){if(i>=lines.length)return;const L=lines[i];const typed=L.startsWith('$');if(j<L.length){out+=L[j++];el.innerHTML=out.replace(/</g,'&lt;')+'<span class="cur"> </span>';setTimeout(tick,typed?22:3)}else{out+='\n';i++;j=0;setTimeout(tick,typed?350:120)}})()};
 if(io){const o=new IntersectionObserver(es=>{if(es[0].isIntersecting){run();o.disconnect()}},{threshold:.3});o.observe(el)}else run()});
// journey stepper
$$('.journey').forEach(j=>{const steps=JSON.parse(j.dataset.steps);let n=-1;const say=$('.say',j);const upd=()=>{$$('.node',j).forEach(x=>x.classList.remove('on'));if(n<0){say.textContent='Press Next to follow one request through the system.';return}const s=steps[n];(s.nodes||[]).forEach(id=>{const e=j.querySelector('[data-n="'+id+'"]');if(e)e.classList.add('on')});say.innerHTML='<b>Step '+(n+1)+'/'+steps.length+':</b> '+s.t};
 $('.nx',j).onclick=()=>{n=(n+1)%steps.length;upd()};$('.pv',j).onclick=()=>{n=n<=0?steps.length-1:n-1;upd()};let timer;$('.pl',j).onclick=function(){if(timer){clearInterval(timer);timer=null;this.textContent='Auto-play';return}this.textContent='Stop';timer=setInterval(()=>{n=(n+1)%steps.length;upd()},2200)};upd()});
// checklists
$$('.checklist').forEach(c=>{const key='lmsd-chk-'+location.pathname.split('/').pop()+'-'+(c.dataset.key||'x');let st={};try{st=JSON.parse(ls.get(key)||'{}')}catch(e){}
 $$('input',c).forEach((i,idx)=>{i.checked=!!st[idx];i.onchange=()=>{st[idx]=i.checked;ls.set(key,JSON.stringify(st));prog()}});prog();function prog(){const a=$$('input',c);const d=a.filter(x=>x.checked).length;const p=$('.prog',c);if(p)p.textContent=d+' / '+a.length+' done'}});
// quizzes
$$('.quiz .q').forEach(q=>{const ans=+q.dataset.a;$$('.opt',q).forEach((o,i)=>o.onclick=()=>{if(q.dataset.done)return;q.dataset.done=1;o.classList.add(i===ans?'right':'wrong');if(i!==ans)$$('.opt',q)[ans].classList.add('right');$('.why',q).classList.add('show');const all=$$('.quiz .q'),dn=all.filter(x=>x.dataset.done).length,ok=$$('.quiz .opt.right').length;const sc=$('#score');if(sc)sc.textContent='Answered '+dn+'/'+all.length})});
// page progress (visited)
const me=location.pathname.split('/').pop()||'index.html';let vis={};try{vis=JSON.parse(ls.get('lmsd-visited')||'{}')}catch(e){}vis[me]=1;ls.set('lmsd-visited',JSON.stringify(vis));
const links=$$('nav.side a[data-page]');links.forEach(a=>{if(vis[a.dataset.page])a.textContent='✓ '+a.textContent});
const bar=$('nav.side .bar i');if(bar)bar.style.width=Math.round(100*links.filter(a=>vis[a.dataset.page]).length/links.length)+'%';
})();
