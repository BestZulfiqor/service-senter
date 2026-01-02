let chatConnection = null;
let currentUserId = null;
let isAdmin = false;
let selectedUserId = null;
let chatUsers = [];

async function initializeChat() {
    const token = localStorage.getItem('token');
    const userRole = localStorage.getItem('userRole');
    
    if (!token) {
        return;
    }

    currentUserId = parseInt(localStorage.getItem('userId'));
    isAdmin = userRole === 'Admin';

    const chatButton = document.getElementById('chatButton');
    if (chatButton) {
        chatButton.classList.remove('hidden');
    }
    
    // Show notification button for admin
    if (isAdmin) {
        const notificationButton = document.getElementById('notificationButton');
        if (notificationButton) {
            notificationButton.classList.remove('hidden');
        }
    }

    try {
        // Get the current host to avoid proxy issues
        const currentHost = window.location.hostname === '127.0.0.1' || window.location.hostname === 'localhost' 
            ? `${window.location.protocol}//${window.location.hostname}:${window.location.port || 5000}`
            : window.location.origin;
            
        chatConnection = new signalR.HubConnectionBuilder()
            .withUrl(`${currentHost}/chatHub`, {
                accessTokenFactory: () => localStorage.getItem('token'),
                skipNegotiation: false,
                transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling
            })
            .withAutomaticReconnect()
            .build();

        chatConnection.on('ReceiveMessage', (message) => {
            if (message.senderId === selectedUserId || message.receiverId === selectedUserId) {
                displayMessage(message);
            }
            updateUnreadCount();
            updateUserList();
            
            // Update notifications for admin
            if (isAdmin) {
                loadNotifications();
                updateNotificationBadge();
            }
            
            const chatModal = document.getElementById('chatModal');
            if (chatModal.classList.contains('hidden')) {
                showNotification('Новое сообщение');
            }
        });

        chatConnection.on('MessageSent', (message) => {
            console.log('Message sent successfully', message);
        });

        chatConnection.on('UpdateUserList', (users) => {
            if (isAdmin) {
                chatUsers = users;
                renderUserList();
            }
        });

        chatConnection.on('UserStatusChanged', (data) => {
            if (isAdmin) {
                const user = chatUsers.find(u => u.id === data.userId);
                if (user) {
                    user.isOnline = data.isOnline;
                    renderUserList();
                }
            }
        });

        chatConnection.on('MessageRead', (data) => {
            // Update message read status in UI
            const messageElement = document.querySelector(`[data-message-id="${data.messageId}"]`);
            if (messageElement) {
                const readIndicator = messageElement.querySelector('.read-indicator');
                if (readIndicator) {
                    readIndicator.style.display = 'block';
                }
            }
        });

        chatConnection.onreconnected(error => {
            console.log('SignalR Reconnected', error);
            updateUnreadCount();
            if (isAdmin) {
                loadChatUsers();
            } else {
                loadAdminUsers();
            }
        });

        chatConnection.onreconnecting(error => {
            console.log('SignalR Reconnecting...', error);
        });

        chatConnection.onclose(error => {
            console.log('SignalR Connection Closed', error);
            // Try to reconnect after 5 seconds
            setTimeout(() => {
                if (localStorage.getItem('token')) {
                    initializeChat();
                }
            }, 5000);
        });

        await chatConnection.start();
        console.log('SignalR Connected');
        
        if (isAdmin) {
            await loadChatUsers();
            await loadNotifications();
            updateNotificationBadge();
        } else {
            await loadAdminUsers();
        }
        await updateUnreadCount();
    } catch (err) {
        console.error('SignalR Connection Error:', err);
        setTimeout(initializeChat, 5000);
    }
}

async function loadChatUsers() {
    const token = localStorage.getItem('token');
    if (!token) return;

    try {
        const currentHost = window.location.hostname === '127.0.0.1' || window.location.hostname === 'localhost' 
            ? `${window.location.protocol}//${window.location.hostname}:${window.location.port || 5000}`
            : window.location.origin;
            
        const response = await fetch(`${currentHost}/api/chat/users`, {
            headers: {
                'Authorization': `Bearer ${token}`
            }
        });

        if (response.ok) {
            const users = await response.json();
            chatUsers = users;
            renderUserList();
        }
    } catch (error) {
        console.error('Error loading chat users:', error);
    }
}

