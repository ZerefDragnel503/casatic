import { useEffect, useMemo, useState } from 'react';
import { useAuth } from '../../context/AuthContext';
import api from '../../api/client';
import {
  Receipt, Download, RefreshCw, Search, Save, X, CheckCircle2,
  AlertTriangle, Clock, Ban, Plus
} from 'lucide-react';

const ESTADOS = ['Pendiente', 'Pagada', 'Vencida', 'Anulada'];

function money(value) {
  return Number(value || 0).toLocaleString('en-US', { style: 'currency', currency: 'USD' });
}

function toDateInput(value) {
  if (!value) return '';
  return new Date(value).toISOString().slice(0, 10);
}

function fromDateInput(value) {
  return value ? new Date(`${value}T00:00:00Z`).toISOString() : null;
}

function estadoClass(estado) {
  if (estado === 'Pagada') return 'bg-emerald-50 text-emerald-700 border-emerald-200';
  if (estado === 'Vencida') return 'bg-red-50 text-red-700 border-red-200';
  if (estado === 'Anulada') return 'bg-surface-100 text-surface-600 border-surface-200';
  return 'bg-amber-50 text-amber-700 border-amber-200';
}

function estadoIcon(estado) {
  if (estado === 'Pagada') return CheckCircle2;
  if (estado === 'Vencida') return AlertTriangle;
  if (estado === 'Anulada') return Ban;
  return Clock;
}

async function downloadFactura(url, filename) {
  const response = await api.get(url, { responseType: 'blob' });
  const blobUrl = URL.createObjectURL(response.data);
  const link = document.createElement('a');
  link.href = blobUrl;
  link.download = filename;
  document.body.appendChild(link);
  link.click();
  link.remove();
  URL.revokeObjectURL(blobUrl);
}

