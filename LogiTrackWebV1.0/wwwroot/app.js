// ============================================================
// LogiTrack - Customer Portal (index.html)
// ============================================================

let cachedLookups = { warehouses: [], stations: [] };
let lastQuote = null;

document.addEventListener('DOMContentLoaded', () => {
    checkAuth();
    setupTabs();
    setMinDate();

    document.getElementById('btn-login')?.addEventListener('click', handleLogin);
    document.getElementById('btn-logout')?.addEventListener('click', () => logout('index.html'));
    document.getElementById('btn-quote')?.addEventListener('click', calculateQuote);
    document.getElementById('btn-submit')?.addEventListener('click', submitRequest);
    document.getElementById('btn-track')?.addEventListener('click', trackShipment);
    document.getElementById('btn-refresh-user')?.addEventListener('click', loadUserShipments);

    ['req-weight', 'req-warehouse', 'req-station', 'req-date'].forEach(id => {
        document.getElementById(id)?.addEventListener('change', () => {
            const btn = document.getElementById('btn-submit');
            if (btn) btn.disabled = true;
        });
    });
});

// ---------- Auth / routing ----------

function checkAuth() {
    const token = getToken();

    const loginView = document.getElementById('view-login');
    const appView = document.getElementById('view-app');

    if (!token) {
        if (loginView) loginView.hidden = false;
        if (appView) appView.hidden = true;
        return;
    }

    const role = getRole();
    const home = ROLE_HOME[role];

    // Non-customer roles belong on their own portal.
    if (home && home !== 'index.html') {
        window.location.href = home;
        return;
    }

    if (role !== 'Customer') {
        logout('index.html');
        return;
    }

    if (loginView) loginView.hidden = true;
    if (appView) appView.hidden = false;

    const userEl = document.getElementById('user-name');
    if (userEl) userEl.textContent = getUsername() || 'Customer';

    loadWarehousesAndStations();
}

async function handleLogin() {
    const username = document.getElementById('login-username').value.trim();
    const password = document.getElementById('login-password').value.trim();
    const errBox = document.getElementById('login-error');

    if (errBox) errBox.hidden = true;

    if (!username || !password) {
        if (errBox) {
            errBox.innerText = 'Username/email or Driver ID and password are required.';
            errBox.hidden = false;
        }
        return;
    }

    try {
        const res = await fetch(`${API_ORIGIN}/api/Auth/Login`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ emailOrUsername: username, password })
        });

        const data = await res.json();

        if (!res.ok) {
            throw new Error(data.message || data.Message || 'Login failed.');
        }

        const rawRoles =
            (data.user && (data.user.roles || data.user.Roles)) || [];

        const role = pickPrimaryRole(rawRoles);

        if (!role) {
            throw new Error('User has no assigned role.');
        }

        localStorage.setItem('authToken', data.token);
        localStorage.setItem('username',
            (data.user && (data.user.userName || data.user.UserName)) || username);
        localStorage.setItem('role', role);

        if (data.driverId) {
            localStorage.setItem('driverId', data.driverId);
        } else {
            localStorage.removeItem('driverId');
        }

        const home = ROLE_HOME[role] || 'index.html';
        if (home !== 'index.html') {
            window.location.href = home;
            return;
        }

        // Customer → stay on index.html
        checkAuth();
    } catch (e) {
        console.error('Login error:', e);
        if (errBox) {
            errBox.innerText = e.message;
            errBox.hidden = false;
        }
    }
}

// ---------- Tabs ----------

function setupTabs() {
    const tabs = document.querySelectorAll('.tabs .tab');
    tabs.forEach(tab => {
        tab.addEventListener('click', () => {
            tabs.forEach(t => t.classList.remove('is-active'));
            tab.classList.add('is-active');

            const target = tab.getAttribute('data-tab');
            document.querySelectorAll('#view-app .panel').forEach(p => p.hidden = true);
            const activePanel = document.getElementById(`panel-${target}`);
            if (activePanel) activePanel.hidden = false;

            if (target === 'list') loadUserShipments();
        });
    });
}

function setMinDate() {
    const dateInput = document.getElementById('req-date');
    if (dateInput) {
        dateInput.min = new Date().toISOString().split('T')[0];
    }
}

// ---------- Lookups ----------

async function loadWarehousesAndStations() {
    try {
        const data = await apiFetch('/CustomerUi/lookups');
        cachedLookups = data;

        const whSelect = document.getElementById('req-warehouse');
        if (whSelect) {
            whSelect.innerHTML = '<option value="">Select origin warehouse</option>';
            (data.warehouses || []).forEach(w => {
                whSelect.innerHTML +=
                    `<option value="${w.id}">${escapeHtml(w.name)}</option>`;
            });
        }

        const stSelect = document.getElementById('req-station');
        if (stSelect) {
            stSelect.innerHTML = '<option value="">Select destination station</option>';
            (data.stations || []).forEach(s => {
                stSelect.innerHTML +=
                    `<option value="${s.id}">${escapeHtml(s.name || s.stationName)}</option>`;
            });
        }
    } catch (err) {
        console.error('Dropdown load error:', err);
    }
}

// ---------- Quote & Submit ----------

