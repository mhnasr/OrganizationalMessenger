import { setConnection, setIsPageFocused, currentChat } from './variables.js';
import { setupSignalR } from './signalr.js';
import { markMessagesAsRead, removeUnreadSeparator, loadMessageSettings } from './messages.js';
import { setupScrollListener } from './message-handlers.js';
import './message-menu.js';
import './forward.js';
import './reply.js';
import './reactions.js';

// ✅ Import ماژول‌های Group/Channel
import './modules/group/group-manager.js';
import './modules/channel/channel-manager.js';

export async function initChat() {
    window.currentUserId = parseInt(document.getElementById('currentUserId')?.value || '0');
    console.log('🔍 Current User ID:', window.currentUserId);

    if (window.currentUserId === 0) {
        console.error('❌ Current User ID = 0');
        return;
    }

    console.log('🚀 Initializing chat...');

    await loadMessageSettings();
    console.log('✅ Message settings loaded');

    const conn = await setupSignalR();
    setConnection(conn);

    setupEventListeners();
    setupScrollListener();
    setupCreateMenu(); // ✅ فعال‌سازی منوی ایجاد

    window.addEventListener('focus', function () {
        setIsPageFocused(true);
        if (currentChat) {
            markMessagesAsRead();
            removeUnreadSeparator();
        }
    });

    window.addEventListener('blur', function () {
        setIsPageFocused(false);
    });

    console.log('✅ Init complete');
}

async function setupEventListeners() {
    console.log('🎯 Setting up event listeners...');

    const { selectChat, handleTabClick } = await import('./chats.js');
    const { sendMessage } = await import('./reply.js');
    const { handleFileSelect } = await import('./files.js');
    const { toggleEmojiPicker } = await import('./emoji.js');
    const { setupVoiceRecording } = await import('./voice.js');

    const sendBtn = document.getElementById('sendBtn');
    if (sendBtn) {
        sendBtn.addEventListener('click', () => {
            sendMessage();
            setTimeout(() => {
                removeUnreadSeparator();
            }, 500);
        });
    }

    const messageInput = document.getElementById('messageInput');
    if (messageInput) {
        messageInput.addEventListener('keydown', function (e) {
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                sendMessage();
                setTimeout(() => {
                    removeUnreadSeparator();
                }, 500);
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
    });

    setupVoiceRecording();

    console.log('✅ Event listeners attached');
}

// ✅ Setup منوی ایجاد
function setupCreateMenu() {
    const createMenuBtn = document.getElementById('createMenuBtn');
    const createMenu = document.getElementById('createMenu');

    if (createMenuBtn && createMenu) {
        createMenuBtn.addEventListener('click', (e) => {
            e.stopPropagation();
            const isVisible = createMenu.style.display === 'block';
            createMenu.style.display = isVisible ? 'none' : 'block';
        });

        // بستن با کلیک بیرون
        document.addEventListener('click', (e) => {
            if (!createMenu.contains(e.target) && e.target !== createMenuBtn) {
                createMenu.style.display = 'none';
            }
        });

        console.log('✅ Create menu initialized');
    }
}

export function toggleMessageInput(show) {
    const inputArea = document.getElementById('messageInputArea');
    if (inputArea) {
        inputArea.style.display = show ? 'flex' : 'none';
    }
}