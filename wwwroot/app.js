const API_BASE_URL = '/api';

let currentCustomers = [];
let currentTechnicians = [];
let currentServiceRequests = [];

document.addEventListener('DOMContentLoaded', () => {
    initializeTabs();
    loadDashboardStats();
    loadServiceRequests();
    loadCustomers();
    loadTechnicians();
    
    setInterval(() => {
        loadDashboardStats();
        const activeTab = document.querySelector('.tab-btn.active').dataset.tab;
        if (activeTab === 'requests') loadServiceRequests();
        else if (activeTab === 'customers') loadCustomers();
        else if (activeTab === 'technicians') loadTechnicians();
    }, 30000);
});

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
        
        document.getElementById('totalRequests').textContent = stats.totalRequests;
        document.getElementById('newRequests').textContent = stats.newRequests;
        document.getElementById('inProgressRequests').textContent = stats.inProgressRequests;
        document.getElementById('completedRequests').textContent = stats.completedRequests;
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
            <td>${request.customer?.fullName || 'Не указан'}</td>
            <td>${request.deviceBrand} ${request.deviceModel}</td>
            <td>${request.problemDescription.substring(0, 50)}...</td>
            <td>${getStatusBadge(request.status)}</td>
            <td>${request.assignedTechnician?.fullName || 'Не назначен'}</td>
            <td>${request.finalCost ? request.finalCost + ' ₽' : (request.estimatedCost ? request.estimatedCost + ' ₽' : '-')}</td>
            <td>${formatDate(request.createdAt)}</td>
            <td>
                <div class="action-buttons">
                    <button class="btn btn-info" onclick="showRequestDetails(${request.id})">Детали</button>
                    <button class="btn btn-edit" onclick="editServiceRequest(${request.id})">Изменить</button>
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
                    <div class="value">${request.customer?.fullName || 'Не указан'}</div>
                </div>
                <div class="detail-item">
                    <label>Телефон:</label>
                    <div class="value">${request.customer?.phone || '-'}</div>
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
                    <div class="value">${request.assignedTechnician?.fullName || 'Не назначен'}</div>
                </div>
                <div class="detail-item">
                    <label>Стоимость:</label>
                    <div class="value">${request.finalCost ? request.finalCost + ' ₽' : (request.estimatedCost ? request.estimatedCost + ' ₽ (оценочная)' : '-')}</div>
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
        const response = await fetch(`${API_BASE_URL}/technicians`);
        currentTechnicians = await response.json();
        renderTechnicians();
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
            <td><span class="status-badge ${technician.isActive ? 'status-completed' : 'status-cancelled'}">${technician.isActive ? 'Активен' : 'Неактивен'}</span></td>
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
                ${currentTechnicians.filter(t => t.isActive).map(t => `<option value="${t.id}">${t.fullName}</option>`).join('')}
            </select>
        </div>
    `;
    
    showModal('Новая заявка', formFields, async (formData) => {
        try {
            const response = await fetch(`${API_BASE_URL}/servicerequests`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
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
