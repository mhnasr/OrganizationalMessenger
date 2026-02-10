const API_BASE = '/Chat';
let connection = null;
let currentUser = null;
let currentChatId = null; // receiverId
let allUsers = [];
let typingTimeout = null;

$(document).ready(async function () {
    console.log('🚀 Chat loading...');

    await loadCurrentUser();
    await loadUsers();
    await connectSignalR();
    setupEventListeners();
});

async function loadCurrentUser() {
    const res = await fetch(`${API_BASE}/GetCurrentUser`);
    currentUser = await res.json();
    $('#currentUserName').text(currentUser.fullName);
    $('#currentUserAvatar').text(currentUser.fullName[0]);
}

async function loadUsers() {
    console.log('🔄 Loading users...');
    try {
        $('#usersList').html('<div class="text-center p-4"><div class="spinner-border text-primary" role="status"></div><div class="mt-2">در حال بارگذاری...</div></div>');

        const res = await fetch('/Chat/GetUsers', {
            method: 'GET',
            headers: {
                'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val(),
                'Content-Type': 'application/json'
            }
        });

        if (!res.ok) {
            throw new Error(`HTTP ${res.status}: ${res.statusText}`);
        }

        const users = await res.json();
        console.log('✅ Users loaded:', users.length);

        allUsers = users;
        displayUsers(users);

    } catch (error) {
        console.error('❌ Load users failed:', error);
        $('#usersList').html(`
            <div class="text-center text-danger p-5">
                <i class="bi bi-exclamation-triangle fs-1 mb-3"></i>
                <div>خطا در بارگذاری مخاطبین</div>
                <small class="opacity-75">F12 Console را بررسی کنید</small>
                <button class="btn btn-outline-danger btn-sm mt-3" onclick="loadUsers()">
                    تلاش مجدد
                </button>
            </div>
        `);
    }
}


function getToken() {
    return window.__antiforgery || $('input[name="__RequestVerificationToken"]').val();
}

function displayUsers(users) {
    $('#usersList').html(users.map(u => `
        <div class="user-item p-3 border-bottom cursor-pointer hover-bg-light" data-user-id="${u.userId}" onclick="selectUser(${u.userId})">
            <div class="d-flex align-items-center">
                <div class="position-relative me-3">
                    <div class="avatar rounded-circle bg-primary text-white d-flex align-items-center justify-content-center" 
                         style="width:45px;height:45px;font-size:16px;">${u.fullName[0]}</div>
                    <div class="online-indicator ${u.isOnline ? 'online' : 'offline'}"></div>
                </div>
                <div class="flex-grow-1">
                    <div class="fw-bold">${u.fullName}</div>
                    <small class="text-${u.isOnline ? 'success' : 'muted'}">${u.isOnline ? 'آنلاین' : 'آفلاین'}</small>
                </div>
            </div>
        </div>
    `).join(''));
}

async function selectUser(userId) {
    currentChatId = userId;

    const user = allUsers.find(u => u.userId == userId);
    $('#chatContent').removeClass('d-none');
    $('#emptyChat').addClass('d-none');
    $('#chatUserName').text(user.fullName);
    $('#chatUserStatus').text(user.isOnline ? 'آنلاین' : 'آفلاین')
        .removeClass('bg-success bg-secondary')
        .addClass(user.isOnline ? 'bg-success' : 'bg-secondary');

    $('#messageInput, #sendBtn').prop('disabled', false);
    $('#messagesContainer').html('در حال بارگذاری...');

    // Load messages + Join
    await loadMessages(userId);
    if (connection) connection.invoke('JoinChat', userId);

    $('.user-item').removeClass('active');
    $(`.user-item[data-user-id="${userId}"]`).addClass('active');
}

async function loadMessages(receiverId) {
    const res = await fetch(`${API_BASE}/GetMessages?receiverId=${receiverId}`);
    const messages = await res.json();

    $('#messagesContainer').empty();
    messages.forEach(msg => addMessageToUI(msg));
}

async function connectSignalR() {
    const token = $('input[name="__RequestVerificationToken"]').val();
    connection = new signalR.HubConnectionBuilder()
        .withUrl('/hubs/chat', { accessTokenFactory: () => token })
        .build();

    connection.on('ReceiveMessage', msg => {
        if (currentChatId == msg.chatId) {
            addMessageToUI(msg);
        }
    });

    connection.on('UserOnline', userId => {
        const user = allUsers.find(u => u.userId == userId);
        if (user) user.isOnline = true;
        updateUserStatus(userId, true);
    });

    connection.on('UserOffline', userId => {
        const user = allUsers.find(u => u.userId == userId);
        if (user) user.isOnline = false;
        updateUserStatus(userId, false);
    });

    connection.on('UserTyping', (chatId, senderId, isTyping) => {
        if (chatId == currentChatId) {
            $('#typingIndicator').toggle(isTyping);
        }
    });

    await connection.start();
}

async function sendMessage() {
    const content = $('#messageInput').val().trim();
    if (!content || !currentChatId || !connection) return;

    $('#messageInput').prop('disabled', true);
    try {
        await connection.invoke('SendMessage', currentChatId, content);
        $('#messageInput').val('');
    } catch (e) {
        console.error('Send error:', e);
    } finally {
        $('#messageInput').prop('disabled', false).focus();
    }
}

function addMessageToUI(msg) {
    const isOwn = msg.senderId == currentUser.userId;
    const time = new Date(msg.sentAt).toLocaleTimeString('fa-IR');
    const status = isOwn ? (msg.isDelivered ? '✓✓' : '✓') : '';

    const html = `
        <div class="message mb-3 ${isOwn ? 'sent' : 'received'}">
            <div class="message-bubble p-3 rounded ${isOwn ? 'bg-primary text-white ms-auto' : 'bg-light'}">
                <div>${msg.content}</div>
                <div class="message-meta mt-1 d-flex justify-content-between">
                    <small class="opacity-75">${time}</small>
                    ${isOwn ? `<small class="message-status">${status}</small>` : ''}
                </div>
            </div>
        </div>
    `;

    $('#messagesContainer').append(html);
    $('#messagesContainer')[0].scrollTop = $('#messagesContainer')[0].scrollHeight;
}

function updateUserStatus(userId, isOnline) {
    $(`.user-item[data-user-id="${userId}"] .online-indicator`)
        .removeClass('online offline')
        .addClass(isOnline ? 'online' : 'offline');
}

function setupEventListeners() {
    $('#sendBtn').click(sendMessage);
    $('#messageInput').on('keypress', e => e.which === 13 && sendMessage())
        .on('input', function () {
            if (connection && currentChatId) {
                connection.invoke('Typing', currentChatId, true);
                clearTimeout(typingTimeout);
                typingTimeout = setTimeout(() =>
                    connection.invoke('Typing', currentChatId, false), 1500);
            }
        });

    $('#searchInput').on('input', function () {
        const q = $(this).val().toLowerCase();
        const filtered = allUsers.filter(u =>
            u.fullName.toLowerCase().includes(q) ||
            u.username.toLowerCase().includes(q));
        displayUsers(filtered);
    });

    $('#emojiBtn').click(() => $('#emojiPicker').toggle());
}
