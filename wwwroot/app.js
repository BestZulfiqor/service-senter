const API_BASE_URL = '/api';

let currentCustomers = [];
let currentTechnicians = [];
let currentServiceRequests = [];
let currentTransactions = [];
let currentUser = null;
let currentUsers = [];

document.addEventListener('DOMContentLoaded', () => {
    checkAuth();
    initializeTabs();
    loadDashboardStats();

    setInterval(() => {
        loadDashboardStats();
        const activeTab = document.querySelector('.tab-btn.active')?.dataset.tab;
        if (activeTab === 'requests') loadServiceRequests();
        else if (activeTab === 'customers') loadCustomers();
        else if (activeTab === 'technicians') loadTechnicians();
        else if (activeTab === 'users') loadUsers();
        else if (activeTab === 'financial') loadTransactions();
    }, 30000);
});

function checkAuth() {
    const token = localStorage.getItem('token');
    const userRole = localStorage.getItem('userRole');
    const userName = localStorage.getItem('userFullName');

    if (token && userRole) {
        // Пользователь авторизован
        document.getElementById('loginBtn').style.display = 'none';
        document.getElementById('userMenu').style.display = 'flex';
        document.getElementById('userName').textContent = userName;

        currentUser = {
            role: userRole,
            name: userName,
            customerId: localStorage.getItem('customerId'),
            technicianId: localStorage.getItem('technicianId')
        };

        showInterfaceByRole(userRole);
    } else {
        // Пользователь не авторизован - показываем публичную часть
        document.getElementById('loginBtn').style.display = 'block';
        document.getElementById('userMenu').style.display = 'none';
    }
}

function showInterfaceByRole(role) {
    if (role === 'Admin') {
        // Админ - показываем админ панель
        document.getElementById('admin').style.display = 'block';
        document.getElementById('dashboard').style.display = 'none';
        loadServiceRequests();
        loadCustomers();
        loadTechnicians();
        loadUsers();
    } else if (role === 'Client') {
        // Клиент - показываем его заявки
        document.getElementById('dashboardLink').style.display = 'block';
        document.getElementById('dashboard').style.display = 'block';
        document.getElementById('clientDashboard').style.display = 'block';
        document.getElementById('technicianDashboard').style.display = 'none';
        document.getElementById('admin').style.display = 'none';
        loadClientRequests();
    } else if (role === 'Technician') {
        // Техник - показываем назначенные ему заявки
        document.getElementById('dashboardLink').style.display = 'block';
        document.getElementById('dashboard').style.display = 'block';
        document.getElementById('clientDashboard').style.display = 'none';
        document.getElementById('technicianDashboard').style.display = 'block';
        document.getElementById('admin').style.display = 'none';
        loadTechnicianRequests();
    }
}

function logout() {
    localStorage.clear();
    window.location.href = '/';
}

function scrollToAdmin() {
    if (currentUser && currentUser.role === 'Admin') {
        document.getElementById('admin').scrollIntoView({ behavior: 'smooth' });
    } else if (currentUser) {
        document.getElementById('dashboard').scrollIntoView({ behavior: 'smooth' });
    } else {
        window.location.href = '/auth.html';
    }
}