function readRequestForm() {
    const weight = parseFloat(document.getElementById('req-weight').value);
    const warehouseId = parseInt(document.getElementById('req-warehouse').value);
    const endStationId = parseInt(document.getElementById('req-station').value);
    const receiveDate = document.getElementById('req-date').value;

    if (!weight || weight <= 0) return { error: 'Please enter a valid weight.' };
    if (!warehouseId) return { error: 'Please select the origin warehouse.' };
    if (!endStationId) return { error: 'Please select the destination station.' };
    if (!receiveDate) return { error: 'Please select the receive date.' };

    return { payload: { weight, warehouseId, endStationId, receiveDate } };
}

async function calculateQuote() {
    const { payload, error } = readRequestForm();
    if (error) { alert(error); return; }

    try {
        const quote = await apiFetch('/CustomerUi/quote', {
            method: 'POST',
            body: JSON.stringify(payload)
        });

        lastQuote = quote;

        document.getElementById('q-base').innerText = `${quote.baseFee.toFixed(2)} ${quote.currency}`;
        document.getElementById('q-weight').innerText = `${quote.weightCharge.toFixed(2)} ${quote.currency}`;
        document.getElementById('quote-total').innerText = quote.total.toFixed(2);

        document.getElementById('quote-empty').hidden = true;
        document.getElementById('quote-body').hidden = false;
        document.getElementById('btn-submit').disabled = false;
    } catch (e) {
        alert(e.message);
    }
}

async function submitRequest() {
    const { payload, error } = readRequestForm();
    if (error) { alert(error); return; }

    try {
        await apiFetch('/CustomerUi/requests', {
            method: 'POST',
            body: JSON.stringify(payload)
        });

        alert('Your shipment request was submitted successfully!');
        document.getElementById('quote-empty').hidden = false;
        document.getElementById('quote-body').hidden = true;
        document.getElementById('btn-submit').disabled = true;
        document.querySelector('[data-tab="list"]').click();
    } catch (e) {
        alert(e.message);
    }
}

// ---------- My Shipments ----------

async function loadUserShipments() {
    const tbody = document.getElementById('user-shipments-body');
    const emptyBox = document.getElementById('requests-empty');
    if (!tbody) return;

    try {
        const list = await apiFetch('/CustomerUi/requests');

        tbody.innerHTML = '';
        if (!list || list.length === 0) {
            if (emptyBox) emptyBox.hidden = false;
            return;
        }
        if (emptyBox) emptyBox.hidden = true;

        list.forEach(item => {
            tbody.innerHTML += `
                <tr>
                    <td><strong>${escapeHtml(item.trackingNumber)}</strong></td>
                    <td>${escapeHtml(item.warehouseName)} &rarr; ${escapeHtml(item.endStationName)}</td>
                    <td>${item.weight} kg</td>
                    <td><span class="badge ${getStatusBadgeClass(item.status)}">${escapeHtml(item.status)}</span></td>
                    <td>
                        ${item.canCancel
                    ? `<button class="btn-action btn-delete" onclick="cancelShipment(${item.id})">Cancel</button>`
                    : '<span style="color:var(--text-muted); font-size:0.8rem;">Locked</span>'}
                    </td>
                </tr>
            `;
        });
    } catch (e) {
        console.error(e);
    }
}

async function cancelShipment(id) {
    if (!confirm('Are you sure you want to cancel this request?')) return;
    try {
        await apiFetch(`/CustomerUi/requests/${id}/cancel`, { method: 'PUT' });
        loadUserShipments();
    } catch (e) {
        alert(e.message);
    }
}

// ---------- Track ----------

async function trackShipment() {
    const query = document.getElementById('track-input').value.trim();
    const resultDiv = document.getElementById('track-result');
    if (!query) { alert('Please enter a tracking number.'); return; }

    try {
        const details = await apiFetch(`/CustomerUi/track/${encodeURIComponent(query)}`);
        resultDiv.hidden = false;

        if (!details) {
            resultDiv.innerHTML = `<p style="color: var(--danger);">No shipment found for "${escapeHtml(query)}".</p>`;
            return;
        }

        const checkpointsHtml = (details.checkpoints && details.checkpoints.length > 0)
            ? details.checkpoints.map(cp => `
                <div style="padding:10px 0; border-bottom:1px solid var(--border-color);">
                    <strong>${escapeHtml(cp.stationName)}</strong>
                    <div style="color:var(--text-muted); font-size:0.85rem;">${escapeHtml(cp.info || '')}</div>
                    <div style="color:var(--text-muted); font-size:0.78rem;">${formatDateTime(cp.timestamp)}</div>
                </div>
            `).join('')
            : '<p style="color:var(--text-muted);">No checkpoints yet.</p>';

        resultDiv.innerHTML = `
            <div class="card" style="background: var(--bg-dark);">
                <h3>Status: <span style="color:var(--accent-primary);">${escapeHtml(details.status)}</span></h3>
                <p><strong>Tracking #:</strong> ${escapeHtml(details.trackingNumber)}</p>
                <p><strong>From:</strong> ${escapeHtml(details.warehouseName)} &rarr; <strong>To:</strong> ${escapeHtml(details.endStationName)}</p>
                <p><strong>Weight:</strong> ${details.weight} kg</p>
                <div style="margin-top:16px;">${checkpointsHtml}</div>
            </div>
        `;
    } catch (e) {
        resultDiv.hidden = false;
        resultDiv.innerHTML = `<p style="color: var(--danger);">${escapeHtml(e.message)}</p>`;
    }
}