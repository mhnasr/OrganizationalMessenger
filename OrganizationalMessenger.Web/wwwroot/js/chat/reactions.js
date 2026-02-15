// ============================================
// Message Reactions
// ============================================

import { getCsrfToken } from './utils.js';

const popularEmojis = [
    '❤️', '👍', '👎', '😂', '😮', '😢', '😍', '🔥',
    '🌹', '👏', '🙏', '💯', '✅', '❌', '⭐', '💪',
    '🤝', '💡', '🚀', '🎯'
];

export function showReactionPicker(messageId) {
    console.log('😊 Showing reaction picker for message:', messageId);

    // حذف picker قبل��
    const existingPicker = document.getElementById('reactionPicker');
    if (existingPicker) {
        existingPicker.remove();
        return;
    }

    const messageEl = document.querySelector(`[data-message-id="${messageId}"]`);
    if (!messageEl) return;

    // ✅ پیدا کردن دکمه add که روی آن کلیک شده
    const addBtn = messageEl.querySelector('.reaction-add-btn');
    if (!addBtn) return;

    // ✅ تشخیص نوع پیام
    const isSent = messageEl.classList.contains('sent');
    console.log('📍 Message type:', isSent ? 'sent' : 'received');

    const picker = document.createElement('div');
    picker.id = 'reactionPicker';
    picker.className = `reaction-picker ${isSent ? 'picker-left' : 'picker-right'}`;
    picker.innerHTML = `
        <div class="reaction-picker-emojis">
            ${popularEmojis.map(emoji => `
                <button class="reaction-emoji-btn" data-emoji="${emoji}">
                    ${emoji}
                </button>
            `).join('')}
        </div>
    `;

    // ✅ اضافه کردن picker به message-reactions (کنار دکمه)
    const reactionsContainer = messageEl.querySelector('.message-reactions');
    if (reactionsContainer) {
        reactionsContainer.appendChild(picker);
        console.log('✅ Picker added to reactions container');
    }

    // Event listeners
    picker.querySelectorAll('.reaction-emoji-btn').forEach(btn => {
        btn.addEventListener('click', (e) => {
            e.stopPropagation();
            const emoji = btn.dataset.emoji;
            addOrChangeReaction(messageId, emoji);
            picker.remove();
        });
    });

    // بستن با کلیک بیرون
    setTimeout(() => {
        document.addEventListener('click', function closePicker(e) {
            if (!picker.contains(e.target) && !addBtn.contains(e.target)) {
                picker.remove();
                document.removeEventListener('click', closePicker);
            }
        });
    }, 100);
}

export async function addOrChangeReaction(messageId, emoji) {
    console.log('🎭 Add or change reaction:', messageId, emoji);

    try {
        const response = await fetch('/Chat/ReactToMessage', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getCsrfToken()
            },
            body: JSON.stringify({
                messageId: messageId,
                emoji: emoji
            })
        });

        const result = await response.json();
        console.log('📥 Reaction response:', result);

        if (result.success) {
            updateReactionsUI(messageId, result.reactions);

            if (window.connection?.state === signalR.HubConnectionState.Connected) {
                await window.connection.invoke(
                    "NotifyMessageReaction",
                    messageId,
                    emoji,
                    result.action,
                    result.reactions
                );
                console.log('✅ SignalR notified about reaction');
            }
        } else {
            console.error('❌ Reaction failed:', result.message);
        }
    } catch (error) {
        console.error('❌ Add or change reaction error:', error);
    }
}

export async function toggleReaction(messageId, emoji) {
    console.log('🔄 Toggle reaction:', messageId, emoji);

    const messageEl = document.querySelector(`[data-message-id="${messageId}"]`);
    if (!messageEl) return;

    const reactionItem = messageEl.querySelector(`.reaction-item[data-emoji="${emoji}"]`);
    const isMyReaction = reactionItem?.classList.contains('my-reaction');

    if (isMyReaction) {
        console.log('🗑️ Removing my reaction');
        await addOrChangeReaction(messageId, emoji);
    } else {
        console.log('➕ Adding new reaction (will replace old one)');
        await addOrChangeReaction(messageId, emoji);
    }
}

function updateReactionsUI(messageId, reactions) {
    const messageEl = document.querySelector(`[data-message-id="${messageId}"]`);
    if (!messageEl) return;

    const reactionsContainer = messageEl.querySelector('.message-reactions');
    if (!reactionsContainer) {
        const messageWrapper = messageEl.querySelector('.message-wrapper');
        if (messageWrapper) {
            const newContainer = document.createElement('div');
            newContainer.className = 'message-reactions';
            messageWrapper.appendChild(newContainer);
            renderReactions(messageId, reactions, newContainer);
        }
    } else {
        renderReactions(messageId, reactions, reactionsContainer);
    }
}

function renderReactions(messageId, reactions, container) {
    if (!reactions || reactions.length === 0) {
        container.innerHTML = `
            <button class="reaction-add-btn" onclick="window.showReactionPicker(${messageId})">
                <i class="far fa-smile"></i>
            </button>
        `;
        return;
    }

    const reactionsItems = reactions.map(r => `
        <div class="reaction-item ${r.hasReacted ? 'my-reaction' : ''}" 
             data-emoji="${r.emoji}"
             onclick="window.toggleReaction(${messageId}, '${r.emoji}')"
             title="${r.users.map(u => u.name).join(', ')}">
            <span class="reaction-emoji">${r.emoji}</span>
            <span class="reaction-count">${r.count}</span>
        </div>
    `).join('');

    container.innerHTML = `
        ${reactionsItems}
        <button class="reaction-add-btn" onclick="window.showReactionPicker(${messageId})">
            <i class="far fa-smile"></i>
        </button>
    `;
}

window.showReactionPicker = showReactionPicker;
window.toggleReaction = toggleReaction;
window.addOrChangeReaction = addOrChangeReaction;

console.log('✅ reactions.js loaded');