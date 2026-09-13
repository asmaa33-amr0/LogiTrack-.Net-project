// ============================================================
// LogiTrack - Admin Portal (admin.html)
// ============================================================

let adminShipments = [];

document.addEventListener('DOMContentLoaded', async () => {
    if (!requireRole('Admin', 'index.html')) return;

    document.getElementById('admin-user-name').textContent = getUsername() || 'Admin';
    document.getElementById('btn-admin-logout')?.addEventListener('click', () => logout('index.html'));
    document.getElementById('btn-refresh-admin')?.addEventListener('click', loadAll);

    document.querySelectorAll('.tabs .tab').forEach(tab => {
        tab.addEventListener('click', () => {
            document.querySelectorAll('.tabs .tab').forEach(t => t.classList.remove('is-active'));
            tab.classList.add('is-active');
            document.querySelectorAll('.panel').forEach(p => p.hidden = true);
            const panel = document.getElementById(`panel-${tab.dataset.tab}`);
            if (panel) panel.hidden = false;
        });
    });

    await loadAll();
});

async function loadAll() {
    try {
        await Promise.all([loadShipments(), loadWarehouses(), loadDrivers(), loadStations()]);
    } catch (e) {
        console.error(e);
    }
}

function api(path, options = {}) {
    return apiFetch(path, options);
}

function value(obj, camel, pascal = camel.charAt(0).toUpperCase() + camel.slice(1)) {
    return obj?.[camel] ?? obj?.[pascal];
}

