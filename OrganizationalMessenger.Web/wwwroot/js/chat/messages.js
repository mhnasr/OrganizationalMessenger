// ============================================
// Messages - Load, Display, Settings
// ============================================

import {
    currentChat, isLoadingMessages, setIsLoadingMessages, hasMoreMessages, setHasMoreMessages,
    lastSenderId, setLastSenderId, messageGroupCount, setMessageGroupCount,
    messageSettings, setMessageSettings, isPageFocused
} from './variables.js';
import { escapeHtml, formatPersianTime, scrollToBottom, getInitials, getCsrfToken } from './utils.js';
import { renderFileAttachment } from './files.js';
import { connection } from './variables.js';

export async function loadMessageSettings() {
    try {
        const response = await fetch('/Chat/GetMessageSettings');

        if (!response.ok) {
            console.warn('⚠️ Settings API not available, using defaults');
            setMessageSettings({
                allowEdit: true,
                allowDelete: true,
                editTimeLimit: 3600,
                deleteTimeLimit: 7200
            });
            return;
        }

        const result = await response.json();

        if (result && result.success) {
            setMessageSettings({
                allowEdit: result.allowEdit || false,
                allowDelete: result.allowDelete || false,
                editTimeLimit: result.editTimeLimit || 3600,
                deleteTimeLimit: result.deleteTimeLimit || 7200
            });
            console.log('✅ Message settings loaded:', messageSettings);
        }
    } catch (error) {
        console.warn('⚠️ Load settings error:', error.message);
        setMessageSettings({
            allowEdit: true,
            allowDelete: true,
            editTimeLimit: 3600,
            deleteTimeLimit: 7200
        });
    }
}

export async function loadMessages(append = false) {
    if (!currentChat) return;
    if (isLoadingMessages) return;

    setIsLoadingMessages(true);

    try {
        let url = `/Chat/GetMessages?pageSize=50`;

        if (currentChat.type === 'private') {
            url += `&userId=${currentChat.id}`;
        } else if (currentChat.type === 'group') {
            url += `&groupId=${currentChat.id}`;
        }

        if (append) {
            const firstMessage = document.querySelector('#messagesContainer .message[data-message-id]');
            if (firstMessage) {
                const oldestId = firstMessage.dataset.messageId;
                url += `&beforeMessageId=${oldestId}`;
            }
        }

        const response = await fetch(url);
        const data = await response.json();

        const container = document.getElementById('messagesContainer');

        if (!append) {
            container.innerHTML = '';
            setLastSenderId(null);
            setMessageGroupCount(0);
        }

        const previousScrollHeight = container.scrollHeight;

        if (append && data.messages.length > 0) {
            const existingMessages = container.innerHTML;
            container.innerHTML = '';
            data.messages.forEach(msg => displayMessage(msg));
            container.innerHTML += existingMessages;

            const newScrollHeight = container.scrollHeight;
            container.scrollTop = newScrollHeight - previousScrollHeight;
        } else {
            const unreadMessages = data.messages.filter(msg =>
                msg.senderId !== window.currentUserId && !msg.isRead && !msg.isDeleted
            );

            const shouldShowSeparator = !isPageFocused && unreadMessages.length > 0;
            let unreadSeparatorAdded = false;

            if (shouldShowSeparator) {
                const firstUnreadId = unreadMessages[0].id;

                data.messages.forEach((msg) => {
                    if (!unreadSeparatorAdded && firstUnreadId && msg.id === firstUnreadId) {
                        addUnreadSeparator(container, unreadMessages.length);
                        unreadSeparatorAdded = true;
                    }
                    displayMessage(msg);
                });
            } else {
                data.messages.forEach(msg => displayMessage(msg));
            }

            scrollToBottom();
        }

        setHasMoreMessages(data.hasMore);

        console.log(`✅ Loaded ${data.messages.length} messages, hasMore: ${data.hasMore}`);
    } catch (error) {
        console.error('❌ Load messages error:', error);
    } finally {
        setIsLoadingMessages(false);
    }
}

