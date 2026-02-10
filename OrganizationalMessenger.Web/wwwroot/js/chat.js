// Global Variables
let currentChat = null;
let currentUserId = 0;
let connection = null;
let unreadChats = new Map();

// UI Elements
const chatListEl = document.getElementById('chatList');
const messagesContainerEl = document.getElementById('messagesContainer');
const messageInputEl = document.getElementById('messageInput');
const sendBtnEl = document.getElementById('sendBtn');
const messageInputAreaEl = document.getElementById('messageInputArea');
const chatTitleEl = document.getElementById('chatTitle');

document.addEventListener('DOMContentLoaded', function () {
    const userIdInput = document.querySelector('input[name="currentUserId"]');
    currentUserId = parseInt(userIdInput?.value) || 0;
    console.log('Current User ID:', currentUserId);

    initSignalR();
    setupEventListeners();
    loadChats('all');
});

function initSignalR() {
    if (typeof signalR === 'undefined') {
        console.error('❌ SignalR library not loaded!');
        return;
    }

    connection = new signalR.HubConnectionBuilder()
        .withUrl("/chatHub")
        .withAutomaticReconnect()
        .configureLogging(signalR.LogLevel.Information)
        .build();

    connection.start().then(() => {
        console.log("✅ SignalR Connected");
        setupSignalREvents(); // ✅ بعد از اتصال
    }).catch(err => {
        console.error("❌ SignalR Error:", err);
    });
}

function setupSignalREvents() {
    if (!connection) return;
    
    connection.on("NewMessageReceived", handleNewMessage);
    connection.on("MessageSent", (data) => {
        console.log("✅ Message delivered:", data);
        loadChats();
    });
    connection.on("UserOnline", () => loadChats());
    connection.on("UserOffline", () => loadChats());
}

function handleNewMessage(data) {
    console.log("🔔 پیام جدید:", data);

    if (currentChat?.id == data.chatId && currentChat.type === 'private') {
        displayMessage({
            senderId: data.senderId,
            content: data.content,
            sentAt: data.sentAt
        });
        messagesContainerEl.scrollTop = messagesContainerEl.scrollHeight;
        return;
    }

    loadChatsWithPriority(data.chatId, data.senderName, data.content, 1);
    showNotification(data.senderName, data.content);
}

// ✅ HTTP API - تضمینی ذخیره میشه
async function sendMessageViaAPI(text) {
    const request = {
        receiverId: currentChat.type === 'private' ? currentChat.id : null,
        groupId: currentChat.type === 'group' ? currentChat.id : null,
        channelId: currentChat.type === 'channel' ? currentChat.id : null,
        messageText: text,
        type: 0
    };

    const response = await fetch('/Chat/SendMessage', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': getAntiForgeryToken()
        },
        body: JSON.stringify(request)
    });

    if (!response.ok) {
        const errorText = await response.text();
        throw new Error(`HTTP ${response.status}: ${errorText}`);
    }

    return await response.json();
}

async function sendMessage() {
    const text = messageInputEl.value.trim();
    if (!text || !currentChat) {
        console.log('❌ No text or chat');
        return;
    }

    console.log('📤 Sending:', text);
    
    // نمایش فوری
    displayMessage({ 
        content: text, 
        senderId: currentUserId, 
        sentAt: new Date().toISOString()
    });
    messageInputEl.value = '';

    try {
        // ✅ 1. HTTP API (ذخیره تضمینی)
       // await sendMessageViaAPI(text);
        console.log('✅ Saved to database');

        // 2. SignalR (Real-time)
        if (connection?.state === signalR.HubConnectionState.Connected) {
            if (currentChat.type === 'private') {
                await connection.invoke("SendPrivateMessage", currentChat.id, text);
            }
        }
        
        loadChats();
    } catch (err) {
        console.error("❌ Error:", err);
        // حذف پیام موقت
        if (messagesContainerEl.lastElementChild) {
            messagesContainerEl.lastElementChild.remove();
        }
        messageInputEl.value = text;
    }
}

function getAntiForgeryToken() {
    return document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
}

// باقی توابع بدون تغییر...
async function loadChats(tab = 'all') {
    console.log('🔄 Loading chats:', tab);
    try {
        const response = await fetch(`/Chat/GetChats?tab=${tab}`);
        if (!response.ok) throw new Error(`HTTP ${response.status}`);
        
        const chats = await response.json();
        console.log('✅ Chats loaded:', chats?.length || 0);
        renderChats(chats || []);
    } catch (error) {
        console.error('❌ Load chats error:', error);
        chatListEl.innerHTML = '<div style="padding: 20px; text-align: center; color: #f56565;">خطا در بارگذاری چت‌ها</div>';
    }
}

function renderChats(chats) {
    chatListEl.innerHTML = '';
    
    if (!chats || chats.length === 0) {
        chatListEl.innerHTML = '<div style="padding: 20px; text-align: center; color: #999;">هیچ چتی یافت نشد</div>';
        return;
    }

    chats.forEach(chat => {
        const chatItem = createChatItem(chat);
        chatListEl.appendChild(chatItem);
    });
}

