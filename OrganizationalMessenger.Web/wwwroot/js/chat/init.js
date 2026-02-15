// ============================================
// Initialization
// ============================================

import { setConnection, setIsPageFocused } from './variables.js';
import { loadMessageSettings } from './messages.js';
import { setupSignalR } from './signalr.js';
// import { setupEventListeners } from './event-listeners.js'; // ❌ غیرفعال کنید
import { setupScrollListener } from './message-handlers.js';
import { markMessagesAsRead, removeUnreadSeparator } from './messages.js';
import { currentChat } from './variables.js';
import { sendMessage } from './reply.js';
import { handleFileSelect } from './files.js';
import { toggleEmojiPicker } from './emoji.js';
import { setupVoiceRecording } from './voice.js';
import { selectChat, handleTabClick } from './chats.js';
import { connection, emojiPickerVisible } from './variables.js';

export async function initChat() {
    window.currentUserId = parseInt(document.getElementById('currentUserId')?.value || '0');
    console.log('🔍 Current User ID:', window.currentUserId);

    if (window.currentUserId === 0) {
        console.error('❌ Current User ID = 0');
        return;
    }

    console.log('🚀 Initializing chat...');

    toggleMessageInput(false);

    await loadMessageSettings().catch(err => console.warn('⚠️ Settings load failed:', err));

    const conn = await setupSignalR();
    setConnection(conn);

    // ✅ setupEventListeners را اینجا inline بنویسید
    setupEventListenersInline();

    setupScrollListener();

    window.addEventListener('focus', function () {
        setIsPageFocused(true);
        console.log('🟢 Page focused');
        if (currentChat) {
            markMessagesAsRead();
            removeUnreadSeparator();
        }
    });

    window.addEventListener('blur', function () {
        setIsPageFocused(false);
        console.log('🔴 Page blurred');
    });

    console.log('✅ Init complete');
}

function setupEventListenersInline() {
    console.log('🎯 Setting up event listeners...');

    const sendBtn = document.getElementById('sendBtn');
    if (sendBtn) {
        sendBtn.addEventListener('click', sendMessage);
    }

    const messageInput = document.getElementById('messageInput');
    if (messageInput) {
        messageInput.addEventListener('keydown', function (e) {
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                sendMessage();
            }
        });

        messageInput.addEventListener('input', function () {
            this.style.height = 'auto';
            const maxHeight = 120;
            const newHeight = Math.min(this.scrollHeight, maxHeight);
            this.style.height = newHeight + 'px';

            if (this.scrollHeight > maxHeight) {
                this.style.overflowY = 'auto';
            } else {
                this.style.overflowY = 'hidden';
            }
        });
    }

    const attachBtn = document.getElementById('attachBtn');
    if (attachBtn) {
        attachBtn.addEventListener('click', () => {
            document.getElementById('fileInput')?.click();
        });
    }

    const fileInput = document.getElementById('fileInput');
    if (fileInput) {
        fileInput.addEventListener('change', handleFileSelect);
    }

    const emojiBtn = document.getElementById('emojiBtn');
    if (emojiBtn) {
        emojiBtn.addEventListener('click', (e) => {
            e.stopPropagation();
            toggleEmojiPicker();
        });
    }

    document.addEventListener('click', function (e) {
        const chatItem = e.target.closest('.chat-item');
        if (chatItem) {
            selectChat(chatItem);
            return;
        }

        const tabBtn = e.target.closest('.tab-btn');
        if (tabBtn) {
            handleTabClick(tabBtn);
            return;
        }

        const isEmojiBtn = e.target.closest('#emojiBtn');
        const isPickerContainer = e.target.closest('#emojiPickerContainer');
        if (!isEmojiBtn && !isPickerContainer && emojiPickerVisible) {
            toggleEmojiPicker();
        }

        if (!e.target.closest('.message-menu')) {
            document.querySelectorAll('.message-menu-dropdown').forEach(m => {
                m.style.display = 'none';
                m.closest('.message')?.classList.remove('menu-open');
            });
        }
    });

    setupVoiceRecording();

    console.log('✅ Event listeners attached');
}

export function toggleMessageInput(show) {
    const inputArea = document.getElementById('messageInputArea');
    if (!inputArea) return;

    console.log('🔄 toggleMessageInput:', show);

    if (show) {
        inputArea.classList.add('show');
        inputArea.style.display = 'flex';
    } else {
        inputArea.classList.remove('show');
        inputArea.style.display = 'none';
    }
}