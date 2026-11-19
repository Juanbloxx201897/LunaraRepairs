import { useEffect, useMemo, useState } from 'react';
import { Link, NavLink, Route, Routes, useNavigate } from 'react-router-dom';
import { api, getToken, getUser, logout } from './services/api.js';

const STATUSES = ['Recibido','Diagnóstico','Pendiente de aprobación','Aprobado','En reparación','En pruebas','Listo para recoger','Entregado','Rechazado por el cliente'];
const money = value => value == null ? 'Pendiente' : new Intl.NumberFormat('es-CO',{style:'currency',currency:'COP',maximumFractionDigits:0}).format(value);
const date = value => value ? new Date(value).toLocaleDateString('es-CO') : '—';

// Estructura común de navegación y encabezado; las rutas cargan aquí cada pantalla.
function Shell({ role, userEmail, onLogout, children }) {
  const isAdmin = role === 'Administrador';
  const isTechnician = role === 'Técnico';
  return <div className="app-shell">
    <aside className="sidebar">
      <div className="brand-mark">L<span>.</span></div><div className="brand-name">lunara<span>repairs</span></div>
      <div className="sidebar-label">ESPACIO DE TRABAJO</div>
      <NavLink to="/" end className="nav-item">{isTechnician ? '▤' : '▦'} <span>{isTechnician ? 'Mi trabajo' : 'Resumen'}</span></NavLink>
      {(isAdmin || isTechnician) && <NavLink to="/reparaciones" className="nav-item">⚙ <span>{isTechnician ? 'Mis órdenes' : 'Reparaciones'}</span></NavLink>}
      {isAdmin && <>
        <NavLink to="/clientes" className="nav-item">♙ <span>Clientes</span></NavLink>
        <NavLink to="/dispositivos" className="nav-item">▣ <span>Dispositivos</span></NavLink>
        <NavLink to="/repuestos" className="nav-item">⚒ <span>Repuestos</span></NavLink>
        <NavLink to="/tecnicos" className="nav-item">♟ <span>Técnicos</span></NavLink>
      </>}
      <div className="sidebar-bottom"><div className="avatar">{userEmail?.[0]?.toUpperCase() || 'L'}</div><div><strong>{userEmail}</strong><small>{role}</small></div></div>
    </aside>
    <main className="main-area">
      <header className="topbar"><div><span className="eyebrow">LUNARA SOFTWARE</span><h1>Lunara Repairs</h1></div>
        <div className="role-switch">
          <span>{role}</span>
          <button type="button" className="btn btn-outline-secondary btn-sm ms-2" onClick={onLogout}>Cerrar sesión</button>
        </div>
      </header>{children}
      <footer>© {new Date().getFullYear()} Lunara Software <span>Hecho para simplificar cada reparación.</span></footer>
    </main>
  </div>;
}

// Componente visual reutilizable para representar métricas recibidas desde la API.
function MetricBars({ title, description, items }) {
  const maxValue = Math.max(1, ...items.map(item => item.value));
  return <section className="panel metric-chart">
    <div className="panel-heading"><div><h3>{title}</h3><p>{description}</p></div></div>
    {items.length === 0
      ? <div className="empty-state">Aún no hay datos para graficar.</div>
      : <div className="metric-bars">{items.map(item => (
        <div className="metric-bar-row" key={item.label}>
          <span title={item.label}>{item.label}</span>
          <div className="metric-bar-track" role="img" aria-label={`${item.label}: ${item.value}`}>
            <div className="metric-bar-fill" style={{ width: `${item.value === 0 ? 0 : Math.max(4, item.value / maxValue * 100)}%` }} />
          </div>
          <strong>{item.value}</strong>
        </div>
      ))}</div>}
  </section>;
}

// El dashboard consulta las métricas y los registros recientes; solo se ocupa de presentarlos.
function Dashboard({ user }) {
  const [stats,setStats]=useState(null), [repairs,setRepairs]=useState([]), [spareParts,setSpareParts]=useState([]), [error,setError]=useState('');
  useEffect(()=>{ Promise.all([api.stats(),api.repairs(),api.spareParts()]).then(([s,r,p])=>{setStats(s);setRepairs(r);setSpareParts(p)}).catch(e=>setError(e.message)); },[]);
  const cards = stats ? [
    ['Órdenes registradas',stats.total,'Todas las órdenes'],
    ['Por diagnosticar',stats.received,'Recién recibidas'],
    ['Esperando aprobación',stats.pendingApproval,'Requieren respuesta'],
    ['En reparación',stats.repairing,'Trabajo técnico'],
    ['Listas para recoger',stats.ready,'Pendientes de entrega'],
    ['Entregadas',stats.delivered,'Proceso finalizado'],
    ['Ingresos generados',money(stats.deliveredRevenue),'Órdenes entregadas']
  ] : [];
  const repairMetrics = stats ? [
    ['Recibido', stats.received], ['Diagnóstico', stats.diagnosing], ['Pendiente aprobación', stats.pendingApproval],
    ['Aprobado', stats.approved], ['En reparación', stats.repairing], ['En pruebas', stats.testing],
    ['Listo para recoger', stats.ready], ['Entregado', stats.delivered], ['Rechazado', stats.rejected]
  ].map(([label, value]) => ({ label, value })) : [];
  const inventoryMetrics = Object.values(spareParts.reduce((totals, part) => {
    totals[part.category] = totals[part.category] || { label: part.category, value: 0 };
    totals[part.category].value += part.quantity;
    return totals;
  }, {}));
  return <><section className="welcome"><div><span className="pill">PANEL GENERAL</span><h2>Bienvenido, <em>{user?.displayName || 'Administrador'}</em></h2><p>Supervisa las reparaciones y mantén cada servicio bajo control.</p></div><Link className="btn btn-lunara" to="/reparaciones">Ver reparaciones ↗</Link></section>
    {error && <div className="alert alert-warning">No se pudo conectar con la API: {error}. Comprueba que el backend y SQL Server estén funcionando.</div>}
    <div className="stats-grid">{cards.map((c,i)=><div className="stat-card" key={c[0]}><div className="stat-top"><span>{c[0]}</span><span className={`stat-icon icon-${i}`}>{['▦','◷','◉','⚒','✓','↗','＄'][i]}</span></div><strong className={i === 6 ? 'revenue-value' : ''}>{c[1]}</strong><small>{c[2]}</small></div>)}</div>
    <div className="metric-charts">
      <MetricBars title="Órdenes por estado" description="Distribución de las reparaciones registradas." items={repairMetrics} />
      <MetricBars title="Existencias por categoría" description="Unidades disponibles en el inventario de repuestos." items={inventoryMetrics} />
    </div>
    <section className="panel"><div className="panel-heading"><div><h3>Actividad reciente</h3><p>Últimas órdenes registradas en el sistema</p></div><Link to="/reparaciones">Ver todas →</Link></div>
      <RepairTable repairs={repairs.slice(0,6)} /></section>
    <section className="bottom-note"><div className="note-icon">✦</div><div><strong>Un solo lugar para cada reparación</strong><p>Desde la recepción hasta la entrega, cada actualización queda registrada.</p></div><span className="demo-label">MVP · DEMO</span></section>
  </>;
}

// Traduce el estado de una orden a una etiqueta con estilo, sin modificar el dato del servidor.
function StatusBadge({status}) {
  const className = status?.toLowerCase().includes('rechazado') ? 'rejected' : status?.includes('Entregado') ? 'done' : status?.includes('Listo') ? 'ready' : status?.includes('Pendiente') ? 'pending' : status?.includes('reparación') ? 'working' : 'neutral';
  return <span className={`status-badge ${className}`}>{status}</span>;
}
// Tabla compartida por el resumen, la lista de reparaciones y los portales de usuario.
function RepairTable({repairs}) {
  if (!repairs.length) return <div className="empty-state">No hay órdenes para mostrar.</div>;
  return <div className="table-responsive"><table className="table align-middle lunara-table"><thead><tr><th>ORDEN</th><th>CLIENTE</th><th>DISPOSITIVO</th><th>ESTADO</th><th>FECHA</th><th></th></tr></thead><tbody>
    {repairs.map(r=><tr key={r.id}><td><strong>{r.code}</strong></td><td>{r.client?.fullName || 'Cliente'}</td><td>{r.device ? `${r.device.brand} ${r.device.model}` : 'Dispositivo'}</td><td><StatusBadge status={r.status}/></td><td>{date(r.createdAt)}</td><td><Link to={`/reparaciones/${r.id}`} className="table-action">Abrir ↗</Link></td></tr>)}
  </tbody></table></div>;
}