export function displayMessage(msg) {
    const isSent = msg.senderId === window.currentUserId;
    const container = document.getElementById('messagesContainer');

    const isConsecutive = lastSenderId === msg.senderId && messageGroupCount < 10;
    if (isConsecutive) {
        setMessageGroupCount(messageGroupCount + 1);
    } else {
        setLastSenderId(msg.senderId);
        setMessageGroupCount(1);
    }

    const messageEl = document.createElement('div');
    messageEl.className = `message ${isSent ? 'sent' : 'received'} ${isConsecutive ? 'consecutive' : ''}`;
    messageEl.dataset.messageId = msg.id;

    const sentAt = msg.sentAt || new Date().toISOString();
    messageEl.dataset.sentAt = sentAt;

    if (msg.isDeleted) {
        messageEl.classList.add('deleted');
        messageEl.innerHTML = `
            <div class="message-wrapper">
                <div class="message-bubble">
                    <div class="message-content deleted-message">
                        <div class="deleted-text">
                            ${isSent ? 'شما این پیام را حذف کردید' : 'این پیام حذف شده است'}
                        </div>
                    </div>
                </div>
            </div>
        `;
        container.appendChild(messageEl);
        return;
    }

    const sendTime = formatPersianTime(msg.sentAt || new Date());

    let avatarSectionHtml = '';
    if (!isConsecutive) {
        const avatarContent = msg.senderAvatar ?
            `<img src="${msg.senderAvatar}" alt="${escapeHtml(msg.senderName)}" class="message-avatar-img" />` :
            getInitials(msg.senderName);

        avatarSectionHtml = `
            <div class="message-avatar-section">
                <div class="message-avatar">${avatarContent}</div>
                <div class="message-sender">${escapeHtml(msg.senderName)}</div>
            </div>
        `;
    }

    let attachmentsHtml = '';
    if (msg.attachments && msg.attachments.length > 0) {
        attachmentsHtml = msg.attachments.map(file => renderFileAttachment(file)).join('');
    }

    let messageTextHtml = '';
    const hasAttachments = msg.attachments && msg.attachments.length > 0;
    const messageContent = msg.content || msg.messageText || '';

    if (hasAttachments) {
        if (messageContent && !messageContent.startsWith('📎') && !messageContent.startsWith('🎤')) {
            messageTextHtml = `<div class="message-caption" data-editable="true">${escapeHtml(messageContent)}</div>`;
        }
    } else {
        if (messageContent) {
            messageTextHtml = `<div class="message-text" data-editable="true">${escapeHtml(messageContent)}</div>`;
        }
    }

    const editedBadge = msg.isEdited ? '<span class="edited-badge">ویرایش شده</span>' : '';

    let statusHtml = '';
    if (isSent) {
        const readTime = msg.readAt ? formatPersianTime(msg.readAt) : null;
        if (msg.isRead && readTime) {
            statusHtml = `
                <div class="sent-info">
                    ارسال: ${sendTime} &nbsp;&nbsp; مشاهده: ${readTime}
                    <span class="tick double-blue">✓✓</span>
                    ${editedBadge}
                </div>`;
        } else if (msg.isDelivered) {
            statusHtml = `
                <div class="sent-info">
                    ارسال: ${sendTime}
                    <span class="tick double-gray">✓✓</span>
                    ${editedBadge}
                </div>`;
        } else {
            statusHtml = `
                <div class="sent-info">
                    ارسال: ${sendTime}
                    <span class="tick single">✓</span>
                    ${editedBadge}
                </div>`;
        }
    } else {
        statusHtml = `<div class="message-time">${sendTime} ${editedBadge}</div>`;
    }

    let replyHtml = '';
    if (msg.replyToMessageId) {
        replyHtml = `
            <div class="message-reply" onclick="scrollToMessage(${msg.replyToMessageId})">
                <i class="fas fa-reply"></i>
                <div class="message-reply-content">
                    <strong>${escapeHtml(msg.replyToSenderName || 'کاربر')}</strong>
                    <p>${escapeHtml((msg.replyToText || 'پیام').substring(0, 50))}</p>
                </div>
            </div>
        `;
    }

    const messageMenuHtml = createMessageMenu(msg.id, isSent, sentAt);

    messageEl.innerHTML = `
        <div class="message-wrapper">
            ${!isConsecutive ? avatarSectionHtml : ''}
            <div class="message-bubble">
                <div class="message-content">
                    ${replyHtml}
                    ${attachmentsHtml}
                    ${messageTextHtml}
                    ${statusHtml}
                </div>
                ${messageMenuHtml}
            </div>
        </div>
    `;

    container.appendChild(messageEl);

    if (!isSent && currentChat) {
        setTimeout(() => markMessagesAsRead(), 500);
    }
}