async function loadAdminUsers() {
    const token = localStorage.getItem('token');
    if (!token) return;

    try {
        const currentHost = window.location.hostname === '127.0.0.1' || window.location.hostname === 'localhost' 
            ? `${window.location.protocol}//${window.location.hostname}:${window.location.port || 5000}`
            : window.location.origin;
            
        console.log('Loading admin users from:', `${currentHost}/api/chat/users`);
        const response = await fetch(`${currentHost}/api/chat/users`, {
            headers: {
                'Authorization': `Bearer ${token}`
            }
        });

        if (response.ok) {
            const users = await response.json();
            console.log('Loaded admin users:', users);
            chatUsers = users;
            if (users.length > 0) {
                console.log('Selecting first admin:', users[0]);
                selectUser(users[0].id);
            } else {
                console.log('No admin users found');
                // No admin users available, show message
                const chatMessages = document.getElementById('chatMessages');
                const chatInput = document.getElementById('chatInput');
                const chatSendBtn = document.querySelector('.chat-send-btn');
                
                chatMessages.innerHTML = '<div class="chat-welcome"><p>Нет доступных администраторов для чата</p></div>';
                chatInput.disabled = true;
                chatSendBtn.disabled = true;
            }
        } else {
            console.error('Failed to load admin users:', response.status, response.statusText);
        }
    } catch (error) {
        console.error('Error loading admin users:', error);
    }
}

function renderUserList() {
    const userListContainer = document.getElementById('userListContainer');
    if (!userListContainer) return;

    userListContainer.innerHTML = '';

    chatUsers.forEach(user => {
        const userItem = document.createElement('div');
        userItem.className = `user-item ${selectedUserId === user.id ? 'selected' : ''}`;
        userItem.onclick = () => selectUser(user.id);

        const lastMessageText = user.lastMessage ? 
            (user.lastMessage.senderId === currentUserId ? `Вы: ${user.lastMessage.message}` : user.lastMessage.message) : 
            'Нет сообщений';

        userItem.innerHTML = `
            <div class="user-info">
                <div class="user-name">${escapeHtml(user.name)}</div>
                <div class="user-email">${escapeHtml(user.email)}</div>
                <div class="user-role">${user.role}</div>
            </div>
            <div class="user-status">
                ${user.unreadCount > 0 ? `<div class="unread-badge">${user.unreadCount}</div>` : ''}
                <div class="online-indicator ${user.isOnline ? 'online' : ''}"></div>
                <div class="last-message">${escapeHtml(lastMessageText)}</div>
            </div>
        `;

        userListContainer.appendChild(userItem);
    });
}

function searchUsers() {
    const searchInput = document.getElementById('userSearchInput');
    const searchTerm = searchInput.value.toLowerCase();
    
    const filteredUsers = chatUsers.filter(user => 
        user.name.toLowerCase().includes(searchTerm) ||
        user.email.toLowerCase().includes(searchTerm) ||
        user.role.toLowerCase().includes(searchTerm)
    );

    const userListContainer = document.getElementById('userListContainer');
    userListContainer.innerHTML = '';

    filteredUsers.forEach(user => {
        const userItem = document.createElement('div');
        userItem.className = `user-item ${selectedUserId === user.id ? 'selected' : ''}`;
        userItem.onclick = () => selectUser(user.id);

        const lastMessageText = user.lastMessage ? 
            (user.lastMessage.senderId === currentUserId ? `Вы: ${user.lastMessage.message}` : user.lastMessage.message) : 
            'Нет сообщений';

        userItem.innerHTML = `
            <div class="user-info">
                <div class="user-name">${escapeHtml(user.name)}</div>
                <div class="user-email">${escapeHtml(user.email)}</div>
                <div class="user-role">${user.role}</div>>
            </div>
            <div class="user-status">
                ${user.unreadCount > 0 ? `<div class="unread-badge">${user.unreadCount}</div>` : ''}
                <div class="online-indicator ${user.isOnline ? 'online' : ''}"></div>
                <div class="last-message">${escapeHtml(lastMessageText)}</div>
            </div>
        `;

        userListContainer.appendChild(userItem);
    });
}

