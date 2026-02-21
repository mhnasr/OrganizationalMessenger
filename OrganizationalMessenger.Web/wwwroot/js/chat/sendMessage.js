// ========================================
// Universal Message Sender
// ========================================

import { currentChat } from './variables.js';
import { getCsrfToken } from './utils.js';

export async function sendMessageUniversal({
    text = '',
    file = null,
    type = 'text',
    replyToId = null,
    duration = null
}) {
    if (!currentChat || !window.connection) {
        console.error('❌ currentChat یا connection موجود نیست');
        return null;
    }

    const payload = {
        receiverId: currentChat.type === 'private' ? currentChat.id : null,
        groupId: currentChat.type === 'group' ? currentChat.id : null,
        channelId: currentChat.type === 'channel' ? currentChat.id : null,
        messageText: text?.trim() || '',
        replyToId: replyToId || null,
        type: getMessageType(type),
        fileAttachmentId: file?.id || null,
        duration: duration || null
    };

    try {
        console.log('📤 ارسال پیام:', payload);

        const response = await fetch('/Chat/SendMessage', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getCsrfToken()
            },
            body: JSON.stringify(payload)
        });

        const result = await response.json();
        if (!result.success) {
            console.error('❌ ارسال ناموفق:', result.message);
            return null;
        }

        await broadcastToSignalR(result.messageId, text, file, replyToId);

        console.log('✅ پیام ارسال شد:', result.messageId);
        return result;

    } catch (error) {
        console.error('❌ خطا در ارسال پیام:', error);
        return null;
    }
}

function getMessageType(type) {
    const typeMap = {
        'text': 0,
        'image': 1,
        'video': 2,
        'audio': 3,
        'document': 5
    };
    return typeMap[type.toLowerCase()] || 0;
}

async function broadcastToSignalR(messageId, messageText, file = null, replyToId = null) {
    if (window.connection?.state !== signalR.HubConnectionState.Connected) {
        console.warn('⚠️ SignalR وصل نیست');
        return;
    }

    if (currentChat.type === 'private') {
        if (file) {
            await window.connection.invoke(
                "SendPrivateMessageWithFile",
                currentChat.id,
                messageText,
                messageId,
                file.id
            );
        } else {
            await window.connection.invoke(
                "SendPrivateMessage",
                currentChat.id,
                messageText,
                replyToId
            );
        }
    }
}

// Export to window
window.sendMessageUniversal = sendMessageUniversal;
