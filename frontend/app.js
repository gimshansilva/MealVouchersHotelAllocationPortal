const DEV_PORTS = ['5500','5173'];
const API_BASE = localStorage.getItem('mvha_api_base') || (DEV_PORTS.includes(location.port) || location.protocol==='file:' ? 'http://localhost:5080/api' : '/api');
const state = {
  screen: 'login', service: 'meal', modal: null, flights: [], requests: [], selectedFlightId: null,
  user: JSON.parse(localStorage.getItem('mvha_user') || 'null'), token: localStorage.getItem('mvha_token') || null
};

const app = document.getElementById('app');
const toast = document.getElementById('toast');
const esc = (v='') => String(v).replace(/[&<>"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));

function showToast(message){
  toast.textContent=message; toast.classList.add('show');
  clearTimeout(showToast.t); showToast.t=setTimeout(()=>toast.classList.remove('show'),2600);
}
function fmtDateTime(v){
  if(!v) return '';
  const d=new Date(v); if(Number.isNaN(d.getTime())) return v;
  return d.toLocaleDateString('en-GB',{day:'2-digit',month:'2-digit',year:'numeric'}).replaceAll('/','-')+' '+d.toLocaleTimeString('en-GB',{hour:'2-digit',minute:'2-digit'});
}
function authHeaders(){ return state.token ? {Authorization:`Bearer ${state.token}`} : {}; }
async function api(path, options={}){
  const res=await fetch(`${API_BASE}${path}`,{...options,headers:{'Content-Type':'application/json',...authHeaders(),...(options.headers||{})}});
  const text=await res.text(); let data=null; try{data=text?JSON.parse(text):null}catch{}
  if(res.status===401 && state.token){ logout(true); throw new Error('Session expired. Please log in again.'); }
  if(!res.ok) throw new Error(data?.message || data?.title || text || `HTTP ${res.status}`);
  return data;
}

function logout(silent){
  localStorage.removeItem('mvha_token'); localStorage.removeItem('mvha_user');
  state.token=null; state.user=null; state.screen='login'; state.modal=null; state.flights=[]; state.requests=[]; state.selectedFlightId=null;
  if(!silent) render();
}
function fmtTime(v){
  if(!v) return '';
  const d=new Date(v); if(Number.isNaN(d.getTime())) return '';
  return d.toLocaleTimeString('en-GB',{hour:'2-digit',minute:'2-digit'});
}
function fmtDate(v){
  if(!v) return '';
  const d=new Date(v); if(Number.isNaN(d.getTime())) return '';
  return d.toLocaleDateString('en-GB',{day:'2-digit',month:'2-digit',year:'numeric'}).replaceAll('/','-');
}

function loginView(){
  return `<main class="login-page">
    <section class="login-card" aria-label="Login">
      <img class="login-logo" src="assets/portal-logo.png" alt="Meal and hotel allocation">
      <h1 class="login-title">Meal Vouchers &amp; Hotel<br>Allocation</h1>
      <div class="login-divider"><strong>Login</strong> Here</div>
      <form id="loginForm">
        <div class="login-field"><img class="field-icon" src="assets/icon-username.png" alt=""><input id="username" autocomplete="username" placeholder="Username" required></div>
        <div class="login-field"><img class="field-icon" src="assets/icon-password.png" alt=""><input id="password" type="password" autocomplete="current-password" placeholder="Password" required></div>
        <button class="login-button" type="submit">Login</button>
      </form>
      <img class="login-it-logo" src="assets/it-systems-logo.png" alt="SriLankan IT Systems">
    </section>
  </main>`;
}

function shellView(){
  const selected = state.selectedFlightId ? state.flights.find(f=>f.id===state.selectedFlightId) : null;
  const isMulti = state.requests.length>1;
  return `<div class="portal">
    <header class="header">
      <div class="brand"><img class="brand-logo" src="assets/portal-logo.png"><div class="brand-title">Meal Vouchers &amp; Hotel Allocation</div></div>
      <div class="header-right">
        <div class="user-block">${esc(state.user?.displayName||'')}<br>${esc(state.user?.employeeNo||'')}</div>
        <button class="user-avatar" id="avatarBtn" title="Account" aria-label="Account menu"><img src="assets/icon-user-avatar.png" alt=""></button><div class="user-menu" id="userMenu" hidden><button id="logoutBtn">Log out</button></div>
        <button id="changeService" class="service-button">Change Service</button>
      </div>
    </header>
    <div class="content-row">
      <aside class="sidebar">
        <button class="nav-img" data-nav="request" aria-label="Request"><img src="assets/nav-request.png" alt="Request"></button>
        <button class="nav-img" data-nav="history" aria-label="History"><img src="assets/nav-history.png" alt="History"></button>
      </aside>
      <main class="main">${requestView(selected,isMulti)}</main>
    </div>
    <footer class="footer">© 2026 SriLankan IT Systems. All rights reserved</footer>
  </div>
  ${state.modal==='service'?serviceModal():''}${state.modal?.type==='update' ? updateModal():''}`;
}

function requestView(selected,isMulti){
  const flightPanel = state.requests.length ? requestsPanel() : `<div class="panel"><h2 class="panel-title">Meal Request Flights</h2><div class="empty-state empty-tall"><img class="empty-image empty-meals" src="assets/empty-meals.png" alt="No Meal Requested Flights yet"></div></div>`;
  return `<div class="dashboard">
    <section class="panel">
      <h2 class="panel-title">Request Meal</h2>
      <label class="form-label" for="flightSelect">Select Flight</label>
      <select id="flightSelect" class="select"><option value="">Select Flight</option>${state.flights.map(f=>`<option value="${f.id}" ${selected?.id===f.id?'selected':''}>${esc(f.flightNo)} - ${esc(f.from)} - ${esc(f.to)}</option>`).join('')}</select>
      ${selected ? '' : '<div class="divider"></div>'}
      ${selected ? selectedFlightForm(selected) : `<div class="empty-state"><img class="empty-image empty-flight" src="assets/empty-flight.png" alt="No Selected Flight"></div>`}
    </section>
    ${flightPanel}
  </div>`;
}

function selectedFlightForm(f){
  return `<div class="flight-card">
    <div class="flight-grid-top"><div><div class="flight-label">Flight No</div><div class="flight-value">${esc(f.flightNo)}</div></div><div><div class="flight-label">Sector</div><div class="flight-value">${esc(f.from)} - ${esc(f.to)}</div></div><div><div class="flight-label">No of Pax</div><div class="flight-value">${esc(f.pax)}</div></div></div>
    <div class="flight-grid-bottom"><div><div class="flight-label">STD</div><div class="flight-value">${fmtDateTime(f.std)}</div></div><div><div class="flight-label">ETD</div><div class="flight-value">${fmtDateTime(f.etd)}</div></div></div>
    <img class="flight-watermark" src="assets/flight-watermark.png" alt="">
  </div>
  <div class="form-row"><div><label class="form-label">No of Meals Required</label><input id="meals" class="input" type="number" min="0" placeholder="No of Meals"></div><div><label class="form-label">Meal Type</label><select id="mealType" class="select"><option value="">Select Meal Type</option><option>Light Refreshment</option><option>Lunch</option><option>Dinner</option></select></div></div>
  <div style="margin-top:14px"><label class="form-label">Reason</label><select id="reason" class="select"><option value="">Select Reason</option><option>Flight Delay</option><option>Schedule Change</option><option>Operational Requirement</option></select></div>
  <div class="actions"><button class="outline-btn" id="cancelRequest">Cancel Request</button><button class="primary-action" id="setDelay">Set as a Delay Flight</button></div>`;
}

function requestsPanel(){
  return `<div class="panel"><h2 class="panel-title">Meal Request Flights</h2>${state.requests.map(r=>`<article class="request-card">
    <div class="rc-left">
      <div class="request-name">${esc(r.mealType)}</div>
      <div class="rc-route">
        <div class="rc-airport"><strong>${esc(r.from)}</strong><span class="rc-time">${esc(fmtTime(r.std))}</span><span class="rc-tag">STD</span></div>
        <div class="rc-mid"><img class="route-img" src="assets/route-plane.png" alt=""><strong>${esc(r.flightNo)}</strong><span class="rc-tag">${esc(isoDate(r.std))}</span></div>
        <div class="rc-airport"><strong>${esc(r.to)}</strong><span class="rc-time">${esc(fmtTime(r.etd))}</span><span class="rc-tag">ETD</span></div>
      </div>
    </div>
    <div class="rc-right">
      <div class="rc-stats">
        <div><span class="req-stat-label">No of Pax</span><div class="req-stat-value">${esc(r.pax)}</div></div>
        <div><span class="req-stat-label">Meal Requests</span><div class="req-stat-value">${esc(r.mealsRequired)}</div></div>
        <div><span class="req-stat-label">Meal Issued</span><div class="req-stat-value">${esc(r.mealsIssued??0)}</div></div>
      </div>
      <div class="request-action"><button class="update-btn" data-update="${r.id}">Update Meals</button><button class="delete-btn" data-delete="${r.id}" title="Delete" aria-label="Delete"><img src="assets/icon-trash.png" alt=""></button></div>
    </div>
  </article>`).join('')}</div>`;
}

function serviceModal(){
  const card=(key,icon,label)=>`<div class="svc-card ${state.service===key?'selected':''}" data-service="${key}" role="button" tabindex="0"><div class="svc-icon"><img src="assets/${icon}" alt=""></div><div class="svc-label">${label}</div></div>`;
  return `<div class="modal-backdrop" data-backdrop><div class="modal service-modal" role="dialog" aria-label="Select Service">
    <div class="update-head"><span>Select Service</span><button class="close" data-close aria-label="Close">×</button></div>
    <div class="service-body">
      <div class="service-options">${card('meal','icon-meal.png','Meal Vouchers')}${card('hotel','icon-hotel.png','Hotel Allocation')}</div>
      <button id="applyService" class="modal-apply">Apply</button>
    </div>
  </div></div>`;
}

function isoDate(v){
  const d=new Date(v); if(Number.isNaN(d.getTime())) return '';
  return `${d.getFullYear()}-${String(d.getMonth()+1).padStart(2,'0')}-${String(d.getDate()).padStart(2,'0')}`;
}
function updateModal(){
  const r=state.requests.find(x=>String(x.id)===String(state.modal?.id)); if(!r) return '';
  return `<div class="modal-backdrop" data-backdrop><div class="modal update-modal" role="dialog" aria-label="Update Meal Request">
    <div class="update-head"><span>Update Meal Request</span><button class="close" data-close aria-label="Close">×</button></div>
    <div class="update-body">
      <div class="update-card">
        <div class="update-flight">
          <div class="update-type">${esc(r.mealType)}</div>
          <div class="update-route">
            <div class="u-airport"><strong>${esc(r.from)}</strong><span class="u-time">${esc(fmtTime(r.std))}</span><span class="u-tag">STD</span></div>
            <div class="u-mid"><img class="route-img" src="assets/route-plane.png" alt=""><strong>${esc(r.flightNo)}</strong><span class="u-tag">${esc(isoDate(r.std))}</span></div>
            <div class="u-airport"><strong>${esc(r.to)}</strong><span class="u-time">${esc(fmtTime(r.etd))}</span><span class="u-tag">ETD</span></div>
          </div>
        </div>
        <div class="update-stats">
          <div><span class="u-label">No of Pax</span><strong>${esc(r.pax)}</strong></div>
          <div><span class="u-label">Meal Requests</span><strong>${esc(r.mealsRequired)}</strong></div>
          <div><span class="u-label">Meal Issued</span><strong>${esc(r.mealsIssued??0)}</strong></div>
        </div>
      </div>
      <div class="update-bottom">
        <div><label class="form-label" for="updateMeals">No of Meals Required</label><input id="updateMeals" class="input" type="number" min="1" placeholder="No of Meals"></div>
        <button id="updateSave" class="update-small-btn">Update</button>
      </div>
    </div>
  </div></div>`;
}

function render(){app.innerHTML=state.screen==='login'?loginView():shellView(); bind();}

function bind(){
  const login=document.getElementById('loginForm');
  if(login) login.addEventListener('submit',async e=>{e.preventDefault();try{const data=await api('/auth/login',{method:'POST',body:JSON.stringify({username:document.getElementById('username').value,password:document.getElementById('password').value})});state.token=data.token;state.user=data.user;localStorage.setItem('mvha_token',state.token);localStorage.setItem('mvha_user',JSON.stringify(state.user));state.screen='portal';state.modal='service';await loadData();render();}catch(err){showToast(err.message)}});
  document.getElementById('avatarBtn')?.addEventListener('click',e=>{e.stopPropagation();const m=document.getElementById('userMenu');m.hidden=!m.hidden});
  document.getElementById('logoutBtn')?.addEventListener('click',()=>logout());
  document.getElementById('changeService')?.addEventListener('click',()=>{state.modal='service';render()});
  document.querySelectorAll('[data-close]').forEach(b=>b.addEventListener('click',()=>{state.modal=null;render()}));
  document.querySelectorAll('[data-service]').forEach(x=>{const pick=()=>{state.service=x.dataset.service;render()};x.addEventListener('click',pick);x.addEventListener('keydown',e=>{if(e.key==='Enter'||e.key===' '){e.preventDefault();pick()}})});
  document.getElementById('applyService')?.addEventListener('click',()=>{if(state.service!=='meal'){showToast('Hotel Allocation is outside the seven supplied screens.');return;}state.modal=null;render()});
  document.querySelectorAll('[data-nav]').forEach(b=>b.addEventListener('click',()=>{if(b.dataset.nav==='history')showToast('History is outside the supplied seven screens.')}));
  const fs=document.getElementById('flightSelect');
  fs?.addEventListener('change',()=>{state.selectedFlightId=Number(fs.value)||null;render()});
  document.getElementById('cancelRequest')?.addEventListener('click',()=>{state.selectedFlightId=null;render()});
  document.getElementById('setDelay')?.addEventListener('click',submitRequest);
  document.querySelectorAll('[data-update]').forEach(b=>b.addEventListener('click',()=>{state.modal={type:'update',id:b.dataset.update};render()}));
  document.querySelectorAll('[data-delete]').forEach(b=>b.addEventListener('click',()=>deleteRequest(b.dataset.delete)));
  document.getElementById('updateSave')?.addEventListener('click',saveUpdate);
  document.getElementById('updateMeals')?.addEventListener('keydown',e=>{if(e.key==='Enter')saveUpdate()});
  document.querySelector('[data-backdrop]')?.addEventListener('mousedown',e=>{if(e.target===e.currentTarget){state.modal=null;render()}});
  if(state.modal?.type==='update') document.getElementById('updateMeals')?.focus();
}

async function loadData(){
  try{state.flights=await api('/flights');state.requests=await api('/meal-requests');return true}
  catch(err){showToast(err.message);return false}
}
async function submitRequest(){
  const f=state.flights.find(x=>x.id===state.selectedFlightId); const meals=Number(document.getElementById('meals')?.value); const mealType=document.getElementById('mealType')?.value; const reason=document.getElementById('reason')?.value;
  if(!f||!meals||!mealType||!reason){showToast('Please complete all meal request fields.');return}
  try{await api('/meal-requests',{method:'POST',body:JSON.stringify({flightId:f.id,mealsRequired:meals,mealType,reason})});state.selectedFlightId=null;await loadData();render();showToast('Meal request saved.')}catch(err){showToast(err.message)}
}
async function deleteRequest(id){if(!confirm('Delete this meal request?'))return;try{await api(`/meal-requests/${id}`,{method:'DELETE'});await loadData();render();showToast('Meal request deleted.')}catch(err){showToast(err.message)}}
async function saveUpdate(){const id=state.modal.id;const raw=document.getElementById('updateMeals')?.value;const meals=Number(raw);if(raw===''||raw==null||!Number.isInteger(meals)||meals<1){showToast('Enter a valid number of meals (1 or more).');return}try{await api(`/meal-requests/${id}`,{method:'PUT',body:JSON.stringify({mealsRequired:meals})});state.modal=null;await loadData();render();showToast('Meal request updated.')}catch(err){showToast(err.message)}}

async function boot(){
  if(state.token){
    state.screen='portal';
    const ok=await loadData();
    if(state.token){ render(); return; }   // still logged in (a 401 clears the token)
  }
  state.screen='login'; render();
}
document.addEventListener('click',()=>{const m=document.getElementById('userMenu');if(m)m.hidden=true});
document.addEventListener('keydown',e=>{if(e.key==='Escape'&&state.modal){state.modal=null;render()}});
boot();