function Repairs({ role }) {
  const [repairs,setRepairs]=useState([]),[search,setSearch]=useState(''),[error,setError]=useState(''),[refresh,setRefresh]=useState(0);
  useEffect(()=>{api.repairs(search).then(setRepairs).catch(e=>setError(e.message));},[search,refresh]);
  return <section className="panel page-panel"><div className="panel-heading"><div><span className="pill">{role === 'Técnico' ? 'MI TRABAJO' : 'OPERACIÓN'}</span><h3>{role === 'Técnico' ? 'Órdenes asignadas' : 'Reparaciones'}</h3><p>{role === 'Técnico' ? 'Consulta tus órdenes y actualiza cada equipo desde su detalle.' : 'Registra, asigna y supervisa el ciclo de vida de cada servicio.'}</p></div>{role === 'Administrador' && <Link to="/nueva-reparacion" className="btn btn-lunara">+ Nueva orden</Link>}</div>
    <input className="form-control search-input" placeholder="Buscar por código, cliente o dispositivo..." value={search} onChange={e=>setSearch(e.target.value)}/>
    {error && <div className="alert alert-warning">{error}</div>}<RepairTable repairs={repairs}/>
    <button className="btn btn-outline-secondary mt-3" onClick={()=>setRefresh(x=>x+1)}>Actualizar lista</button>
  </section>;
}

function ClientRepairHistory({ repairs }) {
  if (!repairs.length) return <p className="muted mb-0">Este cliente todavía no tiene reparaciones anteriores.</p>;

  return <div className="table-responsive">
    <table className="table align-middle lunara-table client-history-table">
      <thead><tr><th>ORDEN</th><th>EQUIPO</th><th>PROBLEMA</th><th>ESTADO</th><th>FECHA</th></tr></thead>
      <tbody>{repairs.map(repair => (
        <tr key={repair.id}>
          <td><Link to={`/reparaciones/${repair.id}`} className="table-action">{repair.code}</Link></td>
          <td>{repair.device}</td>
          <td>{repair.problemDescription}</td>
          <td><StatusBadge status={repair.status} /></td>
          <td>{date(repair.createdAt)}</td>
        </tr>
      ))}</tbody>
    </table>
  </div>;
}

// Guía el registro completo: crea cliente, dispositivo y orden mediante llamadas al backend.
function NewRepair() {
  const [techs, setTechs] = useState([]);
  const [form, setForm] = useState({
    cedula: '',
    fullName: '',
    email: '',
    phone: '',
    type: 'Celular',
    brand: '',
    model: '',
    serialNumber: '',
    deviceDescription: '',
    problemDescription: '',
    technicianId: ''
  });
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  const [clientLookup, setClientLookup] = useState('idle');
  const [matchedClient, setMatchedClient] = useState(null);
  const [clientHistory, setClientHistory] = useState([]);
  const navigate = useNavigate();

  useEffect(() => {
    api.technicians()
      .then(setTechs)
      .catch(() => setTechs([]));
  }, []);

  useEffect(() => {
    const cedula = form.cedula.trim().replace(/[.\s-]/g, '');
    if (!/^\d{5,20}$/.test(cedula)) {
      setClientLookup('idle');
      setMatchedClient(null);
      setClientHistory([]);
      return undefined;
    }

    let active = true;
    setClientLookup('searching');
    const timeout = setTimeout(async () => {
      try {
        const client = await api.clientByCedula(cedula);
        if (!active) return;

        setMatchedClient(client);
        setClientLookup(client ? 'found' : 'new');
        setClientHistory([]);
        if (client) {
          setForm(current => ({
            ...current,
            fullName: client.fullName,
            email: client.email,
            phone: client.phone
          }));
          const history = await api.clientRepairs(client.id);
          if (active) setClientHistory(history);
        }
      } catch (lookupError) {
        if (!active) return;
        setClientLookup('error');
        setError(`No se pudo consultar la cédula: ${lookupError.message}`);
      }
    }, 250);

    return () => {
      active = false;
      clearTimeout(timeout);
    };
  }, [form.cedula]);

  function set(key, value) {
    setForm(current => ({ ...current, [key]: value }));
  }

  function changeCedula(value) {
    setError('');
    setMatchedClient(null);
    setClientHistory([]);
    setClientLookup('idle');
    setForm(current => ({
      ...current,
      cedula: value,
      fullName: '',
      email: '',
      phone: ''
    }));
  }

  async function submit(e) {
    e.preventDefault();
    setError('');
    setBusy(true);

    try {
      // El backend registra el cliente (o lo reutiliza), el equipo y la orden en una operación.
      const repair = await api.registerRepair({
        cedula: form.cedula.trim().replace(/[.\s-]/g, ''),
        fullName: form.fullName.trim(),
        email: form.email.trim(),
        phone: form.phone.trim(),
        type: form.type,
        brand: form.brand.trim(),
        model: form.model.trim(),
        serialNumber: form.serialNumber.trim(),
        deviceDescription: form.deviceDescription.trim(),
        problemDescription: form.problemDescription.trim(),
        technicianId: form.technicianId
          ? Number(form.technicianId)
          : null,
      });

      navigate(`/reparaciones/${repair.id}`);
    } catch (err) {
      setError(
        `No se pudo completar el registro: ${err.message}. ` +
        'Comprueba la conexión con el backend.'
      );
    } finally {
      setBusy(false);
    }
  }

  return (
    <section className="panel form-panel">
      <Link to="/reparaciones" className="back-link">
        ← Volver a reparaciones
      </Link>

      <span className="pill">NUEVO SERVICIO</span>
      <h3>Registrar reparación</h3>
      <p className="muted">
        Registra al cliente, su equipo y la reparación en un solo paso.
      </p>

      {error && <div className="alert alert-warning">{error}</div>}

      <form onSubmit={submit} className="form-grid">
        <div className="full-width">
          <h5>Información del cliente</h5>
        </div>

        <div>
          <label className="form-label" htmlFor="repair-client-cedula">Cédula *</label>
          <input
            id="repair-client-cedula"
            className="form-control"
            required
            inputMode="numeric"
            autoComplete="off"
            value={form.cedula}
            onChange={e => changeCedula(e.target.value)}
            placeholder="Escribe la cédula para buscar"
          />
          {clientLookup === 'searching' && <small className="muted">Buscando cliente…</small>}
          {clientLookup === 'found' && <small className="client-lookup-found">Cliente encontrado; se conservará su historial.</small>}
          {clientLookup === 'new' && <small className="muted">Cédula nueva: se registrará un cliente.</small>}
          {clientLookup === 'error' && <small className="text-danger">No se pudo verificar la cédula.</small>}
        </div>

        <div>
          <label className="form-label" htmlFor="repair-client-name">Nombre completo *</label>
          <input
            id="repair-client-name"
            className="form-control"
            required
            readOnly={Boolean(matchedClient)}
            value={form.fullName}
            onChange={e => set('fullName', e.target.value)}
            placeholder="Nombre del cliente"
          />
        </div>

        <div>
          <label className="form-label" htmlFor="repair-client-email">Correo electrónico *</label>
          <input
            id="repair-client-email"
            className="form-control"
            type="email"
            required
            readOnly={Boolean(matchedClient)}
            value={form.email}
            onChange={e => set('email', e.target.value)}
            placeholder="cliente@correo.com"
          />
        </div>

        <div>
          <label className="form-label" htmlFor="repair-client-phone">Teléfono con indicativo *</label>
          <input
            id="repair-client-phone"
            className="form-control"
            type="tel"
            required
            pattern="\+[1-9][0-9]{7,14}"
            title="Incluye el indicativo internacional, sin espacios; por ejemplo +573001234567."
            readOnly={Boolean(matchedClient)}
            value={form.phone}
            onChange={e => set('phone', e.target.value)}
            placeholder="+573001234567"
          />
        </div>

        {matchedClient && <div className="full-width client-history-preview">
          <h5>Reparaciones anteriores de {matchedClient.fullName}</h5>
          {clientHistory.length
            ? <ClientRepairHistory repairs={clientHistory} />
            : <p className="muted mb-0">Este cliente todavía no tiene reparaciones anteriores.</p>}
        </div>}

        <div className="full-width">
          <hr />
          <h5>Información del equipo</h5>
        </div>

        <div>
          <label className="form-label">Tipo de equipo *</label>
          <select
            className="form-select"
            value={form.type}
            onChange={e => set('type', e.target.value)}
          >
            {['Celular', 'Portátil', 'Tableta', 'Consola', 'Computador', 'Otro']
              .map(type => <option key={type}>{type}</option>)}
          </select>
        </div>

        <div>
          <label className="form-label">Marca *</label>
          <input
            className="form-control"
            required
            value={form.brand}
            onChange={e => set('brand', e.target.value)}
            placeholder="Ej. Samsung"
          />
        </div>

        <div>
          <label className="form-label">Modelo *</label>
          <input
            className="form-control"
            required
            value={form.model}
            onChange={e => set('model', e.target.value)}
            placeholder="Ej. Galaxy A54"
          />
        </div>

        <div>
          <label className="form-label">Número de serie *</label>
          <input
            className="form-control"
            required
            value={form.serialNumber}
            onChange={e => set('serialNumber', e.target.value)}
            placeholder="Número de serie o identificador del equipo"
          />
        </div>

        <div className="full-width">
          <hr />
          <h5>Detalles del equipo y la reparación</h5>
        </div>

        <div className="full-width">
          <label className="form-label">Descripción del equipo *</label>
          <input
            className="form-control"
            required
            value={form.deviceDescription}
            onChange={e => set('deviceDescription', e.target.value)}
            placeholder="Color, accesorios, estado físico u otros detalles"
          />
        </div>

        <div className="full-width">
          <label className="form-label">Problema reportado *</label>
          <textarea
            className="form-control"
            rows="4"
            required
            value={form.problemDescription}
            onChange={e => set('problemDescription', e.target.value)}
            placeholder="¿Qué falla presenta el equipo?"
          />
        </div>

        <div className="full-width">
          <label className="form-label">Técnico asignado</label>
          <select
            className="form-select"
            value={form.technicianId}
            onChange={e => set('technicianId', e.target.value)}
          >
            <option value="">Sin asignar</option>
            {techs.map(t => (
              <option key={t.id} value={t.id}>{t.fullName}</option>
            ))}
          </select>
        </div>

        <div className="full-width form-actions">
          <button className="btn btn-lunara" disabled={busy || clientLookup === 'searching' || clientLookup === 'error'}>
            {busy ? 'Registrando...' : 'Crear reparación'}
          </button>
        </div>
      </form>
    </section>
  );
}


