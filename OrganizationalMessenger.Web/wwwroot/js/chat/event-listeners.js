// ============================================
// Event Listeners Setup
// ============================================

import { sendMessage } from './reply.js';
import { handleFileSelect } from './files.js';
import { toggleEmojiPicker } from './emoji.js';
import { setupVoiceRecording } from './voice.js';
import { selectChat, handleTabClick } from './chats.js';
import { connection, currentChat, emojiPickerVisible } from './variables.js';

export function setupEventListeners() {
    console.log('🎯 Setting up event listeners...');

    // Send Button
    const sendBtn = document.getElementById('sendBtn');
    if (sendBtn) {
        sendBtn.addEventListener('click', sendMessage);
        console.log('✅ Send button listener attached');
    }

    // Message Input
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

            handleTyping();
        });
        console.log('✅ Message input listeners attached');
    }

    // Attach Button
    const attachBtn = document.getElementById('attachBtn');
    if (attachBtn) {
        attachBtn.addEventListener('click', () => {
            document.getElementById('fileInput')?.click();
        });
        console.log('✅ Attach button listener attached');
    }

    // File Input
    const fileInput = document.getElementById('fileInput');
    if (fileInput) {
        fileInput.addEventListener('change', handleFileSelect);
        console.log('✅ File input listener attached');
    }

    // Emoji Button
    const emojiBtn = document.getElementById('emojiBtn');
    if (emojiBtn) {
        emojiBtn.addEventListener('click', (e) => {
            e.stopPropagation();
            toggleEmojiPicker();
        });
        console.log('✅ Emoji button listener attached');
    }

    // Global Click Handler
    document.addEventListener('click', function (e) {
        // Chat Item Click
        const chatItem = e.target.closest('.chat-item');
        if (chatItem) {
            console.log('🖱️ Chat item clicked');
            selectChat(chatItem);
            return;
        }

        // Tab Button Click
        const tabBtn = e.target.closest('.tab-btn');
        if (tabBtn) {
            handleTabClick(tabBtn);
            return;
        }

        // Close Emoji Picker
        const isEmojiBtn = e.target.closest('#emojiBtn');
        const isPickerContainer = e.target.closest('#emojiPickerContainer');
        if (!isEmojiBtn && !isPickerContainer && emojiPickerVisible) {
            toggleEmojiPicker();
        }

        // Close Message Menu
        if (!e.target.closest('.message-menu')) {
            document.querySelectorAll('.message-menu-dropdown').forEach(m => {
                m.style.display = 'none';
                m.closest('.message')?.classList.remove('menu-open');
            });
        }
    });
    console.log('✅ Global click listener attached');

    // Voice Recording
    setupVoiceRecording();

    console.log('✅ All event listeners attached successfully');
}

function handleTyping() {
    if (!currentChat || !connection || currentChat.type !== 'private') return;

    if (window.typingTimerGlobal) {
        clearTimeout(window.typingTimerGlobal);
    }

    if (connection.state === signalR.HubConnectionState.Connected) {
        connection.invoke("SendTyping", currentChat.id).catch(err => {
            console.error('❌ SendTyping error:', err);
        });

        window.typingTimerGlobal = setTimeout(() => {
            if (connection.state === signalR.HubConnectionState.Connected) {
                connection.invoke("SendStoppedTyping", currentChat.id).catch(err => {
                    console.error('❌ SendStoppedTyping error:', err);
                });
            }
        }, 2000);
    }
}