function createChatItem(chat) {
    const chatItem = document.createElement('div');
    chatItem.className = 'chat-item';
    chatItem.dataset.chatId = chat.id;
    chatItem.dataset.chatType = chat.type;

    const avatarClass = chat.isOnline ? 'online' : '';
    const unreadCount = chat.unreadCount > 0 ? chat.unreadCount : 0;
    const badgeHtml = unreadCount > 0 
        ? `<span class="chat-badge">${unreadCount > 99 ? '99+' : unreadCount}</span>`
        : '';

    chatItem.innerHTML = `
        <img src="${chat.avatar || '/images/default-avatar.png'}" 
             class="chat-avatar ${avatarClass}" alt="${escapeHtml(chat.name)}">
        <div class="chat-info">
            <div class="chat-name">${escapeHtml(chat.name)}</div>
            <div class="chat-preview">${escapeHtml(chat.lastMessage?.substring(0, 30) || 'بدون پیام')}...</div>
        </div>
        <div class="chat-time">${formatTime(chat.lastMessageTime)}</div>
        ${badgeHtml}
    `;

    chatItem.addEventListener('click', function() {
        const chatData = {
            id: parseInt(chatItem.dataset.chatId),
            type: chatItem.dataset.chatType,
            name: chatItem.querySelector('.chat-name').textContent,
            avatar: chatItem.querySelector('.chat-avatar').src
        };
        selectChat(chatData, this);
        this.querySelector('.chat-badge')?.remove();
    });

    return chatItem;
}

function setupEventListeners() {
    if (sendBtnEl) sendBtnEl.addEventListener('click', sendMessage);
    
    if (messageInputEl) {
        messageInputEl.addEventListener('keypress', function(e) {
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                sendMessage();
            }
        });
    }

    document.querySelectorAll('.tab-btn').forEach(btn => {
        btn.addEventListener('click', function() {
            document.querySelectorAll('.tab-btn').forEach(b => b.classList.remove('active'));
            this.classList.add('active');
            loadChats(this.dataset.tab);
        });
    });

    const searchInput = document.getElementById('searchInput');
    if (searchInput) {
        searchInput.addEventListener('input', function(e) {
            filterChats(e.target.value);
        });
    }
}

// باقی توابع...
function selectChat(chat, chatItemElement) {
    currentChat = chat;
    if (chatTitleEl) chatTitleEl.textContent = chat.name;
    if (messageInputAreaEl) messageInputAreaEl.style.display = 'flex';
    loadMessages();

    document.querySelectorAll('.chat-item').forEach(item => item.classList.remove('active'));
    if (chatItemElement) chatItemElement.classList.add('active');
}

async function loadMessages() {
    if (!currentChat) return;
    try {
        const params = new URLSearchParams();
        if (currentChat.type === 'private') params.append('userId', currentChat.id);
        else if (currentChat.type === 'group') params.append('groupId', currentChat.id);

        const response = await fetch(`/Chat/GetMessages?${params}`);
        if (!response.ok) throw new Error(`HTTP ${response.status}`);

        const messages = await response.json();
        messagesContainerEl.innerHTML = !messages?.length 
            ? '<p class="empty-state">پیامی وجود ندارد</p>'
            : '';
        
        messages?.forEach(displayMessage);
        messagesContainerEl.scrollTop = messagesContainerEl.scrollHeight;
    } catch (error) {
        console.error('خطا:', error);
    }
}

function displayMessage(msg) {
    const isSent = msg.senderId === currentUserId;
    const messageEl = document.createElement('div');
    messageEl.className = `message ${isSent ? 'sent' : 'received'}`;
    messageEl.innerHTML = `
        <div class="message-bubble">
            <div>${escapeHtml(msg.content || msg.messageText)}</div>
            <div class="message-time">${formatTime(msg.sentAt || msg.createdAt)}</div>
            ${isSent ? '<div class="message-status sent"></div>' : ''}
        </div>
    `;
    messagesContainerEl.appendChild(messageEl);
}

async function loadChatsWithPriority(chatId, senderName, lastMessage, unreadCount) {
    try {
        const chats = await (await fetch('/Chat/GetChats?tab=all')).json();
        const index = chats.findIndex(c => c.id == chatId && c.type === 'private');
        if (index > -1) {
            const chat = chats.splice(index, 1)[0];
            chat.unreadCount = unreadCount;
            chat.lastMessage = lastMessage;
            chat.lastMessageTime = new Date();
            chats.unshift(chat);
        }
        renderChats(chats);
        chatListEl.scrollTop = 0;
    } catch (error) {
        console.error('خطا:', error);
    }
}

function showNotification(senderName, content) {
    const audio = new Audio('/sounds/notification.mp3');
    audio.volume = 0.3;
    audio.play().catch(() => {});

    const toast = document.createElement('div');
    toast.style.cssText = `
        position: fixed; top: 20px; right: 20px; 
        background: #667eea; color: white; 
        padding: 16px 20px; border-radius: 12px; 
        z-index: 10000; max-width: 300px;
    `;
    toast.innerHTML = `<strong>${senderName}</strong>: ${content.substring(0, 50)}...`;
    document.body.appendChild(toast);
    setTimeout(() => toast.remove(), 4000);
}

function formatTime(dateString) {
    const date = new Date(dateString);
    const now = new Date();
    const diffMinutes = Math.floor((now - date) / 60000);
    if (diffMinutes < 1) return 'الان';
    if (diffMinutes < 60) return `${diffMinutes}د`;
    const diffHours = Math.floor(diffMinutes / 60);
    if (diffHours < 24) return `${diffHours}س`;
    return date.toLocaleDateString('fa-IR');
}

function escapeHtml(text) {
    const map = { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#039;' };
    return text ? text.replace(/[&<>"']/g, m => map[m]) : '';
}

function filterChats(searchTerm) {
    document.querySelectorAll('.chat-item').forEach(item => {
        const name = item.querySelector('.chat-name')?.textContent.toLowerCase() || '';
        const preview = item.querySelector('.chat-preview')?.textContent.toLowerCase() || '';
        item.style.display = (name.includes(searchTerm.toLowerCase()) || preview.includes(searchTerm.toLowerCase())) ? 'flex' : 'none';
    });
}