// Muestra el detalle y ofrece acciones que se validan y persisten en el backend.
function RepairDetail({ role }) {
  const { pathname } = window.location;
  const id=pathname.split('/').pop();
  const [repair,setRepair]=useState(null),[error,setError]=useState(''),[diagnosis,setDiagnosis]=useState(''),[budget,setBudget]=useState(''),[note,setNote]=useState(''),[work,setWork]=useState('');
  const [technicians,setTechnicians]=useState([]),[assignedTechnicianId,setAssignedTechnicianId]=useState('');
  const [busy,setBusy]=useState(false);
  const load=()=>api.repair(id).then(r=>{setRepair(r);setDiagnosis(r.diagnosis||'');setBudget(r.budget??'');setWork(r.workPerformed||'');setAssignedTechnicianId(r.technicianId == null ? '' : String(r.technicianId));}).catch(e=>setError(e.message));
  useEffect(()=>{load();},[id]);
  useEffect(()=>{if(role==='Administrador')api.technicians().then(setTechnicians).catch(e=>setError(e.message));},[role]);
  async function act(fn){setBusy(true);setError('');try{await fn();await load();}catch(e){setError(e.message);}finally{setBusy(false);}}
  if(!repair)return <section className="panel">{error||'Cargando reparación...'}</section>;
  return <section className="panel detail-panel"><Link to="/reparaciones" className="back-link">← Todas las reparaciones</Link>
    {error&&<div className="alert alert-warning">{error}</div>}
    <div className="detail-header"><div><span className="pill">ORDEN {repair.code}</span><h3>{repair.device?.brand} {repair.device?.model}</h3><p>{repair.problemDescription}</p></div><StatusBadge status={repair.status}/></div>
    <div className="detail-grid"><div className="detail-box"><small>CLIENTE</small><strong>{repair.client?.fullName}</strong><span>Cédula: {repair.client?.cedula}</span><span>{repair.client?.email}</span></div><div className="detail-box"><small>TÉCNICO</small><strong>{repair.technician?.fullName||'Sin asignar'}</strong><span>Creada el {date(repair.createdAt)}</span></div><div className="detail-box"><small>PRESUPUESTO</small><strong>{money(repair.budget)}</strong><span>Valor estimado</span></div></div>
    {role === 'Administrador' && <div className="workflow-box"><h4>Asignación del técnico</h4><div className="d-flex gap-2 flex-wrap"><select className="form-select" style={{ maxWidth: 360 }} aria-label="Asignar técnico" value={assignedTechnicianId} onChange={e=>setAssignedTechnicianId(e.target.value)}><option value="">Sin asignar</option>{technicians.map(technician=><option key={technician.id} value={technician.id}>{technician.fullName} · {technician.workload} activas · {technician.availability}</option>)}</select><button className="btn btn-lunara" disabled={busy || String(repair.technicianId ?? '') === assignedTechnicianId} onClick={()=>act(()=>api.assignTechnician(id,assignedTechnicianId ? Number(assignedTechnicianId) : null))}>Guardar asignación</button></div></div>}
    <div className="timeline"><h4>Seguimiento del servicio</h4>{(repair.history||[]).map(h=><div className="timeline-item" key={h.id}><span className="timeline-dot"/><div><strong>{h.newStatus}</strong><p>{h.note||'Actualización de estado'}</p><small>{new Date(h.changedAt).toLocaleString('es-CO')}</small></div></div>)}</div>
    {role === 'Técnico' && (repair.status==='Recibido'||repair.status==='Diagnóstico')&&<div className="workflow-box"><h4>Diagnóstico y presupuesto</h4><label className="form-label">Diagnóstico</label><textarea className="form-control mb-3" rows="3" value={diagnosis} onChange={e=>setDiagnosis(e.target.value)}/><label className="form-label">Presupuesto (COP)</label><input type="number" min="0" className="form-control mb-3" value={budget} onChange={e=>setBudget(e.target.value)} required/><label className="form-label">Nota para el historial</label><input className="form-control mb-3" value={note} onChange={e=>setNote(e.target.value)} placeholder="Observación opcional"/><button className="btn btn-lunara" disabled={busy||!diagnosis.trim()||budget==='' } onClick={()=>act(()=>api.diagnosis(id,{diagnosis,budget:Number(budget),note}))}>Enviar presupuesto al administrador</button></div>}
    {role === 'Técnico' && repair.status==='En reparación'&&<div className="workflow-box"><h4>Registrar trabajo realizado</h4><textarea className="form-control mb-3" rows="3" value={work} onChange={e=>setWork(e.target.value)} placeholder="Describe el trabajo técnico..."/><button className="btn btn-outline-primary" disabled={busy||!work.trim()} onClick={()=>act(()=>api.work(id,work,'Trabajo registrado'))}>Guardar trabajo</button></div>}
    {(role === 'Técnico' || role === 'Administrador' || role === 'Cliente') && <div className="workflow-box"><h4>{role === 'Técnico' ? 'Actualizar estado del equipo' : role === 'Administrador' ? 'Revisión del presupuesto' : 'Estado del presupuesto'}</h4><div className="action-wrap">
      {role === 'Administrador' && repair.status==='Pendiente de aprobación'&&<><button className="btn btn-success" disabled={busy} onClick={()=>act(()=>api.status(id,'Aprobado','Presupuesto aprobado por el administrador'))}>Aprobar presupuesto</button><button className="btn btn-outline-danger" disabled={busy} onClick={()=>act(()=>api.status(id,'Rechazado por el cliente','Presupuesto rechazado por el administrador'))}>Rechazar presupuesto</button></>}
      {role === 'Cliente' && <span className="muted">{repair.status==='Pendiente de aprobación' ? 'El presupuesto está pendiente de revisión del administrador.' : `El estado actual de tu servicio es: ${repair.status}.`}</span>}
      {role === 'Técnico' && repair.status==='Aprobado'&&<button className="btn btn-lunara" disabled={busy} onClick={()=>act(()=>api.status(id,'En reparación','Inicio de reparación'))}>Iniciar reparación</button>}
      {role === 'Técnico' && repair.status==='En reparación'&&<button className="btn btn-lunara" disabled={busy} onClick={()=>act(()=>api.status(id,'En pruebas','Reparación terminada; pasa a pruebas'))}>Enviar a pruebas</button>}
      {role === 'Técnico' && repair.status==='En pruebas'&&<><button className="btn btn-lunara" disabled={busy} onClick={()=>act(()=>api.status(id,'Listo para recoger','Pruebas satisfactorias'))}>Pruebas satisfactorias</button><button className="btn btn-outline-warning" disabled={busy} onClick={()=>act(()=>api.status(id,'En reparación','Se requieren ajustes adicionales'))}>Requiere ajustes</button></>}
      {role === 'Técnico' && repair.status==='Listo para recoger'&&<button className="btn btn-success" disabled={busy} onClick={()=>act(()=>api.status(id,'Entregado','Dispositivo entregado al cliente'))}>Registrar entrega</button>}
      {['Entregado','Rechazado por el cliente'].includes(repair.status)&&<span className="muted">Esta orden ha finalizado.</span>}
      {role === 'Técnico' && repair.status==='Pendiente de aprobación'&&<span className="muted">Esperando revisión del presupuesto por el administrador.</span>}
    </div></div>}
    {role === 'Administrador' && <p className="muted">El administrador aprueba o rechaza los presupuestos. Los cambios técnicos de diagnóstico y estado los realiza el técnico asignado.</p>}
  </section>;
}

