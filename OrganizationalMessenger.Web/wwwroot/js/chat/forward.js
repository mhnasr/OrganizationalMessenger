// ============================================
// Forward Messages
// ============================================

import { currentChat } from './variables.js';
import { getCsrfToken, escapeHtml } from './utils.js';
import { scrollToBottom } from './utils.js';

let forwardMode = false;
let selectedMessagesForForward = new Set();
let sentToUsers = new Set();  // ✅ ذخیره کاربرانی که ارسال شده

export function forwardMessage(messageId) {
    console.log('📤 Forward single message:', messageId);
    selectedMessagesForForward.clear();
    selectedMessagesForForward.add(messageId);
    sentToUsers.clear();  // ✅ Reset
    showForwardDialog();
}

export function enterMultiSelectMode() {
    console.log('✅ Entering multi-select mode');
    forwardMode = true;
    selectedMessagesForForward.clear();
    sentToUsers.clear();  // ✅ Reset

    document.querySelectorAll('.message-menu-dropdown').forEach(m => {
        m.style.display = 'none';
    });

    const messages = document.querySelectorAll('.message:not(.deleted)');
    console.log('📋 Found', messages.length, 'messages to add checkbox');

    messages.forEach(msg => {
        msg.classList.add('selectable');

        if (!msg.querySelector('.message-checkbox')) {
            const checkbox = document.createElement('div');
            checkbox.className = 'message-checkbox';
            checkbox.innerHTML = '<i class="far fa-square"></i>';

            checkbox.addEventListener('click', (e) => {
                e.stopPropagation();
                const msgId = parseInt(msg.dataset.messageId);
                console.log('🖱️ Checkbox clicked for message:', msgId);
                toggleMessageSelection(msgId);
            });

            const wrapper = msg.querySelector('.message-wrapper');
            if (wrapper) {
                wrapper.style.position = 'relative';
                wrapper.prepend(checkbox);
                console.log('✅ Checkbox added to message:', msg.dataset.messageId);
            }
        }
    });

    showMultiSelectToolbar();
    console.log('✅ Multi-select mode activated');
}

function toggleMessageSelection(messageId) {
    console.log('🔄 Toggle selection:', messageId);
    const messageEl = document.querySelector(`[data-message-id="${messageId}"]`);
    if (!messageEl) {
        console.error('❌ Message element not found:', messageId);
        return;
    }

    if (selectedMessagesForForward.has(messageId)) {
        selectedMessagesForForward.delete(messageId);
        messageEl.classList.remove('selected');
        const checkbox = messageEl.querySelector('.message-checkbox i');
        if (checkbox) {
            checkbox.className = 'far fa-square';
        }
        console.log('✅ Deselected:', messageId);
    } else {
        selectedMessagesForForward.add(messageId);
        messageEl.classList.add('selected');
        const checkbox = messageEl.querySelector('.message-checkbox i');
        if (checkbox) {
            checkbox.className = 'fas fa-check-square';
        }
        console.log('✅ Selected:', messageId);
    }

    console.log('📊 Total selected:', selectedMessagesForForward.size);
    updateMultiSelectToolbar();
}

function showMultiSelectToolbar() {
    let toolbar = document.getElementById('multiSelectToolbar');
    if (toolbar) {
        toolbar.remove();
    }

    toolbar = document.createElement('div');
    toolbar.id = 'multiSelectToolbar';
    toolbar.className = 'multi-select-toolbar';
    toolbar.innerHTML = `
        <button class="btn-cancel" id="cancelMultiSelectBtn">
            <i class="fas fa-times"></i> انصراف
        </button>
        <span class="selected-count">0 پیام انتخاب شده</span>
        <button class="btn-forward" id="forwardMultipleBtn" disabled>
            <i class="fas fa-share"></i> ارجاع
        </button>
    `;
    document.body.appendChild(toolbar);

    document.getElementById('cancelMultiSelectBtn').addEventListener('click', () => {
        console.log('❌ Cancel button clicked');
        exitMultiSelectMode();
    });

    document.getElementById('forwardMultipleBtn').addEventListener('click', () => {
        console.log('📤 Forward button clicked, selected:', selectedMessagesForForward.size);
        forwardSelectedMessages();
    });

    toolbar.style.display = 'flex';
    updateMultiSelectToolbar();
}

function updateMultiSelectToolbar() {
    const toolbar = document.getElementById('multiSelectToolbar');
    if (!toolbar) return;

    const count = selectedMessagesForForward.size;
    const countEl = toolbar.querySelector('.selected-count');
    const forwardBtn = toolbar.querySelector('.btn-forward');

    if (countEl) {
        countEl.textContent = `${count} پیام انتخاب شده`;
    }

    if (forwardBtn) {
        forwardBtn.disabled = count === 0;
    }
}

function exitMultiSelectMode() {
    console.log('❌ Exiting multi-select mode');
    forwardMode = false;
    selectedMessagesForForward.clear();
    sentToUsers.clear();

    document.querySelectorAll('.message').forEach(msg => {
        msg.classList.remove('selectable', 'selected');
        const checkbox = msg.querySelector('.message-checkbox');
        if (checkbox) checkbox.remove();
    });

    const toolbar = document.getElementById('multiSelectToolbar');
    if (toolbar) {
        toolbar.remove();
    }

    console.log('✅ Multi-select mode deactivated');
}

