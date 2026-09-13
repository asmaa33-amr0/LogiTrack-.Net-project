const API_BASE = 'https://localhost:44394/api/Shipments';
const WAREHOUSES_API = 'https://localhost:44394/api/Warehouses';
const DRIVERS_API = 'https://localhost:44394/api/Drivers';
const DRIVER_UI_BASE = 'https://localhost:44394/api/DriverUi';

const DEFAULT_ROUTE_TEMPLATE = [
    { name: "Warehouse", info: "Package received & prepared at origin warehouse." },
    { name: "Sorting Facility", info: "Package processed and categorized." },
    { name: "Regional Hub", info: "Arrived at localized distribution center." },
    { name: "Out for Delivery", info: "Package dispatched with driver." },
    { name: "Delivered", info: "Successfully handed over to destination." }
];

function getAuthToken() {
    return localStorage.getItem('authToken') || localStorage.getItem('token');
}

function checkAuthStatus() {
    const token = getAuthToken();
    const role = localStorage.getItem('role');

    // No token
    if (!token) {
        const loginModal = document.getElementById('loginModal');

        if (loginModal) {
            loginModal.style.display = 'flex';
        }

        return;
    }

    // Driver
    if (role === 'Driver') {
        window.location.href = 'driver.html';
        return;
    }

    // Any role other than Admin is not allowed here
    if (role !== 'Admin') {
        window.location.href = 'index.html';
        return;
    }

    // Admin is allowed
    const loginModal = document.getElementById('loginModal');

    if (loginModal) {
        loginModal.style.display = 'none';
    }

    loadData();
    loadWarehouses();
    loadDrivers();
}

async function fetchWithAuth(url, options = {}) {
    const token = getAuthToken();
    const headers = {
        'Content-Type': 'application/json',
        ...options.headers,
    };

    if (token) {
        headers['Authorization'] = `Bearer ${token}`;
    }

    const response = await fetch(url, { ...options, headers });

    if (response.status === 401) {
        localStorage.clear();
        const loginModal = document.getElementById('loginModal');
        if (loginModal) loginModal.style.display = 'flex';
    }

    return response;
}

function parseArrayData(data) {
    if (Array.isArray(data)) return data;
    if (data && Array.isArray(data.$values)) return data.$values;
    if (data && Array.isArray(data.data)) return data.data;
    if (data && Array.isArray(data.shipments)) return data.shipments;
    return [];
}

// ==================== SHIPMENTS ====================

async function loadData(searchQuery = '') {
    try {
        const url = searchQuery
            ? `${API_BASE}/search?query=${encodeURIComponent(searchQuery)}`
            : API_BASE;

        const res = await fetchWithAuth(url);
        if (!res.ok) throw new Error(`HTTP error! Status: ${res.status}`);

        const rawData = await res.json();
        const shipments = parseArrayData(rawData);

        renderShipmentsTable(shipments);
    } catch (err) {
        console.error('Error fetching data:', err);
    }
}