function escapeJs(v) {
    return String(v ?? '').replace(/\\/g, '\\\\').replace(/'/g, "\\'").replace(/\n/g, ' ');
}

async function loadShipments() {
    const list = unwrapList(await api('/Shipments'));
    adminShipments = list;
    renderShipments();
}

function renderShipments() {
    const tbody = document.getElementById('admin-shipments-body');
    if (!tbody) return;

    const q = (document.getElementById('admin-search-input')?.value || '').toLowerCase();
    const list = adminShipments.filter(s => {
        const text = [
            value(s, 'trackingNumber'),
            value(s, 'warehouseName'),
            value(s, 'driverName'),
            value(s, 'destinationName')
        ].join(' ').toLowerCase();
        return text.includes(q);
    });

    tbody.innerHTML = list.length ? list.map(s => {
        const id = value(s, 'id');
        const status = value(s, 'status') || 'Pending';
        return `<tr>
            <td><strong>${escapeHtml(value(s, 'trackingNumber') || '-')}</strong></td>
            <td>${escapeHtml(value(s, 'warehouseName') || 'N/A')}</td>
            <td>${escapeHtml(value(s, 'destinationName') || 'N/A')}</td>
            <td>${escapeHtml(value(s, 'driverName') || 'N/A')}</td>
            <td>${value(s, 'weight') ?? 0} kg</td>
            <td><span class="badge ${getStatusBadgeClass(status)}">${escapeHtml(status)}</span></td>
            <td class="row-actions">
                <button class="btn-action btn-view" onclick="openTrackModal(${id})">Track</button>
                <button class="btn-action btn-edit" onclick="openShipmentModal(${id})">Edit</button>
                <button class="btn-action btn-delete" onclick="deleteShipment(${id})">Delete</button>
            </td>
        </tr>`;
    }).join('') : `<tr><td colspan="7" style="text-align:center;">No shipments found.</td></tr>`;

    const pending = adminShipments.filter(s => value(s, 'status') === 'Pending').length;
    const completed = adminShipments.filter(s => value(s, 'status') === 'Delivered').length;
    document.getElementById('stat-total-shipments').textContent = adminShipments.length;
    document.getElementById('stat-pending-shipments').textContent = pending;
    document.getElementById('stat-completed-shipments').textContent = completed;
}

function filterAdminTable() { renderShipments(); }

async function openShipmentModal(id = null) {
    document.getElementById('shipment-id').value = id || '';
    document.getElementById('shipmentModalTitle').textContent = id ? 'Edit Shipment' : 'New Shipment';

    const [warehouses, stations, drivers] = await Promise.all([
        api('/Shipments/warehouses-lookup'),
        api('/Stations'),
        api('/Shipments/drivers-lookup')
    ]);

    const wlist = unwrapList(warehouses);
    const slist = unwrapList(stations);
    const dlist = unwrapList(drivers);

    const ws = document.getElementById('shipment-warehouse');
    const ss = document.getElementById('shipment-station');
    const ds = document.getElementById('shipment-driver');

    ws.innerHTML = '<option value="">Select warehouse</option>' + wlist.map(x =>
        `<option value="${value(x, 'id')}">${escapeHtml(value(x, 'name'))}</option>`).join('');
    ss.innerHTML = '<option value="">Select destination</option>' + slist.map(x =>
        `<option value="${value(x, 'id')}">${escapeHtml(value(x, 'name') || value(x, 'stationName'))}</option>`).join('');
    ds.innerHTML = '<option value="">Unassigned</option>' + dlist.map(x =>
        `<option value="${value(x, 'id')}">${escapeHtml(value(x, 'name'))}</option>`).join('');

    if (id) {
        const data = await api(`/Shipments/${id}`);
        document.getElementById('shipment-tracking').value = value(data, 'trackingNumber') || '';
        document.getElementById('shipment-weight').value = value(data, 'weight') ?? '';
        document.getElementById('shipment-status').value = value(data, 'status') || 'Pending';

        const whId = data.warehouse ? value(data.warehouse, 'id') : null;
        const driverId = data.driver ? value(data.driver, 'id') : null;
        const stationId = data.plannedRoute?.length ? value(data.plannedRoute[0], 'stationId') : null;

        ws.value = whId ?? '';
        ds.value = driverId ?? '';
        ss.value = stationId ?? '';
    } else {
        document.getElementById('shipment-tracking').value = '';
        document.getElementById('shipment-weight').value = '';
        document.getElementById('shipment-status').value = 'Pending';
        document.getElementById('shipment-date').value = '';
    }

    document.getElementById('shipmentModal').style.display = 'flex';
}

async function saveShipment() {
    const id = document.getElementById('shipment-id').value;
    const weight = parseFloat(document.getElementById('shipment-weight').value);
    const warehouseId = parseInt(document.getElementById('shipment-warehouse').value);
    const driverRaw = document.getElementById('shipment-driver').value;
    const status = document.getElementById('shipment-status').value;

    if (!weight || weight <= 0 || !warehouseId) {
        alert('Weight and warehouse are required.');
        return;
    }

    const needsDriver = status === 'In Transit' || status === 'Delivered';
    if (needsDriver && !driverRaw) {
        alert('A driver is required for In Transit / Delivered shipments.');
        return;
    }

    const payload = {
        trackingNumber: document.getElementById('shipment-tracking').value.trim(),
        weight,
        status,
        warehouseId,
        driverId: driverRaw ? parseInt(driverRaw) : null
    };

    try {
        await api(id ? `/Shipments/${id}` : '/Shipments', {
            method: id ? 'PUT' : 'POST',
            body: JSON.stringify(payload)
        });
        closeModal('shipmentModal');
        await loadShipments();
    } catch (e) { alert(e.message); }
}

async function deleteShipment(id) {
    if (!confirm('Delete this shipment?')) return;
    try {
        await api(`/Shipments/${id}`, { method: 'DELETE' });
        await loadShipments();
    } catch (e) { alert(e.message); }
}

async function openTrackModal(id) {
    try {
        const data = await api(`/Shipments/${id}`);
        const cps = data.checkpoints || data.Checkpoints || [];
        document.getElementById('trackModalTitle').textContent =
            `Shipment ${value(data, 'trackingNumber') || id}`;
        document.getElementById('trackModalBody').innerHTML = cps.length
            ? cps.map(c => `<div class="timeline-item">
                <div class="timeline-station">${escapeHtml(value(c, 'stationName'))}</div>
                <div class="timeline-info">${escapeHtml(value(c, 'info') || '')}</div>
                <div class="timeline-time">${formatDateTime(value(c, 'timestamp'))}</div>
              </div>`).join('')
            : '<p class="muted-note">No checkpoints yet.</p>';
        document.getElementById('trackModal').style.display = 'flex';
    } catch (e) { alert(e.message); }
}

async function loadWarehouses() {
    const list = unwrapList(await api('/Warehouses'));
    const tbody = document.getElementById('admin-warehouses-body');
    tbody.innerHTML = list.map(w => `<tr>
        <td>${escapeHtml(value(w, 'name'))}</td>
        <td>${escapeHtml(value(w, 'city'))}</td>
        <td>${value(w, 'latitude') ?? 0}, ${value(w, 'longitude') ?? 0}</td>
        <td class="row-actions">
            <button class="btn-action btn-view" onclick="showWarehouseShipments(${value(w, 'id')}, '${escapeJs(value(w, 'name'))}')">Show Shipments</button>
            <button class="btn-action btn-edit" onclick="openWarehouseModal(${value(w, 'id')})">Edit</button>
            <button class="btn-action btn-delete" onclick="deleteWarehouse(${value(w, 'id')})">Delete</button>
        </td>
    </tr>`).join('') || '<tr><td colspan="4">No warehouses.</td></tr>';
}

async function openWarehouseModal(id = null) {
    document.getElementById('warehouse-id').value = id || '';
    document.getElementById('warehouseModalTitle').textContent = id ? 'Edit Warehouse' : 'New Warehouse';
    if (id) {
        const list = unwrapList(await api('/Warehouses'));
        const item = list.find(x => value(x, 'id') == id);
        document.getElementById('warehouse-name').value = value(item, 'name') || '';
        document.getElementById('warehouse-city').value = value(item, 'city') || '';
        document.getElementById('warehouse-lat').value = value(item, 'latitude') ?? 0;
        document.getElementById('warehouse-lng').value = value(item, 'longitude') ?? 0;
    } else {
        ['warehouse-name', 'warehouse-city', 'warehouse-lat', 'warehouse-lng'].forEach(x => document.getElementById(x).value = '');
    }
    document.getElementById('warehouseModal').style.display = 'flex';
}

async function saveWarehouse() {
    const id = document.getElementById('warehouse-id').value;
    const payload = {
        name: document.getElementById('warehouse-name').value.trim(),
        city: document.getElementById('warehouse-city').value.trim(),
        latitude: parseFloat(document.getElementById('warehouse-lat').value) || 0,
        longitude: parseFloat(document.getElementById('warehouse-lng').value) || 0
    };
    try {
        await api(id ? `/Warehouses/${id}` : '/Warehouses', {
            method: id ? 'PUT' : 'POST', body: JSON.stringify(payload)
        });
        closeModal('warehouseModal');
        await loadWarehouses();
    } catch (e) { alert(e.message); }
}

async function deleteWarehouse(id) {
    if (!confirm('Delete this warehouse?')) return;
    try {
        await api(`/Warehouses/${id}`, { method: 'DELETE' });
        await loadWarehouses();
    } catch (e) { alert(e.message); }
}

async function showWarehouseShipments(id, name) {
    const list = unwrapList(await api(`/Warehouses/${id}/shipments`));
    openListModal(`Shipments — ${name}`, list);
}

async function loadDrivers() {
    const list = unwrapList(await api('/Drivers'));
    const tbody = document.getElementById('admin-drivers-body');
    tbody.innerHTML = list.map(d => `<tr>
        <td>${escapeHtml(value(d, 'fullName'))}</td>
        <td>${escapeHtml(value(d, 'phone'))}</td>
        <td>${escapeHtml(value(d, 'licenseNumber'))}</td>
        <td>${value(d, 'expiryDate') ? formatDateTime(value(d, 'expiryDate')).split(',')[0] : '-'}</td>
        <td>${escapeHtml(value(d, 'username') || 'Not linked')}</td>
        <td class="row-actions">
            <button class="btn-action btn-view" onclick="showDriverShipments(${value(d, 'id')}, '${escapeJs(value(d, 'fullName'))}')">Show Shipments</button>
            <button class="btn-action btn-edit" onclick="openDriverModal(${value(d, 'id')})">Edit</button>
            <button class="btn-action btn-delete" onclick="deleteDriver(${value(d, 'id')})">Delete</button>
        </td>
    </tr>`).join('') || '<tr><td colspan="6">No drivers.</td></tr>';
}

async function openDriverModal(id = null) {
    document.getElementById('driver-id').value = id || '';
    document.getElementById('driverModalTitle').textContent = id ? 'Edit Driver' : 'New Driver';
    ['driver-account-username', 'driver-account-email', 'driver-account-password'].forEach(x => {
        document.getElementById(x).value = '';
    });

    const accountSelect = document.getElementById('driver-user');
    accountSelect.innerHTML = '<option value="">No login account linked</option>';

    if (id) {
        const d = await api(`/Drivers/${id}`);
        document.getElementById('driver-fullname').value = value(d, 'fullName') || '';
        document.getElementById('driver-phone').value = value(d, 'phone') || '';
        document.getElementById('driver-license').value = value(d, 'licenseNumber') || '';
        const exp = value(d, 'expiryDate');
        document.getElementById('driver-expiry').value = exp ? exp.substring(0, 10) : '';

        if (value(d, 'userId')) {
            accountSelect.innerHTML = `<option value="${escapeHtml(value(d, 'userId'))}" selected>${escapeHtml(value(d, 'username') || 'Linked')}</option>`;
        } else {
            const accounts = unwrapList(await api('/Drivers/available-accounts'));
            accountSelect.innerHTML += accounts.map(a =>
                `<option value="${escapeHtml(value(a, 'id'))}">${escapeHtml(value(a, 'username'))} (${escapeHtml(value(a, 'email'))})</option>`).join('');
        }
    } else {
        ['driver-fullname', 'driver-phone', 'driver-license', 'driver-expiry'].forEach(x => document.getElementById(x).value = '');
    }

    document.getElementById('driverModal').style.display = 'flex';
}

async function saveDriver() {
    const id = document.getElementById('driver-id').value;
    const payload = {
        fullName: document.getElementById('driver-fullname').value.trim(),
        phone: document.getElementById('driver-phone').value.trim(),
        licenseNumber: document.getElementById('driver-license').value.trim(),
        expiryDate: document.getElementById('driver-expiry').value || null
    };

    try {
        if (id) {
            await api(`/Drivers/${id}`, { method: 'PUT', body: JSON.stringify(payload) });

            const selectedUser = document.getElementById('driver-user').value;
            const username = document.getElementById('driver-account-username').value.trim();
            const email = document.getElementById('driver-account-email').value.trim();
            const password = document.getElementById('driver-account-password').value;

            const current = await api(`/Drivers/${id}`);
            if (!value(current, 'userId')) {
                if (selectedUser) {
                    // Link to an existing, unlinked account.
                    await api(`/Drivers/${id}/account`, {
                        method: 'PUT',
                        body: JSON.stringify({ userId: selectedUser })
                    });
                } else if (username || email || password) {
                    // Create a brand new login for this driver.
                    if (!username || !email || !password) {
                        throw new Error('Fill username, email and password to create the driver login.');
                    }
                    await api(`/Drivers/${id}/account`, {
                        method: 'POST',
                        body: JSON.stringify({ username, email, password })
                    });
                }
            }
        } else {
            const created = await api('/Drivers', { method: 'POST', body: JSON.stringify(payload) });
            const driverId = value(created, 'id');
            const username = document.getElementById('driver-account-username').value.trim();
            const email = document.getElementById('driver-account-email').value.trim();
            const password = document.getElementById('driver-account-password').value;

            if (username || email || password) {
                if (!username || !email || !password) {
                    throw new Error('Fill username, email and password to create the driver login.');
                }
                await api(`/Drivers/${driverId}/account`, {
                    method: 'POST',
                    body: JSON.stringify({ username, email, password })
                });
            }
        }
        closeModal('driverModal');
        await loadDrivers();
    } catch (e) { alert(e.message); }
}

async function deleteDriver(id) {
    if (!confirm('Delete this driver?')) return;
    try {
        await api(`/Drivers/${id}`, { method: 'DELETE' });
        await loadDrivers();
    } catch (e) { alert(e.message); }
}

async function showDriverShipments(id, name) {
    const list = unwrapList(await api(`/Drivers/${id}/shipments`));
    openListModal(`Shipments — ${name}`, list);
}

async function loadStations() {
    const list = unwrapList(await api('/Stations'));
    const tbody = document.getElementById('admin-stations-body');
    tbody.innerHTML = list.map(s => {
        const name = value(s, 'stationName') || value(s, 'name') || 'N/A';
        const lat = value(s, 'latitude') ?? 0;
        const lng = value(s, 'longitude') ?? 0;
        return `<tr>
            <td>${escapeHtml(name)}</td>
            <td>${lat}, ${lng}</td>
            <td class="row-actions">
                <button class="btn-action btn-edit" onclick="openStationModal(${value(s, 'id')})">Edit</button>
                <button class="btn-action btn-delete" onclick="deleteStation(${value(s, 'id')})">Delete</button>
            </td>
        </tr>`;
    }).join('') || '<tr><td colspan="3">No stations.</td></tr>';
}

async function openStationModal(id = null) {
    document.getElementById('station-id').value = id || '';
    document.getElementById('stationModalTitle').textContent = id ? 'Edit Station' : 'New Station';
    if (id) {
        const list = unwrapList(await api('/Stations'));
        const s = list.find(x => value(x, 'id') == id);
        document.getElementById('station-name').value = value(s, 'stationName') || value(s, 'name') || '';
        document.getElementById('station-lat').value = value(s, 'latitude') ?? 0;
        document.getElementById('station-lng').value = value(s, 'longitude') ?? 0;
    } else {
        ['station-name', 'station-lat', 'station-lng'].forEach(x => document.getElementById(x).value = '');
    }
    document.getElementById('stationModal').style.display = 'flex';
}

async function saveStation() {
    const id = document.getElementById('station-id').value;
    const payload = {
        stationName: document.getElementById('station-name').value.trim(),
        latitude: parseFloat(document.getElementById('station-lat').value) || 0,
        longitude: parseFloat(document.getElementById('station-lng').value) || 0
    };
    try {
        await api(id ? `/Stations/${id}` : '/Stations', {
            method: id ? 'PUT' : 'POST', body: JSON.stringify(payload)
        });
        closeModal('stationModal');
        await loadStations();
    } catch (e) { alert(e.message); }
}

async function deleteStation(id) {
    if (!confirm('Delete this station?')) return;
    try {
        await api(`/Stations/${id}`, { method: 'DELETE' });
        await loadStations();
    } catch (e) { alert(e.message); }
}

function openListModal(title, list) {
    document.getElementById('listModalTitle').textContent = title;
    document.getElementById('listModalBody').innerHTML = list.map(x => `<tr>
        <td>${escapeHtml(value(x, 'trackingNumber'))}</td>
        <td>${escapeHtml(value(x, 'destinationName') || 'N/A')}</td>
        <td>${value(x, 'weight') ?? 0} kg</td>
        <td><span class="badge ${getStatusBadgeClass(value(x, 'status'))}">${escapeHtml(value(x, 'status'))}</span></td>
    </tr>`).join('') || '<tr><td colspan="4">No shipments.</td></tr>';
    document.getElementById('listModal').style.display = 'flex';
}

function closeModal(id) {
    const m = document.getElementById(id);
    if (m) m.style.display = 'none';
}