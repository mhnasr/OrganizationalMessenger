// ============================================
// Reply Functionality
// ============================================

import { replyingToMessage, setReplyingToMessage, connection, currentChat } from './variables.js';
import { escapeHtml } from './utils.js';

export function replyToMessage(messageId) {
    window.toggleMessageMenu(messageId);

    const messageEl = document.querySelector(`[data-message-id="${messageId}"]`);
    if (!messageEl) return;

    const textEl = messageEl.querySelector('[data-editable="true"]');
    const senderEl = messageEl.querySelector('.message-sender');

    const imageEl = messageEl.querySelector('.message-file.image-file img');
    const videoEl = messageEl.querySelector('.message-file.video-file video');

    let messageText = 'فایل ضمیمه';
    let thumbnailUrl = null;
    let fileType = null;

    if (textEl) {
        messageText = textEl.textContent.trim();
    } else if (imageEl) {
        messageText = '🖼️ تصویر';
        thumbnailUrl = imageEl.src;
        fileType = 'image';
    } else if (videoEl) {
        messageText = '🎥 ویدیو';
        thumbnailUrl = videoEl.poster || videoEl.src;
        fileType = 'video';
    }

    const senderName = senderEl ? senderEl.textContent.trim() : 'کاربر';

    setReplyingToMessage({
        id: messageId,
        text: messageText,
        senderName: senderName,
        thumbnail: thumbnailUrl,
        fileType: fileType
    });

    showReplyPreview();

    setTimeout(() => {
        const messageInput = document.getElementById('messageInput');
        if (messageInput) {
            messageInput.focus();
            messageInput.setSelectionRange(messageInput.value.length, messageInput.value.length);
        }
    }, 100);
}

function showReplyPreview() {
    if (!replyingToMessage) return;

    const container = document.getElementById('replyPreviewContainer');
    if (!container) {
        console.error('❌ replyPreviewContainer not found');
        return;
    }

    const existingPreview = document.getElementById('replyPreview');
    if (existingPreview) existingPreview.remove();

    const preview = document.createElement('div');
    preview.id = 'replyPreview';
    preview.className = 'reply-preview';

    let thumbnailHtml = '';
    if (replyingToMessage.thumbnail) {
        thumbnailHtml = `
            <div class="reply-preview-thumbnail">
                <img src="${replyingToMessage.thumbnail}" alt="Preview">
            </div>
        `;
    }

    preview.innerHTML = `
        <div class="reply-preview-content">
            <i class="fas fa-reply"></i>
            ${thumbnailHtml}
            <div class="reply-preview-text">
                <strong>${escapeHtml(replyingToMessage.senderName)}</strong>
                <p>${escapeHtml(replyingToMessage.text.substring(0, 50))}${replyingToMessage.text.length > 50 ? '...' : ''}</p>
            </div>
        </div>
        <button class="reply-preview-close" onclick="window.cancelReply()">
            <i class="fas fa-times"></i>
        </button>
    `;

    container.appendChild(preview);

    setTimeout(() => {
        const messageInput = document.getElementById('messageInput');
        if (messageInput) {
            messageInput.focus();
        }
    }, 100);
}

export function cancelReply() {
    setReplyingToMessage(null);
    const preview = document.getElementById('replyPreview');
    if (preview) preview.remove();
}

export async function sendMessage() {
    const input = document.getElementById('messageInput');
    const text = input.value.trim();

    if (!text || !currentChat || !connection) return;

    if (currentChat.type === 'private') {
        await connection.invoke("SendPrivateMessage",
            currentChat.id,
            text,
            replyingToMessage ? replyingToMessage.id : null
        );
    }

    input.value = '';
    input.style.height = 'auto';
    input.style.overflowY = 'hidden';

    cancelReply();
    hideTypingIndicator();
}

function hideTypingIndicator() {
    const typingEl = document.getElementById('typingIndicator');
    if (typingEl) typingEl.style.display = 'none';
}

// Export to window
window.replyToMessage = replyToMessage;
window.cancelReply = cancelReply;