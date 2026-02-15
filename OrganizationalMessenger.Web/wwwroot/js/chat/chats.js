// ============================================
// Chat List Management
// ============================================

import { currentChat, setCurrentChat, setLastSenderId, setMessageGroupCount, setHasMoreMessages, setIsPageFocused } from './variables.js';
import { loadMessages, markMessagesAsRead } from './messages.js';
import { escapeHtml, formatPersianTime, getInitials, scrollToBottom } from './utils.js';
import { toggleMessageInput } from './init.js';

export async function loadChats(tab = 'all') {
    console.log('📋 Loading chats, tab:', tab);

    try {
        const response = await fetch(`/Chat/GetChats?tab=${tab}`);

        if (!response.ok) {
            console.error('❌ GetChats failed:', response.status);
            return;
        }

        const chats = await response.json();
        console.log('✅ Chats received:', chats.length);

        const container = document.getElementById('chatList');
        if (!container) {
            console.error('❌ chatList container not found');
            return;
        }

        container.innerHTML = '';
        chats.forEach(chat => renderChatItem(chat));

        console.log('✅ Chat list rendered');
    } catch (error) {
        console.error('❌ Load chats error:', error);
    }
}

export function renderChatItem(chat) {
    const container = document.getElementById('chatList');
    const chatEl = document.createElement('div');
    chatEl.className = `chat-item ${chat.type} ${currentChat?.id == chat.id ? 'active' : ''}`;
    chatEl.dataset.chatId = chat.id;
    chatEl.dataset.chatType = chat.type;

    const unreadBadge = chat.unreadCount > 0
        ? `<span class="unread-badge">${chat.unreadCount > 99 ? '99+' : chat.unreadCount}</span>`
        : '';

    let avatarHtml = '';
    if (chat.avatar) {
        avatarHtml = `<img src="${chat.avatar}" class="chat-avatar-img" alt="${escapeHtml(chat.name)}" />`;
    } else {
        avatarHtml = `<div class="chat-avatar-initials">${getInitials(chat.name)}</div>`;
    }

    chatEl.innerHTML = `
        <div class="chat-avatar ${chat.isOnline ? 'online' : ''}">
            ${avatarHtml}
        </div>
        <div class="chat-info">
            <div class="chat-name-row">
                <span class="chat-name">${escapeHtml(chat.name)}</span>
                ${unreadBadge}
            </div>
            <div class="chat-preview">
                <span class="message-time">${formatPersianTime(chat.lastMessageTime)}</span>
            </div>
        </div>
    `;
    container.appendChild(chatEl);
}

export async function selectChat(chatEl) {
    console.log('🔄 Selecting chat:', chatEl.dataset.chatId);

    setLastSenderId(null);
    setMessageGroupCount(0);
    setHasMoreMessages(true);
    setIsPageFocused(true);

    document.querySelectorAll('.chat-item').forEach(item => item.classList.remove('active'));
    chatEl.classList.add('active');

    setCurrentChat({
        id: parseInt(chatEl.dataset.chatId),
        type: chatEl.dataset.chatType
    });

    const inputArea = document.getElementById('messageInputArea');
    if (inputArea) {
        inputArea.style.display = 'flex';
        inputArea.classList.add('show');
    }

    document.getElementById('chatTitle').textContent =
        chatEl.querySelector('.chat-name')?.textContent || 'چت';
    document.querySelectorAll('#chatTopHeader button').forEach(btn => {
        btn.style.display = 'flex';
    });

    await loadMessages(false);
    await markMessagesAsRead();

    setTimeout(() => {
        scrollToBottom();
    }, 100);
}

export function handleTabClick(tabBtn) {
    const tab = tabBtn.dataset.tab;

    document.querySelectorAll('.tab-btn').forEach(btn => {
        btn.classList.remove('active');
    });
    tabBtn.classList.add('active');

    loadChats(tab);
}