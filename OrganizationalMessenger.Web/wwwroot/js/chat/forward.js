// ============================================
// Forward Messages
// ============================================

import { getCsrfToken, escapeHtml } from './utils.js';
import { exitMultiSelectMode } from './multi-select.js';

export function forwardMessage(messageId) {
    window.toggleMessageMenu(messageId);

    const messageEl = document.querySelector(`[data-message-id="${messageId}"]`);
    if (!messageEl) return;

    const textEl = messageEl.querySelector('[data-editable="true"]');
    const messageText = textEl ? textEl.textContent.trim() : '';

    showForwardDialog([messageId], messageText);
}

export async function showForwardDialog(messageIds, previewText = '') {
    try {
        const response = await fetch('/Chat/GetChats?tab=all');
        const chats = await response.json();

        const dialog = document.createElement('div');
        dialog.className = 'forward-dialog-overlay';
        dialog.innerHTML = `
            <div class="forward-dialog">
                <div class="forward-dialog-header">
                    <h3>ارجاع به...</h3>
                    <button class="close-dialog" onclick="window.closeForwardDialog()">✕</button>
                </div>
                <div class="forward-dialog-body">
                    <div class="forward-preview">
                        <i class="fas fa-share"></i>
                        <span>${messageIds.length} پیام انتخاب شده</span>
                        ${previewText ? `<p class="forward-preview-text">"${escapeHtml(previewText.substring(0, 50))}..."</p>` : ''}
                    </div>
                    <div class="forward-search">
                        <i class="fas fa-search"></i>
                        <input type="text" id="forwardSearch" placeholder="جستجوی مخاطب...">
                    </div>
                    <div class="forward-contacts-list" id="forwardContactsList">
                        ${chats.filter(c => c.type === 'private').map(chat => `
                            <div class="forward-contact-item" data-chat-id="${chat.id}">
                                <div class="contact-avatar ${chat.isOnline ? 'online' : ''}">
                                    <img src="${chat.avatar}" alt="${escapeHtml(chat.name)}">
                                </div>
                                <div class="contact-info">
                                    <span class="contact-name">${escapeHtml(chat.name)}</span>
                                </div>
                                <button class="btn-forward-send" data-receiver-id="${chat.id}">
                                    <i class="fas fa-paper-plane"></i> ارسال
                                </button>
                            </div>
                        `).join('')}
                    </div>
                </div>
                <div class="forward-dialog-footer">
                    <button class="btn-close-forward" onclick="window.closeForwardDialog()">
                        <i class="fas fa-times"></i> بستن
                    </button>
                </div>
            </div>
        `;

        document.body.appendChild(dialog);
        document.body.style.overflow = 'hidden';

        document.querySelectorAll('.btn-forward-send').forEach(btn => {
            btn.addEventListener('click', function () {
                const receiverId = parseInt(this.dataset.receiverId);
                sendForward(receiverId, messageIds);
            });
        });

        document.getElementById('forwardSearch').addEventListener('input', function (e) {
            const query = e.target.value.toLowerCase();
            document.querySelectorAll('.forward-contact-item').forEach(item => {
                const name = item.querySelector('.contact-name').textContent.toLowerCase();
                item.style.display = name.includes(query) ? 'flex' : 'none';
            });
        });

    } catch (error) {
        console.error('❌ Forward dialog error:', error);
        alert('خطا در بارگذاری مخاطبین');
    }
}

export function closeForwardDialog() {
    const dialog = document.querySelector('.forward-dialog-overlay');
    if (dialog) {
        dialog.remove();
        document.body.style.overflow = 'auto';
    }
}

async function sendForward(receiverId, messageIds) {
    console.log('📤 Forwarding messages:', messageIds, 'to:', receiverId);

    try {
        const response = await fetch('/Chat/ForwardMessages', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getCsrfToken()
            },
            body: JSON.stringify({
                messageIds: messageIds,
                receiverId: receiverId
            })
        });

        if (!response.ok) {
            console.error('❌ Response not OK:', response.status);
            alert('خطا در ارجاع پیام');
            return;
        }

        const result = await response.json();
        console.log('✅ Forward response:', result);

        if (result.success) {
            const contactItem = document.querySelector(`.forward-contact-item[data-chat-id="${receiverId}"]`);
            if (contactItem) {
                const btn = contactItem.querySelector('.btn-forward-send');
                btn.innerHTML = '<i class="fas fa-check"></i> ارسال شد';
                btn.style.background = '#4caf50';
                btn.disabled = true;
            }

            console.log('✅ Messages forwarded successfully');
        } else {
            alert(result.message || 'خطا در ارجاع پیام');
        }
    } catch (error) {
        console.error('❌ Forward error:', error);
        alert('خطا در ارجاع پیام');
    }
}

export function forwardSelectedMessages() {
    const selectedMessages = window.selectedMessages;
    if (!selectedMessages || selectedMessages.size === 0) return;

    const messageIds = Array.from(selectedMessages);

    const firstMessageEl = document.querySelector(`[data-message-id="${messageIds[0]}"]`);
    const textEl = firstMessageEl?.querySelector('[data-editable="true"]');
    const previewText = textEl ? textEl.textContent : '';

    showForwardDialog(messageIds, previewText);

    exitMultiSelectMode();
}

// Export to window
window.forwardMessage = forwardMessage;
window.closeForwardDialog = closeForwardDialog;
window.forwardSelectedMessages = forwardSelectedMessages;