// Pantalla de directorio: presenta los clientes y envía el formulario a la API.
function Clients() {
  const [clients, setClients] = useState([]);
  const [error, setError] = useState('');
  const [search, setSearch] = useState('');
  const [expandedClientId, setExpandedClientId] = useState(null);
  const [historyByClient, setHistoryByClient] = useState({});
  const [historyLoadingId, setHistoryLoadingId] = useState(null);
  const [historyError, setHistoryError] = useState('');

  const load = () => api.clients().then(setClients).catch(e => setError(e.message));
  useEffect(() => { load(); }, []);

  const searchTerm = search.trim().toLocaleLowerCase();
  const cedulaSearch = searchTerm.replace(/[.\s-]/g, '');
  const filteredClients = clients.filter(client =>
    client.cedula?.includes(cedulaSearch) ||
    [client.fullName, client.email]
      .some(value => value?.toLocaleLowerCase().includes(searchTerm))
  );

  async function toggleHistory(client) {
    if (expandedClientId === client.id) {
      setExpandedClientId(null);
      return;
    }

    setExpandedClientId(client.id);
    setHistoryError('');
    if (Object.hasOwn(historyByClient, client.id)) return;

    setHistoryLoadingId(client.id);
    try {
      const history = await api.clientRepairs(client.id);
      setHistoryByClient(current => ({ ...current, [client.id]: history }));
    } catch (e) {
      setHistoryError(e.message);
    } finally {
      setHistoryLoadingId(null);
    }
  }

  return <section className="panel page-panel">
    <span className="pill">DIRECTORIO</span>
    <h3>Clientes</h3>
    <p className="muted">Busca clientes por cédula, nombre o correo y consulta su historial de servicio.</p>
    {error && <div className="alert alert-warning" role="alert">{error}</div>}
    <div className="client-directory-toolbar">
      <input className="form-control search-input" aria-label="Buscar clientes" placeholder="Busca por cédula, nombre o correo…" value={search} onChange={e => setSearch(e.target.value)} />
      {search && <button type="button" className="btn btn-outline-secondary" onClick={() => setSearch('')}>Limpiar</button>}
      <span className="muted">{filteredClients.length} {filteredClients.length === 1 ? 'cliente' : 'clientes'}</span>
      <Link to="/nueva-reparacion" className="btn btn-lunara">+ Registrar reparación</Link>
    </div>
    <div className="table-responsive"><table className="table lunara-table"><thead><tr><th>CÉDULA</th><th>NOMBRE</th><th>CORREO</th><th>TELÉFONO</th><th>HISTORIAL</th></tr></thead><tbody>
      {filteredClients.map(client => <tr key={client.id}>
        <td><strong>{client.cedula}</strong></td>
        <td>{client.fullName}</td>
        <td>{client.email}</td>
        <td>{client.phone}</td>
        <td>
          <button type="button" className="btn btn-sm btn-outline-primary" onClick={() => toggleHistory(client)}>
            {expandedClientId === client.id ? 'Ocultar' : 'Ver reparaciones'}
          </button>
          {expandedClientId === client.id && <div className="client-history-preview">
            {historyLoadingId === client.id && <p className="muted">Cargando historial…</p>}
            {historyError && <div className="alert alert-warning">{historyError}</div>}
            {Object.hasOwn(historyByClient, client.id) && <ClientRepairHistory repairs={historyByClient[client.id]} />}
          </div>}
        </td>
      </tr>)}
      {!filteredClients.length && <tr><td colSpan="5"><div className="empty-state">No se encontraron clientes.</div></td></tr>}
    </tbody></table></div>
  </section>;
}

// Pantalla visual de equipos: carga clientes y dispositivos y envía nuevos registros a la API.
function Devices() {
  const [clients,setClients]=useState([]),[devices,setDevices]=useState([]),[error,setError]=useState(''),[message,setMessage]=useState('');
  const [form,setForm]=useState({clientId:'',type:'Celular',brand:'',model:'',serialNumber:'',description:''});
  const load=()=>Promise.all([api.clients(),api.devices()]).then(([c,d])=>{setClients(c);setDevices(d);}).catch(e=>setError(e.message));
  useEffect(()=>{load();},[]);
  async function submit(e){e.preventDefault();setError('');setMessage('');try{await api.createDevice({...form,clientId:Number(form.clientId)});setMessage('Dispositivo registrado.');setForm({clientId:'',type:'Celular',brand:'',model:'',serialNumber:'',description:''});load();}catch(e){setError(e.message);}}
  return <section className="panel page-panel"><span className="pill">INVENTARIO</span><h3>Dispositivos</h3><p className="muted">Registra los equipos que ingresan al negocio.</p>{error&&<div className="alert alert-warning">{error}</div>}{message&&<div className="alert alert-success">{message}</div>}
    <form onSubmit={submit} className="form-grid compact-form"><div><label className="form-label">Cliente *</label><select className="form-select" required value={form.clientId} onChange={e=>setForm({...form,clientId:e.target.value})}><option value="">Seleccionar</option>{clients.map(c=><option key={c.id} value={c.id}>{c.cedula} — {c.fullName}</option>)}</select></div><div><label className="form-label">Tipo *</label><select className="form-select" required value={form.type} onChange={e=>setForm({...form,type:e.target.value})}>{['Celular','Portátil','Tableta','Consola','Otro'].map(x=><option key={x}>{x}</option>)}</select></div><div><label className="form-label">Marca *</label><input className="form-control" required value={form.brand} onChange={e=>setForm({...form,brand:e.target.value})}/></div><div><label className="form-label">Modelo *</label><input className="form-control" required value={form.model} onChange={e=>setForm({...form,model:e.target.value})}/></div><div><label className="form-label">Serial *</label><input className="form-control" required value={form.serialNumber} onChange={e=>setForm({...form,serialNumber:e.target.value})}/></div><div><label className="form-label">Descripción *</label><input className="form-control" required value={form.description} onChange={e=>setForm({...form,description:e.target.value})}/></div><div className="full-width"><button className="btn btn-lunara">Registrar dispositivo</button></div></form>
    <div className="table-responsive mt-4"><table className="table lunara-table"><thead><tr><th>DISPOSITIVO</th><th>TIPO</th><th>CLIENTE</th><th>SERIAL</th></tr></thead><tbody>{devices.map(d=><tr key={d.id}><td><strong>{d.brand} {d.model}</strong></td><td>{d.type}</td><td>{d.client?.fullName||clients.find(c=>c.id===d.clientId)?.fullName}</td><td>{d.serialNumber||'—'}</td></tr>)}</tbody></table></div>
  </section>;
}

