// ============================================
// Message Menu - Edit, Delete, Toggle
// ============================================

import { connection } from './variables.js';
import { getCsrfToken, escapeHtml } from './utils.js';
import { replaceWithDeletedNotice } from './messages.js';

export function toggleMessageMenu(messageId) {
    const menu = document.getElementById(`menu-${messageId}`);
    if (!menu) return;

    const messageEl = menu.closest('.message');
    const isSent = messageEl?.classList.contains('sent');

    document.querySelectorAll('.message-menu-dropdown').forEach(m => {
        if (m.id !== `menu-${messageId}`) {
            m.style.display = 'none';
            m.closest('.message')?.classList.remove('menu-open');
        }
    });

    if (menu.style.display === 'none' || !menu.style.display) {
        menu.style.display = 'block';
        messageEl?.classList.add('menu-open');

        if (isSent) {
            menu.style.right = '0';
            menu.style.left = 'auto';
        } else {
            menu.style.left = '0px';
            menu.style.right = 'auto';
        }

        setTimeout(() => {
            const menuRect = menu.getBoundingClientRect();
            const messageRect = messageEl.getBoundingClientRect();
            const windowHeight = window.innerHeight;

            const distanceFromTop = messageRect.top;
            const distanceFromBottom = windowHeight - messageRect.bottom;
            const menuHeight = menuRect.height || 200;

            menu.classList.remove('open-upward', 'open-downward');

            if (distanceFromBottom > menuHeight + 50) {
                menu.style.top = '32px';
                menu.style.bottom = 'auto';
                menu.classList.add('open-downward');
            } else if (distanceFromTop > menuHeight + 50) {
                menu.style.bottom = '32px';
                menu.style.top = 'auto';
                menu.classList.add('open-upward');
            } else {
                menu.style.top = '32px';
                menu.style.bottom = 'auto';
                menu.classList.add('open-downward');
            }
        }, 10);
    } else {
        menu.style.display = 'none';
        messageEl?.classList.remove('menu-open');
    }
}

export async function editMessage(messageId) {
    const messageEl = document.querySelector(`[data-message-id="${messageId}"]`);
    if (!messageEl) return;

    const textEl = messageEl.querySelector('[data-editable="true"]');
    if (!textEl) return;

    const currentText = textEl.textContent.trim();

    toggleMessageMenu(messageId);

    showEditDialog(currentText, async (newText) => {
        if (!newText || newText === currentText) return;

        try {
            const response = await fetch('/Chat/EditMessage', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': getCsrfToken()
                },
                body: JSON.stringify({
                    messageId: messageId,
                    newContent: newText
                })
            });

            const result = await response.json();

            if (result.success) {
                if (connection?.state === signalR.HubConnectionState.Connected) {
                    await connection.invoke("NotifyMessageEdited", messageId, newText, new Date());
                }
                console.log('✅ Message edited successfully');
            } else {
                alert(result.message || 'خطا در ویرایش پیام');
            }
        } catch (error) {
            console.error('❌ Edit message error:', error);
            alert('خطا در ویرایش پیام');
        }
    });
}

function showEditDialog(currentText, onSave) {
    const dialog = document.createElement('div');
    dialog.className = 'edit-dialog-overlay';
    dialog.innerHTML = `
        <div class="edit-dialog">
            <div class="edit-dialog-header">
                <h3>ویرایش پیام</h3>
                <button class="close-dialog" onclick="this.closest('.edit-dialog-overlay').remove(); document.body.style.overflow='auto'">✕</button>
            </div>
            <div class="edit-dialog-body">
                <textarea 
                    id="editMessageText" 
                    class="edit-input"
                    rows="5"
                    placeholder="متن پیام...">${escapeHtml(currentText)}</textarea>
            </div>
            <div class="edit-dialog-footer">
                <button class="btn-cancel" onclick="this.closest('.edit-dialog-overlay').remove(); document.body.style.overflow='auto'">
                    انصراف
                </button>
                <button class="btn-save" id="saveEditBtn">
                    <i class="fas fa-check"></i> ذخیره
                </button>
            </div>
        </div>
    `;

    document.body.appendChild(dialog);
    document.body.style.overflow = 'hidden';

    const textarea = document.getElementById('editMessageText');
    textarea.focus();
    textarea.setSelectionRange(textarea.value.length, textarea.value.length);

    document.getElementById('saveEditBtn').addEventListener('click', () => {
        const newText = textarea.value.trim();
        dialog.remove();
        document.body.style.overflow = 'auto';
        onSave(newText);
    });

    textarea.addEventListener('keydown', (e) => {
        if (e.key === 'Enter' && e.ctrlKey) {
            e.preventDefault();
            document.getElementById('saveEditBtn').click();
        }
    });
}