async function selectUser(userId) {
    console.log('selectUser called with userId:', userId);
    selectedUserId = userId;
    
    const chatContainer = document.getElementById('chatContainer');
    const chatHeaderTitle = document.getElementById('chatHeaderTitle');
    const chatUserList = document.getElementById('chatUserList');
    const chatMessages = document.getElementById('chatMessages');
    const chatInput = document.getElementById('chatInput');
    const chatSendBtn = document.querySelector('.chat-send-btn');

    console.log('Found elements:', { chatContainer, chatHeaderTitle, chatUserList, chatMessages, chatInput, chatSendBtn });

    if (isAdmin) {
        chatContainer.classList.add('admin-mode');
        chatUserList.classList.remove('hidden');
        
        const selectedUser = chatUsers.find(u => u.id === userId);
        if (selectedUser) {
            chatHeaderTitle.textContent = `💬 Чат с ${selectedUser.name}`;
        }
    } else {
        chatContainer.classList.remove('admin-mode');
        chatUserList.classList.add('hidden');
        
        const selectedUser = chatUsers.find(u => u.id === userId);
        if (selectedUser) {
            chatHeaderTitle.textContent = `💬 Чат с поддержкой`;
        }
    }

    // Enable input
    chatInput.disabled = false;
    chatSendBtn.disabled = false;
    chatInput.focus();

    // Update selected state in UI
    document.querySelectorAll('.user-item').forEach(item => {
        item.classList.remove('selected');
    });
    const selectedItem = document.querySelector(`.user-item[onclick="selectUser(${userId})"]`);
    if (selectedItem) {
        selectedItem.classList.add('selected');
    }

    await loadMessages();
}

async function loadMessages() {
    if (!selectedUserId) return;

    const token = localStorage.getItem('token');
    if (!token) return;

    try {
        const currentHost = window.location.hostname === '127.0.0.1' || window.location.hostname === 'localhost' 
            ? `${window.location.protocol}//${window.location.hostname}:${window.location.port || 5000}`
            : window.location.origin;
            
        const response = await fetch(`${currentHost}/api/chat/messages/${selectedUserId}`, {
            headers: {
                'Authorization': `Bearer ${token}`
            }
        });

        if (response.ok) {
            const messages = await response.json();
            const chatMessages = document.getElementById('chatMessages');
            chatMessages.innerHTML = '';
            
            messages.forEach(message => {
                displayMessage(message, false);
            });
            
            scrollToBottom();
        }
    } catch (error) {
        console.error('Error loading messages:', error);
    }
}

function displayMessage(message, animate = true) {
    const chatMessages = document.getElementById('chatMessages');
    const messageDiv = document.createElement('div');
    
    const isSent = message.senderId === currentUserId;
    messageDiv.className = `chat-message ${isSent ? 'sent' : 'received'}`;
    messageDiv.setAttribute('data-message-id', message.id);
    
    if (!animate) {
        messageDiv.style.animation = 'none';
    }

    let messageContent = '';
    
    if (!isSent && isAdmin) {
        messageContent += `<div class="chat-message-sender">${escapeHtml(message.senderName)}</div>`;
    }
    
    messageContent += `<div>${escapeHtml(message.message)}</div>`;
    messageContent += `<span class="chat-message-time">${formatMessageTime(message.sentAt)}</span>`;
    messageContent += message.isRead ? '<span class="read-indicator" style="display: block; font-size: 0.6rem; opacity: 0.7;">✓✓</span>' : '';
    
    messageDiv.innerHTML = messageContent;
    chatMessages.appendChild(messageDiv);
    
    if (animate) {
        scrollToBottom();
    }
}

async function sendChatMessage() {
    const input = document.getElementById('chatInput');
    const message = input.value.trim();
    
    if (!message || !chatConnection || !selectedUserId) return;

    try {
        // Check if token is still valid
        const token = localStorage.getItem('token');
        if (!token) {
            alert('Токен авторизации истек. Пожалуйста, войдите снова.');
            return;
        }
        
        console.log('Sending message to user:', selectedUserId, 'message:', message);
        await chatConnection.invoke('SendMessage', selectedUserId, message);
        input.value = '';
        console.log('Message sent successfully');
    } catch (error) {
        console.error('Error sending message:', error);
        if (error.message.includes('401') || error.message.includes('Unauthorized')) {
            alert('Ошибка авторизации. Попробуйте обновить страницу и войти снова.');
        } else {
            alert('Ошибка отправки сообщения: ' + error.message);
        }
    }
}

function handleChatKeyPress(event) {
    if (event.key === 'Enter') {
        sendChatMessage();
    }
}

function openChatModal() {
    console.log('openChatModal called, isAdmin:', isAdmin, 'chatUsers.length:', chatUsers.length);
    const chatModal = document.getElementById('chatModal');
    chatModal.classList.remove('hidden');
    
    if (isAdmin) {
        console.log('Admin mode - loading users if needed');
        if (chatUsers.length === 0) {
            loadChatUsers();
        }
        // For admin, don't auto-select user, keep input disabled until user selects someone
        const chatInput = document.getElementById('chatInput');
        const chatSendBtn = document.querySelector('.chat-send-btn');
        const chatMessages = document.getElementById('chatMessages');
        
        chatInput.disabled = true;
        chatSendBtn.disabled = true;
        chatMessages.innerHTML = '<div class="chat-welcome"><p>Выберите пользователя для начала чата</p></div>';
    } else if (chatUsers.length > 0) {
        console.log('Non-admin mode - selecting first user:', chatUsers[0]);
        selectUser(chatUsers[0].id);
    } else {
        console.log('Non-admin mode - loading admin users');
        // For non-admin, load admin users first
        loadAdminUsers();
    }
}