// Inventario visual; el backend conserva los repuestos y genera su código de identificación.
function SpareParts() {
  const [parts, setParts] = useState([]);
  const [error, setError] = useState('');
  const [message, setMessage] = useState('');
  const [search, setSearch] = useState('');
  const [categoryFilter, setCategoryFilter] = useState('');
  const [busy, setBusy] = useState(false);
  const [form, setForm] = useState({ name: '', category: '', brand: '', description: '', quantity: '0', unitPrice: '' });
  const load = () => api.spareParts().then(setParts).catch(e => setError(e.message));
  useEffect(() => { load(); }, []);

  const brands = [...new Map(parts.map(part => [part.brand.trim().toLocaleLowerCase(), part.brand.trim()])).values()]
    .sort((a, b) => a.localeCompare(b, 'es'));
  const categories = [...new Set(parts.map(part => part.category.trim()).filter(Boolean))]
    .sort((a, b) => a.localeCompare(b, 'es'));
  const filteredParts = parts.filter(part => {
    const term = search.trim().toLocaleLowerCase();
    const matchesSearch = !term || [part.code, part.name, part.brand, part.category]
      .some(value => value.toLocaleLowerCase().includes(term));
    return matchesSearch && (!categoryFilter || part.category === categoryFilter);
  });

  async function submit(e) {
    e.preventDefault();
    setError('');
    setMessage('');
    setBusy(true);
    try {
      const created = await api.createSparePart({
        ...form,
        name: form.name.trim(),
        category: form.category.trim(),
        brand: form.brand.trim(),
        description: form.description.trim(),
        quantity: Number(form.quantity),
        unitPrice: Number(form.unitPrice)
      });
      setForm({ name: '', category: '', brand: '', description: '', quantity: '0', unitPrice: '' });
      setMessage(`Nuevo repuesto registrado: ${created.name}. Código de identificación: ${created.code}.`);
      await load();
    } catch (e) {
      setError(e.message);
    } finally {
      setBusy(false);
    }
  }

  return <section className="panel page-panel">
    <div className="panel-heading"><div><span className="pill">INVENTARIO</span><h3>Repuestos</h3><p>Clasifica y controla las piezas por categoría, marca y existencias.</p></div><Link to="/repuestos/pedidos" className="btn btn-lunara">Lista de compras →</Link></div>
    {error && <div className="alert alert-warning" role="alert">{error}</div>}
    {message && <div className="alert alert-success" role="status">{message}</div>}
    <form onSubmit={submit} className="form-grid compact-form spare-part-form">
      <div><label className="form-label" htmlFor="part-name">Nombre del repuesto *</label><input id="part-name" className="form-control" required maxLength="120" value={form.name} onChange={e => setForm({ ...form, name: e.target.value })} placeholder="Ej. Pantalla para celular" /></div>
      <div><label className="form-label" htmlFor="part-category">Categoría *</label><input id="part-category" className="form-control" required maxLength="80" list="spare-part-categories" value={form.category} onChange={e => setForm({ ...form, category: e.target.value })} placeholder="Ej. Pantallas" /><datalist id="spare-part-categories">{categories.map(category => <option key={category} value={category} />)}</datalist></div>
      <div><label className="form-label" htmlFor="part-brand">Marca *</label><input id="part-brand" className="form-control" required maxLength="80" list="spare-part-brands" value={form.brand} onChange={e => setForm({ ...form, brand: e.target.value })} placeholder="Elige o escribe una marca" /><datalist id="spare-part-brands">{brands.map(brand => <option key={brand} value={brand} />)}</datalist></div>
      <div><label className="form-label" htmlFor="part-quantity">Cantidad *</label><input id="part-quantity" className="form-control" type="number" min="0" step="1" required value={form.quantity} onChange={e => setForm({ ...form, quantity: e.target.value })} /></div>
      <div><label className="form-label" htmlFor="part-price">Precio unitario (COP) *</label><input id="part-price" className="form-control" type="number" min="0" step="1" required value={form.unitPrice} onChange={e => setForm({ ...form, unitPrice: e.target.value })} /></div>
      <div><label className="form-label" htmlFor="part-description">Descripción *</label><input id="part-description" className="form-control" required maxLength="300" value={form.description} onChange={e => setForm({ ...form, description: e.target.value })} placeholder="Detalles del repuesto" /></div>
      <div className="full-width"><button className="btn btn-lunara" type="submit" disabled={busy}>{busy ? 'Guardando…' : 'Agregar repuesto'}</button></div>
    </form>
    <div className="inventory-filters">
      <input className="form-control" aria-label="Buscar repuestos" placeholder="Buscar por código, nombre o marca…" value={search} onChange={e => setSearch(e.target.value)} />
      <select className="form-select" aria-label="Filtrar por categoría" value={categoryFilter} onChange={e => setCategoryFilter(e.target.value)}><option value="">Todas las categorías</option>{categories.map(category => <option key={category}>{category}</option>)}</select>
    </div>
    <div className="table-responsive"><table className="table align-middle lunara-table"><thead><tr><th>CÓDIGO</th><th>REPUESTO</th><th>CATEGORÍA</th><th>MARCA</th><th>EXISTENCIAS</th><th>PRECIO UNITARIO</th></tr></thead><tbody>
      {filteredParts.map(part => <tr key={part.id}><td><strong>{part.code}</strong></td><td><strong>{part.name}</strong>{part.description && <small className="part-description">{part.description}</small>}</td><td>{part.category}</td><td>{part.brand}</td><td><span className={`stock-badge ${part.quantity === 0 ? 'stock-empty' : part.quantity <= 5 ? 'stock-low' : ''}`}>{part.quantity} {part.quantity === 1 ? 'unidad' : 'unidades'}</span></td><td>{money(part.unitPrice)}</td></tr>)}
      {!filteredParts.length && <tr><td colSpan="6"><div className="empty-state">{parts.length ? 'No hay repuestos que coincidan con los filtros.' : 'Aún no hay repuestos registrados.'}</div></td></tr>}
    </tbody></table></div>
  </section>;
}

