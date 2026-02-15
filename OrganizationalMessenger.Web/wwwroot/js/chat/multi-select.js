// ============================================
// Multi-Select Mode
// ============================================

import { multiSelectMode, setMultiSelectMode, selectedMessages } from './variables.js';
import { forwardSelectedMessages } from './forward.js';

export function enterMultiSelectMode() {
    setMultiSelectMode(true);
    selectedMessages.clear();

    document.querySelectorAll('.message').forEach(msg => {
        if (msg.classList.contains('deleted')) return;

        const messageId = msg.dataset.messageId;

        if (!msg.querySelector('.message-checkbox')) {
            const checkbox = document.createElement('div');
            checkbox.className = 'message-checkbox';
            checkbox.innerHTML = '<i class="far fa-circle"></i>';
            checkbox.onclick = () => toggleMessageSelection(messageId);

            msg.querySelector('.message-bubble').appendChild(checkbox);
        }
    });

    showMultiSelectToolbar();

    document.querySelectorAll('.message-menu-dropdown').forEach(m => {
        m.style.display = 'none';
    });
}

function toggleMessageSelection(messageId) {
    const messageEl = document.querySelector(`[data-message-id="${messageId}"]`);
    if (!messageEl) return;

    const checkbox = messageEl.querySelector('.message-checkbox');

    if (selectedMessages.has(messageId)) {
        selectedMessages.delete(messageId);
        checkbox.innerHTML = '<i class="far fa-circle"></i>';
        checkbox.classList.remove('selected');
    } else {
        selectedMessages.add(messageId);
        checkbox.innerHTML = '<i class="fas fa-check-circle"></i>';
        checkbox.classList.add('selected');
    }

    updateMultiSelectToolbar();
}

function showMultiSelectToolbar() {
    const existingToolbar = document.getElementById('multiSelectToolbar');
    if (existingToolbar) return;

    const toolbar = document.createElement('div');
    toolbar.id = 'multiSelectToolbar';
    toolbar.className = 'multi-select-toolbar';
    toolbar.innerHTML = `
        <button class="toolbar-btn" onclick="window.exitMultiSelectMode()">
            <i class="fas fa-times"></i> لغو
        </button>
        <span class="selected-count">0 انتخاب شده</span>
        <button class="toolbar-btn primary" onclick="window.forwardSelectedMessages()" disabled>
            <i class="fas fa-share"></i> ارجاع
        </button>
    `;

    document.body.appendChild(toolbar);
}

function updateMultiSelectToolbar() {
    const countEl = document.querySelector('.selected-count');
    const forwardBtn = document.querySelector('.multi-select-toolbar .primary');

    if (countEl) {
        countEl.textContent = `${selectedMessages.size} انتخاب شده`;
    }

    if (forwardBtn) {
        forwardBtn.disabled = selectedMessages.size === 0;
    }
}

export function exitMultiSelectMode() {
    setMultiSelectMode(false);
    selectedMessages.clear();

    document.querySelectorAll('.message-checkbox').forEach(cb => cb.remove());

    document.getElementById('multiSelectToolbar')?.remove();
}

// Export to window
window.enterMultiSelectMode = enterMultiSelectMode;
window.exitMultiSelectMode = exitMultiSelectMode;
window.selectedMessages = selectedMessages;