function renderShipmentsTable(shipments) {
    const tbody = document.getElementById('shipmentsTableBody');
    if (!tbody) return;

    tbody.innerHTML = '';
    let inTransit = 0;

    if (shipments.length === 0) {
        tbody.innerHTML = `<tr><td colspan="6" style="text-align:center;">No shipments found.</td></tr>`;
    } else {
        shipments.forEach(item => {
            const id = item.id ?? item.Id ?? '-';
            const trackingNumber = item.trackingNumber ?? item.TrackingNumber ?? 'N/A';
            const weight = item.weight ?? item.Weight ?? 0;
            const warehouseName = item.warehouseName ?? item.WarehouseName ?? (item.warehouse?.name ?? item.Warehouse?.Name ?? 'N/A');
            const driverName = item.driverName ?? item.DriverName ?? (item.driver?.fullName ?? item.driver?.name ?? item.Driver?.FullName ?? 'N/A');
            const status = item.status ?? item.Status ?? 'Pending';

            if (status.toLowerCase().includes('transit')) inTransit++;

            let badgeClass = 'badge-pending';
            if (status.toLowerCase().includes('transit')) badgeClass = 'badge-in-transit';
            if (status.toLowerCase().includes('deliver')) badgeClass = 'badge-delivered';

            tbody.innerHTML += `
                <tr>
                    <td>
                        <span class="id-tag">#${id}</span>
                        <span class="tracking-tag">(${trackingNumber})</span>
                    </td>
                    <td>${weight} kg</td>
                    <td>${warehouseName}</td>
                    <td>${driverName}</td>
                    <td>
                        <select onchange="updateShipmentStatus(${id}, this.value)" class="badge ${badgeClass}" style="border:none; outline:none; cursor:pointer;">
                            <option value="Pending" ${status === 'Pending' ? 'selected' : ''}>Pending</option>
                            <option value="In Transit" ${status === 'In Transit' ? 'selected' : ''}>In Transit</option>
                            <option value="Delivered" ${status === 'Delivered' ? 'selected' : ''}>Delivered</option>
                        </select>
                    </td>
                    <td>
                        <button class="btn-action btn-view" onclick="openDetailsModal(${id})">Track</button>
                        <button class="btn-action btn-edit" onclick="openEditShipmentModal(${id})">Edit</button>
                        <button class="btn-action btn-delete" onclick="deleteShipment(${id})">Delete</button>
                    </td>
                </tr>
            `;
        });
    }

    const totalOrdersEl = document.getElementById('totalOrders');
    const inTransitCountEl = document.getElementById('inTransitCount');

    if (totalOrdersEl) totalOrdersEl.innerText = shipments.length;
    if (inTransitCountEl) inTransitCountEl.innerText = inTransit;
}

function handleSearch() {
    const query = document.getElementById('searchInput').value;
    loadData(query);
}

async function updateShipmentStatus(id, newStatus) {
    try {
        const response = await fetchWithAuth(`${DRIVER_UI_BASE}/shipment/${id}/status`, {
            method: 'PUT',
            body: JSON.stringify(newStatus)
        });

        if (!response.ok) throw new Error(`Status update failed: ${response.status}`);
        loadData();
    } catch (error) {
        console.error('Error updating status:', error);
        alert(`Error updating status: ${error.message}`);
    }
}

async function deleteShipment(id) {
    if (!confirm('Are you sure you want to delete this shipment?')) return;
    try {
        const res = await fetchWithAuth(`${API_BASE}/${id}`, { method: 'DELETE' });
        if (res.ok) loadData();
        else alert('Failed to delete shipment');
    } catch (err) {
        console.error(err);
    }
}

async function openCreateModal() {
    document.getElementById('modalTitle').innerText = 'Add New Shipment';
    document.getElementById('editShipmentId').value = '';
    document.getElementById('trackingNum').value = '';
    document.getElementById('weight').value = '';
    document.getElementById('status').value = 'Pending';
    document.getElementById('customerUserId').value = '';

    await populateDropdowns();
    document.getElementById('shipmentModal').style.display = 'flex';
}

async function openEditShipmentModal(id) {
    try {
        const res = await fetchWithAuth(`${API_BASE}/${id}`);
        if (!res.ok) throw new Error('Shipment not found');

        const data = await res.json();
        document.getElementById('modalTitle').innerText = 'Edit Shipment';
        document.getElementById('editShipmentId').value = data.id ?? data.Id;
        document.getElementById('trackingNum').value = data.trackingNumber ?? data.TrackingNumber ?? '';
        document.getElementById('weight').value = data.weight ?? data.Weight ?? 0;
        document.getElementById('status').value = data.status ?? data.Status ?? 'Pending';
        document.getElementById('customerUserId').value = data.customerUserId ?? data.CustomerUserId ?? '';

        await populateDropdowns(data.warehouseId ?? data.WarehouseId, data.driverId ?? data.DriverId);
        document.getElementById('shipmentModal').style.display = 'flex';
    } catch (err) {
        alert(err.message);
    }
}