function Technicians() {
  const [technicians, setTechnicians] = useState([]);
  const [error, setError] = useState('');
  const [message, setMessage] = useState('');
  const [busy, setBusy] = useState(false);
  const [passwords, setPasswords] = useState({});
  const [secrets, setSecrets] = useState({});
  const [form, setForm] = useState({ fullName: '', email: '', password: '', secret: import.meta.env.DEV ? 'admin' : '' });
  const load = () => api.technicians().then(setTechnicians).catch(e => setError(e.message));
  useEffect(() => { load(); }, []);

  async function create(e) {
    e.preventDefault();
    setError('');
    setMessage('');
    setBusy(true);
    try {
      const created = await api.createTechnician(form);
      setForm({ fullName: '', email: '', password: '', secret: import.meta.env.DEV ? 'admin' : '' });
      setMessage(`Técnico creado. Usuario: ${created.username}. Comparte la contraseña inicial de forma segura.`);
      await load();
    } catch (e) {
      setError(e.message);
    } finally {
      setBusy(false);
    }
  }

  async function updateAccess(technician, active = !technician.accountActive, changePassword = false) {
    setError('');
    setMessage('');
    setBusy(true);
    try {
      const needsPassword = !technician.hasAccount || changePassword;
      await api.updateTechnicianAccess(technician.id, {
        active,
        newPassword: needsPassword ? passwords[technician.id] || null : null,
        secret: needsPassword ? secrets[technician.id] || (import.meta.env.DEV ? 'admin' : null) : null
      });
      setPasswords(current => ({ ...current, [technician.id]: '' }));
      setSecrets(current => ({ ...current, [technician.id]: '' }));
      setMessage(`Acceso de ${technician.fullName} actualizado.`);
      await load();
    } catch (e) {
      setError(e.message);
    } finally {
      setBusy(false);
    }
  }

  return <section className="panel page-panel">
    <span className="pill">EQUIPO DE TRABAJO</span>
    <h3>Técnicos y accesos</h3>
    <p className="muted">Crea cuentas, revisa la carga de trabajo y activa o suspende sesiones. El usuario de acceso se genera con la parte del correo anterior a @.</p>
    {error && <div className="alert alert-warning" role="alert">{error}</div>}
    {message && <div className="alert alert-success" role="status">{message}</div>}
    <form onSubmit={create} className="form-grid compact-form technician-create-form">
      <div><label className="form-label">Nombre completo *</label><input className="form-control" required value={form.fullName} onChange={e => setForm({ ...form, fullName: e.target.value })} /></div>
      <div><label className="form-label">Correo del técnico y base de su usuario *</label><input className="form-control" type="email" required value={form.email} onChange={e => setForm({ ...form, email: e.target.value })} placeholder="usuario@empresa.com" /></div>
      <div><label className="form-label">Contraseña inicial (mín. 6 caracteres) *</label><input className="form-control" type="password" minLength="6" autoComplete="new-password" required value={form.password} onChange={e => setForm({ ...form, password: e.target.value })} /></div>
      <div><label className="form-label">Clave secreta de administración *</label><input className="form-control" type="password" required autoComplete="current-password" placeholder="admin en Desarrollo" value={form.secret} onChange={e => setForm({ ...form, secret: e.target.value })} /></div>
      <div className="full-width"><button className="btn btn-lunara" disabled={busy}>{busy ? 'Guardando…' : 'Crear técnico y cuenta'}</button><small className="muted ms-3">En Desarrollo, la clave secreta es admin.</small></div>
    </form>

    <div className="table-responsive technicians-table mt-4"><table className="table align-middle lunara-table">
      <thead><tr><th>TÉCNICO</th><th>ACCESO</th><th>ÓRDENES ACTIVAS</th><th>DISPONIBILIDAD</th><th>GESTIÓN DE CUENTA</th></tr></thead>
      <tbody>{technicians.map(technician => <tr key={technician.id}>
        <td><strong>{technician.fullName}</strong><small className="part-description">Usuario: {technician.username || technician.email.split('@')[0]}</small><small className="part-description">{technician.email}</small></td>
        <td><span className={`status-badge ${technician.accountActive ? 'ready' : technician.hasAccount ? 'rejected' : 'neutral'}`}>{technician.hasAccount ? technician.accountActive ? 'Activo' : 'Suspendido' : 'Sin cuenta'}</span></td>
        <td><strong>{technician.workload}</strong></td>
        <td><span className={`status-badge ${technician.availability === 'Disponible' ? 'ready' : technician.availability === 'Alta carga' ? 'pending' : 'working'}`}>{technician.availability}</span></td>
        <td><div className="technician-access-actions">
          <input className="form-control" aria-label={`${technician.hasAccount ? 'Nueva contraseña' : 'Contraseña inicial'} de ${technician.fullName}`} type="password" minLength="6" placeholder={technician.hasAccount ? 'Nueva contraseña (mín. 6)' : 'Contraseña inicial (mín. 6)'} value={passwords[technician.id] || ''} onChange={e => setPasswords({ ...passwords, [technician.id]: e.target.value })} />
          {Boolean(passwords[technician.id]) && <input className="form-control" aria-label={`Clave secreta de administración para ${technician.fullName}`} type="password" placeholder="Clave secreta (admin)" value={secrets[technician.id] || (import.meta.env.DEV ? 'admin' : '')} onChange={e => setSecrets({ ...secrets, [technician.id]: e.target.value })} />}
          <button type="button" className={`btn btn-sm ${technician.accountActive ? 'btn-outline-danger' : 'btn-outline-success'}`} disabled={busy || (!technician.hasAccount && ((passwords[technician.id] || '').length < 6 || !(secrets[technician.id] || import.meta.env.DEV)))} onClick={() => updateAccess(technician, technician.hasAccount ? !technician.accountActive : true)}>
            {technician.hasAccount ? technician.accountActive ? 'Suspender' : 'Activar' : 'Crear acceso y activar'}
          </button>
          {technician.hasAccount && Boolean(passwords[technician.id]) && <button type="button" className="btn btn-sm btn-outline-primary" disabled={busy || passwords[technician.id].length < 6 || !(secrets[technician.id] || import.meta.env.DEV)} onClick={() => updateAccess(technician, technician.accountActive, true)}>Cambiar clave</button>}
        </div></td>
      </tr>)}
      {!technicians.length && <tr><td colSpan="5"><div className="empty-state">Aún no hay técnicos registrados.</div></td></tr>}</tbody>
    </table></div>
    <p className="muted small mt-3">La disponibilidad se calcula por órdenes activas: disponible (0–1), ocupado (2–3) y alta carga (4 o más).</p>
  </section>;
}

