// ============================================
// SignalR Setup & Event Handlers
// ============================================

import { getCsrfToken } from './utils.js';
import { loadChats } from './chats.js';
import { handleReceiveMessage, handleMessageSent, updateMessageStatus } from './message-handlers.js';
import { replaceWithDeletedNotice } from './messages.js';
import { formatPersianTime, escapeHtml } from './utils.js';
import { currentChat, connection as globalConnection } from './variables.js';

export async function setupSignalR() {
    console.log('🔌 Setting up SignalR...');

    try {
        const connection = new signalR.HubConnectionBuilder()
            .withUrl('/chatHub', {
                accessTokenFactory: () => getCsrfToken()
            })
            .withAutomaticReconnect()
            .configureLogging(signalR.LogLevel.Information)
            .build();

        console.log('✅ SignalR HubConnection created');

        // Event handlers
        connection.on("ReceiveMessage", (data) => handleReceiveMessage(data));
        connection.on("MessageSent", (data) => handleMessageSent(data));
        connection.on("MessageDelivered", (data) => updateMessageStatus(data.messageId, 'delivered'));

        connection.on("MessageRead", (data) => {
            const msgEl = document.querySelector(`[data-message-id="${data.messageId}"]`);
            if (!msgEl) return;

            updateMessageStatus(data.messageId, 'read', data.readAt);

            const sendInfoEl = msgEl.querySelector('.sent-info');
            if (sendInfoEl) {
                const readTime = formatPersianTime(data.readAt);
                if (!sendInfoEl.textContent.includes('مشاهده')) {
                    sendInfoEl.innerHTML += `&nbsp;&nbsp; مشاهده: ${readTime}`;
                }
            }
        });

        connection.on("UserOnline", (userId) => {
            console.log('🟢 User online:', userId);

            if (currentChat?.id == userId && currentChat.type === 'private') {
                document.querySelectorAll('#messagesContainer .message.sent').forEach(msgEl => {
                    const msgId = parseInt(msgEl.dataset.messageId);
                    if (msgId && !msgEl.querySelector('.double-blue')) {
                        updateMessageStatus(msgId, 'delivered');
                    }
                });
            }

            markUserOnline(userId);
        });

        connection.on("UserOffline", (userId, lastSeen) => markUserOffline(userId, lastSeen));
        connection.on("UserTyping", (message) => showTypingIndicator(message));
        connection.on("UserStoppedTyping", () => hideTypingIndicator());
        connection.on("Error", (error) => console.error('❌', error));

        connection.on("MessageDeleted", (data) => {
            console.log('🗑️ MessageDeleted received:', data);

            const messageEl = document.querySelector(`[data-message-id="${data.messageId}"]`);
            if (!messageEl) {
                console.warn('⚠️ Message element not found in DOM');
                return;
            }

            if (data.showNotice) {
                console.log('📝 Mode: WhatsApp (show notice)');
                replaceWithDeletedNotice(messageEl);
            } else {
                console.log('🗑️ Mode: Telegram (remove completely)');
                messageEl.style.animation = 'fadeOut 0.3s ease';
                setTimeout(() => {
                    messageEl.remove();
                    console.log('✅ Message removed from DOM');
                }, 300);
            }
        });

        connection.on("MessageEdited", (data) => {
            console.log('✏️ MessageEdited:', data);

            const messageEl = document.querySelector(`[data-message-id="${data.messageId}"]`);
            if (!messageEl) return;

            const textEl = messageEl.querySelector('[data-editable="true"]');
            if (textEl) {
                textEl.textContent = data.newContent;
            }

            const sentInfo = messageEl.querySelector('.sent-info');
            const messageTime = messageEl.querySelector('.message-time');

            messageEl.querySelectorAll('.edited-badge').forEach(badge => badge.remove());

            const editedBadge = '<span class="edited-badge">ویرایش شده</span>';

            if (sentInfo) {
                sentInfo.insertAdjacentHTML('beforeend', editedBadge);
            } else if (messageTime) {
                messageTime.insertAdjacentHTML('beforeend', ' ' + editedBadge);
            }
        });

        console.log('✅ SignalR event handlers registered');

        // شروع اتصال
        await connection.start();
        console.log('✅ SignalR Connected');
        await loadChats('all');

        return connection;

    } catch (error) {
        console.error('❌ SignalR Setup Error:', error);
        await loadChats('all');
        return null;
    }
}

function markUserOnline(userId) {
    document.querySelectorAll('.chat-item').forEach(item => {
        if (parseInt(item.dataset.chatId) === userId) {
            item.querySelector('.chat-avatar')?.classList.add('online');
        }
    });
}

function markUserOffline(userId, lastSeen) {
    document.querySelectorAll('.chat-item').forEach(item => {
        if (parseInt(item.dataset.chatId) === userId) {
            item.querySelector('.chat-avatar')?.classList.remove('online');
        }
    });
}

function showTypingIndicator(message) {
    const typingEl = document.getElementById('typingIndicator');
    if (typingEl && currentChat?.type === 'private') {
        typingEl.textContent = message;
        typingEl.style.display = 'block';
    }
}

function hideTypingIndicator() {
    const typingEl = document.getElementById('typingIndicator');
    if (typingEl) typingEl.style.display = 'none';
}