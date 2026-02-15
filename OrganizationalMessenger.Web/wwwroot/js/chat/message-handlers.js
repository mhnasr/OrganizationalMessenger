// ============================================
// Message Handlers - Receive, Send, Status
// ============================================

import { currentChat, connection, isPageFocused } from './variables.js';
import { displayMessage, addUnreadSeparator, markMessagesAsRead, removeUnreadSeparator } from './messages.js';
import { loadChats } from './chats.js';
import { formatPersianTime, scrollToBottom } from './utils.js';
import { hasMoreMessages, isLoadingMessages } from './variables.js';
import { loadMessages } from './messages.js';


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
                removeUnreadSeparator(); // ✅ حذف separator
            }, 100);
        } else {
            setTimeout(() => {
                if (connection?.state === signalR.HubConnectionState.Connected) {
                    connection.invoke("ConfirmDelivery", data.id);
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
        data.sentAt = new Date().toISOString();
    }

    displayMessage(data);
    scrollToBottom();
}
export function updateMessageStatus(messageId, status, readAt = null) {
    console.log(`🔄 Updating message ${messageId} to ${status}`);

    const messageEl = document.querySelector(`[data-message-id="${messageId}"]`);

    // ✅ فقط برای پیام‌های ارسالی (sent)
    if (!messageEl?.classList.contains('sent')) {
        console.log('⚠️ Message is not sent, skipping status update');
        return;
    }

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
    console.log(`✅ Message ${messageId} status updated to ${status}`);
}
export function setupScrollListener() {
    const container = document.getElementById('messagesContainer');
    if (!container) return;

    let isLoadingMore = false;

    container.addEventListener('scroll', async function () {
        // ✅ جلوگیری از چند بار صدا زدن همزمان
        if (isLoadingMore) {
            return;
        }

        if (container.scrollTop < 100 && hasMoreMessages && !isLoadingMessages) {
            console.log('🔄 Loading more messages...');
            isLoadingMore = true;

            try {
                await loadMessages(true);
            } finally {
                // ✅ بعد از 500ms دوباره اجازه بده
                setTimeout(() => {
                    isLoadingMore = false;
                }, 500);
            }
        }
    });

    console.log('✅ Scroll listener attached');
}

function showNotification(title, body) {
    if (Notification.permission === 'granted') {
        new Notification(title, { body });
    }
}