function SparePartPurchases() {
  const [parts, setParts] = useState([]);
  const [purchases, setPurchases] = useState([]);
  const [error, setError] = useState('');
  const [message, setMessage] = useState('');
  const [busy, setBusy] = useState(false);
  const [filter, setFilter] = useState('');
  const [form, setForm] = useState({ sparePartId: '', name: '', category: '', brand: '', description: '', supplier: '', quantity: '1', unitPrice: '0' });
  const load = () => Promise.all([api.spareParts(), api.sparePartPurchases()])
    .then(([inventory, orders]) => { setParts(inventory); setPurchases(orders); })
    .catch(e => setError(e.message));
  useEffect(() => { load(); }, []);

  function choosePart(value) {
    const part = parts.find(item => String(item.id) === value);
    setForm(current => ({
      ...current,
      sparePartId: value,
      name: part?.name || '',
      category: part?.category || '',
      brand: part?.brand || '',
      description: part?.description || '',
      unitPrice: part ? String(part.unitPrice) : '0'
    }));
  }

  async function submit(e) {
    e.preventDefault();
    setError('');
    setMessage('');
    setBusy(true);
    try {
      await api.createSparePartPurchase({
        ...form,
        sparePartId: form.sparePartId ? Number(form.sparePartId) : null,
        quantity: Number(form.quantity),
        unitPrice: Number(form.unitPrice)
      });
      setMessage('Solicitud agregada a la lista de compras.');
      setForm({ sparePartId: '', name: '', category: '', brand: '', description: '', supplier: '', quantity: '1', unitPrice: '0' });
      await load();
    } catch (e) {
      setError(e.message);
    } finally {
      setBusy(false);
    }
  }

  async function markPurchased(order) {
    setError('');
    setMessage('');
    setBusy(true);
    try {
      await api.markSparePartPurchased(order.id);
      setMessage(`${order.name} marcado como comprado; se actualizaron las existencias.`);
      await load();
    } catch (e) {
      setError(e.message);
    } finally {
      setBusy(false);
    }
  }

  const visible = purchases.filter(order => !filter || order.status === filter);
  return <section className="panel page-panel">
    <div className="panel-heading"><div><span className="pill">ABASTECIMIENTO</span><h3>Pedidos y compras de repuestos</h3><p>Registra qué hace falta, a qué proveedor se pide y confirma su compra.</p></div><Link to="/repuestos" className="btn btn-outline-primary">Ver inventario</Link></div>
    {error && <div className="alert alert-warning" role="alert">{error}</div>}
    {message && <div className="alert alert-success" role="status">{message}</div>}
    <form onSubmit={submit} className="form-grid compact-form purchase-form">
      <div className="full-width"><label className="form-label">Repuesto del inventario (opcional)</label><select className="form-select" value={form.sparePartId} onChange={e => choosePart(e.target.value)}><option value="">Es un repuesto nuevo</option>{parts.map(part => <option key={part.id} value={part.id}>{part.code} — {part.name} ({part.quantity} disponibles)</option>)}</select></div>
      {!form.sparePartId && <>
        <div><label className="form-label">Nombre *</label><input className="form-control" required value={form.name} onChange={e => setForm({ ...form, name: e.target.value })} /></div>
        <div><label className="form-label">Categoría *</label><input className="form-control" required value={form.category} onChange={e => setForm({ ...form, category: e.target.value })} /></div>
        <div><label className="form-label">Marca *</label><input className="form-control" required value={form.brand} onChange={e => setForm({ ...form, brand: e.target.value })} /></div>
        <div><label className="form-label">Descripción</label><input className="form-control" value={form.description} onChange={e => setForm({ ...form, description: e.target.value })} /></div>
      </>}
      <div><label className="form-label">Proveedor *</label><input className="form-control" required value={form.supplier} onChange={e => setForm({ ...form, supplier: e.target.value })} placeholder="Empresa o contacto" /></div>
      <div><label className="form-label">Cantidad *</label><input className="form-control" type="number" min="1" step="1" required value={form.quantity} onChange={e => setForm({ ...form, quantity: e.target.value })} /></div>
      <div><label className="form-label">Precio unitario estimado (COP) *</label><input className="form-control" type="number" min="0" step="1" required value={form.unitPrice} onChange={e => setForm({ ...form, unitPrice: e.target.value })} /></div>
      <div className="full-width"><button className="btn btn-lunara" disabled={busy}>{busy ? 'Guardando…' : 'Agregar a lista de compras'}</button></div>
    </form>
    <div className="purchase-filters">
      <div className="purchase-counts"><span className="status-badge pending">{purchases.filter(x => x.status === 'Pedido').length} pedidos pendientes</span><span className="status-badge ready">{purchases.filter(x => x.status === 'Comprado').length} comprados</span></div>
      <select className="form-select" aria-label="Filtrar compras" value={filter} onChange={e => setFilter(e.target.value)}><option value="">Todos los pedidos</option><option>Pedido</option><option>Comprado</option></select>
    </div>
    <div className="table-responsive"><table className="table align-middle lunara-table"><thead><tr><th>REPUESTO</th><th>CANTIDAD</th><th>PROVEEDOR</th><th>PRECIO UNITARIO</th><th>ESTADO</th><th>FECHA</th><th></th></tr></thead><tbody>
      {visible.map(order => <tr key={order.id}><td><strong>{order.name}</strong><small className="part-description">{order.brand} · {order.category}</small></td><td>{order.quantity}</td><td>{order.supplier}</td><td>{money(order.unitPrice)}</td><td><span className={`status-badge ${order.status === 'Comprado' ? 'ready' : 'pending'}`}>{order.status}</span></td><td>{date(order.purchasedAt || order.createdAt)}</td><td>{order.status === 'Pedido' && <button className="btn btn-sm btn-outline-success" disabled={busy} onClick={() => markPurchased(order)}>Marcar comprado</button>}</td></tr>)}
      {!visible.length && <tr><td colSpan="7"><div className="empty-state">No hay pedidos en esta categoría.</div></td></tr>}
    </tbody></table></div>
  </section>;
}

// Portal por rol. Filtra en pantalla las órdenes devueltas por la API para la experiencia de demostración.
function DemoPortal({ role, user }) {
  const [repairs, setRepairs] = useState([]);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState('');

  const isTechnician = role === 'Técnico';

  useEffect(() => {
    api.repairs()
      .then(setRepairs)
      .catch(e => setError(e.message))
      .finally(() => setLoading(false));
  }, []);

  const filtered = repairs.filter(r => {
    const matchesRole = isTechnician
      ? r.technicianId === user?.technicianId
      : r.clientId === user?.clientId;

    const text = [
      r.id,
      r.problemDescription,
      r.status,
      r.device?.brand,
      r.device?.model
    ].filter(Boolean).join(' ').toLowerCase();

    return matchesRole && text.includes(search.toLowerCase());
  });

  const active = filtered.filter(r =>
    r.status !== 'Entregado' &&
    r.status !== 'Rechazado por el cliente'
  );

  const completed = filtered.filter(r =>
    r.status === 'Entregado'
  );

  return (
    <section className="page-panel">
      <div className="panel mb-4">
        <span className="pill">
          PORTAL DE {role.toUpperCase()}
        </span>

        <h3 className="mt-2">
          {isTechnician
            ? `Bienvenido, ${user?.displayName || user?.username || 'técnico'}`
            : 'Seguimiento de mis equipos'}
        </h3>

        <p className="muted">
          {isTechnician
            ? 'Consulta tus órdenes asignadas y revisa su avance.'
            : 'Consulta el estado de tus servicios y las novedades de reparación.'}
        </p>

        <div className="form-grid mt-3">
          <div className="panel">
            <span className="muted small">
              {isTechnician ? 'Órdenes asignadas' : 'Servicios registrados'}
            </span>
            <h3>{filtered.length}</h3>
          </div>

          <div className="panel">
            <span className="muted small">En proceso</span>
            <h3>{active.length}</h3>
          </div>

          <div className="panel">
            <span className="muted small">Entregados</span>
            <h3>{completed.length}</h3>
          </div>
        </div>
      </div>

      <div className="panel">
        <div className="d-flex justify-content-between align-items-center flex-wrap gap-2 mb-3">
          <div>
            <h4>
              {isTechnician ? 'Mis órdenes de trabajo' : 'Mis servicios'}
            </h4>
            <p className="muted small mb-0">
              Busca por número de orden, estado o descripción.
            </p>
          </div>

          <input
            className="form-control"
            style={{ maxWidth: '300px' }}
            placeholder="Buscar reparación..."
            value={search}
            onChange={e => setSearch(e.target.value)}
          />
        </div>

        {loading && <p>Cargando reparaciones...</p>}

        {error && (
          <div className="alert alert-warning">
            No se pudieron cargar las reparaciones: {error}
          </div>
        )}

        {!loading && !error && filtered.length === 0 && (
          <div className="text-center py-4">
            <h5>No hay reparaciones para mostrar</h5>
            <p className="muted">
              Cuando haya servicios disponibles para este perfil,
              aparecerán aquí.
            </p>
          </div>
        )}

        {!loading && !error && filtered.length > 0 && (
          <RepairTable repairs={filtered} />
        )}
      </div>

      {isTechnician && <p className="small muted mt-3">Solo se muestran las órdenes asignadas a tu cuenta. Actualiza el diagnóstico y el estado desde el detalle de cada equipo.</p>}
    </section>
  );
}


