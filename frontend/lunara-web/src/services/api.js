const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5080';
const TOKEN_KEY = 'lunara_token';
const USER_KEY = 'lunara_user';

// Guarda la sesión en el navegador para mantener el acceso al recargar la página.
export const getToken = () => localStorage.getItem(TOKEN_KEY);

export const getUser = () => {
  try {
    return JSON.parse(localStorage.getItem(USER_KEY) || 'null');
  } catch {
    return null;
  }
};

export const logout = () => {
  localStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem(USER_KEY);
};

async function request(path, options = {}) {
  const token = getToken();

  // Todas las pantallas usan este punto de entrada para enviar solicitudes y mostrar errores del API.
  const headers = {
    ...(options.body ? { 'Content-Type': 'application/json' } : {}),
    ...(options.headers || {}),
  };

  if (token) {
    headers.Authorization = `Bearer ${token}`;
  }

  const response = await fetch(`${API_URL}${path}`, {
    ...options,
    headers,
  });

  if (response.status === 401 && token) {
    logout();
    window.dispatchEvent(new Event('lunara-session-expired'));
  }

  if (!response.ok) {
    let message = `Error HTTP ${response.status}`;

    try {
      const body = await response.json();
      message = body.message || body.title || body.detail || message;
    } catch {}

    throw new Error(message);
  }

  if (response.status === 204) return null;
  return response.json();
}

const json = (method, body) => ({
  method,
  body: JSON.stringify(body),
});

export const api = {
  // Al iniciar sesión, conserva el token y los datos básicos del usuario para las siguientes llamadas.
  login: async (identifier, password, role) => {
    const result = await request(
      '/api/Auth/login',
      json('POST', {
        email: identifier,
        username: identifier,
        password,
        role
      })
    );

    const token = result.token || result.accessToken;

    if (!token) {
      throw new Error('El servidor respondió, pero no devolvió un token JWT.');
    }

    localStorage.setItem(TOKEN_KEY, token);

    const user = result.user || {
      email: result.email || identifier,
      username: result.username || identifier,
      displayName: result.displayName || result.username || identifier,
      role: result.role || result.roles?.[0] || '',
      clientId: result.clientId ?? null,
      technicianId: result.technicianId ?? null,
    };

    localStorage.setItem(USER_KEY, JSON.stringify(user));

    return user;
  },
  recoverAdminPassword: (body) =>
    request('/api/Auth/admin/recover', json('POST', body)),

  clients: () => request('/api/clients'),
  clientByCedula: async (cedula) => {
    const matches = await request(`/api/clients?cedula=${encodeURIComponent(cedula)}`);
    return matches[0] || null;
  },
  clientRepairs: (id) => request(`/api/clients/${id}/repairs`),
  devices: () => request('/api/devices'),
  createDevice: (body) => request('/api/devices', json('POST', body)),
  spareParts: () => request('/api/spareparts'),
  sparePartCatalog: () => request('/api/spareparts/catalog'),
  createSparePart: (body) => request('/api/spareparts', json('POST', body)),
  sparePartPurchases: () => request('/api/spareparts/purchases'),
  createSparePartPurchase: (body) =>
    request('/api/spareparts/purchases', json('POST', body)),
  markSparePartPurchased: (id) =>
    request(`/api/spareparts/purchases/${id}/purchased`, json('PUT', {})),
  laborPriceGuide: () => request('/api/laborprices'),
  createLaborPrice: (body) => request('/api/laborprices', json('POST', body)),
  updateLaborPrice: (id, body) => request(`/api/laborprices/${id}`, json('PUT', body)),
  technicians: () => request('/api/technicians'),
  createTechnician: (body) => request('/api/technicians', json('POST', body)),
  updateTechnicianAccess: (id, body) =>
    request(`/api/technicians/${id}/access`, json('PUT', body)),
  repairs: (search = '') =>
    request(`/api/repairs${search ? `?search=${encodeURIComponent(search)}` : ''}`),
  repair: (id) => request(`/api/repairs/${id}`),
  createRepair: (body) => request('/api/repairs', json('POST', body)),
  registerRepair: (body) => request('/api/repairs/register', json('POST', body)),
  assignTechnician: (id, technicianId) =>
    request(`/api/repairs/${id}/technician`, json('PUT', { technicianId })),
  stats: () => request('/api/repairs/stats'),
  diagnosis: (id, body) =>
    request(`/api/repairs/${id}/diagnosis`, json('PUT', body)),
  sendBudgetEmail: (id) =>
    request(`/api/repairs/${id}/budget-email`, json('POST', {})),
  sendReadyEmail: (id) =>
    request(`/api/repairs/${id}/ready-email`, json('POST', {})),
  status: (id, status, note = '') =>
    request(`/api/repairs/${id}/status`, json('PUT', { status, note })),
  work: (id, workPerformed, note = '') =>
    request(`/api/repairs/${id}/work`, json('PUT', { workPerformed, note })),
};

export { API_URL };