function closeChatModal() {
    const chatModal = document.getElementById('chatModal');
    chatModal.classList.add('minimized');
    chatModal.classList.remove('hidden');
    
    // Hide chat button when chat is minimized
    const chatButton = document.getElementById('chatButton');
    if (chatButton) {
        chatButton.classList.add('hidden');
    }
}

function maximizeChatModal() {
    const chatModal = document.getElementById('chatModal');
    chatModal.classList.remove('minimized');
    
    // Show chat button when chat is maximized
    const chatButton = document.getElementById('chatButton');
    if (chatButton) {
        chatButton.classList.remove('hidden');
    }
}

function toggleChatModal() {
    const chatModal = document.getElementById('chatModal');
    if (chatModal.classList.contains('hidden')) {
        openChatModal();
    } else if (chatModal.classList.contains('minimized')) {
        maximizeChatModal();
    } else {
        closeChatModal();
    }
}

async function updateUnreadCount() {
    const token = localStorage.getItem('token');
    if (!token) return;

    try {
        const currentHost = window.location.hostname === '127.0.0.1' || window.location.hostname === 'localhost' 
            ? `${window.location.protocol}//${window.location.hostname}:${window.location.port || 5000}`
            : window.location.origin;
            
        const response = await fetch(`${currentHost}/api/chat/unread-count`, {
            headers: {
                'Authorization': `Bearer ${token}`
            }
        });

        if (response.ok) {
            const count = await response.json();
            const badge = document.getElementById('chatBadge');
            
            if (count > 0) {
                badge.textContent = count;
                badge.classList.remove('hidden');
            } else {
                badge.classList.add('hidden');
            }
        }
    } catch (error) {
        console.error('Error updating unread count:', error);
    }
}

async function updateUserList() {
    if (isAdmin) {
        await loadChatUsers();
    }
}

function scrollToBottom() {
    const chatMessages = document.getElementById('chatMessages');
    if (chatMessages) {
        chatMessages.scrollTop = chatMessages.scrollHeight;
    }
}

function formatMessageTime(dateString) {
    const date = new Date(dateString);
    const now = new Date();
    const diff = now - date;
    
    if (diff < 60000) {
        return 'только что';
    } else if (diff < 3600000) {
        const minutes = Math.floor(diff / 60000);
        return `${minutes} мин назад`;
    } else if (diff < 86400000) {
        return date.toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' });
    } else {
        return date.toLocaleDateString('ru-RU', { day: '2-digit', month: '2-digit' }) + ' ' +
               date.toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' });
    }
}

function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

function showNotification(message) {
    if ('Notification' in window && Notification.permission === 'granted') {
        new Notification('Сервисный Центр', {
            body: message,
            icon: '/favicon.ico'
        });
    }
}

let touchStartY = 0;
let touchEndY = 0;

function handleTouchStart(e) {
    touchStartY = e.changedTouches[0].screenY;
}

function handleTouchEnd(e) {
    touchEndY = e.changedTouches[0].screenY;
    handleSwipeGesture();
}

function handleSwipeGesture() {
    const swipeDistance = touchStartY - touchEndY;
    const minSwipeDistance = 50;
    
    // Swipe down to close chat
    if (swipeDistance < -minSwipeDistance) {
        const chatModal = document.getElementById('chatModal');
        if (!chatModal.classList.contains('hidden')) {
            closeChatModal();
        }
    }
}