export async function deleteMessage(messageId) {
    toggleMessageMenu(messageId);

    showConfirmDialog(
        'حذف پیام',
        'آیا از حذف این پیام اطمینان دارید؟',
        async () => {
            try {
                console.log('🗑️ Deleting message:', messageId);

                const response = await fetch('/Chat/DeleteMessage', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': getCsrfToken()
                    },
                    body: JSON.stringify({
                        messageId: messageId
                    })
                });

                const result = await response.json();
                console.log('📥 Delete response:', result);

                if (result.success) {
                    const messageEl = document.querySelector(`[data-message-id="${messageId}"]`);

                    if (result.showNotice) {
                        console.log('📝 ShowNotice=true, replacing with notice');
                        if (messageEl) {
                            replaceWithDeletedNotice(messageEl);
                        }
                    } else {
                        console.log('🗑️ ShowNotice=false, removing completely');
                        if (messageEl) {
                            messageEl.style.animation = 'fadeOut 0.3s ease';
                            setTimeout(() => {
                                messageEl.remove();
                            }, 300);
                        }
                    }

                    if (connection?.state === signalR.HubConnectionState.Connected) {
                        console.log('📡 Sending SignalR notification...');
                        await connection.invoke("NotifyMessageDeleted",
                            result.messageId,
                            result.showNotice,
                            result.receiverId
                        );
                        console.log('✅ SignalR notification sent');
                    } else {
                        console.error('❌ SignalR not connected!');
                    }
                } else {
                    alert(result.message || 'خطا در حذف پیام');
                }
            } catch (error) {
                console.error('❌ Delete message error:', error);
                alert('خطا در حذف پیام');
            }
        }
    );
}

function showConfirmDialog(title, message, onConfirm) {
    const dialog = document.createElement('div');
    dialog.className = 'confirm-dialog-overlay';
    dialog.innerHTML = `
        <div class="confirm-dialog">
            <div class="confirm-dialog-header">
                <h3>${title}</h3>
            </div>
            <div class="confirm-dialog-body">
                <p>${message}</p>
            </div>
            <div class="confirm-dialog-footer">
                <button class="btn-cancel" onclick="this.closest('.confirm-dialog-overlay').remove(); document.body.style.overflow='auto'">
                    انصراف
                </button>
                <button class="btn-confirm" id="confirmBtn">
                    <i class="fas fa-check"></i> تأیید
                </button>
            </div>
        </div>
    `;

    document.body.appendChild(dialog);
    document.body.style.overflow = 'hidden';

    document.getElementById('confirmBtn').addEventListener('click', () => {
        dialog.remove();
        document.body.style.overflow = 'auto';
        onConfirm();
    });
}

// Export to window
window.toggleMessageMenu = toggleMessageMenu;
window.editMessage = editMessage;
window.deleteMessage = deleteMessage;

// Close menu on outside click
document.addEventListener('click', function (e) {
    if (!e.target.closest('.message-menu')) {
        document.querySelectorAll('.message-menu-dropdown').forEach(m => {
            m.style.display = 'none';
            m.closest('.message')?.classList.remove('menu-open');
        });
    }
});