function createMessageMenu(messageId, isSent, sentAt) {
    if (isSent) {
        const canEdit = canEditMessage(sentAt);
        const canDelete = canDeleteMessage(sentAt);

        return `
            <div class="message-menu">
                <button class="message-menu-btn" onclick="toggleMessageMenu(${messageId})">
                    <i class="fas fa-ellipsis-v"></i>
                </button>
                <div class="message-menu-dropdown" id="menu-${messageId}" style="display: none;">
                    <button onclick="replyToMessage(${messageId})">
                        <i class="fas fa-reply"></i> پاسخ
                    </button>
                    <button onclick="forwardMessage(${messageId})">
                        <i class="fas fa-share"></i> ارجاع
                    </button>
                    <button onclick="enterMultiSelectMode()">
                        <i class="fas fa-check-square"></i> ارجاع چندین پیام
                    </button>
                    ${canEdit ? `
                    <button onclick="editMessage(${messageId})">
                        <i class="fas fa-edit"></i> ویرایش
                    </button>` : ''}
                    ${canDelete ? `
                    <button onclick="deleteMessage(${messageId})" class="delete-btn">
                        <i class="fas fa-trash"></i> حذف
                    </button>` : ''}
                </div>
            </div>
        `;
    } else {
        return `
            <div class="message-menu">
                <button class="message-menu-btn" onclick="toggleMessageMenu(${messageId})">
                    <i class="fas fa-ellipsis-v"></i>
                </button>
                <div class="message-menu-dropdown" id="menu-${messageId}" style="display: none;">
                    <button onclick="replyToMessage(${messageId})">
                        <i class="fas fa-reply"></i> پاسخ
                    </button>
                    <button onclick="forwardMessage(${messageId})">
                        <i class="fas fa-share"></i> ارجاع
                    </button>
                    <button onclick="enterMultiSelectMode()">
                        <i class="fas fa-check-square"></i> ارجاع چندین پیام
                    </button>
                    <button onclick="reportMessage(${messageId})" class="report-btn">
                        <i class="fas fa-flag"></i> گزارش
                    </button>
                </div>
            </div>
        `;
    }
}

function canEditMessage(sentAt) {
    if (!messageSettings.allowEdit) return false;
    const sentDate = new Date(sentAt);
    const now = new Date();
    const elapsed = (now - sentDate) / 1000;
    return elapsed <= messageSettings.editTimeLimit;
}

function canDeleteMessage(sentAt) {
    if (!messageSettings.allowDelete) return false;
    const sentDate = new Date(sentAt);
    const now = new Date();
    const elapsed = (now - sentDate) / 1000;
    return elapsed <= messageSettings.deleteTimeLimit;
}

export function addUnreadSeparator(container, count) {
    const separator = document.createElement('div');
    separator.className = 'unread-separator';
    separator.innerHTML = `
        <div class="unread-line"></div>
        <span class="unread-label">${count} پیام خوانده نشده</span>
        <div class="unread-line"></div>
    `;
    container.appendChild(separator);
}

export function removeUnreadSeparator() {
    const separator = document.querySelector('.unread-separator');
    if (separator) {
        separator.style.animation = 'fadeOut 0.4s ease';
        setTimeout(() => {
            separator.remove();
        }, 400);
    }
}

export async function markMessagesAsRead() {
    if (!currentChat?.id || currentChat.type !== 'private') return;

    const unreadReceivedIds = Array.from(
        document.querySelectorAll('#messagesContainer .message.received[data-message-id]')
    )
        .filter(el => !el.querySelector('.double-blue'))
        .map(el => parseInt(el.dataset.messageId))
        .filter(id => id);

    if (unreadReceivedIds.length === 0) {
        removeUnreadBadge();
        return;
    }

    try {
        await fetch('/Chat/MarkMessagesAsRead', {
            method: 'POST',
            headers: {
                'RequestVerificationToken': getCsrfToken(),
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ messageIds: unreadReceivedIds })
        });

        if (connection?.state === signalR.HubConnectionState.Connected) {
            await connection.invoke("NotifyMessagesRead", unreadReceivedIds);
        }

        removeUnreadBadge();
    } catch (error) {
        console.error('❌ Mark as read error:', error);
    }
}

function removeUnreadBadge() {
    if (!currentChat) return;

    const chatItem = document.querySelector(`.chat-item[data-chat-id="${currentChat.id}"]`);
    if (!chatItem) return;

    const badge = chatItem.querySelector('.unread-badge');
    if (badge) {
        badge.style.animation = 'fadeOutBadge 0.3s ease';
        setTimeout(() => {
            badge.remove();
        }, 300);
    }
}

export function replaceWithDeletedNotice(messageEl) {
    const isSent = messageEl.classList.contains('sent');
    const messageBubble = messageEl.querySelector('.message-bubble');
    if (!messageBubble) return;

    messageBubble.innerHTML = `
        <div class="message-content deleted-message">
            <div class="deleted-text">
                ${isSent ? 'شما این پیام را حذف کردید' : 'این پیام حذف شده است'}
            </div>
        </div>
    `;

    messageEl.classList.add('deleted');
}

export function scrollToMessage(messageId) {
    const messageEl = document.querySelector(`[data-message-id="${messageId}"]`);
    if (!messageEl) {
        alert('پیام مورد نظر یافت نشد');
        return;
    }

    messageEl.scrollIntoView({ behavior: 'smooth', block: 'center' });
    messageEl.classList.add('highlight');
    setTimeout(() => {
        messageEl.classList.remove('highlight');
    }, 2000);
}

// Export to window for onclick handlers
window.scrollToMessage = scrollToMessage;