document.addEventListener('DOMContentLoaded', () => {
    const token = localStorage.getItem('token');
    if (token) {
        initializeChat();
    }
    
    // Close notification dropdown when clicking outside
    document.addEventListener('click', (e) => {
        const notificationButton = document.getElementById('notificationButton');
        const notificationDropdown = document.getElementById('notificationDropdown');
        
        if (notificationButton && notificationDropdown && 
            !notificationButton.contains(e.target) && 
            !notificationDropdown.contains(e.target)) {
            notificationDropdown.classList.add('hidden');
        }
    });
    
    // Add swipe gesture listeners to chat modal
    const chatModal = document.getElementById('chatModal');
    if (chatModal) {
        chatModal.addEventListener('touchstart', handleTouchStart, { passive: true });
        chatModal.addEventListener('touchend', handleTouchEnd, { passive: true });
        
        // Add click handler for minimized chat
        chatModal.addEventListener('click', (e) => {
            if (chatModal.classList.contains('minimized')) {
                // Only maximize if clicking on the header (not the close button)
                if (!e.target.classList.contains('chat-close')) {
                    maximizeChatModal();
                }
            }
        });
    }
    
    // Handle ESC key to minimize chat instead of closing
    document.addEventListener('keydown', (e) => {
        if (e.key === 'Escape') {
            const chatModal = document.getElementById('chatModal');
            if (!chatModal.classList.contains('hidden')) {
                if (chatModal.classList.contains('minimized')) {
                    // If already minimized, hide completely
                    chatModal.classList.add('hidden');
                    chatModal.classList.remove('minimized');
                    const chatButton = document.getElementById('chatButton');
                    if (chatButton) {
                        chatButton.classList.remove('hidden');
                    }
                } else {
                    // Minimize instead of close
                    closeChatModal();
                    chatModal.classList.add('minimized');
                    const chatButton = document.getElementById('chatButton');
                    if (chatButton) {
                        chatButton.classList.add('hidden');
                    }
                }
            }
        }
    });
});

// Notification System
let notifications = [];

function toggleNotificationDropdown() {
    const dropdown = document.getElementById('notificationDropdown');
    if (!dropdown) return;
    
    dropdown.classList.toggle('hidden');
    
    if (!dropdown.classList.contains('hidden')) {
        loadNotifications();
    }
}

function closeNotificationDropdown() {
    const dropdown = document.getElementById('notificationDropdown');
    if (dropdown) {
        dropdown.classList.add('hidden');
    }
}

async function loadNotifications() {
    if (!isAdmin) return;
    
    const token = localStorage.getItem('token');
    if (!token) return;
    
    try {
        const currentHost = window.location.hostname === '127.0.0.1' || window.location.hostname === 'localhost' 
            ? `${window.location.protocol}//${window.location.hostname}:${window.location.port || 5000}`
            : window.location.origin;
            
        const response = await fetch(`${currentHost}/api/chat/conversations`, {
            headers: {
                'Authorization': `Bearer ${token}`
            }
        });
        
        if (response.ok) {
            const conversations = await response.json();
            notifications = conversations;
            renderNotifications();
        }
    } catch (error) {
        console.error('Error loading notifications:', error);
    }
}

function renderNotifications() {
    const notificationList = document.getElementById('notificationList');
    if (!notificationList) return;
    
    notificationList.innerHTML = '';
    
    if (notifications.length === 0) {
        notificationList.innerHTML = '<div class="notification-empty"><p>Нет новых сообщений</p></div>';
        return;
    }
    
    notifications.forEach(conv => {
        const notificationItem = document.createElement('div');
        notificationItem.className = `notification-item ${conv.unreadCount > 0 ? 'unread' : ''}`;
        notificationItem.onclick = () => selectUserFromNotification(conv.userId);
        
        const initials = conv.userName ? conv.userName.split(' ').map(n => n[0]).join('').toUpperCase().substring(0, 2) : '??';
        
        notificationItem.innerHTML = `
            <div class="notification-avatar">${initials}</div>
            <div class="notification-content">
                <div class="notification-name">${escapeHtml(conv.userName)}</div>
                <div class="notification-message">${conv.lastMessage ? escapeHtml(conv.lastMessage.message) : 'Нет сообщений'}</div>
                <div class="notification-time">${conv.lastMessage ? formatMessageTime(conv.lastMessage.sentAt) : ''}</div>
            </div>
            ${conv.unreadCount > 0 ? `<div class="notification-badge-item">${conv.unreadCount}</div>` : ''}
        `;
        
        notificationList.appendChild(notificationItem);
    });
}

function selectUserFromNotification(userId) {
    closeNotificationDropdown();
    
    // Open chat and select user
    const chatModal = document.getElementById('chatModal');
    if (chatModal) {
        chatModal.classList.remove('hidden');
        chatModal.classList.remove('minimized');
    }
    
    selectUser(userId);
}

function updateNotificationBadge() {
    if (!isAdmin) return;
    
    const notificationButton = document.getElementById('notificationButton');
    const notificationBadge = document.getElementById('notificationBadge');
    
    if (notificationButton && notificationBadge) {
        notificationButton.classList.remove('hidden');
        
        const unreadCount = notifications.reduce((sum, conv) => sum + conv.unreadCount, 0);
        
        if (unreadCount > 0) {
            notificationBadge.textContent = unreadCount > 99 ? '99+' : unreadCount;
            notificationBadge.classList.remove('hidden');
        } else {
            notificationBadge.classList.add('hidden');
        }
    }
}