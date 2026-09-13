// ============================================================
// LogiTrack - Shared auth & API helper
// ============================================================

const API_ORIGIN = window.location.origin;

// Roles in descending privilege order — must match the backend.
const ROLE_PRIORITY = ['Admin', 'Warehouse', 'Station', 'Driver', 'Customer'];

// Where each role lands after login.
const ROLE_HOME = {
    Admin: 'admin.html',
    Warehouse: 'warehouse.html',
    Station: 'station.html',
    Driver: 'driver.html',
    Customer: 'index.html'
};

// These endpoints should NOT trigger an automatic logout on 401.
const AUTH_FREE_PATHS = ['/Auth/Login', '/Auth/Register'];

function getToken() { return localStorage.getItem('authToken'); }
function getRole() { return localStorage.getItem('role'); }
function getUsername() { return localStorage.getItem('username'); }
function getDriverId() { return localStorage.getItem('driverId'); }
function isAuthenticated() { return !!getToken(); }

function pickPrimaryRole(roles) {
    if (!Array.isArray(roles) || roles.length === 0) return null;
    for (const r of ROLE_PRIORITY) {
        if (roles.includes(r)) return r;
    }
    return roles[0];
}

function logout(redirect = 'index.html') {
    ['authToken', 'token', 'username', 'role', 'driverId']
        .forEach(k => localStorage.removeItem(k));
    window.location.href = redirect;
}

function requireRole(requiredRole, loginPage = 'index.html') {
    const role = getRole();
    if (!isAuthenticated() || !role) {
        window.location.href = loginPage;
        return false;
    }
    // Admin has access everywhere.
    if (role === 'Admin') return true;
    if (role !== requiredRole) {
        window.location.href = loginPage;
        return false;
    }
    return true;
}

async function apiFetch(path, options = {}) {
    const headers = Object.assign(
        { 'Content-Type': 'application/json' },
        options.headers || {}
    );
    const token = getToken();
    if (token) headers['Authorization'] = `Bearer ${token}`;

    const res = await fetch(`${API_ORIGIN}/api${path}`, { ...options, headers });

    const isAuthFree = AUTH_FREE_PATHS.some(p => path.startsWith(p));

    if (res.status === 401 && !isAuthFree) {
        logout();
        throw new Error('Session expired');
    }

    let data = null;
    try { data = await res.json(); } catch { data = null; }

    if (!res.ok) {
        let msg = (data && (data.message || data.Message)) || `Request failed (${res.status})`;
        const errors = data && (data.errors || data.Errors);
        if (Array.isArray(errors) && errors.length) {
            const details = errors.map(e => e.description || e.Description || e).join(' ');
            if (details) msg += ` (${details})`;
        }
        throw new Error(msg);
    }

    return data;
}

function unwrapList(data) {
    if (Array.isArray(data)) return data;
    if (data && Array.isArray(data.$values)) return data.$values;
    if (data && Array.isArray(data.data)) return data.data;
    if (data && Array.isArray(data.shipments)) return data.shipments;
    return [];
}

function escapeHtml(str) {
    if (str === null || str === undefined) return '';
    return String(str)
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;');
}

function getStatusBadgeClass(status) {
    switch (status) {
        case 'Pending': return 'badge-pending';
        case 'In Transit': return 'badge-in-transit';
        case 'Delivered': return 'badge-delivered';
        case 'Cancelled': return 'badge-pending';
        default: return 'badge-pending';
    }
}

function formatDateTime(value) {
    if (!value) return '-';
    const d = new Date(value);
    if (isNaN(d.getTime())) return value;
    return d.toLocaleString();
}