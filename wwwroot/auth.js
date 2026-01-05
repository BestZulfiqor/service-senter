const API_BASE_URL = '/api';

function switchAuthTab(tab) {
    document.querySelectorAll('.auth-tab').forEach(btn => {
        btn.classList.remove('active');
    });
    document.querySelectorAll('.auth-form').forEach(form => {
        form.classList.remove('active');
    });
    
    if (tab === 'login') {
        document.querySelector('[onclick="switchAuthTab(\'login\')"]').classList.add('active');
        document.getElementById('loginForm').classList.add('active');
    } else {
        document.querySelector('[onclick="switchAuthTab(\'register\')"]').classList.add('active');
        document.getElementById('registerForm').classList.add('active');
    }
}

// Login Form
document.getElementById('loginForm').addEventListener('submit', async (e) => {
    e.preventDefault();
    
    const email = document.getElementById('loginEmail').value;
    const password = document.getElementById('loginPassword').value;
    const errorDiv = document.getElementById('loginError');
    
    errorDiv.style.display = 'none';
    
    try {
        const response = await fetch(`${API_BASE_URL}/auth/login`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ email, password })
        });
        
        if (response.ok) {
            const data = await response.json();
            
            // Сохраняем токен и данные пользователя
            localStorage.setItem('token', data.token);
            localStorage.setItem('userEmail', data.email);
            localStorage.setItem('userFullName', data.fullName);
            localStorage.setItem('userRole', data.role);
            if (data.customerId) localStorage.setItem('customerId', data.customerId);
            if (data.technicianId) localStorage.setItem('technicianId', data.technicianId);
            
            // Перенаправляем на главную страницу
            window.location.href = '/';
        } else {
            const error = await response.json();
            errorDiv.textContent = error.message || 'Ошибка входа';
            errorDiv.style.display = 'block';
        }
    } catch (error) {
        console.error('Login error:', error);
        errorDiv.textContent = 'Ошибка соединения с сервером';
        errorDiv.style.display = 'block';
    }
});

// Register Form
document.getElementById('registerForm').addEventListener('submit', async (e) => {
    e.preventDefault();
    
    const fullName = document.getElementById('registerFullName').value;
    const email = document.getElementById('registerEmail').value;
    const phone = document.getElementById('registerPhone').value;
    const password = document.getElementById('registerPassword').value;
    
    const errorDiv = document.getElementById('registerError');
    const successDiv = document.getElementById('registerSuccess');
    
    errorDiv.style.display = 'none';
    successDiv.style.display = 'none';
    
    try {
        const response = await fetch(`${API_BASE_URL}/auth/register`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ fullName, email, phone, password })
        });
        
        if (response.ok) {
            const data = await response.json();
            
            successDiv.textContent = 'Регистрация успешна! Перенаправление...';
            successDiv.style.display = 'block';
            
            // Сохраняем токен и данные пользователя
            localStorage.setItem('token', data.token);
            localStorage.setItem('userEmail', data.email);
            localStorage.setItem('userFullName', data.fullName);
            localStorage.setItem('userRole', data.role);
            if (data.customerId) localStorage.setItem('customerId', data.customerId);
            if (data.technicianId) localStorage.setItem('technicianId', data.technicianId);
            
            // Перенаправляем на главную страницу через 2 секунды
            setTimeout(() => {
                window.location.href = '/';
            }, 2000);
        } else {
            const error = await response.json();
            errorDiv.textContent = error.message || 'Ошибка регистрации';
            errorDiv.style.display = 'block';
        }
    } catch (error) {
        console.error('Register error:', error);
        errorDiv.textContent = 'Ошибка соединения с сервером';
        errorDiv.style.display = 'block';
    }
});

// Проверяем, если пользователь уже авторизован
if (localStorage.getItem('token')) {
    window.location.href = '/';
}
