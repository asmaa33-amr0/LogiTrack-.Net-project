// ============================================================
// LogiTrack - Driver Portal (driver.html)
// ============================================================

let allDriverShipments = [];

document.addEventListener('DOMContentLoaded', () => {
    if (!requireRole('Driver', 'index.html')) return;
    checkDriverAuth();
    setupDriverEvents();
});

function checkDriverAuth() {
    const nameEl = document.getElementById('driver-name');
    if (nameEl) nameEl.textContent = getUsername() || 'Driver';

    const driverId = getDriverId();
    if (!driverId) {
        const tbody = document.getElementById('driver-shipments-body');
        if (tbody) {
            tbody.innerHTML = `<tr><td colspan="6" style="text-align:center; color: var(--danger);">
                Your account is not yet linked to a driver record. Ask an admin to link it from the Drivers page.
            </td></tr>`;
        }
        return;
    }

    loadDriverData();
    loadStationsForModal();
}

function setupDriverEvents() {
    document.getElementById('btn-logout')?.addEventListener('click', () => logout('index.html'));
    document.getElementById('btn-refresh')?.addEventListener('click', loadDriverData);
}

async function loadDriverData() {
    const driverId = getDriverId();
    if (!driverId) return;

    try {
        const data = await apiFetch(`/DriverUi/driver/${driverId}`);

        const driverName = data.driverName ?? data.DriverName;
        const shipments = data.shipments ?? data.Shipments ?? [];

        const nameEl = document.getElementById('driver-name');
        if (nameEl && driverName) nameEl.textContent = driverName;

        allDriverShipments = shipments;
        renderDriverTable(allDriverShipments);
    } catch (err) {
        console.error(err);
        const tbody = document.getElementById('driver-shipments-body');
        if (tbody) {
            tbody.innerHTML = `<tr><td colspan="6" style="text-align:center; color: var(--danger);">
                ${escapeHtml(err.message || 'Failed to load deliveries.')}
            </td></tr>`;
        }
    }
}

function filterDriverShipments() {
    const query = document.getElementById('driver-search-input').value.toLowerCase();
    const filtered = allDriverShipments.filter(item => {
        const t = item.trackingNumber ?? item.TrackingNumber ?? '';
        const d = item.destinationName ?? item.DestinationName ?? '';
        const w = item.warehouseName ?? item.WarehouseName ?? '';
        return t.toLowerCase().includes(query) ||
            d.toLowerCase().includes(query) ||
            w.toLowerCase().includes(query);
    });
    renderDriverTable(filtered);
}

function renderDriverTable(shipments) {
    const tbody = document.getElementById('driver-shipments-body');
    const activeEl = document.getElementById('stat-active-count');
    const completedEl = document.getElementById('stat-completed-count');
    if (!tbody) return;
    tbody.innerHTML = '';

    let active = 0;
    let completed = 0;

    if (!shipments || shipments.length === 0) {
        tbody.innerHTML = `<tr><td colspan="6" style="text-align:center; color: var(--text-muted);">No assigned deliveries found.</td></tr>`;
        if (activeEl) activeEl.textContent = '0';
        if (completedEl) completedEl.textContent = '0';
        return;
    }

    shipments.forEach(item => {
        const id = item.id ?? item.Id;
        const tracking = item.trackingNumber ?? item.TrackingNumber ?? 'N/A';
        const warehouse = item.warehouseName ?? item.WarehouseName ?? 'Warehouse';
        const destination = item.destinationName ?? item.DestinationName ?? 'N/A';
        const weight = item.weight ?? item.Weight;
        const status = item.status ?? item.Status ?? 'Pending';

        if (status === 'Delivered') completed++; else active++;

        const row = document.createElement('tr');
        row.innerHTML = `
            <td><strong>${escapeHtml(tracking)}</strong></td>
            <td>${escapeHtml(warehouse)}</td>
            <td>${escapeHtml(destination)}</td>
            <td>${weight ? weight + ' kg' : 'N/A'}</td>
            <td><span class="badge ${getStatusBadgeClass(status)}">${escapeHtml(status)}</span></td>
            <td>
                <div style="display:flex; gap:6px;">
                    ${renderStatusButton({ id, status })}
                    <button class="btn-action btn-edit" onclick="openRouteModal(${id})">+ Route</button>
                </div>
            </td>
        `;
        tbody.appendChild(row);
    });

    if (activeEl) activeEl.textContent = active;
    if (completedEl) completedEl.textContent = completed;
}

function renderStatusButton(shipment) {
    if (shipment.status === 'Pending') {
        return `<button class="btn-action btn-add" onclick="updateStatus(${shipment.id}, 'In Transit')">Start</button>`;
    }
    if (shipment.status === 'In Transit') {
        return `<button class="btn-action btn-add" onclick="updateStatus(${shipment.id}, 'Delivered')">Deliver</button>`;
    }
    return `<span style="color:var(--text-muted); font-size:0.8rem;">Done</span>`;
}

async function updateStatus(shipmentId, newStatus) {
    try {
        await apiFetch(`/DriverUi/shipment/${shipmentId}/status`, {
            method: 'PUT',
            body: JSON.stringify({ status: newStatus })
        });
        loadDriverData();
    } catch (err) {
        alert(err.message);
    }
}

async function loadStationsForModal() {
    try {
        const list = unwrapList(await apiFetch('/Stations'));
        const select = document.getElementById('route-station-select');
        if (select) {
            select.innerHTML = '<option value="">Select Checkpoint Station</option>';
            list.forEach(s => {
                const name = s.name ?? s.Name ?? s.stationName ?? s.StationName;
                if (!name) return;
                select.innerHTML += `<option value="${escapeHtml(name)}">${escapeHtml(name)}</option>`;
            });
        }
    } catch (e) {
        console.error(e);
    }
}

function openRouteModal(shipmentId) {
    document.getElementById('modal-shipment-id').value = shipmentId;
    document.getElementById('route-notes').value = '';
    document.getElementById('routeModal').style.display = 'flex';
}

function closeModal(id) {
    document.getElementById(id).style.display = 'none';
}

async function submitRoutePoint() {
    const shipmentId = document.getElementById('modal-shipment-id').value;
    const stationName = document.getElementById('route-station-select').value;
    const info = document.getElementById('route-notes').value;

    if (!stationName) { alert('Please select a station.'); return; }

    try {
        await apiFetch(`/DriverUi/shipment/${shipmentId}/checkpoint`, {
            method: 'POST',
            body: JSON.stringify({ stationName, info })
        });
        closeModal('routeModal');
        loadDriverData();
    } catch (e) {
        alert(e.message);
    }
}