function forwardSelectedMessages() {
    console.log('📤 forwardSelectedMessages called');
    console.log('📊 Selected messages:', Array.from(selectedMessagesForForward));

    if (selectedMessagesForForward.size === 0) {
        console.warn('⚠️ No messages selected!');
        alert('هیچ پیامی انتخاب نشده است');
        return;
    }

    console.log('✅ Showing forward dialog for', selectedMessagesForForward.size, 'messages');
    showForwardDialog();
}

function showForwardDialog() {
    console.log('📋 Showing forward dialog');

    fetch('/Chat/GetChats?tab=private')
        .then(res => res.json())
        .then(chats => {
            console.log('✅ Loaded', chats.length, 'contacts');

            const dialog = document.createElement('div');
            dialog.className = 'forward-dialog-overlay';
            dialog.innerHTML = `
                <div class="forward-dialog">
                    <div class="forward-dialog-header">
                        <h3>ارجاع به (${selectedMessagesForForward.size} پیام)</h3>
                        <button class="close-dialog" id="closeForwardDialog">✕</button>
                    </div>
                    <div class="forward-dialog-body">
                        <input type="text" id="forwardSearchInput" class="forward-search" placeholder="جستجوی مخاطب..." />
                        <div class="forward-contacts-list" id="forwardContactsList">
                            ${chats.map(chat => `
                                <div class="forward-contact-item" data-user-id="${chat.id}">
                                    <img src="${chat.avatar}" alt="${escapeHtml(chat.name)}" class="forward-avatar" />
                                    <div class="forward-contact-info">
                                        <span class="forward-contact-name">${escapeHtml(chat.name)}</span>
                                    </div>
                                    <button class="forward-send-btn" data-user-id="${chat.id}">
                                        <i class="fas fa-paper-plane"></i> ارسال
                                    </button>
                                </div>
                            `).join('')}
                        </div>
                    </div>
                    <div class="forward-dialog-footer">
                        <button class="btn-done" id="doneForwardDialog">
                            <i class="fas fa-check"></i> اتمام
                        </button>
                    </div>
                </div>
            `;

            document.body.appendChild(dialog);
            document.body.style.overflow = 'hidden';

            // ✅ دکمه بستن
            document.getElementById('closeForwardDialog').addEventListener('click', () => {
                dialog.remove();
                document.body.style.overflow = 'auto';
                if (forwardMode) {
                    exitMultiSelectMode();
                }
            });

            // ✅ دکمه اتمام
            document.getElementById('doneForwardDialog').addEventListener('click', () => {
                dialog.remove();
                document.body.style.overflow = 'auto';
                if (sentToUsers.size > 0) {
                    //alert(`پیام‌ها برای ${sentToUsers.size} نفر ارسال شد`);
                }
                if (forwardMode) {
                    exitMultiSelectMode();
                }
            });

            // ✅ دکمه‌های ارسال
            document.querySelectorAll('.forward-send-btn').forEach(btn => {
                btn.addEventListener('click', async (e) => {
                    e.stopPropagation();
                    const userId = parseInt(btn.dataset.userId);
                    const contactItem = btn.closest('.forward-contact-item');

                    console.log('👤 Send button clicked for user:', userId);

                    // ✅ جلوگیری از ارسال مجدد
                    if (sentToUsers.has(userId)) {
                        console.log('⚠️ Already sent to this user');
                        return;
                    }

                    // ✅ غیرفعال کردن دکمه
                    btn.disabled = true;
                    btn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> در حال ارسال...';

                    // ✅ ارسال
                    const success = await sendForwardedMessages(userId);

                    if (success) {
                        sentToUsers.add(userId);
                        btn.innerHTML = '<i class="fas fa-check"></i> ارسال شد';
                        btn.classList.add('sent');
                        contactItem.classList.add('sent');
                        console.log('✅ Sent to user:', userId);
                    } else {
                        btn.disabled = false;
                        btn.innerHTML = '<i class="fas fa-paper-plane"></i> ارسال';
                        alert('خطا در ارسال');
                    }
                });
            });

            // ✅ جستجو
            const searchInput = document.getElementById('forwardSearchInput');
            searchInput.addEventListener('input', (e) => {
                const query = e.target.value.toLowerCase();
                document.querySelectorAll('.forward-contact-item').forEach(item => {
                    const name = item.querySelector('.forward-contact-name').textContent.toLowerCase();
                    item.style.display = name.includes(query) ? 'flex' : 'none';
                });
            });
        })
        .catch(err => {
            console.error('❌ Load contacts error:', err);
            alert('خطا در بارگذاری مخاطبین');
        });
}

async function sendForwardedMessages(receiverId) {
    const messageIds = Array.from(selectedMessagesForForward);

    console.log('📤 Forwarding messages:', messageIds, 'to user:', receiverId);

    if (messageIds.length === 0) {
        return false;
    }

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

        const result = await response.json();
        console.log('📥 Forward response:', result);

        if (result.success) {
            console.log(`✅ ${result.forwardedIds.length} messages forwarded`);

            // ✅ اگر به چت فعلی ارسال شد، رفرش کن
            if (currentChat && currentChat.id === receiverId) {
                console.log('📨 Messages forwarded to current chat, reloading...');

                setTimeout(async () => {
                    const { loadMessages } = await import('./messages.js');
                    await loadMessages(false);
                    scrollToBottom();
                }, 500);
            }

            return true;
        } else {
            console.error('❌ Forward failed:', result.message);
            return false;
        }
    } catch (error) {
        console.error('❌ Forward error:', error);
        return false;
    }
}

// ✅ Export to window
window.forwardMessage = forwardMessage;
window.enterMultiSelectMode = enterMultiSelectMode;
window.forwardSelectedMessages = forwardSelectedMessages;

console.log('✅ forward.js loaded');