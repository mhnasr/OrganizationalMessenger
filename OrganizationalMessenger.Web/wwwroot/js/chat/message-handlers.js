// ============================================
// Message Handlers - Receive, Send, Status
// ============================================

import { currentChat, connection, isPageFocused } from './variables.js';
import { displayMessage, addUnreadSeparator, markMessagesAsRead, removeUnreadSeparator } from './messages.js';
import { loadChats } from './chats.js';
import { formatPersianTime, scrollToBottom } from './utils.js';
import { hasMoreMessages, isLoadingMessages } from './variables.js';
import { loadMessages } from './messages.js';

import { currentChat, isPageFocused } from './variables.js';

export function handleReceiveMessage(data) {
    console.log('📨 ReceiveMessage:', data);

    const isCurrentChat = currentChat &&
        (currentChat.id == data.chatId || currentChat.id == data.senderId);

    if (isCurrentChat) {
        if (!isPageFocused || document.hidden) {
            const existingSeparator = document.querySelector('.unread-separator');
            if (!existingSeparator) {
                const container = document.getElementById('messagesContainer');
                addUnreadSeparator(container, 1);
            }
        }

        displayMessage(data);
        scrollToBottom();

        if (isPageFocused && !document.hidden) {
            setTimeout(() => {
                markMessagesAsRead();
                removeUnreadSeparator();
            }, 100);
        } else {
            setTimeout(() => {
                // ✅ استفاده از window.connection
                if (window.connection?.state === signalR.HubConnectionState.Connected) {
                    window.connection.invoke("ConfirmDelivery", data.id);
                }
            }, 100);
        }
    } else {
        loadChats();
        showNotification(data.senderName, data.content);
    }
}
export function handleMessageSent(data) {
    console.log('✅ MessageSent received:', data);

    const tempMessages = document.querySelectorAll('.message[data-temp="true"]');
    tempMessages.forEach(msg => msg.remove());

    if (!data.sentAt) {
        console.warn('⚠️ No sentAt, using current time');
        data.sentAt = new Date().toISOString();
    } else {
        try {
            const date = new Date(data.sentAt);

            if (isNaN(date.getTime())) {
                console.error('❌ Invalid sentAt:', data.sentAt);
                data.sentAt = new Date().toISOString();
            } else {
                const now = new Date();
                if (date > now) {
                    console.warn('⚠️ sentAt is in future, using current time');
                    data.sentAt = now.toISOString();
                } else {
                    data.sentAt = date.toISOString();
                }
            }
        } catch (e) {
            console.error('❌ sentAt parse error:', e);
            data.sentAt = new Date().toISOString();
        }
    }

    console.log('📅 Corrected sentAt:', data.sentAt);

    displayMessage(data);
    scrollToBottom();
}

export function updateMessageStatus(messageId, status, readAt = null) {
    const messageEl = document.querySelector(`[data-message-id="${messageId}"]`);
    if (!messageEl?.classList.contains('sent')) return;

    const sendInfoEl = messageEl.querySelector('.sent-info');
    if (!sendInfoEl) return;

    const sendTimeMatch = sendInfoEl.textContent.match(/ارسال:\s*(\d{1,2}:\d{2})/);
    const sendTime = sendTimeMatch ? sendTimeMatch[1] : formatPersianTime(new Date());

    const hasEditedBadge = sendInfoEl.querySelector('.edited-badge');
    const editedBadgeHtml = hasEditedBadge ? hasEditedBadge.outerHTML : '';

    let newStatusHtml = '';
    if (status === 'read' && readAt) {
        const readTime = formatPersianTime(readAt);
        newStatusHtml = `
            <div class="sent-info">
                ارسال: ${sendTime} &nbsp;&nbsp; مشاهده: ${readTime}
                <span class="tick double-blue">✓✓</span>
                ${editedBadgeHtml}
            </div>
        `;
    } else if (status === 'delivered') {
        newStatusHtml = `
            <div class="sent-info">
                ارسال: ${sendTime}
                <span class="tick double-gray">✓✓</span>
                ${editedBadgeHtml}
            </div>
        `;
    } else {
        newStatusHtml = `
            <div class="sent-info">
                ارسال: ${sendTime}
                <span class="tick single">✓</span>
                ${editedBadgeHtml}
            </div>
        `;
    }

    sendInfoEl.outerHTML = newStatusHtml;
}

export function setupScrollListener() {
    const container = document.getElementById('messagesContainer');
    if (!container) return;

    container.addEventListener('scroll', async function () {
        if (container.scrollTop < 100 && hasMoreMessages && !isLoadingMessages) {
            console.log('🔄 Loading more messages...');
            await loadMessages(true);
        }
    });

    console.log('✅ Scroll listener attached');
}

function showNotification(title, body) {
    if (Notification.permission === 'granted') {
        new Notification(title, { body });
    }
}