function getAuthHeaders() {
    const token = localStorage.getItem('token');
    return {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`
    };
}

// Загрузка заявок клиента
async function loadClientRequests() {
    const customerId = localStorage.getItem('customerId');
    if (!customerId) return;

    try {
        const response = await fetch(`${API_BASE_URL}/servicerequests`, {
            headers: getAuthHeaders()
        });
        const allRequests = await response.json();

        // Фильтруем только заявки этого клиента
        const clientRequests = allRequests.filter(r => r.customerId == customerId);
        renderClientRequests(clientRequests);
    } catch (error) {
        console.error('Ошибка загрузки заявок клиента:', error);
    }
}

function renderClientRequests(requests) {
    const tbody = document.getElementById('clientRequestsTable');
    tbody.innerHTML = '';

    requests.forEach(request => {
        const row = document.createElement('tr');
        row.innerHTML = `
            <td>${request.id}</td>
            <td>${request.deviceBrand} ${request.deviceModel}</td>
            <td>${request.problemDescription.substring(0, 50)}...</td>
            <td>${getStatusBadge(request.status)}</td>
            <td>${request.assignedTechnicianName || 'Не назначен'}</td>
            <td>${request.finalCost ? request.finalCost + ' с' : (request.estimatedCost ? request.estimatedCost + ' с' : '-')}</td>
            <td>${formatDate(request.createdAt)}</td>
            <td>
                <button class="btn btn-info" onclick="showRequestDetails(${request.id})">Детали</button>
            </td>
        `;
        tbody.appendChild(row);
    });
}

// Функция создания заявки для клиента
function showCreateRequestForClient() {
    const customerId = localStorage.getItem('customerId');
    if (!customerId) {
        alert('Ошибка: не удалось определить ID клиента');
        return;
    }

    const formFields = `
        <div class="form-group">
            <label>Тип устройства:</label>
            <input type="text" name="deviceType" required placeholder="Например: Ноутбук, Телефон">
        </div>
        <div class="form-group">
            <label>Бренд:</label>
            <input type="text" name="deviceBrand" required placeholder="Например: Apple, Samsung">
        </div>
        <div class="form-group">
            <label>Модель:</label>
            <input type="text" name="deviceModel" required placeholder="Например: iPhone 13, Galaxy S21">
        </div>
        <div class="form-group">
            <label>Серийный номер:</label>
            <input type="text" name="serialNumber" placeholder="Если известен">
        </div>
        <div class="form-group">
            <label>Описание проблемы:</label>
            <textarea name="problemDescription" required placeholder="Опишите подробно проблему с устройством"></textarea>
        </div>
    `;

    showModal('Новая заявка на ремонт', formFields, async (formData) => {
        try {
            const response = await fetch(`${API_BASE_URL}/servicerequests`, {
                method: 'POST',
                headers: getAuthHeaders(),
                body: JSON.stringify({
                    customerId: parseInt(customerId),
                    deviceType: formData.deviceType,
                    deviceBrand: formData.deviceBrand,
                    deviceModel: formData.deviceModel,
                    serialNumber: formData.serialNumber || '',
                    problemDescription: formData.problemDescription,
                    status: 'Новая',
                    estimatedCost: null,
                    assignedTechnicianId: null
                })
            });

            if (response.ok) {
                closeModal();
                loadClientRequests();
                alert('Заявка успешно создана!');
            } else {
                const error = await response.json();
                alert('Ошибка создания заявки: ' + (error.message || 'Неизвестная ошибка'));
            }
            // В функции showInterfaceByRole, в блоке для роли Client:
        } catch (error) {
            console.error('Ошибка:', error);
            alert('Ошибка создания заявки');
        }
    });
}

// Загрузка заявок техника
async function loadTechnicianRequests() {
    const technicianId = localStorage.getItem('technicianId');
    if (!technicianId) return;

    try {
        const response = await fetch(`${API_BASE_URL}/servicerequests`, {
            headers: getAuthHeaders()
        });
        const allRequests = await response.json();

        // Фильтруем только заявки назначенные этому технику
        const technicianRequests = allRequests.filter(r => r.assignedTechnicianId == technicianId);
        renderTechnicianRequests(technicianRequests);
    } catch (error) {
        console.error('Ошибка загрузки заявок техника:', error);
    }
}

function renderTechnicianRequests(requests) {
    const tbody = document.getElementById('technicianRequestsTable');
    tbody.innerHTML = '';

    requests.forEach(request => {
        const row = document.createElement('tr');
        row.innerHTML = `
            <td>${request.id}</td>
            <td>${request.customerName}</td>
            <td>${request.deviceBrand} ${request.deviceModel}</td>
            <td>${request.problemDescription.substring(0, 50)}...</td>
            <td>${getStatusBadge(request.status)}</td>
            <td>${request.finalCost ? request.finalCost + ' с' : (request.estimatedCost ? request.estimatedCost + ' с' : '-')}</td>
            <td>${formatDate(request.createdAt)}</td>
            <td>
                <div class="action-buttons">
                    <button class="btn btn-info" onclick="showRequestDetails(${request.id})">Детали</button>
                    <button class="btn btn-edit" onclick="updateRequestStatus(${request.id})">Обновить</button>
                </div>
            </td>
        `;
        tbody.appendChild(row);
    });
}

function updateRequestStatus(id) {
    const request = currentServiceRequests.find(r => r.id === id);
    if (!request) return;

    const formFields = `
        <div class="form-group">
            <label>Статус:</label>
            <select name="status" required>
                <option value="Новая" ${request.status === 'Новая' ? 'selected' : ''}>Новая</option>
                <option value="В работе" ${request.status === 'В работе' ? 'selected' : ''}>В работе</option>
                <option value="Завершена" ${request.status === 'Завершена' ? 'selected' : ''}>Завершена</option>
            </select>
        </div>
        <div class="form-group">
            <label>Итоговая стоимость:</label>
            <input type="number" name="finalCost" value="${request.finalCost || ''}" step="0.01">
        </div>
        <div class="form-group">
            <label>Комментарий к работе:</label>
            <textarea name="workLog" placeholder="Опишите выполненную работу"></textarea>
        </div>
    `;

    showModal('Обновить статус заявки', formFields, async (formData) => {
        try {
            const response = await fetch(`${API_BASE_URL}/servicerequests/${id}`, {
                method: 'PUT',
                headers: getAuthHeaders(),
                body: JSON.stringify({
                    id: id,
                    customerId: request.customerId,
                    deviceType: request.deviceType,
                    deviceBrand: request.deviceBrand,
                    deviceModel: request.deviceModel,
                    serialNumber: request.serialNumber,
                    problemDescription: request.problemDescription,
                    status: formData.status,
                    estimatedCost: request.estimatedCost,
                    finalCost: formData.finalCost ? parseFloat(formData.finalCost) : null,
                    assignedTechnicianId: request.assignedTechnicianId,
                    createdAt: request.createdAt,
                    completedAt: formData.status === 'Завершена' ? new Date().toISOString() : request.completedAt
                })
            });

            if (response.ok) {
                // Добавляем запись в журнал работ если есть комментарий
                if (formData.workLog) {
                    await fetch(`${API_BASE_URL}/worklogs`, {
                        method: 'POST',
                        headers: getAuthHeaders(),
                        body: JSON.stringify({
                            serviceRequestId: id,
                            description: formData.workLog,
                            loggedBy: currentUser.name
                        })
                    });
                }

                closeModal();
                loadTechnicianRequests();
            } else {
                alert('Ошибка обновления заявки');
            }
        } catch (error) {
            console.error('Ошибка:', error);
            alert('Ошибка обновления заявки');
        }
    });
}

function initializeTabs() {
    const tabButtons = document.querySelectorAll('.tab-btn');
    tabButtons.forEach(btn => {
        btn.addEventListener('click', () => {
            const tabName = btn.dataset.tab;
            switchTab(tabName);
        });
    });
}

function switchTab(tabName) {
    document.querySelectorAll('.tab-btn').forEach(btn => {
        btn.classList.remove('active');
    });
    document.querySelectorAll('.tab-content').forEach(content => {
        content.classList.add('hidden');
    });

    document.querySelector(`[data-tab="${tabName}"]`).classList.add('active');
    document.getElementById(tabName).classList.remove('hidden');
}

async function loadDashboardStats() {
    try {
        const response = await fetch(`${API_BASE_URL}/servicerequests/statistics`);
        const stats = await response.json();

        // Обновляем статистику в админ панели
        const adminTotalRequests = document.getElementById('adminTotalRequests');
        if (adminTotalRequests) adminTotalRequests.textContent = stats.totalRequests;

        const newRequests = document.getElementById('newRequests');
        if (newRequests) newRequests.textContent = stats.newRequests;

        const inProgressRequests = document.getElementById('inProgressRequests');
        if (inProgressRequests) inProgressRequests.textContent = stats.inProgressRequests;

        const completedRequests = document.getElementById('completedRequests');
        if (completedRequests) completedRequests.textContent = stats.completedRequests;

        // Обновляем статистику на главной странице (секция About)
        const aboutTotalRequests = document.querySelectorAll('#totalRequests');
        aboutTotalRequests.forEach(el => el.textContent = stats.totalRequests);

        // Загружаем количество клиентов
        const customersResponse = await fetch(`${API_BASE_URL}/customers`);
        const customers = await customersResponse.json();
        const totalCustomers = document.getElementById('totalCustomers');
        if (totalCustomers) totalCustomers.textContent = customers.length;
    } catch (error) {
        console.error('Ошибка загрузки статистики:', error);
    }
}

async function loadServiceRequests() {
    try {
        const response = await fetch(`${API_BASE_URL}/servicerequests`);
        currentServiceRequests = await response.json();
        renderServiceRequests();
    } catch (error) {
        console.error('Ошибка загрузки заявок:', error);
    }
}

function renderServiceRequests() {
    const tbody = document.getElementById('requestsTableBody');
    tbody.innerHTML = '';

    currentServiceRequests.forEach(request => {
        const row = document.createElement('tr');
        row.innerHTML = `
            <td>${request.id}</td>
            <td>${request.customerName || 'Не указан'}</td>
            <td>${request.deviceBrand} ${request.deviceModel}</td>
            <td>${request.problemDescription.substring(0, 50)}...</td>
            <td>${getStatusBadge(request.status)}</td>
            <td>${request.assignedTechnicianName || 'Не назначен'}</td>
            <td>${request.finalCost ? request.finalCost + ' с' : (request.estimatedCost ? request.estimatedCost + ' с' : '-')}</td>
            <td>${formatDate(request.createdAt)}</td>
            <td>
                <div class="action-buttons">
                    <button class="btn btn-info" onclick="showRequestDetails(${request.id})">Детали</button>
                    <button class="btn btn-edit" onclick="editServiceRequest(${request.id})">Изменить</button>
                    ${request.status === 'Завершена' && !request.hasReceipt ? 
                        `<button class="btn btn-success" onclick="generateReceipt(${request.id})">🧾 Чек</button>` : 
                        request.hasReceipt ? 
                        `<span class="status-badge status-completed">Чек есть</span>` : 
                        ''
                    }
                    <button class="btn btn-danger" onclick="deleteServiceRequest(${request.id})">Удалить</button>
                </div>
            </td>
        `;
        tbody.appendChild(row);
    });
}

function getStatusBadge(status) {
    const statusClasses = {
        'Новая': 'status-new',
        'В работе': 'status-progress',
        'Завершена': 'status-completed',
        'Отменена': 'status-cancelled'
    };
    const className = statusClasses[status] || 'status-new';
    return `<span class="status-badge ${className}">${status}</span>`;
}

function formatDate(dateString) {
    const date = new Date(dateString);
    return date.toLocaleDateString('ru-RU') + ' ' + date.toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' });
}

async function showRequestDetails(id) {
    try {
        const response = await fetch(`${API_BASE_URL}/servicerequests/${id}`);
        const request = await response.json();

        const logsResponse = await fetch(`${API_BASE_URL}/worklogs/service-request/${id}`);
        const logs = await logsResponse.json();

        const detailsContent = document.getElementById('detailsContent');
        detailsContent.innerHTML = `
            <div class="details-grid">
                <div class="detail-item">
                    <label>Клиент:</label>
                    <div class="value">${request.customerName || 'Не указан'}</div>
                </div>
                <div class="detail-item">
                    <label>Телефон:</label>
                    <div class="value">${request.customerPhone || '-'}</div>
                </div>
                <div class="detail-item">
                    <label>Устройство:</label>
                    <div class="value">${request.deviceType}</div>
                </div>
                <div class="detail-item">
                    <label>Бренд и модель:</label>
                    <div class="value">${request.deviceBrand} ${request.deviceModel}</div>
                </div>
                <div class="detail-item">
                    <label>Серийный номер:</label>
                    <div class="value">${request.serialNumber || '-'}</div>
                </div>
                <div class="detail-item">
                    <label>Статус:</label>
                    <div class="value">${getStatusBadge(request.status)}</div>
                </div>
                <div class="detail-item">
                    <label>Техник:</label>
                    <div class="value">${request.assignedTechnicianName || 'Не назначен'}</div>
                </div>
                <div class="detail-item">
                    <label>Стоимость:</label>
                    <div class="value">${request.finalCost ? request.finalCost + ' с' : (request.estimatedCost ? request.estimatedCost + ' с (оценочная)' : '-')}</div>
                </div>
                <div class="detail-item" style="grid-column: 1 / -1;">
                    <label>Описание проблемы:</label>
                    <div class="value">${request.problemDescription}</div>
                </div>
            </div>
            
            <div class="work-logs">
                <h3>История работы</h3>
                ${logs.length > 0 ? logs.map(log => `
                    <div class="work-log-item">
                        <div class="log-header">
                            <span><strong>${log.loggedBy}</strong></span>
                            <span>${formatDate(log.loggedAt)}</span>
                        </div>
                        <div class="log-description">${log.description}</div>
                    </div>
                `).join('') : '<p>История пуста</p>'}
            </div>
        `;

        document.getElementById('detailsModal').classList.remove('hidden');
    } catch (error) {
        console.error('Ошибка загрузки деталей заявки:', error);
        alert('Ошибка загрузки деталей заявки');
    }
}

function closeDetailsModal() {
    document.getElementById('detailsModal').classList.add('hidden');
}

async function loadCustomers() {
    try {
        const response = await fetch(`${API_BASE_URL}/customers`);
        currentCustomers = await response.json();
        renderCustomers();
    } catch (error) {
        console.error('Ошибка загрузки клиентов:', error);
    }
}

function renderCustomers() {
    const tbody = document.getElementById('customersTableBody');
    tbody.innerHTML = '';

    currentCustomers.forEach(customer => {
        const row = document.createElement('tr');
        row.innerHTML = `
            <td>${customer.id}</td>
            <td>${customer.fullName}</td>
            <td>${customer.phone}</td>
            <td>${customer.email || '-'}</td>
            <td>${formatDate(customer.registeredAt)}</td>
            <td>${customer.serviceRequests?.length || 0}</td>
            <td>
                <div class="action-buttons">
                    <button class="btn btn-edit" onclick="editCustomer(${customer.id})">Изменить</button>
                    <button class="btn btn-danger" onclick="deleteCustomer(${customer.id})">Удалить</button>
                </div>
            </td>
        `;
        tbody.appendChild(row);
    });
}

async function loadTechnicians() {
    try {
        const response = await fetch(`${API_BASE_URL}/technicians`, {
            headers: getAuthHeaders()
        });
        if (response.ok) {
            currentTechnicians = await response.json();
            renderTechnicians();
        } else {
            console.error('Ошибка загрузки техников:', response.status);
        }
    } catch (error) {
        console.error('Ошибка загрузки техников:', error);
    }
}

function renderTechnicians() {
    const tbody = document.getElementById('techniciansTableBody');
    tbody.innerHTML = '';

    currentTechnicians.forEach(technician => {
        const row = document.createElement('tr');
        row.innerHTML = `
            <td>${technician.id}</td>
            <td>${technician.fullName}</td>
            <td>${technician.phone}</td>
            <td>${technician.specialization}</td>
            <td><span class="status-badge ${technician.IsActive ? 'status-completed' : 'status-cancelled'}">${technician.IsActive ? 'Активен' : 'Неактивен'}</span></td>
            <td>${technician.serviceRequests?.length || 0}</td>
            <td>
                <div class="action-buttons">
                    <button class="btn btn-edit" onclick="editTechnician(${technician.id})">Изменить</button>
                    <button class="btn btn-danger" onclick="deleteTechnician(${technician.id})">Удалить</button>
                </div>
            </td>
        `;
        tbody.appendChild(row);
    });
}

function showAddRequestModal() {
    const formFields = `
        <div class="form-group">
            <label>Клиент:</label>
            <select name="customerId" required>
                <option value="">Выберите клиента</option>
                ${currentCustomers.map(c => `<option value="${c.id}">${c.fullName}</option>`).join('')}
            </select>
        </div>
        <div class="form-group">
            <label>Тип устройства:</label>
            <input type="text" name="deviceType" required>
        </div>
        <div class="form-group">
            <label>Бренд:</label>
            <input type="text" name="deviceBrand" required>
        </div>
        <div class="form-group">
            <label>Модель:</label>
            <input type="text" name="deviceModel" required>
        </div>
        <div class="form-group">
            <label>Серийный номер:</label>
            <input type="text" name="serialNumber">
        </div>
        <div class="form-group">
            <label>Описание проблемы:</label>
            <textarea name="problemDescription" required></textarea>
        </div>
        <div class="form-group">
            <label>Оценочная стоимость:</label>
            <input type="number" name="estimatedCost" step="0.01">
        </div>
        <div class="form-group">
            <label>Техник:</label>
            <select name="assignedTechnicianId">
                <option value="">Не назначен</option>
                ${currentTechnicians.filter(t => t.IsActive).map(t => `<option value="${t.id}">${t.fullName}</option>`).join('')}
            </select>
        </div>
    `;

    showModal('Новая заявка', formFields, async (formData) => {
        try {
            const response = await fetch(`${API_BASE_URL}/servicerequests`, {
                method: 'POST',
                headers: getAuthHeaders(),
                body: JSON.stringify({
                    customerId: parseInt(formData.customerId),
                    deviceType: formData.deviceType,
                    deviceBrand: formData.deviceBrand,
                    deviceModel: formData.deviceModel,
                    serialNumber: formData.serialNumber,
                    problemDescription: formData.problemDescription,
                    estimatedCost: formData.estimatedCost ? parseFloat(formData.estimatedCost) : null,
                    assignedTechnicianId: formData.assignedTechnicianId ? parseInt(formData.assignedTechnicianId) : null
                })
            });

            if (response.ok) {
                closeModal();
                loadServiceRequests();
                loadDashboardStats();
            } else {
                alert('Ошибка создания заявки');
            }
        } catch (error) {
            console.error('Ошибка:', error);
            alert('Ошибка создания заявки');
        }
    });
}

function editServiceRequest(id) {
    const request = currentServiceRequests.find(r => r.id === id);
    if (!request) return;

    const formFields = `
        <div class="form-group">
            <label>Клиент:</label>
            <select name="customerId" required>
                ${currentCustomers.map(c => `<option value="${c.id}" ${c.id === request.customerId ? 'selected' : ''}>${c.fullName}</option>`).join('')}
            </select>
        </div>
        <div class="form-group">
            <label>Тип устройства:</label>
            <input type="text" name="deviceType" value="${request.deviceType}" required>
        </div>
        <div class="form-group">
            <label>Бренд:</label>
            <input type="text" name="deviceBrand" value="${request.deviceBrand}" required>
        </div>
        <div class="form-group">
            <label>Модель:</label>
            <input type="text" name="deviceModel" value="${request.deviceModel}" required>
        </div>
        <div class="form-group">
            <label>Серийный номер:</label>
            <input type="text" name="serialNumber" value="${request.serialNumber || ''}">
        </div>
        <div class="form-group">
            <label>Описание проблемы:</label>
            <textarea name="problemDescription" required>${request.problemDescription}</textarea>
        </div>
        <div class="form-group">
            <label>Статус:</label>
            <select name="status" required>
                <option value="Новая" ${request.status === 'Новая' ? 'selected' : ''}>Новая</option>
                <option value="В работе" ${request.status === 'В работе' ? 'selected' : ''}>В работе</option>
                <option value="Завершена" ${request.status === 'Завершена' ? 'selected' : ''}>Завершена</option>
                <option value="Отменена" ${request.status === 'Отменена' ? 'selected' : ''}>Отменена</option>
            </select>
        </div>
        <div class="form-group">
            <label>Оценочная стоимость:</label>
            <input type="number" name="estimatedCost" value="${request.estimatedCost || ''}" step="0.01">
        </div>
        <div class="form-group">
            <label>Итоговая стоимость:</label>
            <input type="number" name="finalCost" value="${request.finalCost || ''}" step="0.01">
        </div>
        <div class="form-group">
            <label>Техник:</label>
            <select name="assignedTechnicianId">
                <option value="">Не назначен</option>
                ${currentTechnicians.map(t => `<option value="${t.id}" ${t.id === request.assignedTechnicianId ? 'selected' : ''}>${t.fullName}</option>`).join('')}
            </select>
        </div>
    `;

    showModal('Редактировать заявку', formFields, async (formData) => {
        try {
            const response = await fetch(`${API_BASE_URL}/servicerequests/${id}`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    id: id,
                    customerId: parseInt(formData.customerId),
                    deviceType: formData.deviceType,
                    deviceBrand: formData.deviceBrand,
                    deviceModel: formData.deviceModel,
                    serialNumber: formData.serialNumber,
                    problemDescription: formData.problemDescription,
                    status: formData.status,
                    estimatedCost: formData.estimatedCost ? parseFloat(formData.estimatedCost) : null,
                    finalCost: formData.finalCost ? parseFloat(formData.finalCost) : null,
                    assignedTechnicianId: formData.assignedTechnicianId ? parseInt(formData.assignedTechnicianId) : null,
                    createdAt: request.createdAt,
                    completedAt: formData.status === 'Завершена' ? new Date().toISOString() : request.completedAt
                })
            });

            if (response.ok) {
                closeModal();
                loadServiceRequests();
                loadDashboardStats();
            } else {
                alert('Ошибка обновления заявки');
            }
        } catch (error) {
            console.error('Ошибка:', error);
            alert('Ошибка обновления заявки');
        }
    });
}

async function deleteServiceRequest(id) {
    if (!confirm('Вы уверены, что хотите удалить эту заявку?')) return;

    try {
        const response = await fetch(`${API_BASE_URL}/servicerequests/${id}`, {
            method: 'DELETE'
        });

        if (response.ok) {
            loadServiceRequests();
            loadDashboardStats();
        } else {
            alert('Ошибка удаления заявки');
        }
    } catch (error) {
        console.error('Ошибка:', error);
        alert('Ошибка удаления заявки');
    }
}

function showAddCustomerModal() {
    const formFields = `
        <div class="form-group">
            <label>ФИО:</label>
            <input type="text" name="fullName" required>
        </div>
        <div class="form-group">
            <label>Телефон:</label>
            <input type="tel" name="phone" required>
        </div>
        <div class="form-group">
            <label>Email:</label>
            <input type="email" name="email">
        </div>
    `;

    showModal('Новый клиент', formFields, async (formData) => {
        try {
            const response = await fetch(`${API_BASE_URL}/customers`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(formData)
            });

            if (response.ok) {
                closeModal();
                loadCustomers();
            } else {
                alert('Ошибка создания клиента');
            }
        } catch (error) {
            console.error('Ошибка:', error);
            alert('Ошибка создания клиента');
        }
    });
}

function editCustomer(id) {
    const customer = currentCustomers.find(c => c.id === id);
    if (!customer) return;

    const formFields = `
        <div class="form-group">
            <label>ФИО:</label>
            <input type="text" name="fullName" value="${customer.fullName}" required>
        </div>
        <div class="form-group">
            <label>Телефон:</label>
            <input type="tel" name="phone" value="${customer.phone}" required>
        </div>
        <div class="form-group">
            <label>Email:</label>
            <input type="email" name="email" value="${customer.email || ''}">
        </div>
    `;

    showModal('Редактировать клиента', formFields, async (formData) => {
        try {
            const response = await fetch(`${API_BASE_URL}/customers/${id}`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    id: id,
                    ...formData,
                    registeredAt: customer.registeredAt
                })
            });

            if (response.ok) {
                closeModal();
                loadCustomers();
            } else {
                alert('Ошибка обновления клиента');
            }
        } catch (error) {
            console.error('Ошибка:', error);
            alert('Ошибка обновления клиента');
        }
    });
}

async function deleteCustomer(id) {
    if (!confirm('Вы уверены, что хотите удалить этого клиента? Это также удалит все его заявки.')) return;

    try {
        const response = await fetch(`${API_BASE_URL}/customers/${id}`, {
            method: 'DELETE'
        });

        if (response.ok) {
            loadCustomers();
            loadServiceRequests();
            loadDashboardStats();
        } else {
            alert('Ошибка удаления клиента');
        }
    } catch (error) {
        console.error('Ошибка:', error);
        alert('Ошибка удаления клиента');
    }
}

function showAddTechnicianModal() {
    const formFields = `
        <div class="form-group">
            <label>ФИО:</label>
            <input type="text" name="fullName" required>
        </div>
        <div class="form-group">
            <label>Телефон:</label>
            <input type="tel" name="phone" required>
        </div>
        <div class="form-group">
            <label>Специализация:</label>
            <input type="text" name="specialization" required>
        </div>
        <div class="form-group">
            <label>
                <input type="checkbox" name="isActive" checked>
                Активен
            </label>
        </div>
    `;

    showModal('Новый техник', formFields, async (formData) => {
        try {
            const response = await fetch(`${API_BASE_URL}/technicians`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    fullName: formData.fullName,
                    phone: formData.phone,
                    specialization: formData.specialization,
                    isActive: formData.isActive === 'on'
                })
            });

            if (response.ok) {
                closeModal();
                loadTechnicians();
            } else {
                alert('Ошибка создания техника');
            }
        } catch (error) {
            console.error('Ошибка:', error);
            alert('Ошибка создания техника');
        }
    });
}

function editTechnician(id) {
    const technician = currentTechnicians.find(t => t.id === id);
    if (!technician) return;

    const formFields = `
        <div class="form-group">
            <label>ФИО:</label>
            <input type="text" name="fullName" value="${technician.fullName}" required>
        </div>
        <div class="form-group">
            <label>Телефон:</label>
            <input type="tel" name="phone" value="${technician.phone}" required>
        </div>
        <div class="form-group">
            <label>Специализация:</label>
            <input type="text" name="specialization" value="${technician.specialization}" required>
        </div>
        <div class="form-group">
            <label>
                <input type="checkbox" name="isActive" ${technician.isActive ? 'checked' : ''}>
                Активен
            </label>
        </div>
    `;

    showModal('Редактировать техника', formFields, async (formData) => {
        try {
            const response = await fetch(`${API_BASE_URL}/technicians/${id}`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    id: id,
                    fullName: formData.fullName,
                    phone: formData.phone,
                    specialization: formData.specialization,
                    isActive: formData.isActive === 'on'
                })
            });

            if (response.ok) {
                closeModal();
                loadTechnicians();
            } else {
                alert('Ошибка обновления техника');
            }
        } catch (error) {
            console.error('Ошибка:', error);
            alert('Ошибка обновления техника');
        }
    });
}

async function deleteTechnician(id) {
    if (!confirm('Вы уверены, что хотите удалить этого техника?')) return;

    try {
        const response = await fetch(`${API_BASE_URL}/technicians/${id}`, {
            method: 'DELETE'
        });

        if (response.ok) {
            loadTechnicians();
            loadServiceRequests();
        } else {
            alert('Ошибка удаления техника');
        }
    } catch (error) {
        console.error('Ошибка:', error);
        alert('Ошибка удаления техника');
    }
}

function showModal(title, formFieldsHtml, onSubmit) {
    document.getElementById('modalTitle').textContent = title;
    document.getElementById('formFields').innerHTML = formFieldsHtml;

    const form = document.getElementById('modalForm');
    form.onsubmit = (e) => {
        e.preventDefault();
        const formData = new FormData(form);
        const data = {};
        formData.forEach((value, key) => {
            data[key] = value;
        });
        onSubmit(data);
    };

    document.getElementById('modal').classList.remove('hidden');
}

function closeModal() {
    document.getElementById('modal').classList.add('hidden');
}

// Управление пользователями и ролями
async function loadUsers() {
    try {
        const response = await fetch(`${API_BASE_URL}/admin/users`, {
            headers: getAuthHeaders()
        });
        currentUsers = await response.json();
        renderUsers();
    } catch (error) {
        console.error('Ошибка загрузки пользователей:', error);
    }
}

function renderUsers() {
    const tbody = document.getElementById('usersTableBody');
    if (!tbody) return;
    
    tbody.innerHTML = '';

    currentUsers.forEach(user => {
        const row = document.createElement('tr');
        row.innerHTML = `
            <td>${user.id}</td>
            <td>${user.email}</td>
            <td>${user.fullName}</td>
            <td>${user.phoneNumber || '-'}</td>
            <td>
                <span class="status-badge ${getRoleBadgeClass(user.roles[0])}">${user.roles[0]}</span>
            </td>
            <td>
                <div class="action-buttons">
                    <button class="btn btn-edit" onclick="changeUserRole(${user.id}, '${user.roles[0]}')">Изменить роль</button>
                    ${user.roles[0] !== 'Admin' ? `<button class="btn btn-danger" onclick="deleteUser(${user.id})">Удалить</button>` : ''}
                </div>
            </td>
        `;
        tbody.appendChild(row);
    });
}

function getRoleBadgeClass(role) {
    const roleClasses = {
        'Admin': 'status-completed',
        'Technician': 'status-progress',
        'Client': 'status-new'
    };
    return roleClasses[role] || 'status-new';
}

function changeUserRole(userId, currentRole) {
    const roles = ['Client', 'Technician', 'Admin'];
    const availableRoles = roles.filter(r => r !== currentRole);
    
    const formFields = `
        <div class="form-group">
            <label>Текущая роль:</label>
            <input type="text" value="${currentRole}" disabled style="background: #f5f5f5;">
        </div>
        <div class="form-group">
            <label>Новая роль:</label>
            <select name="newRole" required>
                <option value="">Выберите роль</option>
                ${availableRoles.map(role => `<option value="${role}">${role}</option>`).join('')}
            </select>
        </div>
    `;

    showModal('Изменить роль пользователя', formFields, async (formData) => {
        try {
            const response = await fetch(`${API_BASE_URL}/admin/users/${userId}/change-role`, {
                method: 'POST',
                headers: getAuthHeaders(),
                body: JSON.stringify({ newRole: formData.newRole })
            });

            if (response.ok) {
                closeModal();
                loadUsers();
                loadCustomers();
                loadTechnicians();
                alert('Роль пользователя успешно изменена!\n\nПользователь должен будет перезайти в систему для применения изменений.');
            } else {
                const error = await response.json();
                alert('Ошибка изменения роли: ' + (error.message || 'Неизвестная ошибка'));
            }
        } catch (error) {
            console.error('Ошибка:', error);
            alert('Ошибка изменения роли');
        }
    });
}

async function deleteUser(userId) {
    if (!confirm('Вы уверены, что хотите удалить этого пользователя?')) return;

    try {
        const response = await fetch(`${API_BASE_URL}/admin/users/${userId}`, {
            method: 'DELETE',
            headers: getAuthHeaders()
        });

        if (response.ok) {
            loadUsers();
            loadCustomers();
            loadTechnicians();
            loadServiceRequests();
            alert('Пользователь успешно удален!');
        } else {
            const error = await response.json();
            alert('Ошибка удаления: ' + (error.message || 'Неизвестная ошибка'));
        }
    } catch (error) {
        console.error('Ошибка:', error);
        alert('Ошибка удаления пользователя');
    }
}

// Управление чеками
async function generateReceipt(serviceRequestId) {
    if (!confirm('Вы уверены, что хотите сгенерировать чек для этой заявки?')) {
        return;
    }

    try {
        const response = await fetch(`${API_BASE_URL}/receipts/generate/${serviceRequestId}`, {
            method: 'POST',
            headers: getAuthHeaders()
        });

        if (response.ok) {
            const receipt = await response.json();
            loadServiceRequests();
            
            // Показываем информацию о сгенерированном чеке
            alert(`Чек успешно сгенерирован!\n\nНомер: ${receipt.receiptNumber}\nСумма: ${receipt.totalAmount} с\nОписание: ${receipt.servicesDescription}`);
        } else {
            let errorMessage = 'Неизвестная ошибка';
            try {
                const error = await response.json();
                errorMessage = error.message || error.title || 'Ошибка сервера';
            } catch (e) {
                errorMessage = `Ошибка ${response.status}: ${response.statusText}`;
            }
            alert('Ошибка генерации чека: ' + errorMessage);
        }
    } catch (error) {
        console.error('Ошибка:', error);
        alert('Ошибка генерации чека');
    }
}

// Финансовые функции
async function loadTransactions() {
    try {
        const response = await fetch(`${API_BASE_URL}/financial/transactions`, {
            headers: getAuthHeaders()
        });
        if (response.ok) {
            currentTransactions = await response.json();
            renderTransactions();
        }
    } catch (error) {
        console.error('Ошибка загрузки транзакций:', error);
    }
}

function renderTransactions() {
    const tbody = document.getElementById('transactionsTableBody');
    tbody.innerHTML = '';

    currentTransactions.forEach(transaction => {
        const row = document.createElement('tr');
        row.innerHTML = `
            <td>${formatDate(transaction.transactionDate)}</td>
            <td><span class="status-badge ${transaction.type === 'Income' ? 'status-completed' : 'status-cancelled'}">${transaction.type === 'Income' ? 'Доход' : 'Расход'}</span></td>
            <td>${transaction.category}</td>
            <td>${transaction.description}</td>
            <td>${transaction.amount} с</td>
            <td>${transaction.paymentMethod}</td>
            <td>
                <div class="action-buttons">
                    <button class="btn btn-danger" onclick="deleteTransaction(${transaction.id})">Удалить</button>
                </div>
            </td>
        `;
        tbody.appendChild(row);
    });
}

async function generateFinancialReport() {
    const startDate = document.getElementById('startDate').value;
    const endDate = document.getElementById('endDate').value;

    if (!startDate || !endDate) {
        alert('Пожалуйста, выберите даты начала и конца периода');
        return;
    }

    try {
        const response = await fetch(`${API_BASE_URL}/financial/report?startDate=${startDate}&endDate=${endDate}`, {
            headers: getAuthHeaders()
        });

        if (response.ok) {
            const report = await response.json();
            displayFinancialReport(report);
        } else {
            alert('Ошибка загрузки отчета');
        }
    } catch (error) {
        console.error('Ошибка:', error);
        alert('Ошибка загрузки отчета');
    }
}

function displayFinancialReport(report) {
    document.getElementById('totalIncome').textContent = report.totalIncome + ' с';
    document.getElementById('totalExpenses').textContent = report.totalExpenses + ' с';
    document.getElementById('netProfit').textContent = (report.totalIncome - report.totalExpenses) + ' с';

    // Отображение категорий доходов
    const incomeCategories = document.getElementById('incomeCategories');
    incomeCategories.innerHTML = '';
    report.incomeByCategory.forEach(cat => {
        const div = document.createElement('div');
        div.className = 'category-item';
        div.innerHTML = `
            <span>${cat.category}: ${cat.amount} с (${cat.percentage.toFixed(1)}%)</span>
            <small>${cat.transactionCount} транзакций</small>
        `;
        incomeCategories.appendChild(div);
    });

    // Отображение категорий расходов
    const expenseCategories = document.getElementById('expenseCategories');
    expenseCategories.innerHTML = '';
    report.expensesByCategory.forEach(cat => {
        const div = document.createElement('div');
        div.className = 'category-item';
        div.innerHTML = `
            <span>${cat.category}: ${cat.amount} с (${cat.percentage.toFixed(1)}%)</span>
            <small>${cat.transactionCount} транзакций</small>
        `;
        expenseCategories.appendChild(div);
    });
}

function showAddTransactionModal() {
    const formFields = `
        <div class="form-group">
            <label>Тип:</label>
            <select name="type" required>
                <option value="Income">Доход</option>
                <option value="Expense">Расход</option>
            </select>
        </div>
        <div class="form-group">
            <label>Категория:</label>
            <select name="category" required>
                <option value="Service">Услуга</option>
                <option value="Parts">Запчасти</option>
                <option value="Rent">Аренда</option>
                <option value="Utilities">Коммунальные услуги</option>
                <option value="Salary">Зарплата</option>
                <option value="Other">Другое</option>
            </select>
        </div>
        <div class="form-group">
            <label>Сумма:</label>
            <input type="number" name="amount" step="0.01" required>
        </div>
        <div class="form-group">
            <label>Описание:</label>
            <textarea name="description" required></textarea>
        </div>
        <div class="form-group">
            <label>Способ оплаты:</label>
            <select name="paymentMethod" required>
                <option value="Наличные">Наличные</option>
                <option value="Карта">Карта</option>
                <option value="Перевод">Перевод</option>
            </select>
        </div>
    `;

    showModal('Добавить транзакцию', formFields, async (formData) => {
        try {
            const response = await fetch(`${API_BASE_URL}/financial/transaction`, {
                method: 'POST',
                headers: getAuthHeaders(),
                body: JSON.stringify({
                    type: formData.type,
                    category: formData.category,
                    amount: parseFloat(formData.amount),
                    description: formData.description,
                    paymentMethod: formData.paymentMethod
                })
            });

            if (response.ok) {
                closeModal();
                loadTransactions();
                alert('Транзакция успешно добавлена!');
            } else {
                const error = await response.json();
                alert('Ошибка добавления транзакции: ' + (error.message || 'Неизвестная ошибка'));
            }
        } catch (error) {
            console.error('Ошибка:', error);
            alert('Ошибка добавления транзакции');
        }
    });
}

async function deleteTransaction(transactionId) {
    if (!confirm('Вы уверены, что хотите удалить эту транзакцию?')) return;

    try {
        const response = await fetch(`${API_BASE_URL}/financial/transaction/${transactionId}`, {
            method: 'DELETE',
            headers: getAuthHeaders()
        });

        if (response.ok) {
            loadTransactions();
            alert('Транзакция успешно удалена!');
        } else {
            alert('Ошибка удаления транзакции');
        }
    } catch (error) {
        console.error('Ошибка:', error);
        alert('Ошибка удаления транзакции');
    }
}