// Formulario de acceso; la verificación de credenciales corresponde al backend.
function Login({ onLogin }) {
  const [profile, setProfile] = useState('Admin');
  const [username, setUsername] = useState('admin');
  const [password, setPassword] = useState('');
  const [recoverySecret, setRecoverySecret] = useState('');
  const [newAdminPassword, setNewAdminPassword] = useState('');
  const [recovering, setRecovering] = useState(false);
  const [error, setError] = useState('');
  const [message, setMessage] = useState('');
  const [busy, setBusy] = useState(false);

  async function submit(e) {
    e.preventDefault();
    setError('');
    setMessage('');
    setBusy(true);
    try {
      await api.login(username.trim(), profile === 'Client' ? null : password, profile);
      onLogin();
    } catch (err) {
      setError(err.message || 'No fue posible iniciar sesión.');
    } finally {
      setBusy(false);
    }
  }

  async function recoverAdmin(e) {
    e.preventDefault();
    setError('');
    setMessage('');
    setBusy(true);
    try {
      await api.recoverAdminPassword({
        username: username.trim(),
        recoverySecret,
        newPassword: newAdminPassword
      });
      setMessage('Contraseña restablecida. Inicia sesión con la nueva contraseña.');
      setPassword('');
      setRecoverySecret('');
      setNewAdminPassword('');
      setRecovering(false);
    } catch (err) {
      setError(err.message || 'No fue posible restablecer la contraseña.');
    } finally {
      setBusy(false);
    }
  }

  function chooseProfile(nextProfile) {
    setProfile(nextProfile);
    setUsername(nextProfile === 'Admin' ? 'admin' : '');
    setPassword('');
    setError('');
    setMessage('');
    setRecovering(false);
  }

  const identifierLabel = profile === 'Admin'
    ? 'Usuario de administrador'
    : profile === 'Technician'
      ? 'Usuario de técnico'
      : 'Cédula del cliente';

  return (
    <main className="main-area" style={{ minHeight: '100vh', display: 'grid', placeItems: 'center', padding: '24px' }}>
      <section className="panel login-panel" style={{ width: '100%', maxWidth: '480px' }}>
        <div className="brand-mark">L<span>.</span></div>
        <div className="brand-name">lunara<span>repairs</span></div>
        <h2 className="mt-4">Bienvenido a Lunara</h2>
        <p className="muted">Elige el perfil que usarás para iniciar sesión.</p>
        <div className="login-profiles" role="tablist" aria-label="Perfil de acceso">
          {[['Admin', 'Administrador'], ['Technician', 'Técnico'], ['Client', 'Cliente']].map(([value, label]) => (
            <button
              key={value}
              type="button"
              role="tab"
              aria-selected={profile === value}
              className={`login-profile ${profile === value ? 'active' : ''}`}
              onClick={() => chooseProfile(value)}
            >
              {label}
            </button>
          ))}
        </div>
        {error && <div className="alert alert-danger">{error}</div>}
        {message && <div className="alert alert-success" role="status">{message}</div>}
        {!recovering ? <form onSubmit={submit}>
          <div className="mb-3">
            <label className="form-label" htmlFor="login-username">{identifierLabel}</label>
            <input
              id="login-username"
              className="form-control"
              type="text"
              autoComplete="username"
              required
              value={username}
              onChange={e => setUsername(e.target.value)}
              placeholder={profile === 'Admin' ? 'admin' : profile === 'Technician' ? 'Usuario asignado por el administrador' : 'Número de cédula'}
            />
            {profile === 'Technician' && <small className="muted">Es el usuario mostrado en tu cuenta; no es necesario escribir el correo completo.</small>}
          </div>
          {profile !== 'Client' && <div className="mb-3">
            <label className="form-label" htmlFor="login-password">Contraseña</label>
            <input id="login-password" className="form-control" type="password" autoComplete="current-password" required value={password} onChange={e => setPassword(e.target.value)} placeholder={profile === 'Admin' ? 'admin' : ''} />
          </div>}
          {profile === 'Admin' && <small className="muted d-block mb-3">Acceso de demostración: admin / admin.</small>}
          {profile === 'Client' && <small className="muted d-block mb-3">Ingresa tu cédula para consultar únicamente tus servicios.</small>}
          <button className="btn btn-lunara w-100" type="submit" disabled={busy}>
            {busy ? 'Iniciando sesión…' : `Iniciar sesión como ${profile === 'Admin' ? 'administrador' : profile === 'Technician' ? 'técnico' : 'cliente'}`}
          </button>
          {profile === 'Admin' && <button type="button" className="login-recovery-link" onClick={() => { setRecovering(true); setError(''); setMessage(''); }}>
            ¿Perdiste la contraseña del administrador?
          </button>}
        </form> : <form onSubmit={recoverAdmin}>
          <p className="muted mb-3">Restablece el acceso con el secreto privado configurado en el servidor.</p>
          <div className="mb-3">
            <label className="form-label" htmlFor="recovery-username">Usuario de administrador</label>
            <input id="recovery-username" className="form-control" required value={username} onChange={e => setUsername(e.target.value)} />
          </div>
          <div className="mb-3">
            <label className="form-label" htmlFor="recovery-secret">Secreto de recuperación</label>
            <input id="recovery-secret" className="form-control" type="password" autoComplete="off" required value={recoverySecret} onChange={e => setRecoverySecret(e.target.value)} />
          </div>
          <div className="mb-3">
            <label className="form-label" htmlFor="recovery-password">Nueva contraseña</label>
            <input id="recovery-password" className="form-control" type="password" minLength="12" autoComplete="new-password" required value={newAdminPassword} onChange={e => setNewAdminPassword(e.target.value)} />
          </div>
          <button className="btn btn-lunara w-100" type="submit" disabled={busy}>
            {busy ? 'Restableciendo…' : 'Restablecer contraseña'}
          </button>
          <button type="button" className="login-recovery-link" onClick={() => { setRecovering(false); setError(''); }}>
            Volver al inicio de sesión
          </button>
        </form>}
      </section>
    </main>
  );
}

// Punto de entrada: recupera la sesión y decide entre login, portal de usuario o panel administrativo.
export default function App() {
  const [sessionVersion, setSessionVersion] = useState(0);
  const token = getToken();
  const user = token ? getUser() : null;

  function refreshSession() {
    setSessionVersion(v => v + 1);
  }

  function handleLogout() {
    logout();
    refreshSession();
  }

  useEffect(() => {
    const expireSession = () => refreshSession();
    window.addEventListener('lunara-session-expired', expireSession);
    return () => window.removeEventListener('lunara-session-expired', expireSession);
  }, []);

  if (!token || !user) {
    return <Login onLogin={refreshSession} />;
  }

  const roleMap = {
    Admin: 'Administrador',
    Administrator: 'Administrador',
    Technician: 'Técnico',
    Client: 'Cliente'
  };
  const role = roleMap[user.role] || user.role || 'Usuario';
  const isAdmin = role === 'Administrador';
  const isTechnician = role === 'Técnico';

  return (
    <Shell role={role} userEmail={user.username || user.email} onLogout={handleLogout}>
      <div className="content-area">
        <Routes>
          <Route path="/" element={role === 'Administrador' ? <Dashboard user={user} /> : <DemoPortal role={role} user={user} />} />
          <Route path="/reparaciones" element={isAdmin || isTechnician ? <Repairs role={role} /> : <DemoPortal role={role} user={user} />} />
          <Route path="/nueva-reparacion" element={isAdmin ? <NewRepair /> : <DemoPortal role={role} user={user} />} />
          <Route path="/reparaciones/:id" element={<RepairDetail role={role} />} />
          <Route path="/clientes" element={isAdmin ? <Clients /> : <DemoPortal role={role} user={user} />} />
          <Route path="/dispositivos" element={isAdmin ? <Devices /> : <DemoPortal role={role} user={user} />} />
          <Route path="/repuestos" element={isAdmin ? <SpareParts /> : <DemoPortal role={role} user={user} />} />
          <Route path="/repuestos/pedidos" element={isAdmin ? <SparePartPurchases /> : <DemoPortal role={role} user={user} />} />
          <Route path="/tecnicos" element={isAdmin ? <Technicians /> : <DemoPortal role={role} user={user} />} />
          <Route path="*" element={<div className="panel"><h3>Página no encontrada</h3><Link to="/">Volver al inicio</Link></div>} />
        </Routes>
      </div>
    </Shell>
  );
}
