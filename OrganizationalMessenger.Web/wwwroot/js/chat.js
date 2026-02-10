// SignalR Connection
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/chat")
    .withAutomaticReconnect()
    .build();

let currentChat = null;

// UI Elements
const chatListEl = document.getElementById('chatList');
const messagesContainerEl = document.getElementById('messagesContainer');
const messageInputEl = document.getElementById('messageInput');
const sendBtnEl = document.getElementById('sendBtn');
const micBtnEl = document.getElementById('micBtn');
const attachBtnEl = document.getElementById('attachBtn');
const emoteBtnEl = document.getElementById('emojiBtn');

// Load Chats
async function loadChats(tab = 'all') {
    try {
        const response = await fetch(`/Chat/GetChats?tab=${tab}`);
        const chats = await response.json();

        chatListEl.innerHTML = '';

        chats.forEach(chat => {
            const chatItem = document.createElement('div');
            chatItem.className = 'chat-item';
            chatItem.innerHTML = `
                <img src="${chat.avatar || '/images/default-avatar.png'}" 
                     class="chat-avatar ${chat.isOnline ? 'online' : ''}">
                <div class="chat-info">
                    <div class="chat-name">${chat.name}</div>
                    <div class="chat-preview">${chat.lastMessage?.substring(0, 30) || 'بدون پیام'}</div>
                </div>
                <div class="chat-time">${formatTime(chat.lastMessageTime)}</div>
                ${chat.unreadCount > 0 ? `<span class="chat-badge">${chat.unreadCount}</span>` : ''}
            `;

            chatItem.onclick = () => selectChat(chat);
            chatListEl.appendChild(chatItem);
        });
    } catch (error) {
        console.error('خطا در بارگذاری چتها:', error);
    }
}

// Select Chat
function selectChat(chat) {
    currentChat = chat;

    // Update Header
    document.getElementById('chatTitle').textContent = chat.name;

    // Show/Hide Call Buttons
    if (chat.type === 'private') {
        document.getElementById('callVoiceBtn').style.display = 'block';
        document.getElementById('callVideoBtn').style.display = 'block';
    } else {
        document.getElementById('callVoiceBtn').style.display = 'none';
        document.getElementById('callVideoBtn').style.display = 'none';
    }

    // Show Message Input
    document.getElementById('messageInputArea').style.display = 'flex';

    // Load Messages
    loadMessages();

    // Update Active State
    document.querySelectorAll('.chat-item').forEach(item => {
        item.classList.remove('active');
    });
    event.currentTarget.classList.add('active');
}

// Load Messages
async function loadMessages() {
    if (!currentChat) return;

    try {
        const params = new URLSearchParams();
        if (currentChat.type === 'private') params.append('userId', currentChat.id);
        else if (currentChat.type === 'group') params.append('groupId', currentChat.id);
        else if (currentChat.type === 'channel') params.append('channelId', currentChat.id);

        const response = await fetch(`/Chat/GetMessages?${params}`);
        const messages = await response.json();

        messagesContainerEl.innerHTML = '';

        messages.forEach(msg => {
            const messageEl = document.createElement('div');
            messageEl.className = `message ${msg.senderId === currentUserId ? 'sent' : 'received'}`;
            messageEl.innerHTML = `
                <div class="message-bubble">
                    <div>${msg.content || msg.messageText}</div>
                    <div class="message-time">${formatTime(msg.sentAt)}</div>
                    <div class="message-status ${msg.isDelivered ? 'delivered' : 'sent'}"></div>
                </div>
            `;

            messagesContainerEl.appendChild(messageEl);
        });

        // Scroll to bottom
        messagesContainerEl.scrollTop = messagesContainerEl.scrollHeight;
    } catch (error) {
        console.error('خطا در بارگذاری پیامها:', error);
    }
}

// Send Message
sendBtnEl.onclick = async () => {
    const text = messageInputEl.value.trim();
    if (!text || !currentChat) return;

    try {
        const response = await fetch('/Chat/SendMessage', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                receiverId: currentChat.type === 'private' ? currentChat.id : null,
                groupId: currentChat.type === 'group' ? currentChat.id : null,
                channelId: currentChat.type === 'channel' ? currentChat.id : null,
                messageText: text,
                type: 0
            })
        });

        if (response.ok) {
            messageInputEl.value = '';
            loadMessages();
            loadChats(); // Refresh chat list
        }
    } catch (error) {
        console.error('خطا در ارسال پیام:', error);
    }
};

// Helper Functions
function formatTime(dateString) {
    const date = new Date(dateString);
    const now = new Date();

    const diffMinutes = Math.floor((now - date) / 60000);

    if (diffMinutes < 1) return 'الآن';
    if (diffMinutes < 60) return `${diffMinutes}د`;

    const diffHours = Math.floor(diffMinutes / 60);
    if (diffHours < 24) return `${diffHours}س`;

    return date.toLocaleDateString('fa-IR');
}

// Tab Switching
document.querySelectorAll('.tab-btn').forEach(btn => {
    btn.onclick = (e) => {
        document.querySelectorAll('.tab-btn').forEach(b => b.classList.remove('active'));
        e.target.classList.add('active');
        loadChats(e.target.dataset.tab);
    };
});

// SignalR Connection
connection.start().catch(err => {
    console.error('خطا در اتصال SignalR:', err);
    return new Promise(resolve => setTimeout(() => resolve(connection.start()), 5000));
});

// Initial Load
loadChats();