function FacturaCard({ factura, onEdit, isAdmin }) {
  const Icon = estadoIcon(factura.estado);
  return (
    <div className="card-base p-5 sm:p-6">
      <div className="flex flex-col lg:flex-row lg:items-start lg:justify-between gap-5">
        <div className="min-w-0">
          <div className="flex flex-wrap items-center gap-2 mb-2">
            <span className="font-mono text-xs bg-surface-100 text-surface-600 px-2.5 py-1 rounded-lg">
              {factura.numero}
            </span>
            <span className={`inline-flex items-center gap-1.5 text-xs font-bold px-2.5 py-1 rounded-full border ${estadoClass(factura.estado)}`}>
              <Icon size={13} /> {factura.estado}
            </span>
          </div>
          <h3 className="text-lg font-bold text-surface-900 truncate">{factura.socioNombre}</h3>
          <p className="text-sm text-surface-500 mt-1">{factura.descripcion}</p>
          <div className="flex flex-wrap gap-x-5 gap-y-1 mt-3 text-xs text-surface-500">
            <span>Plan: <strong className="text-surface-700">{factura.planNombre}</strong></span>
            <span>Periodo: <strong className="text-surface-700">{factura.planPeriodo}</strong></span>
            <span>Vence: <strong className="text-surface-700">{toDateInput(factura.fechaVencimiento)}</strong></span>
            <span>DTE: <strong className="text-surface-700">{factura.selloRecepcion ? 'Con sello' : 'Interna'}</strong></span>
          </div>
          {factura.numeroControl && (
            <p className="font-mono text-[11px] text-surface-400 mt-2 truncate">{factura.numeroControl}</p>
          )}
        </div>

        <div className="lg:text-right flex-shrink-0">
          <p className="text-xs uppercase tracking-widest text-surface-400 font-bold">Total</p>
          <p className="text-2xl font-extrabold text-casatic-700">{money(factura.total)}</p>
          <p className="text-xs text-surface-400">IVA incluido: {money(factura.iva)}</p>
          <div className="flex flex-wrap justify-start lg:justify-end gap-2 mt-4">
            {isAdmin && (
              <button onClick={() => onEdit(factura)} className="btn-secondary">
                <Save size={16} /> Editar
              </button>
            )}
            <button
              onClick={() => downloadFactura(
                isAdmin ? `/facturacion/${factura.id}/descargar` : '/facturacion/mi-factura/descargar',
                `Factura-${factura.numero}.html`
              )}
              className="btn-primary"
            >
              <Download size={16} /> Descargar
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}

function FacturaForm({ factura, planes, socios, onSave, onCancel }) {
  const isNew = !factura.id;
  const [form, setForm] = useState(() => ({
    socioId: factura.socioId || socios[0]?.id || '',
    tipoDocumento: factura.tipoDocumento || 'Factura interna',
    codigoGeneracion: factura.codigoGeneracion || '',
    numeroControl: factura.numeroControl || '',
    selloRecepcion: factura.selloRecepcion || '',
    ambiente: factura.ambiente || 'Produccion',
    condicionOperacion: factura.condicionOperacion || 'Credito',
    formaPago: factura.formaPago || 'Transferencia',
    referenciaPago: factura.referenciaPago || '',
    planNombre: factura.planNombre || planes[1]?.nombre || planes[0]?.nombre || 'Socios Miembros',
    planPeriodo: factura.planPeriodo || planes[1]?.periodo || 'anual',
    descripcion: factura.descripcion || planes[1]?.descripcion || 'Membresia CASATIC',
    subtotal: factura.subtotal ?? planes[1]?.montoSugerido ?? 400,
    estado: factura.estado || 'Pendiente',
    fechaEmision: toDateInput(factura.fechaEmision || new Date().toISOString()),
    fechaVencimiento: toDateInput(factura.fechaVencimiento || new Date(Date.now() + 30 * 86400000).toISOString()),
    fechaPago: toDateInput(factura.fechaPago),
    notas: factura.notas || '',
  }));
  const [saving, setSaving] = useState(false);

  const applyPlan = (nombre) => {
    const plan = planes.find(p => p.nombre === nombre);
    if (!plan) return setForm(prev => ({ ...prev, planNombre: nombre }));
    setForm(prev => ({
      ...prev,
      planNombre: plan.nombre,
      planPeriodo: plan.periodo,
      descripcion: plan.descripcion,
      subtotal: plan.montoSugerido,
    }));
  };

  const submit = async (e) => {
    e.preventDefault();
    setSaving(true);
    await onSave(factura.id, {
      ...form,
      subtotal: Number(form.subtotal),
      fechaEmision: fromDateInput(form.fechaEmision),
      fechaVencimiento: fromDateInput(form.fechaVencimiento),
      fechaPago: form.estado === 'Pagada' ? fromDateInput(form.fechaPago || form.fechaEmision) : null,
    });
    setSaving(false);
  };

  return (
    <div className="fixed inset-0 z-50 bg-black/40 backdrop-blur-sm flex items-center justify-center p-4">
      <form onSubmit={submit} className="bg-white w-full max-w-2xl rounded-2xl shadow-elevated border border-surface-200 overflow-hidden">
        <div className="px-6 py-4 border-b border-surface-100 flex items-center justify-between">
          <div>
            <h2 className="font-bold text-surface-900">{isNew ? 'Nueva factura' : `Editar factura ${factura.numero}`}</h2>
            <p className="text-sm text-surface-500">{isNew ? 'Selecciona el socio y define el plan' : factura.socioNombre}</p>
          </div>
          <button type="button" onClick={onCancel} className="btn-icon btn-ghost"><X size={18} /></button>
        </div>

        <div className="p-6 grid grid-cols-1 md:grid-cols-2 gap-4 max-h-[75vh] overflow-y-auto">
          {isNew && (
            <div className="md:col-span-2">
              <label className="input-label">Cliente / socio</label>
              <select value={form.socioId} onChange={(e) => setForm({ ...form, socioId: e.target.value })} className="input-field" required>
                <option value="" disabled>Seleccionar socio</option>
                {socios.map(socio => <option key={socio.id} value={socio.id}>{socio.nombreEmpresa}</option>)}
              </select>
            </div>
          )}
          <div>
            <label className="input-label">Plan</label>
            <select value={form.planNombre} onChange={(e) => applyPlan(e.target.value)} className="input-field">
              {planes.map(plan => <option key={plan.nombre} value={plan.nombre}>{plan.nombre}</option>)}
            </select>
          </div>
          <div>
            <label className="input-label">Estado</label>
            <select value={form.estado} onChange={(e) => setForm({ ...form, estado: e.target.value })} className="input-field">
              {ESTADOS.map(e => <option key={e} value={e}>{e}</option>)}
            </select>
          </div>
          <div>
            <label className="input-label">Periodo</label>
            <input value={form.planPeriodo} onChange={(e) => setForm({ ...form, planPeriodo: e.target.value })} className="input-field" />
          </div>
          <div>
            <label className="input-label">Subtotal</label>
            <input type="number" min="0" step="0.01" value={form.subtotal} onChange={(e) => setForm({ ...form, subtotal: e.target.value })} className="input-field" />
          </div>
          <div>
            <label className="input-label">Tipo documento</label>
            <input value={form.tipoDocumento} onChange={(e) => setForm({ ...form, tipoDocumento: e.target.value })} className="input-field" />
          </div>
          <div>
            <label className="input-label">Ambiente</label>
            <select value={form.ambiente} onChange={(e) => setForm({ ...form, ambiente: e.target.value })} className="input-field">
              <option value="Produccion">Produccion</option>
              <option value="Pruebas">Pruebas</option>
            </select>
          </div>
          <div>
            <label className="input-label">Condicion</label>
            <select value={form.condicionOperacion} onChange={(e) => setForm({ ...form, condicionOperacion: e.target.value })} className="input-field">
              <option value="Credito">Credito</option>
              <option value="Contado">Contado</option>
              <option value="Otro">Otro</option>
            </select>
          </div>
          <div>
            <label className="input-label">Forma de pago</label>
            <input value={form.formaPago} onChange={(e) => setForm({ ...form, formaPago: e.target.value })} className="input-field" />
          </div>
          <div>
            <label className="input-label">Emision</label>
            <input type="date" value={form.fechaEmision} onChange={(e) => setForm({ ...form, fechaEmision: e.target.value })} className="input-field" />
          </div>
          <div>
            <label className="input-label">Vencimiento</label>
            <input type="date" value={form.fechaVencimiento} onChange={(e) => setForm({ ...form, fechaVencimiento: e.target.value })} className="input-field" />
          </div>
          <div className="md:col-span-2">
            <label className="input-label">Descripcion</label>
            <input value={form.descripcion} onChange={(e) => setForm({ ...form, descripcion: e.target.value })} className="input-field" />
          </div>
          <div>
            <label className="input-label">Codigo de generacion</label>
            <input value={form.codigoGeneracion} onChange={(e) => setForm({ ...form, codigoGeneracion: e.target.value })} placeholder="Se genera automaticamente si queda vacio" className="input-field font-mono text-xs" />
          </div>
          <div>
            <label className="input-label">Numero de control</label>
            <input value={form.numeroControl} onChange={(e) => setForm({ ...form, numeroControl: e.target.value })} placeholder="Se genera automaticamente si queda vacio" className="input-field font-mono text-xs" />
          </div>
          <div className="md:col-span-2">
            <label className="input-label">Sello de recepcion Hacienda</label>
            <input value={form.selloRecepcion} onChange={(e) => setForm({ ...form, selloRecepcion: e.target.value })} placeholder="Completar solo cuando Hacienda otorgue sello" className="input-field font-mono text-xs" />
          </div>
          <div className="md:col-span-2">
            <label className="input-label">Referencia de pago</label>
            <input value={form.referenciaPago} onChange={(e) => setForm({ ...form, referenciaPago: e.target.value })} className="input-field" />
          </div>
          <div className="md:col-span-2">
            <label className="input-label">Notas</label>
            <textarea rows="3" value={form.notas} onChange={(e) => setForm({ ...form, notas: e.target.value })} className="input-field" />
          </div>
          <div className="md:col-span-2 rounded-lg border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-900">
            Para que sea DTE fiscal debe estar firmado electronicamente, transmitido a Hacienda y contar con sello de recepcion. Sin sello, el archivo descargado se marca como factura interna.
          </div>
        </div>

        <div className="px-6 py-4 bg-surface-50 border-t border-surface-100 flex justify-end gap-2">
          <button type="button" onClick={onCancel} className="btn-secondary">Cancelar</button>
          <button type="submit" disabled={saving} className="btn-primary">
            <Save size={16} /> {saving ? 'Guardando...' : 'Guardar'}
          </button>
        </div>
      </form>
    </div>
  );
}

export default function FacturacionPage() {
  const { user } = useAuth();
  const isAdmin = user?.rol === 'Admin';
  const [facturas, setFacturas] = useState([]);
  const [planes, setPlanes] = useState([]);
  const [socios, setSocios] = useState([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState('');
  const [editing, setEditing] = useState(null);

  const load = async () => {
    setLoading(true);
    const requests = [
      api.get('/facturacion/planes'),
      isAdmin ? api.get('/facturacion') : api.get('/facturacion/mi-factura'),
    ];
    if (isAdmin) requests.push(api.get('/socios'));
    const [planesRes, facturasRes, sociosRes] = await Promise.all(requests);
    setPlanes(planesRes.data);
    setFacturas(isAdmin ? facturasRes.data : [facturasRes.data]);
    if (isAdmin) setSocios(sociosRes.data);
    setLoading(false);
  };

  useEffect(() => { load(); }, [isAdmin]);

  const filtered = useMemo(() => {
    const term = search.toLowerCase();
    return facturas.filter(f =>
      f.socioNombre?.toLowerCase().includes(term) ||
      f.numero?.toLowerCase().includes(term) ||
      f.estado?.toLowerCase().includes(term)
    );
  }, [facturas, search]);

  const save = async (id, payload) => {
    if (id) {
      await api.put(`/facturacion/${id}`, payload);
    } else {
      await api.post('/facturacion', payload);
    }
    setEditing(null);
    await load();
  };

  const generar = async () => {
    await api.post('/facturacion/generar-todas');
    await load();
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center py-24">
        <div className="w-10 h-10 border-4 border-casatic-200 border-t-casatic-600 rounded-full animate-spin" />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div className="flex items-center gap-3">
          <div className="w-11 h-11 bg-casatic-100 rounded-2xl flex items-center justify-center">
            <Receipt size={22} className="text-casatic-600" />
          </div>
          <div>
            <h1 className="text-xl sm:text-2xl font-bold text-surface-900">Facturacion</h1>
            <p className="text-sm text-surface-500">
              {isAdmin ? `${facturas.length} facturas de socios` : 'Factura de membresia de tu empresa'}
            </p>
          </div>
        </div>
        {isAdmin && (
          <div className="flex flex-wrap gap-2 self-start sm:self-auto">
            <button onClick={() => setEditing({})} className="btn-primary">
              <Plus size={16} /> Nueva factura
            </button>
            <button onClick={generar} className="btn-secondary">
              <RefreshCw size={16} /> Generar faltantes
            </button>
          </div>
        )}
      </div>

      {isAdmin && (
        <div className="card-base p-3 flex flex-wrap items-center gap-3">
          <div className="flex-1 min-w-[220px] relative">
            <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-surface-400" />
            <input
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Buscar por empresa, numero o estado..."
              className="input-field pl-9 text-sm"
            />
          </div>
          <span className="text-xs text-surface-400 font-medium px-2">{filtered.length} resultados</span>
        </div>
      )}

      <div className="grid grid-cols-1 xl:grid-cols-2 gap-4">
        {filtered.map(factura => (
          <FacturaCard key={factura.id} factura={factura} onEdit={setEditing} isAdmin={isAdmin} />
        ))}
      </div>

      {filtered.length === 0 && (
        <div className="card-base py-16 text-center">
          <Receipt size={42} className="mx-auto mb-3 text-surface-300" />
          <h3 className="text-lg font-bold text-surface-700">Sin facturas</h3>
          <p className="text-sm text-surface-400 mt-1">No hay registros que coincidan.</p>
        </div>
      )}

      {editing && (
        <FacturaForm factura={editing} planes={planes} socios={socios} onSave={save} onCancel={() => setEditing(null)} />
      )}
    </div>
  );
}