async function populateDropdowns(selectedWarehouse = null, selectedDriver = null) {
    const whRes = await fetchWithAuth(WAREHOUSES_API);
    const drRes = await fetchWithAuth(DRIVERS_API);

    const warehouses = parseArrayData(await whRes.json());
    const drivers = parseArrayData(await drRes.json());

    const whSelect = document.getElementById('warehouseSelect');
    const drSelect = document.getElementById('driverSelect');

    whSelect.innerHTML = '<option value="">Select Warehouse</option>';
    warehouses.forEach(w => {
        const id = w.id ?? w.Id;
        const name = w.name ?? w.Name;
        whSelect.innerHTML += `<option value="${id}" ${id == selectedWarehouse ? 'selected' : ''}>${name}</option>`;
    });

    drSelect.innerHTML = '<option value="">Select Driver</option>';
    drivers.forEach(d => {
        const id = d.id ?? d.Id;
        const name = d.fullName ?? d.FullName;
        drSelect.innerHTML += `<option value="${id}" ${id == selectedDriver ? 'selected' : ''}>${name}</option>`;
    });
}

async function saveShipment() {
    const id = document.getElementById('shipment-id').value;
    const weight = parseFloat(document.getElementById('shipment-weight').value);
    const warehouseId = parseInt(document.getElementById('shipment-warehouse').value);
    const driverIdRaw = document.getElementById('shipment-driver').value;
    const status = document.getElementById('shipment-status').value;

    if (!weight || weight <= 0 || !warehouseId) {
        alert('Weight and warehouse are required.');
        return;
    }

    const needsDriver = status === 'In Transit' || status === 'Delivered';
    if (needsDriver && !driverIdRaw) {
        alert('A driver is required once a shipment is In Transit or Delivered.');
        return;
    }

    const payload = {
        trackingNumber: document.getElementById('shipment-tracking').value.trim(),
        weight,
        status,
        warehouseId,
        driverId: driverIdRaw ? parseInt(driverIdRaw) : null
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

// ==================== WAREHOUSES ====================

async function loadWarehouses() {
    try {
        const res = await fetchWithAuth(WAREHOUSES_API);
        if (!res.ok) return;

        const data = await res.json();
        const warehouses = parseArrayData(data);
        const tbody = document.getElementById('warehousesTableBody');
        if (!tbody) return;

        tbody.innerHTML = '';
        warehouses.forEach(w => {
            const id = w.id ?? w.Id;
            const name = w.name ?? w.Name;
            const city = w.city ?? w.City ?? '-';

            tbody.innerHTML += `
                <tr>
                    <td>#${id}</td>
                    <td>${name}</td>
                    <td>${city}</td>
                    <td>
                        <button class="btn-action btn-view" onclick="filterShipmentsByWarehouse(${id})">Show Shipments</button>
                        <button class="btn-action btn-edit" onclick="openEditWarehouseModal(${id}, '${name}', '${city}')">Edit</button>
                        <button class="btn-action btn-delete" onclick="deleteWarehouse(${id})">Delete</button>
                    </td>
                </tr>
            `;
        });
    } catch (err) {
        console.error('Error loading warehouses:', err);
    }
}

async function filterShipmentsByWarehouse(warehouseId) {
    switchTab('shipments');
    const res = await fetchWithAuth(`${API_BASE}?warehouseId=${warehouseId}`);
    if (res.ok) {
        const data = await res.json();
        const allShipments = parseArrayData(data);
        const filtered = allShipments.filter(s => (s.warehouseId ?? s.WarehouseId) == warehouseId);
        renderShipmentsTable(filtered);
    }
}

function openWarehouseModal() {
    document.getElementById('warehouseModalTitle').innerText = 'Add New Warehouse';
    document.getElementById('editWarehouseId').value = '';
    document.getElementById('warehouseName').value = '';
    document.getElementById('warehouseCity').value = '';
    document.getElementById('warehouseModal').style.display = 'flex';
}

function openEditWarehouseModal(id, name, city) {
    document.getElementById('warehouseModalTitle').innerText = 'Edit Warehouse';
    document.getElementById('editWarehouseId').value = id;
    document.getElementById('warehouseName').value = name;
    document.getElementById('warehouseCity').value = city;
    document.getElementById('warehouseModal').style.display = 'flex';
}

async function saveWarehouse() {
    const id = document.getElementById('editWarehouseId').value;
    const payload = {
        name: document.getElementById('warehouseName').value,
        city: document.getElementById('warehouseCity').value
    };

    const url = id ? `${WAREHOUSES_API}/${id}` : WAREHOUSES_API;
    const method = id ? 'PUT' : 'POST';

    const res = await fetchWithAuth(url, {
        method: method,
        body: JSON.stringify(payload)
    });

    if (res.ok) {
        closeModal('warehouseModal');
        loadWarehouses();
    }
}

async function deleteWarehouse(id) {
    if (!confirm('Delete this warehouse?')) return;
    const res = await fetchWithAuth(`${WAREHOUSES_API}/${id}`, { method: 'DELETE' });
    if (res.ok) loadWarehouses();
}

// ==================== DRIVERS ====================

async function loadDrivers() {
    try {
        const res = await fetchWithAuth(DRIVERS_API);
        if (!res.ok) return;

        const data = await res.json();
        const drivers = parseArrayData(data);
        const tbody = document.getElementById('driversTableBody');
        if (!tbody) return;

        tbody.innerHTML = '';
        drivers.forEach(d => {
            const id = d.id ?? d.Id;
            const name = d.fullName ?? d.FullName;
            const phone = d.phoneNumber ?? d.PhoneNumber ?? '-';

            tbody.innerHTML += `
                <tr>
                    <td>#${id}</td>
                    <td>${name}</td>
                    <td>${phone}</td>
                    <td>
                        <button class="btn-action btn-view" onclick="filterShipmentsByDriver(${id})">Show Shipments</button>
                        <button class="btn-action btn-edit" onclick="openEditDriverModal(${id}, '${name}', '${phone}')">Edit</button>
                        <button class="btn-action btn-delete" onclick="deleteDriver(${id})">Delete</button>
                    </td>
                </tr>
            `;
        });

        const activeDriversEl = document.getElementById('activeDrivers');
        if (activeDriversEl) activeDriversEl.innerText = drivers.length;
    } catch (err) {
        console.error('Error loading drivers:', err);
    }
}

async function filterShipmentsByDriver(driverId) {
    switchTab('shipments');
    const res = await fetchWithAuth(`${DRIVER_UI_BASE}/driver/${driverId}`);
    if (res.ok) {
        const data = await res.json();
        renderShipmentsTable(parseArrayData(data));
    }
}

function openDriverModal() {
    document.getElementById('driverModalTitle').innerText = 'Add New Driver';
    document.getElementById('editDriverId').value = '';
    document.getElementById('driverName').value = '';
    document.getElementById('driverPhone').value = '';
    document.getElementById('driverModal').style.display = 'flex';
}

function openEditDriverModal(id, name, phone) {
    document.getElementById('driverModalTitle').innerText = 'Edit Driver';
    document.getElementById('editDriverId').value = id;
    document.getElementById('driverName').value = name;
    document.getElementById('driverPhone').value = phone;
    document.getElementById('driverModal').style.display = 'flex';
}

async function saveDriver() {
    const id = document.getElementById('editDriverId').value;
    const payload = {
        fullName: document.getElementById('driverName').value,
        phoneNumber: document.getElementById('driverPhone').value
    };

    const url = id ? `${DRIVERS_API}/${id}` : DRIVERS_API;
    const method = id ? 'PUT' : 'POST';

    const res = await fetchWithAuth(url, {
        method: method,
        body: JSON.stringify(payload)
    });

    if (res.ok) {
        closeModal('driverModal');
        loadDrivers();
    }
}

async function deleteDriver(id) {
    if (!confirm('Delete driver?')) return;
    const res = await fetchWithAuth(`${DRIVERS_API}/${id}`, { method: 'DELETE' });
    if (res.ok) loadDrivers();
}

// ==================== ROUTE TRACKER / DETAILS ====================

async function openDetailsModal(id) {
    try {
        const res = await fetchWithAuth(`${DRIVER_UI_BASE}/shipment/${id}`);
        if (!res.ok) throw new Error(`Server status ${res.status}`);

        const data = await res.json();
        const status = data.status ?? data.Status ?? 'Pending';
        const whName = data.warehouseName ?? data.WarehouseName ?? (data.warehouse?.name ?? data.Warehouse?.Name ?? 'Origin Warehouse');

        document.getElementById('detId').innerText = `#${data.id ?? data.Id ?? '-'}`;
        document.getElementById('detTracking').innerText = data.trackingNumber ?? data.TrackingNumber ?? '-';
        document.getElementById('detWeight').innerText = `${data.weight ?? data.Weight ?? '-'} kg`;
        document.getElementById('detStatus').innerText = status;
        document.getElementById('detWarehouse').innerText = whName;

        const addr = data.addressDto ?? data.AddressDto ?? data.address ?? data.Address ?? {};
        document.getElementById('detCity').innerText = addr.city ?? addr.City ?? '-';

        let checkpoints = [];
        try {
            const cpRes = await fetchWithAuth(`${DRIVER_UI_BASE}/shipment/${id}/checkpoints`);
            if (cpRes.ok) checkpoints = parseArrayData(await cpRes.json());
        } catch (e) {
            console.warn('Could not fetch checkpoints:', e);
        }

        renderRouteLine(checkpoints, status, whName);
        document.getElementById('detailsModal').style.display = 'flex';
    } catch (err) {
        console.error('Error fetching details:', err);
        alert(`Could not load details: ${err.message}`);
    }
}

function renderRouteLine(checkpoints, status, warehouseName) {
    const routeStationsContainer = document.getElementById('routeStations');
    const progressBar = document.getElementById('routeProgressBar');
    if (!routeStationsContainer || !progressBar) return;

    routeStationsContainer.innerHTML = '';
    let stationsList = [];

    if (Array.isArray(checkpoints) && checkpoints.length > 0) {
        stationsList = checkpoints.map(cp => ({
            name: cp.stationName ?? cp.StationName ?? 'Checkpoint',
            info: cp.info ?? cp.Info ?? 'No details provided.',
            timestamp: cp.timestamp ?? cp.Timestamp ?? null
        }));
    } else {
        stationsList = DEFAULT_ROUTE_TEMPLATE.map((st, idx) => idx === 0 ? { ...st, name: warehouseName } : st);
    }

    const totalStations = stationsList.length;
    let activeIndex = totalStations - 1;

    if (!checkpoints || checkpoints.length === 0) {
        status = (status || '').toLowerCase();
        if (status.includes('transit')) activeIndex = 3;
        else if (status.includes('deliver')) activeIndex = 4;
        else activeIndex = 1;
    }

    const progressPercent = totalStations > 1 ? (activeIndex / (totalStations - 1)) * 100 : 100;
    progressBar.style.width = `${progressPercent}%`;

    stationsList.forEach((station, index) => {
        const wrapper = document.createElement('div');
        wrapper.className = 'station-wrapper';

        let pointClass = 'station-point';
        if (index < activeIndex) pointClass += ' completed';
        else if (index === activeIndex) pointClass += ' active';

        const timeFormatted = station.timestamp ? new Date(station.timestamp).toLocaleString() : null;

        wrapper.innerHTML = `
            <div class="${pointClass}" onclick="selectStation('${station.name.replace(/'/g, "\\'")}', '${station.info.replace(/'/g, "\\'")}', '${timeFormatted || ''}')">
                ${index < activeIndex ? '✓' : index + 1}
            </div>
            <div class="station-label">${station.name}</div>
        `;
        routeStationsContainer.appendChild(wrapper);
    });

    const activeStation = stationsList[activeIndex];
    const initialTime = activeStation.timestamp ? new Date(activeStation.timestamp).toLocaleString() : null;
    selectStation(activeStation.name, activeStation.info, initialTime);
}

function selectStation(name, info, time) {
    const titleEl = document.getElementById('stationTitle');
    const infoEl = document.getElementById('stationInfo');

    if (titleEl) titleEl.innerText = `Checkpoint: ${name}`;
    let displayText = info;
    if (time && time !== 'null') displayText += ` | Time: ${time}`;
    if (infoEl) infoEl.innerText = displayText;
}

function switchTab(tabName) {
    document.querySelectorAll('.tab-section').forEach(t => t.classList.remove('active'));
    document.querySelectorAll('.nav-item').forEach(n => n.classList.remove('active'));

    const activeTab = document.getElementById(`${tabName}-tab`);
    const activeNav = document.getElementById(`nav-${tabName}`);

    if (activeTab) activeTab.classList.add('active');
    if (activeNav) activeNav.classList.add('active');
}

function closeModal(modalId) {
    const modal = document.getElementById(modalId);
    if (modal) modal.style.display = 'none';
}

document.addEventListener('DOMContentLoaded', checkAuthStatus);