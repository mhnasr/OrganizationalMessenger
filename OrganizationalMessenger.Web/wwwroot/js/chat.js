// ✅ متغیرهای سراسری
let connection = null;
let currentChat = null;
let typingTimer = null;
let emojiPickerVisible = false;
let lastSeenMessageId = null;
let isLoadingMessages = false;
let hasMoreMessages = true;
let lastSenderId = null;
let messageGroupCount = 0;

let multiSelectMode = false;
let selectedMessages = new Set();

let replyingToMessage = null;

// ✅ متغیر برای تشخیص فوکوس
let isPageFocused = true;
let isTyping = false;

// ✅ متغیرهای Zoom
let isZoomed = false;
let currentPreviewImage = null;

// ✅ متغیرهای Voice Recording
let mediaRecorder = null;
let audioChunks = [];
let isRecording = false;
let recordingStartTime = null;
let recordingTimer = null;
let currentPlayingAudio = null;
let waveformInterval = null;




let messageSettings = {
    allowEdit: true,
    allowDelete: true,
    editTimeLimit: 3600,
    deleteTimeLimit: 7200
};

const MessageStatus = { Sent: 1, Delivered: 2, Read: 3 };

// ✅ DOM آماده شد
document.addEventListener('DOMContentLoaded', function () {
    console.log('📄 DOM Loaded');
    initChat();
});

// ✅ تابع اصلی راه‌اندازی
async function initChat() {
    window.currentUserId = parseInt(document.getElementById('currentUserId')?.value || '0');
    console.log('🔍 Current User ID:', window.currentUserId);

    if (window.currentUserId === 0) {
        console.error('❌ Current User ID = 0');
        return;
    }

    console.log('🚀 Initializing chat...');

    toggleMessageInput(false);

    // بارگذاری تنظیمات (async)
    loadMessageSettings().catch(err => console.warn('⚠️ Settings load failed:', err));

    // راه‌اندازی SignalR
    setupSignalR();

    // Setup event listeners
    setupEventListeners();
    setupScrollListener();

    console.log('✅ Init complete');
}

// ✅ دریافت تنظیمات
// ✅ چک کردن تنظیمات در Console
async function loadMessageSettings() {
    try {
        const response = await fetch('/Chat/GetMessageSettings');

        if (!response.ok) {
            console.warn('⚠️ Settings API not available, using defaults');
            messageSettings = {
                allowEdit: true,
                allowDelete: true,
                editTimeLimit: 3600,
                deleteTimeLimit: 7200
            };
            return;
        }

        const result = await response.json();

        if (result && result.success) {
            messageSettings = {
                allowEdit: result.allowEdit || false,
                allowDelete: result.allowDelete || false,
                editTimeLimit: result.editTimeLimit || 3600,
                deleteTimeLimit: result.deleteTimeLimit || 7200
            };
            console.log('✅ Message settings loaded:', messageSettings);
        }
    } catch (error) {
        console.warn('⚠️ Load settings error:', error.message);
        messageSettings = {
            allowEdit: true,
            allowDelete: true,
            editTimeLimit: 3600,
            deleteTimeLimit: 7200
        };
    }
}
// ✅ چک کردن تنظیمات در Console
async function loadMessages(append = false) {
    if (!currentChat) return;
    if (isLoadingMessages) return;

    isLoadingMessages = true;

    try {
        let url = `/Chat/GetMessages?pageSize=50`;

        if (currentChat.type === 'private') {
            url += `&userId=${currentChat.id}`;
        } else if (currentChat.type === 'group') {
            url += `&groupId=${currentChat.id}`;
        }

        if (append) {
            const firstMessage = document.querySelector('#messagesContainer .message[data-message-id]');
            if (firstMessage) {
                const oldestId = firstMessage.dataset.messageId;
                url += `&beforeMessageId=${oldestId}`;
            }
        }

        const response = await fetch(url);
        const data = await response.json();

        const container = document.getElementById('messagesContainer');

        if (!append) {
            container.innerHTML = '';
            lastSenderId = null;
            messageGroupCount = 0;
        }

        const previousScrollHeight = container.scrollHeight;

        if (append && data.messages.length > 0) {
            const existingMessages = container.innerHTML;
            container.innerHTML = '';
            data.messages.forEach(msg => displayMessage(msg));
            container.innerHTML += existingMessages;

            const newScrollHeight = container.scrollHeight;
            container.scrollTop = newScrollHeight - previousScrollHeight;
        } else {
            // ✅ اگر صفحه فوکوس دارد، separator نمایش نده
            const unreadMessages = data.messages.filter(msg =>
                msg.senderId !== window.currentUserId && !msg.isRead && !msg.isDeleted
            );

            const shouldShowSeparator = !isPageFocused && unreadMessages.length > 0;
            let unreadSeparatorAdded = false;

            if (shouldShowSeparator) {
                const firstUnreadId = unreadMessages[0].id;

                data.messages.forEach((msg) => {
                    if (!unreadSeparatorAdded && firstUnreadId && msg.id === firstUnreadId) {
                        addUnreadSeparator(container, unreadMessages.length);
                        unreadSeparatorAdded = true;
                    }
                    displayMessage(msg);
                });
            } else {
                data.messages.forEach(msg => displayMessage(msg));
            }

            scrollToBottom();
        }

        hasMoreMessages = data.hasMore;

        console.log(`✅ Loaded ${data.messages.length} messages, hasMore: ${hasMoreMessages}`);
    } catch (error) {
        console.error('❌ Load messages error:', error);
    } finally {
        isLoadingMessages = false;
    }
}
// ✅ راه‌اندازی SignalR
function setupSignalR() {
    console.log('🔌 Setting up SignalR...');

    try {
        connection = new signalR.HubConnectionBuilder()
            .withUrl('/chatHub', {
                accessTokenFactory: () => getCsrfToken()
            })
            .withAutomaticReconnect()
            .configureLogging(signalR.LogLevel.Information)
            .build();

        console.log('✅ SignalR HubConnection created');

        // Event handlers
        connection.on("ReceiveMessage", (data) => handleReceiveMessage(data));
        connection.on("MessageSent", (data) => handleMessageSent(data));
        connection.on("MessageDelivered", (data) => updateMessageStatus(data.messageId, 'delivered'));

        connection.on("MessageRead", (data) => {
            const msgEl = document.querySelector(`[data-message-id="${data.messageId}"]`);
            if (!msgEl) return;

            updateMessageStatus(data.messageId, 'read', data.readAt);

            const sendInfoEl = msgEl.querySelector('.sent-info');
            if (sendInfoEl) {
                const readTime = formatPersianTime(data.readAt);
                if (!sendInfoEl.textContent.includes('مشاهده')) {
                    sendInfoEl.innerHTML += `&nbsp;&nbsp; مشاهده: ${readTime}`;
                }
            }
        });

      

        connection.on("UserOnline", (userId) => {
            console.log('🟢 User online:', userId);

            if (currentChat?.id == userId && currentChat.type === 'private') {
                document.querySelectorAll('#messagesContainer .message.sent').forEach(msgEl => {
                    const msgId = parseInt(msgEl.dataset.messageId);
                    if (msgId && !msgEl.querySelector('.double-blue')) {
                        updateMessageStatus(msgId, 'delivered');
                    }
                });
            }

            markUserOnline(userId);
        });

        connection.on("UserOffline", (userId, lastSeen) => markUserOffline(userId, lastSeen));
        connection.on("UserTyping", (message) => showTypingIndicator(message));
        connection.on("UserStoppedTyping", () => hideTypingIndicator());
        connection.on("Error", (error) => showError(error));

        console.log('✅ SignalR event handlers registered');

        // شروع اتصال
        connection.start()
            .then(() => {
                console.log('✅ SignalR Connected');
                loadChats('all');
            })
            .catch(err => {
                console.error('❌ SignalR Connection Error:', err);
                loadChats('all');
            });



        // در setupSignalR:
        // اضافه کنید به setupSignalR:
        connection.on("MessageDeleted", (data) => {
            console.log('🗑️ MessageDeleted received:', {
                messageId: data.messageId,
                showNotice: data.showNotice,
                currentUserId: window.currentUserId,
                senderId: data.senderId,
                receiverId: data.receiverId
            });

            const messageEl = document.querySelector(`[data-message-id="${data.messageId}"]`);
            console.log('🔍 Message element found:', !!messageEl);

            if (!messageEl) {
                console.warn('⚠️ Message element not found in DOM');
                return;
            }

            if (data.showNotice) {
                console.log('📝 Mode: WhatsApp (show notice)');
                replaceWithDeletedNotice(messageEl);
            } else {
                console.log('🗑️ Mode: Telegram (remove completely)');
                messageEl.style.animation = 'fadeOut 0.3s ease';
                setTimeout(() => {
                    messageEl.remove();
                    console.log('✅ Message removed from DOM');
                }, 300);
            }
        });

        connection.on("MessageEdited", (data) => {
            console.log('✏️ MessageEdited:', data);

            const messageEl = document.querySelector(`[data-message-id="${data.messageId}"]`);
            if (!messageEl) return;

            // ✅ به‌روزرسانی متن
            const textEl = messageEl.querySelector('[data-editable="true"]');
            if (textEl) {
                textEl.textContent = data.newContent;
            }

            // ✅ اضافه کردن "ویرایش شده" در همه جاها
            const sentInfo = messageEl.querySelector('.sent-info');
            const messageTime = messageEl.querySelector('.message-time');

            // حذف بج قدیمی (اگر وجود داشت)
            messageEl.querySelectorAll('.edited-badge').forEach(badge => badge.remove());

            // اضافه کردن بج جدید
            const editedBadge = '<span class="edited-badge">ویرایش شده</span>';

            if (sentInfo) {
                sentInfo.insertAdjacentHTML('beforeend', editedBadge);
            } else if (messageTime) {
                messageTime.insertAdjacentHTML('beforeend', ' ' + editedBadge);
            }
        });





    } catch (error) {
        console.error('❌ SignalR Setup Error:', error);
        loadChats('all');
    }
}

// ✅ بارگذاری لیست چت‌ها
async function loadChats(tab = 'all') {
    console.log('📋 Loading chats, tab:', tab);

    try {
        const response = await fetch(`/Chat/GetChats?tab=${tab}`);

        if (!response.ok) {
            console.error('❌ GetChats failed:', response.status);
            return;
        }

        const chats = await response.json();
        console.log('✅ Chats received:', chats.length);

        const container = document.getElementById('chatList');
        if (!container) {
            console.error('❌ chatList container not found');
            return;
        }

        container.innerHTML = '';
        chats.forEach(chat => renderChatItem(chat));

        console.log('✅ Chat list rendered');
    } catch (error) {
        console.error('❌ Load chats error:', error);
    }
}

function toggleMessageInput(show) {
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

function renderChatItem(chat) {
    const container = document.getElementById('chatList');
    const chatEl = document.createElement('div');
    chatEl.className = `chat-item ${chat.type} ${currentChat?.id == chat.id ? 'active' : ''}`;
    chatEl.dataset.chatId = chat.id;
    chatEl.dataset.chatType = chat.type;

    const unreadBadge = chat.unreadCount > 0
        ? `<span class="unread-badge">${chat.unreadCount > 99 ? '99+' : chat.unreadCount}</span>`
        : '';

    let avatarHtml = '';
    if (chat.avatar) {
        avatarHtml = `<img src="${chat.avatar}" class="chat-avatar-img" alt="${escapeHtml(chat.name)}" />`;
    } else {
        avatarHtml = `<div class="chat-avatar-initials">${getInitials(chat.name)}</div>`;
    }

    chatEl.innerHTML = `
        <div class="chat-avatar ${chat.isOnline ? 'online' : ''}">
            ${avatarHtml}
        </div>
        <div class="chat-info">
            <div class="chat-name-row">
                <span class="chat-name">${escapeHtml(chat.name)}</span>
                ${unreadBadge}
            </div>
            <div class="chat-preview">
                <span class="message-time">${formatPersianTime(chat.lastMessageTime)}</span>
            </div>
        </div>
    `;
    container.appendChild(chatEl);
}


async function selectChat(chatEl) {
    console.log('🔄 Selecting chat:', chatEl.dataset.chatId);

    lastSenderId = null;
    messageGroupCount = 0;
    hasMoreMessages = true;
    isPageFocused = true; // ✅ فرض می‌کنیم فوکوس داریم

    document.querySelectorAll('.chat-item').forEach(item => item.classList.remove('active'));
    chatEl.classList.add('active');

    currentChat = {
        id: parseInt(chatEl.dataset.chatId),
        type: chatEl.dataset.chatType
    };

    const inputArea = document.getElementById('messageInputArea');
    inputArea.style.display = 'flex';
    inputArea.classList.add('show');

    document.getElementById('chatTitle').textContent =
        chatEl.querySelector('.chat-name')?.textContent || 'چت';
    document.querySelectorAll('#chatTopHeader button').forEach(btn => {
        btn.style.display = 'flex';
    });

    await loadMessages(false);

    // ✅ بلافاصله پیام‌ها را read کن (چون داریم چت را باز می‌کنیم)
    await markMessagesAsRead();

    // ✅ اسکرول - separator نمایش نده
    setTimeout(() => {
        scrollToBottom();
    }, 100);
}
function removeUnreadSeparator() {
    const separator = document.querySelector('.unread-separator');
    if (separator) {
        separator.style.animation = 'fadeOut 0.4s ease';
        setTimeout(() => {
            separator.remove();
        }, 400);
    }
}



async function loadMessages(append = false) {
    if (!currentChat) return;
    if (isLoadingMessages) return;

    isLoadingMessages = true;

    try {
        let url = `/Chat/GetMessages?pageSize=50`;

        if (currentChat.type === 'private') {
            url += `&userId=${currentChat.id}`;
        } else if (currentChat.type === 'group') {
            url += `&groupId=${currentChat.id}`;
        }

        if (append) {
            const firstMessage = document.querySelector('#messagesContainer .message[data-message-id]');
            if (firstMessage) {
                const oldestId = firstMessage.dataset.messageId;
                url += `&beforeMessageId=${oldestId}`;
            }
        }

        const response = await fetch(url);
        const data = await response.json();

        const container = document.getElementById('messagesContainer');

        if (!append) {
            container.innerHTML = '';
            lastSenderId = null;
            messageGroupCount = 0;
        }

        const previousScrollHeight = container.scrollHeight;

        if (append && data.messages.length > 0) {
            const existingMessages = container.innerHTML;
            container.innerHTML = '';
            data.messages.forEach(msg => displayMessage(msg));
            container.innerHTML += existingMessages;

            const newScrollHeight = container.scrollHeight;
            container.scrollTop = newScrollHeight - previousScrollHeight;
        } else {
            // ✅ اگر صفحه فوکوس دارد، separator نمایش نده
            const unreadMessages = data.messages.filter(msg =>
                msg.senderId !== window.currentUserId && !msg.isRead && !msg.isDeleted
            );

            const shouldShowSeparator = !isPageFocused && unreadMessages.length > 0;
            let unreadSeparatorAdded = false;

            if (shouldShowSeparator) {
                const firstUnreadId = unreadMessages[0].id;

                data.messages.forEach((msg) => {
                    if (!unreadSeparatorAdded && firstUnreadId && msg.id === firstUnreadId) {
                        addUnreadSeparator(container, unreadMessages.length);
                        unreadSeparatorAdded = true;
                    }
                    displayMessage(msg);
                });
            } else {
                data.messages.forEach(msg => displayMessage(msg));
            }

            scrollToBottom();
        }

        hasMoreMessages = data.hasMore;

        console.log(`✅ Loaded ${data.messages.length} messages, hasMore: ${hasMoreMessages}`);
    } catch (error) {
        console.error('❌ Load messages error:', error);
    } finally {
        isLoadingMessages = false;
    }
}



function setupScrollListener() {
    const container = document.getElementById('messagesContainer');
    if (!container) return;

    container.addEventListener('scroll', async function () {
        if (container.scrollTop < 100 && hasMoreMessages && !isLoadingMessages) {
            console.log('🔄 Loading more messages...');
            await loadMessages(true);
        }
    });

    console.log('✅ Scroll listener attached');
}

function addUnreadSeparator(container, count) {
    const separator = document.createElement('div');
    separator.className = 'unread-separator';
    separator.innerHTML = `
        <div class="unread-line"></div>
        <span class="unread-label">${count} پیام خوانده نشده</span>
        <div class="unread-line"></div>
    `;
    container.appendChild(separator);
}

async function sendMessage() {
    const input = document.getElementById('messageInput');
    const text = input.value.trim();

    if (!text || !currentChat || !connection) return;

    if (currentChat.type === 'private') {
        await connection.invoke("SendPrivateMessage", currentChat.id, text);
    }

    input.value = '';
    input.style.height = 'auto';
    input.style.overflowY = 'hidden';

    hideTypingIndicator();
}

function handleTyping() {
    if (!currentChat || !connection || currentChat.type !== 'private') return;

    if (typingTimer) clearTimeout(typingTimer);

    connection.invoke("SendTyping", currentChat.id);

    typingTimer = setTimeout(() => {
        connection.invoke("SendStoppedTyping", currentChat.id);
    }, 2000);
}

function updateMessageStatus(messageId, status, readAt = null) {
    const messageEl = document.querySelector(`[data-message-id="${messageId}"]`);
    if (!messageEl?.classList.contains('sent')) return;

    const sendInfoEl = messageEl.querySelector('.sent-info');
    if (!sendInfoEl) return;

    const sendTimeMatch = sendInfoEl.textContent.match(/ارسال:\s*(\d{1,2}:\d{2})/);
    const sendTime = sendTimeMatch ? sendTimeMatch[1] : formatPersianTime(new Date());

    // ✅ چک کردن وجود بج "ویرایش شده"
    const hasEditedBadge = sendInfoEl.querySelector('.edited-badge');
    const editedBadgeHtml = hasEditedBadge ? hasEditedBadge.outerHTML : '';

    let newStatusHtml = '';
    if (status === 'read' && readAt) {
        const readTime = formatPersianTime(readAt);
        newStatusHtml = `
            <div class="sent-info">
                ارسال: ${sendTime} &nbsp;&nbsp; مشاهده: ${readTime}
                <span class="tick double-blue">✓✓</span>
                ${editedBadgeHtml}
            </div>
        `;
    } else if (status === 'delivered') {
        newStatusHtml = `
            <div class="sent-info">
                ارسال: ${sendTime}
                <span class="tick double-gray">✓✓</span>
                ${editedBadgeHtml}
            </div>
        `;
    } else {
        newStatusHtml = `
            <div class="sent-info">
                ارسال: ${sendTime}
                <span class="tick single">✓</span>
                ${editedBadgeHtml}
            </div>
        `;
    }

    sendInfoEl.outerHTML = newStatusHtml;
}
function markUserOnline(userId) {
    document.querySelectorAll('.chat-item').forEach(item => {
        if (parseInt(item.dataset.chatId) === userId) {
            item.querySelector('.chat-avatar')?.classList.add('online');
        }
    });
}

function markUserOffline(userId, lastSeen) {
    document.querySelectorAll('.chat-item').forEach(item => {
        if (parseInt(item.dataset.chatId) === userId) {
            item.querySelector('.chat-avatar')?.classList.remove('online');
        }
    });
}

function showTypingIndicator(message) {
    const typingEl = document.getElementById('typingIndicator');
    if (typingEl && currentChat?.type === 'private') {
        typingEl.textContent = message;
        typingEl.style.display = 'block';
    }
}

function hideTypingIndicator() {
    const typingEl = document.getElementById('typingIndicator');
    if (typingEl) typingEl.style.display = 'none';
}

function scrollToBottom() {
    const container = document.getElementById('messagesContainer');
    container.scrollTop = container.scrollHeight;
}

function formatPersianTime(dateStr) {
    const date = new Date(dateStr);
    return date.toLocaleTimeString('fa-IR', {
        hour: 'numeric',
        minute: '2-digit'
    });
}

function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

function getCsrfToken() {
    return document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
}

function showError(message) {
    console.error('❌', message);
}

async function markMessagesAsRead() {
    if (!currentChat?.id || currentChat.type !== 'private') return;

    const unreadReceivedIds = Array.from(
        document.querySelectorAll('#messagesContainer .message.received[data-message-id]')
    )
        .filter(el => !el.querySelector('.double-blue'))
        .map(el => parseInt(el.dataset.messageId))
        .filter(id => id);

    if (unreadReceivedIds.length === 0) {
        removeUnreadBadge();
        return;
    }

    try {
        await fetch('/Chat/MarkMessagesAsRead', {
            method: 'POST',
            headers: {
                'RequestVerificationToken': getCsrfToken(),
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ messageIds: unreadReceivedIds })
        });

        if (connection?.state === signalR.HubConnectionState.Connected) {
            await connection.invoke("NotifyMessagesRead", unreadReceivedIds);
        }

        removeUnreadBadge();
    } catch (error) {
        console.error('❌ Mark as read error:', error);
    }
}

function removeUnreadBadge() {
    if (!currentChat) return;

    const chatItem = document.querySelector(`.chat-item[data-chat-id="${currentChat.id}"]`);
    if (!chatItem) return;

    const badge = chatItem.querySelector('.unread-badge');
    if (badge) {
        badge.style.animation = 'fadeOutBadge 0.3s ease';
        setTimeout(() => {
            badge.remove();
        }, 300);
    }
}

function handleReceiveMessage(data) {
    console.log('📨 ReceiveMessage:', data);

    const isCurrentChat = currentChat &&
        (currentChat.id == data.chatId || currentChat.id == data.senderId);

    if (isCurrentChat) {
        // ✅ فقط وقتی صفحه فوکوس ندارد separator نمایش بده
        if (!isPageFocused || document.hidden) {
            const existingSeparator = document.querySelector('.unread-separator');
            if (!existingSeparator) {
                const container = document.getElementById('messagesContainer');
                addUnreadSeparator(container, 1);
            }
        }

        displayMessage(data);
        scrollToBottom();

        // ✅ اگر صفحه فوکوس دارد، فوراً read کن
        if (isPageFocused && !document.hidden) {
            setTimeout(() => {
                markMessagesAsRead();
                removeUnreadSeparator(); // ✅ حذف separator
            }, 100);
        } else {
            // فقط delivery
            setTimeout(() => {
                if (connection?.state === signalR.HubConnectionState.Connected) {
                    connection.invoke("ConfirmDelivery", data.id);
                }
            }, 100);
        }
    } else {
        loadChats();
        showNotification(data.senderName, data.content);
    }
}


function renderFileAttachment(file) {
    // ✅ چک کردن fileType
    const fileType = file.fileType || 'Document';
    console.log('🔍 Rendering file:', file.originalFileName, 'Type:', fileType);

    if (fileType === 'Image') {
        return `
            <div class="message-file image-file">
                <img src="${file.thumbnailUrl || file.fileUrl}" 
                     alt="${file.originalFileName}" 
                     onclick="openImagePreview('${file.fileUrl}')"
                     loading="lazy"
                     style="cursor: pointer;">
                <a href="/api/File/download/${file.id}" 
                   class="file-download-btn" 
                   title="دانلود"
                   onclick="event.stopPropagation()">
                    <i class="fas fa-download"></i>
                </a>
            </div>
        `;
    }
    else if (fileType === 'Video') {
        return `
            <div class="message-file video-file">
                <video controls 
                       preload="metadata" 
                       style="max-width: 400px; width: 100%; border-radius: 12px;">
                    <source src="${file.fileUrl}" type="video/mp4">
                    مرورگر شما از پخش ویدیو پشتیبانی نمی‌کند.
                </video>
                <div class="video-info">
                    <span class="file-name">${file.originalFileName}</span>
                    <span class="file-size">${file.readableSize}</span>
                </div>
                <a href="/api/File/download/${file.id}" 
                   class="file-download-btn" 
                   title="دانلود">
                    <i class="fas fa-download"></i>
                </a>
            </div>
        `;
    }
    else if (fileType === 'Audio') {
        return renderAudioPlayer(file); // ✅
    }
    else {
        const icon = getFileIcon(fileType, file.extension);
        return `
            <div class="message-file document-file">
                <i class="${icon}"></i>
                <div class="file-info">
                    <span class="file-name">${file.originalFileName}</span>
                    <span class="file-size">${file.readableSize}</span>
                </div>
                <a href="/api/File/download/${file.id}" 
                   class="file-download-btn" 
                   title="دانلود">
                    <i class="fas fa-download"></i>
                </a>
            </div>
        `;
    }
}

function openVideoPreview(url, fileName) {
    const modal = document.createElement('div');
    modal.className = 'video-preview-modal';
    modal.innerHTML = `
        <div class="video-preview-overlay" onclick="closeVideoPreview(event)">
            <div class="video-preview-container" onclick="event.stopPropagation()">
                <div class="video-preview-header">
                    <span class="video-title">${escapeHtml(fileName)}</span>
                    <button class="close-preview" onclick="this.closest('.video-preview-modal').remove()">✕</button>
                </div>
                <video controls autoplay style="width: 100%; max-height: 80vh; border-radius: 8px;">
                    <source src="${url}" type="video/mp4">
                    <source src="${url}" type="video/webm">
                    <source src="${url}" type="video/quicktime">
                    مرورگر شما از پخش ویدیو پشتیبانی نمی‌کند.
                </video>
                <div class="video-preview-controls">
                    <a href="${url}" download="${fileName}" class="download-video-btn">
                        <i class="fas fa-download"></i> دانلود ویدیو
                    </a>
                </div>
            </div>
        </div>
    `;
    document.body.appendChild(modal);
    document.body.style.overflow = 'hidden';
}

function closeVideoPreview(event) {
    if (event.target.classList.contains('video-preview-overlay')) {
        event.target.closest('.video-preview-modal').remove();
        document.body.style.overflow = 'auto';
    }
}



function getFileIcon(fileType, extension) {
    const iconMap = {
        'Video': 'fas fa-file-video',
        'Audio': 'fas fa-file-audio',
        'Document': 'fas fa-file-alt',
        'Archive': 'fas fa-file-archive'
    };

    if (fileType in iconMap) return iconMap[fileType];

    if (['.pdf'].includes(extension)) return 'fas fa-file-pdf';
    if (['.doc', '.docx'].includes(extension)) return 'fas fa-file-word';
    if (['.xls', '.xlsx'].includes(extension)) return 'fas fa-file-excel';

    return 'fas fa-file';
}

function handleMessageSent(data) {
    console.log('✅ MessageSent received:', data);

    // حذف پیام‌های موقت
    const tempMessages = document.querySelectorAll('.message[data-temp="true"]');
    tempMessages.forEach(msg => msg.remove());

    // ✅ اطمینان از وجود sentAt
    if (!data.sentAt) {
        console.warn('⚠️ No sentAt, using current time');
        data.sentAt = new Date().toISOString();
    } else {
        try {
            const date = new Date(data.sentAt);

            // ✅ چک معتبر بودن تاریخ
            if (isNaN(date.getTime())) {
                console.error('❌ Invalid sentAt:', data.sentAt);
                data.sentAt = new Date().toISOString();
            } else {
                // ✅ چک اینکه تاریخ در آینده نباشد
                const now = new Date();
                if (date > now) {
                    console.warn('⚠️ sentAt is in future, using current time');
                    data.sentAt = now.toISOString();
                } else {
                    data.sentAt = date.toISOString();
                }
            }
        } catch (e) {
            console.error('❌ sentAt parse error:', e);
            data.sentAt = new Date().toISOString();
        }
    }

    console.log('📅 Corrected sentAt:', data.sentAt);
    console.log('🕐 Current time:', new Date().toISOString());

    // نمایش پیام با منوی کامل
    displayMessage(data);
    scrollToBottom();
}


function handleTabClick(tabBtn) {
    const tab = tabBtn.dataset.tab;

    document.querySelectorAll('.tab-btn').forEach(btn => {
        btn.classList.remove('active');
    });
    tabBtn.classList.add('active');

    loadChats(tab);
}

function showNotification(title, body) {
    if (Notification.permission === 'granted') {
        new Notification(title, { body });
    }
}

function getInitials(name) {
    return name.split(' ').slice(0, 2).map(n => n[0]).join('').toUpperCase();
}




function setupEventListeners() {
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

            handleTyping();
        });
    }

    const attachBtn = document.getElementById('attachBtn');
    if (attachBtn) {
        attachBtn.addEventListener('click', () => {
            document.getElementById('fileInput').click();
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
            console.log('🖱️ Chat item clicked');
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
            hideEmojiPicker();
        }

        if (!e.target.closest('.message-menu')) {
            document.querySelectorAll('.message-menu-dropdown').forEach(m => {
                m.style.display = 'none';
            });
        }
    });



    setupVoiceRecording();

    console.log('✅ Event listeners attached');
}

async function handleFileSelect(e) {
    const files = e.target.files;
    if (!files || files.length === 0) return;

    const file = files[0];

    if (file.size > 100 * 1024 * 1024) {
        alert('حجم فایل نباید بیشتر از 100 مگابایت باشد');
        e.target.value = '';
        return;
    }

    showCaptionDialog(file);
    e.target.value = '';
}

function showCaptionDialog(file) {
    const existingDialog = document.getElementById('captionDialog');
    if (existingDialog) existingDialog.remove();

    const dialog = document.createElement('div');
    dialog.id = 'captionDialog';
    dialog.className = 'caption-dialog-overlay';

    let previewHtml = '';
    if (file.type.startsWith('image/')) {
        const imageUrl = URL.createObjectURL(file);
        previewHtml = `<img src="${imageUrl}" class="file-preview-image" alt="Preview">`;
    } else if (file.type.startsWith('video/')) {
        const videoUrl = URL.createObjectURL(file);
        previewHtml = `<video src="${videoUrl}" class="file-preview-video" controls></video>`;
    } else {
        previewHtml = `
            <div class="file-preview-icon">
                <i class="fas fa-file fa-3x"></i>
                <p>${file.name}</p>
            </div>
        `;
    }

    dialog.innerHTML = `
        <div class="caption-dialog">
            <div class="caption-dialog-header">
                <h3>ارسال فایل</h3>
                <button class="close-dialog" onclick="closeCaptionDialog()">✕</button>
            </div>
            <div class="caption-dialog-body">
                <div class="file-preview">
                    ${previewHtml}
                </div>
                <div class="file-info-caption">
                    <span class="file-name">${file.name}</span>
                    <span class="file-size">${formatFileSize(file.size)}</span>
                </div>
                <textarea 
                    id="fileCaption" 
                    class="caption-input" 
                    placeholder="توضیحات (اختیاری)..."
                    maxlength="1000"
                    rows="3"></textarea>
            </div>
            <div class="caption-dialog-footer">
                <button class="btn-cancel" onclick="closeCaptionDialog()">انصراف</button>
                <button class="btn-send" onclick="sendFileWithCaption()">
                    <i class="fas fa-paper-plane"></i> ارسال
                </button>
            </div>
        </div>
    `;

    document.body.appendChild(dialog);

    window.pendingFile = file;

    setTimeout(() => {
        document.getElementById('fileCaption')?.focus();
    }, 100);

    document.body.style.overflow = 'hidden';
}

function closeCaptionDialog() {
    const dialog = document.getElementById('captionDialog');
    if (dialog) {
        dialog.remove();
        window.pendingFile = null;
        document.body.style.overflow = 'auto';
    }
}

async function sendFileWithCaption() {
    if (!window.pendingFile) return;

    const caption = document.getElementById('fileCaption')?.value.trim() || '';
    const file = window.pendingFile;

    closeCaptionDialog();

    await uploadFile(file, caption);
}

async function uploadFile(file, caption = '') {
    if (!currentChat) {
        alert('لطفاً ابتدا یک چت را انتخاب کنید');
        return;
    }

    const formData = new FormData();
    formData.append('file', file);
    if (caption) {
        formData.append('caption', caption);
    }

    try {
        showUploadProgress(file.name);

        const response = await fetch('/api/File/upload', {
            method: 'POST',
            headers: {
                'RequestVerificationToken': getCsrfToken()
            },
            body: formData
        });

        if (!response.ok) {
            if (response.status === 413) {
                alert('❌ حجم فایل بیش از حد مجاز است.\nحداکثر مجاز: 100 مگابایت');
            } else {
                alert(`خطای سرور: ${response.status}`);
            }
            hideUploadProgress();
            return;
        }

        const result = await response.json();

        if (result.success) {
            await sendFileMessage(result.file, caption);
            hideUploadProgress();
        } else {
            alert(result.message || 'خطا در آپلود فایل');
            hideUploadProgress();
        }
    } catch (error) {
        console.error('❌ Upload error:', error);
        alert('خطا در آپلود فایل');
        hideUploadProgress();
    }
}

async function sendFileMessage(file, caption = '') {
    if (!currentChat || !connection) return;

    const messageText = caption || `📎 ${file.originalFileName}`;

    try {
        const response = await fetch('/Chat/SendMessage', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getCsrfToken()
            },
            body: JSON.stringify({
                receiverId: currentChat.type === 'private' ? currentChat.id : null,
                groupId: currentChat.type === 'group' ? currentChat.id : null,
                messageText: messageText,
                type: getMessageType(file.fileType),
                fileAttachmentId: file.id
            })
        });

        const result = await response.json();
        if (result.success) {
            if (connection?.state === signalR.HubConnectionState.Connected) {
                if (currentChat.type === 'private') {
                    await connection.invoke(
                        "SendPrivateMessageWithFile",
                        currentChat.id,
                        messageText,
                        result.messageId,
                        file.id
                    );
                }
            }
        }
    } catch (error) {
        console.error('❌ Send file message error:', error);
    }
}

function getMessageType(fileType) {
    const typeMap = {
        'Image': 1,
        'Video': 2,
        'Audio': 3,
        'Document': 5
    };
    return typeMap[fileType] || 5;
}

function formatFileSize(bytes) {
    const sizes = ['B', 'KB', 'MB', 'GB'];
    if (bytes === 0) return '0 B';
    const i = Math.floor(Math.log(bytes) / Math.log(1024));
    return Math.round(bytes / Math.pow(1024, i) * 100) / 100 + ' ' + sizes[i];
}

function showUploadProgress(fileName) {
    const container = document.getElementById('messagesContainer');
    const progressEl = document.createElement('div');
    progressEl.id = 'uploadProgress';
    progressEl.className = 'upload-progress';
    progressEl.innerHTML = `
        <div class="upload-progress-content">
            <div class="spinner"></div>
            <span>در حال آپلود: ${fileName}</span>
        </div>
    `;
    container.appendChild(progressEl);
    scrollToBottom();
}

function hideUploadProgress() {
    document.getElementById('uploadProgress')?.remove();
}

function canEditMessage(sentAt) {
    if (!messageSettings.allowEdit) {
        console.log('❌ Edit disabled in settings');
        return false;
    }

    const sentDate = new Date(sentAt);
    const now = new Date();
    const elapsed = (now - sentDate) / 1000; // ثانیه

    const canEdit = elapsed <= messageSettings.editTimeLimit;

    console.log('🔍 canEditMessage:', {
        sentAt,
        elapsed: Math.floor(elapsed),
        limit: messageSettings.editTimeLimit,
        canEdit
    });

    return canEdit;
}

function canDeleteMessage(sentAt) {
    if (!messageSettings.allowDelete) {
        console.log('❌ Delete disabled in settings');
        return false;
    }

    const sentDate = new Date(sentAt);
    const now = new Date();
    const elapsed = (now - sentDate) / 1000;

    const canDelete = elapsed <= messageSettings.deleteTimeLimit;

    console.log('🔍 canDeleteMessage:', {
        sentAt,
        elapsed: Math.floor(elapsed),
        limit: messageSettings.deleteTimeLimit,
        canDelete
    });

    return canDelete;
}

function canDeleteMessage(sentAt) {
    if (!messageSettings.allowDelete) return false;
    const elapsed = (Date.now() - new Date(sentAt).getTime()) / 1000;
    return elapsed <= messageSettings.deleteTimeLimit;
}


function toggleMessageMenu(messageId) {
    const menu = document.getElementById(`menu-${messageId}`);
    if (!menu) return;

    const messageEl = menu.closest('.message');
    const isSent = messageEl?.classList.contains('sent');

    // بستن سایر منوها و حذف کلاس menu-open
    document.querySelectorAll('.message-menu-dropdown').forEach(m => {
        if (m.id !== `menu-${messageId}`) {
            m.style.display = 'none';
            m.closest('.message')?.classList.remove('menu-open');
        }
    });

    if (menu.style.display === 'none' || !menu.style.display) {
        menu.style.display = 'block';

        // ✅ اضافه کردن کلاس برای افزایش z-index
        messageEl?.classList.add('menu-open');

        // تنظیم موقعیت افقی
        if (isSent) {
            menu.style.right = '0';
            menu.style.left = 'auto';
        } else {
            menu.style.left = '0px';
            menu.style.right = 'auto';
        }

        // تشخیص موقعیت عمودی
        setTimeout(() => {
            const menuRect = menu.getBoundingClientRect();
            const messageRect = messageEl.getBoundingClientRect();
            const windowHeight = window.innerHeight;

            const distanceFromTop = messageRect.top;
            const distanceFromBottom = windowHeight - messageRect.bottom;
            const menuHeight = menuRect.height || 200;

            // حذف کلاس‌های قبلی
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

// ✅ بستن منو با کلیک بیرون
document.addEventListener('click', function (e) {
    if (!e.target.closest('.message-menu')) {
        document.querySelectorAll('.message-menu-dropdown').forEach(m => {
            m.style.display = 'none';
            m.closest('.message')?.classList.remove('menu-open');
        });
    }
});

// ✅ ویرایش پیام - با پاپ‌آپ زیبا
async function editMessage(messageId) {
    const messageEl = document.querySelector(`[data-message-id="${messageId}"]`);
    if (!messageEl) return;

    const textEl = messageEl.querySelector('[data-editable="true"]');
    if (!textEl) return;

    const currentText = textEl.textContent.trim();

    // بستن منو
    toggleMessageMenu(messageId);

    // نمایش دیالوگ ویرایش
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

// �� نمایش دیالوگ ویرایش زیبا
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

    // Enter برای ذخیره (Shift+Enter برای خط جدید)
    textarea.addEventListener('keydown', (e) => {
        if (e.key === 'Enter' && e.ctrlKey) {
            e.preventDefault();
            document.getElementById('saveEditBtn').click();
        }
    });
}


// ✅ حذف پیام
// ✅ حذف پیام - با تنظیمات
// ✅ حذف پیام
// ✅ حذف پیام - با اطمینان از اطلاع‌رسانی
async function deleteMessage(messageId) {
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

                    // ✅ به‌روزرسانی UI فرستنده
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

                    // ✅ اطلاع به گیرنده از طریق SignalR - با اطلاعات کامل
                    if (connection?.state === signalR.HubConnectionState.Connected) {
                        console.log('📡 Sending SignalR notification...');

                        // ✅ ارسال با receiverId از response
                        await connection.invoke("NotifyMessageDeleted",
                            result.messageId,  // از response بگیر
                            result.showNotice,
                            result.receiverId   // ✅ اضافه کنید
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
}// 🗑️ تبدیل ��یام به "پیام حذف شده" - بدون تغییر موقعیت


// 🗑️ تبدیل پیام به "پیام حذف شده"
function replaceWithDeletedNotice(messageEl) {
    const isSent = messageEl.classList.contains('sent');
    const messageId = messageEl.dataset.messageId;

    // حذف محتوای قدیمی
    const messageBubble = messageEl.querySelector('.message-bubble');
    if (!messageBubble) return;

    messageBubble.innerHTML = `
        <div class="message-content deleted-message">
            
            <div class="deleted-text">
                ${isSent ? 'شما این پیام را حذف کردید' : 'این پیام حذف شده است'}
            </div>
        </div>
    `;

    messageEl.classList.add('deleted');
}
// 🎨 دیالوگ تأییدیه زیبا
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




//*********************************** */
function displayMessage(msg) {
    const isSent = msg.senderId === window.currentUserId;
    const container = document.getElementById('messagesContainer');

    const isConsecutive = lastSenderId === msg.senderId && messageGroupCount < 10;
    if (isConsecutive) {
        messageGroupCount++;
    } else {
        lastSenderId = msg.senderId;
        messageGroupCount = 1;
    }

    const messageEl = document.createElement('div');
    messageEl.className = `message ${isSent ? 'sent' : 'received'} ${isConsecutive ? 'consecutive' : ''}`;
    messageEl.dataset.messageId = msg.id;

    // ✅ اطمینان از وجود sentAt
    const sentAt = msg.sentAt || new Date().toISOString();
    messageEl.dataset.sentAt = sentAt;

    console.log('🔍 displayMessage:', {
        messageId: msg.id,
        sentAt: sentAt,
        hasAttachments: !!(msg.attachments && msg.attachments.length > 0)
    });

    // چک پیام حذف شده
    if (msg.isDeleted) {
        messageEl.classList.add('deleted');
        messageEl.innerHTML = `
            <div class="message-wrapper">
                <div class="message-bubble">
                    <div class="message-content deleted-message">
                        <div class="deleted-text">
                            ${isSent ? 'شما این پیام را حذف کردید' : 'این پیام حذف شده است'}
                        </div>
                    </div>
                </div>
            </div>
        `;
        container.appendChild(messageEl);
        return;
    }

    const sendTime = formatPersianTime(msg.sentAt || new Date());

    // Avatar Section
    let avatarSectionHtml = '';
    if (!isConsecutive) {
        const avatarContent = msg.senderAvatar ?
            `<img src="${msg.senderAvatar}" alt="${escapeHtml(msg.senderName)}" class="message-avatar-img" />` :
            getInitials(msg.senderName);

        avatarSectionHtml = `
            <div class="message-avatar-section">
                <div class="message-avatar">${avatarContent}</div>
                <div class="message-sender">${escapeHtml(msg.senderName)}</div>
            </div>
        `;
    }

    // Attachments
    let attachmentsHtml = '';
    if (msg.attachments && msg.attachments.length > 0) {
        attachmentsHtml = msg.attachments.map(file => renderFileAttachment(file)).join('');
    }

    // Message Text
    let messageTextHtml = '';
    const hasAttachments = msg.attachments && msg.attachments.length > 0;
    const messageContent = msg.content || msg.messageText || '';

    if (hasAttachments) {
        if (messageContent && !messageContent.startsWith('📎') && !messageContent.startsWith('🎤')) {
            messageTextHtml = `<div class="message-caption" data-editable="true">${escapeHtml(messageContent)}</div>`;
        }
    } else {
        if (messageContent) {
            messageTextHtml = `<div class="message-text" data-editable="true">${escapeHtml(messageContent)}</div>`;
        }
    }

    // Edited Badge
    const editedBadge = msg.isEdited ? '<span class="edited-badge">ویرایش شده</span>' : '';

    // Status
    let statusHtml = '';
    if (isSent) {
        const readTime = msg.readAt ? formatPersianTime(msg.readAt) : null;
        if (msg.isRead && readTime) {
            statusHtml = `
                <div class="sent-info">
                    ارسال: ${sendTime} &nbsp;&nbsp; مشاهده: ${readTime}
                    <span class="tick double-blue">✓✓</span>
                    ${editedBadge}
                </div>`;
        } else if (msg.isDelivered) {
            statusHtml = `
                <div class="sent-info">
                    ارسال: ${sendTime}
                    <span class="tick double-gray">✓✓</span>
                    ${editedBadge}
                </div>`;
        } else {
            statusHtml = `
                <div class="sent-info">
                    ارسال: ${sendTime}
                    <span class="tick single">✓</span>
                    ${editedBadge}
                </div>`;
        }
    } else {
        statusHtml = `<div class="message-time">${sendTime} ${editedBadge}</div>`;
    }

    // Reply Section
    let replyHtml = '';
    if (msg.replyToMessageId) {
        replyHtml = `
            <div class="message-reply" onclick="scrollToMessage(${msg.replyToMessageId})">
                <i class="fas fa-reply"></i>
                <div class="message-reply-content">
                    <strong>${escapeHtml(msg.replyToSenderName || 'کاربر')}</strong>
                    <p>${escapeHtml((msg.replyToText || 'پیام').substring(0, 50))}</p>
                </div>
            </div>
        `;
    }

    // ✅ Message Menu - با محاسبه زمان صحیح
    let messageMenuHtml = '';
    if (isSent) {
        const canEdit = canEditMessage(sentAt); // ✅ استفاده از sentAt اصلاح شده
        const canDelete = canDeleteMessage(sentAt); // ✅ استفاده از sentAt اصلاح شده

        console.log('🔍 Menu permissions:', {
            messageId: msg.id,
            sentAt: sentAt,
            canEdit,
            canDelete,
            settings: messageSettings
        });

        messageMenuHtml = `
            <div class="message-menu">
                <button class="message-menu-btn" onclick="toggleMessageMenu(${msg.id})">
                    <i class="fas fa-ellipsis-v"></i>
                </button>
                <div class="message-menu-dropdown" id="menu-${msg.id}" style="display: none;">
                    <button onclick="replyToMessage(${msg.id})">
                        <i class="fas fa-reply"></i> پاسخ
                    </button>
                    <button onclick="forwardMessage(${msg.id})">
                        <i class="fas fa-share"></i> ارجاع
                    </button>
                    <button onclick="enterMultiSelectMode()">
                        <i class="fas fa-check-square"></i> ارجاع چندین پیام
                    </button>
                    ${canEdit ? `
                    <button onclick="editMessage(${msg.id})">
                        <i class="fas fa-edit"></i> ویرایش
                    </button>
                    ` : ''}
                    ${canDelete ? `
                    <button onclick="deleteMessage(${msg.id})" class="delete-btn">
                        <i class="fas fa-trash"></i> حذف
                    </button>
                    ` : ''}
                </div>
            </div>
        `;
    } else {
        messageMenuHtml = `
            <div class="message-menu">
                <button class="message-menu-btn" onclick="toggleMessageMenu(${msg.id})">
                    <i class="fas fa-ellipsis-v"></i>
                </button>
                <div class="message-menu-dropdown" id="menu-${msg.id}" style="display: none;">
                    <button onclick="replyToMessage(${msg.id})">
                        <i class="fas fa-reply"></i> پاسخ
                    </button>
                    <button onclick="forwardMessage(${msg.id})">
                        <i class="fas fa-share"></i> ارجاع
                    </button>
                    <button onclick="enterMultiSelectMode()">
                        <i class="fas fa-check-square"></i> ارجاع چندین پیام
                    </button>
                    <button onclick="reportMessage(${msg.id})" class="report-btn">
                        <i class="fas fa-flag"></i> گزارش
                    </button>
                </div>
            </div>
        `;
    }

    // ساخت نهایی HTML
    messageEl.innerHTML = `
        <div class="message-wrapper">
            ${!isConsecutive ? avatarSectionHtml : ''}
            <div class="message-bubble">
                <div class="message-content">
                    ${replyHtml}
                    ${attachmentsHtml}
                    ${messageTextHtml}
                    ${statusHtml}
                </div>
                ${messageMenuHtml}
            </div>
        </div>
    `;

    container.appendChild(messageEl);

    if (!isSent && currentChat) {
        setTimeout(() => markMessagesAsRead(), 500);
    }
}


// ✅ اسکرول به پیام reply شده
function scrollToMessage(messageId) {
    const messageEl = document.querySelector(`[data-message-id="${messageId}"]`);
    if (!messageEl) {
        alert('پیام مورد نظر یافت نشد');
        return;
    }

    messageEl.scrollIntoView({ behavior: 'smooth', block: 'center' });

    // هایلایت موقت
    messageEl.classList.add('highlight');
    setTimeout(() => {
        messageEl.classList.remove('highlight');
    }, 2000);
}

// ✅ شروع Reply
function replyToMessage(messageId) {
    toggleMessageMenu(messageId);

    const messageEl = document.querySelector(`[data-message-id="${messageId}"]`);
    if (!messageEl) return;

    const textEl = messageEl.querySelector('[data-editable="true"]');
    const senderEl = messageEl.querySelector('.message-sender');

    // ✅ چک کردن وجود تصویر
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

    replyingToMessage = {
        id: messageId,
        text: messageText,
        senderName: senderName,
        thumbnail: thumbnailUrl,
        fileType: fileType
    };

    showReplyPreview();

    setTimeout(() => {
        const messageInput = document.getElementById('messageInput');
        if (messageInput) {
            messageInput.focus();
            messageInput.setSelectionRange(messageInput.value.length, messageInput.value.length);
        }
    }, 100);
}
// ✅ لغو Reply
function cancelReply() {
    replyingToMessage = null;
    const preview = document.getElementById('replyPreview');
    if (preview) preview.remove();
}

// ✅ ارسال پیام با Reply
async function sendMessage() {
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

// ✅ شروع Reply
function replyToMessage(messageId) {
    toggleMessageMenu(messageId);

    const messageEl = document.querySelector(`[data-message-id="${messageId}"]`);
    if (!messageEl) return;

    const textEl = messageEl.querySelector('[data-editable="true"]');
    const senderEl = messageEl.querySelector('.message-sender');

    const messageText = textEl ? textEl.textContent.trim() : 'فایل ضمیمه';
    const senderName = senderEl ? senderEl.textContent.trim() : 'کاربر';

    replyingToMessage = {
        id: messageId,
        text: messageText,
        senderName: senderName
    };

    showReplyPreview();
}

// ✅ نمایش Preview Reply
// حذف کلاس `input-actions` از همه جا
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

    // ✅ ساخت HTML با thumbnail
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
        <button class="reply-preview-close" onclick="cancelReply()">
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
function cancelReply() {
    replyingToMessage = null;
    const preview = document.getElementById('replyPreview');
    if (preview) preview.remove();
}
function cancelReply() {
    replyingToMessage = null;
    const preview = document.getElementById('replyPreview');
    if (preview) preview.remove();

    // ✅ برگرداندن padding به حالت عادی
    const messagesContainer = document.getElementById('messagesContainer');
    if (messagesContainer) {
        messagesContainer.style.paddingBottom = '150px'; // 70px nav + 80px input
    }
}
// ✅ لغو Reply
function cancelReply() {
    replyingToMessage = null;
    const preview = document.getElementById('replyPreview');
    if (preview) preview.remove();
}

// ✅ ارسال پیام با Reply
async function sendMessage() {
    const input = document.getElementById('messageInput');
    const text = input.value.trim();

    if (!text || !currentChat || !connection) return;

    if (currentChat.type === 'private') {
        // ✅ ارسال با replyToMessageId
        await connection.invoke("SendPrivateMessage",
            currentChat.id,
            text,
            replyingToMessage ? replyingToMessage.id : null  // ✅ اضافه کنید
        );
    }

    input.value = '';
    input.style.height = 'auto';
    input.style.overflowY = 'hidden';

    // ✅ پاک کردن reply preview
    cancelReply();

    hideTypingIndicator();
}




// ✅ ارجاع تک پیام
function forwardMessage(messageId) {
    toggleMessageMenu(messageId);

    const messageEl = document.querySelector(`[data-message-id="${messageId}"]`);
    if (!messageEl) return;

    // دریافت محتوای پیام
    const textEl = messageEl.querySelector('[data-editable="true"]');
    const messageText = textEl ? textEl.textContent.trim() : '';

    // نمایش دیالوگ انتخاب مخاطب
    showForwardDialog([messageId], messageText);
}

// ✅ نمایش دیالوگ Forward
async function showForwardDialog(messageIds, previewText = '') {
    try {
        const response = await fetch('/Chat/GetChats?tab=all');
        const chats = await response.json();

        const dialog = document.createElement('div');
        dialog.className = 'forward-dialog-overlay';
        dialog.innerHTML = `
            <div class="forward-dialog">
                <div class="forward-dialog-header">
                    <h3>ارجاع به...</h3>
                    <button class="close-dialog" onclick="closeForwardDialog()">✕</button>
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
                <!-- ✅ Footer با دکمه بستن -->
                <div class="forward-dialog-footer">
                    <button class="btn-close-forward" onclick="closeForwardDialog()">
                        <i class="fas fa-times"></i> بستن
                    </button>
                </div>
            </div>
        `;

        document.body.appendChild(dialog);
        document.body.style.overflow = 'hidden';

        // Event listener برای دکمه‌های ارسال
        document.querySelectorAll('.btn-forward-send').forEach(btn => {
            btn.addEventListener('click', function () {
                const receiverId = parseInt(this.dataset.receiverId);
                sendForward(receiverId, messageIds);
            });
        });

        // فیلتر جستجو
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
function closeForwardDialog() {
    const dialog = document.querySelector('.forward-dialog-overlay');
    if (dialog) {
        dialog.remove();
        document.body.style.overflow = 'auto';
    }
}

// ✅ ارسال Forward
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
            // نمایش تیک سبز
            const contactItem = document.querySelector(`.forward-contact-item[data-chat-id="${receiverId}"]`);
            if (contactItem) {
                const btn = contactItem.querySelector('.btn-forward-send');
                btn.innerHTML = '<i class="fas fa-check"></i> ارسال شد';
                btn.style.background = '#4caf50';
                btn.disabled = true;
            }

            // ✅ دیالوگ را نبند - فقط تیک سبز نمایش بده
            console.log('✅ Messages forwarded successfully');
        } else {
            alert(result.message || 'خطا در ارجاع پیام');
        }
    } catch (error) {
        console.error('❌ Forward error:', error);
        alert('خطا در ارجاع پیام');
    }
}


// ✅ ورود به حالت انتخاب چندگانه
function enterMultiSelectMode() {
    multiSelectMode = true;
    selectedMessages.clear();

    // اضافه کردن checkbox به پیام‌ها
    document.querySelectorAll('.message').forEach(msg => {
        if (msg.classList.contains('deleted')) return;

        const messageId = msg.dataset.messageId;

        // چک کردن اینکه checkbox قبلاً اضافه نشده
        if (!msg.querySelector('.message-checkbox')) {
            const checkbox = document.createElement('div');
            checkbox.className = 'message-checkbox';
            checkbox.innerHTML = '<i class="far fa-circle"></i>';
            checkbox.onclick = () => toggleMessageSelection(messageId);

            msg.querySelector('.message-bubble').appendChild(checkbox);
        }
    });

    // نمایش toolbar
    showMultiSelectToolbar();

    // بستن منوها
    document.querySelectorAll('.message-menu-dropdown').forEach(m => {
        m.style.display = 'none';
    });
}

// ✅ انتخاب/لغو انتخاب پیام
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

// ✅ نمایش Toolbar
function showMultiSelectToolbar() {
    const existingToolbar = document.getElementById('multiSelectToolbar');
    if (existingToolbar) return;

    const toolbar = document.createElement('div');
    toolbar.id = 'multiSelectToolbar';
    toolbar.className = 'multi-select-toolbar';
    toolbar.innerHTML = `
        <button class="toolbar-btn" onclick="exitMultiSelectMode()">
            <i class="fas fa-times"></i> لغو
        </button>
        <span class="selected-count">0 انتخاب شده</span>
        <button class="toolbar-btn primary" onclick="forwardSelectedMessages()" disabled>
            <i class="fas fa-share"></i> ارجاع
        </button>
    `;

    document.body.appendChild(toolbar);
}

// ✅ به‌روزرسانی Toolbar
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

// ✅ خروج از حالت Multi-Select
function exitMultiSelectMode() {
    multiSelectMode = false;
    selectedMessages.clear();

    // حذف checkbox‌ها
    document.querySelectorAll('.message-checkbox').forEach(cb => cb.remove());

    // حذف toolbar
    document.getElementById('multiSelectToolbar')?.remove();
}

// ✅ ارجاع پیام‌های انتخاب شده
function forwardSelectedMessages() {
    if (selectedMessages.size === 0) return;

    const messageIds = Array.from(selectedMessages);

    // دریافت متن اولین پیام برای preview
    const firstMessageEl = document.querySelector(`[data-message-id="${messageIds[0]}"]`);
    const textEl = firstMessageEl?.querySelector('[data-editable="true"]');
    const previewText = textEl ? textEl.textContent : '';

    showForwardDialog(messageIds, previewText);

    // خروج از حالت Multi-Select
    exitMultiSelectMode();
}



// ✅ تشخیص فوکوس صفحه
window.addEventListener('focus', function () {
    isPageFocused = true;
    console.log('🟢 Page focused');

    // خواندن پیام‌های خوانده نشده
    if (currentChat) {
        markMessagesAsRead();
        removeUnreadSeparator();
    }
});

window.addEventListener('blur', function () {
    isPageFocused = false;
    console.log('🔴 Page blurred');
});

// ✅ تشخیص تایپ
document.getElementById('messageInput')?.addEventListener('input', function () {
    isTyping = this.value.length > 0;
});




// ✅ تشخیص فوکوس صفحه
window.addEventListener('focus', function () {
    isPageFocused = true;
    console.log('🟢 Page focused');

    // خواندن پیام‌های خوانده نشده
    if (currentChat) {
        markMessagesAsRead();
        removeUnreadSeparator();
    }
});

window.addEventListener('blur', function () {
    isPageFocused = false;
    console.log('🔴 Page blurred');
});

// ✅ تشخیص تایپ
document.getElementById('messageInput')?.addEventListener('input', function () {
    isTyping = this.value.length > 0;
});








/******************Voice */
// ============================================
// Voice Recording
// ============================================
function setupVoiceRecording() {
    const micBtn = document.getElementById('micBtn');
    if (!micBtn) return;

    micBtn.addEventListener('mousedown', startRecording);
    micBtn.addEventListener('touchstart', startRecording);
    micBtn.addEventListener('mouseup', stopRecording);
    micBtn.addEventListener('touchend', stopRecording);
    micBtn.addEventListener('mouseleave', cancelRecording);
}

async function startRecording(e) {
    e.preventDefault();

    if (!currentChat) {
        alert('لطفاً ابتدا یک چت را انتخاب کنید');
        return;
    }

    try {
        const stream = await navigator.mediaDevices.getUserMedia({ audio: true });

        mediaRecorder = new MediaRecorder(stream, {
            mimeType: 'audio/webm;codecs=opus'
        });

        audioChunks = [];

        mediaRecorder.ondataavailable = (event) => {
            if (event.data.size > 0) {
                audioChunks.push(event.data);
            }
        };

        mediaRecorder.onstop = async () => {
            const audioBlob = new Blob(audioChunks, { type: 'audio/webm' });
            await sendVoiceMessage(audioBlob);
            stream.getTracks().forEach(track => track.stop());
        };

        mediaRecorder.start();
        isRecording = true;
        recordingStartTime = Date.now();

        const micBtn = document.getElementById('micBtn');
        micBtn.classList.add('recording');
        micBtn.innerHTML = '<i class="fas fa-stop"></i>';

        showRecordingTimer();
        console.log('🎤 Recording started');

    } catch (error) {
        console.error('❌ Microphone error:', error);
        alert('دسترسی به میکروفون رد شد');
    }
}

function stopRecording(e) {
    e.preventDefault();
    if (!isRecording || !mediaRecorder) return;

    isRecording = false;
    mediaRecorder.stop();

    const micBtn = document.getElementById('micBtn');
    micBtn.classList.remove('recording');
    micBtn.innerHTML = '<i class="fas fa-microphone"></i>';

    hideRecordingTimer();
    console.log('🎤 Recording stopped');
}

function cancelRecording(e) {
    if (!isRecording || !mediaRecorder) return;

    isRecording = false;
    mediaRecorder.stop();
    audioChunks = [];

    const micBtn = document.getElementById('micBtn');
    micBtn.classList.remove('recording');
    micBtn.innerHTML = '<i class="fas fa-microphone"></i>';

    hideRecordingTimer();
    console.log('🎤 Recording cancelled');
}

function showRecordingTimer() {
    const inputWrapper = document.querySelector('.input-wrapper');

    const timer = document.createElement('div');
    timer.id = 'recordingTimer';
    timer.className = 'recording-timer';
    timer.innerHTML = `
        <div class="recording-indicator">
            <div class="recording-pulse"></div>
            <span class="recording-time">00:00</span>
        </div>
        <span class="recording-hint">← رها کنید برای ارسال</span>
    `;

    inputWrapper.parentNode.insertBefore(timer, inputWrapper);

    recordingTimer = setInterval(() => {
        const elapsed = Math.floor((Date.now() - recordingStartTime) / 1000);
        const minutes = Math.floor(elapsed / 60).toString().padStart(2, '0');
        const seconds = (elapsed % 60).toString().padStart(2, '0');

        const timeEl = document.querySelector('.recording-time');
        if (timeEl) timeEl.textContent = `${minutes}:${seconds}`;

        if (elapsed >= 300) stopRecording(new Event('mouseup'));
    }, 1000);
}

function hideRecordingTimer() {
    const timer = document.getElementById('recordingTimer');
    if (timer) timer.remove();

    if (recordingTimer) {
        clearInterval(recordingTimer);
        recordingTimer = null;
    }
}

async function sendVoiceMessage(audioBlob) {
    if (!currentChat) return;

    const duration = Math.floor((Date.now() - recordingStartTime) / 1000);

    if (duration < 1) {
        console.log('⚠️ Audio too short');
        return;
    }

    try {
        showUploadProgress('پیام صوتی');

        const audioFile = new File([audioBlob], `voice_${Date.now()}.webm`, {
            type: 'audio/webm'
        });

        const formData = new FormData();
        formData.append('file', audioFile);
        formData.append('duration', duration);
        formData.append('caption', '🎤 پیام صوتی');

        const response = await fetch('/api/File/upload', {
            method: 'POST',
            headers: {
                'RequestVerificationToken': getCsrfToken()
            },
            body: formData
        });

        if (!response.ok) throw new Error('Upload failed');

        const result = await response.json();

        if (result.success) {
            await sendFileMessage(result.file, result.caption || '🎤 پیام صوتی');
            hideUploadProgress();
        } else {
            alert(result.message || 'خطا در آپلود');
            hideUploadProgress();
        }
    } catch (error) {
        console.error('❌ Voice error:', error);
        alert('خطا در ارسال پیام صوتی');
        hideUploadProgress();
    }
}

// ============================================
// Voice Player
// ============================================
// ============================================
// Voice Player
// ============================================
function renderAudioPlayer(file) {
    const audioId = `audio_${file.id}`;
    const duration = file.duration || 0;
    const durationText = formatDuration(duration);

    return `
        <div class="message-file audio-file voice-message" data-audio-id="${file.id}">
            <button class="voice-play-btn" onclick="toggleVoicePlay(${file.id})">
                <i class="fas fa-play"></i>
            </button>
            <div class="voice-content">
                <!-- ✅ Progress Bar -->
                <div class="voice-progress-container" onclick="seekVoice(event, ${file.id})">
                    <div class="voice-progress-bar">
                        <div class="voice-progress-fill" id="progress_${file.id}"></div>
                    </div>
                </div>
                <!-- ✅ Meta -->
                <div class="voice-meta">
                    <span class="voice-duration" id="duration_${file.id}">${durationText}</span>
                    <button class="voice-speed" onclick="changeVoiceSpeed(${file.id}); event.stopPropagation();">
                        <span id="speed_${file.id}">1.0x</span>
                    </button>
                </div>
            </div>
            <audio id="${audioId}" src="${file.fileUrl}" preload="metadata"></audio>
        </div>
    `;
}

function generateWaveformBars(count) {
    const bars = [];
    for (let i = 0; i < count; i++) {
        const height = 30 + Math.random() * 70;
        bars.push(`<div class="wave-bar" style="height: ${height}%"></div>`);
    }
    return bars.join('');
}

function toggleVoicePlay(fileId) {
    const audio = document.getElementById(`audio_${fileId}`);
    const container = document.querySelector(`.message-file[data-audio-id="${fileId}"]`);
    const btn = container?.querySelector('.voice-play-btn');

    if (!audio || !btn) return;

    if (currentPlayingAudio && currentPlayingAudio !== audio) {
        stopVoicePlay(currentPlayingAudio);
    }

    if (audio.paused) {
        audio.play();
        btn.innerHTML = '<i class="fas fa-pause"></i>';
        btn.classList.add('playing');
        currentPlayingAudio = audio;

        // ✅ به‌روزرسانی Progress Bar و Duration
        audio.ontimeupdate = () => {
            updateVoiceDuration(fileId, audio);
            updateProgressBar(fileId, audio);
        };

        audio.onended = () => {
            stopVoicePlay(audio);
            resetVoiceUI(fileId);
        };
    } else {
        stopVoicePlay(audio);
    }
}

function stopVoicePlay(audio) {
    if (!audio) return;

    audio.pause();

    const audioId = audio.id.replace('audio_', '');
    const container = document.querySelector(`.message-file[data-audio-id="${audioId}"]`);
    const btn = container?.querySelector('.voice-play-btn');

    if (btn) {
        btn.innerHTML = '<i class="fas fa-play"></i>';
        btn.classList.remove('playing');
    }

    currentPlayingAudio = null;
}

// ✅ به‌روزرسانی Progress Bar
function updateProgressBar(fileId, audio) {
    const progressFill = document.getElementById(`progress_${fileId}`);

    if (!progressFill || !audio.duration) return;

    const percent = (audio.currentTime / audio.duration) * 100;
    progressFill.style.width = `${percent}%`;
}

// ✅ به‌روزرسانی Duration
function updateVoiceDuration(fileId, audio) {
    const durationEl = document.getElementById(`duration_${fileId}`);

    if (!durationEl || !audio.duration) return;

    const remaining = Math.ceil(audio.duration - audio.currentTime);
    durationEl.textContent = formatDuration(remaining);
}

function resetVoiceUI(fileId) {
    const audio = document.getElementById(`audio_${fileId}`);
    const durationEl = document.getElementById(`duration_${fileId}`);
    const progressFill = document.getElementById(`progress_${fileId}`);

    if (audio && durationEl && audio.duration) {
        durationEl.textContent = formatDuration(Math.ceil(audio.duration));
    }

    // ✅ ریست Progress Bar
    if (progressFill) {
        progressFill.style.width = '0%';
    }
}

// ✅ جست‌وجو در صوت
function seekVoice(event, fileId) {
    event.stopPropagation();

    const audio = document.getElementById(`audio_${fileId}`);
    const progressContainer = event.currentTarget;

    if (!audio || !audio.duration) return;

    const rect = progressContainer.getBoundingClientRect();
    const clickX = event.clientX - rect.left;
    const percent = clickX / rect.width;

    audio.currentTime = percent * audio.duration;

    console.log(`⏩ Seek to ${Math.floor(percent * 100)}%`);
}

function changeVoiceSpeed(fileId) {
    const audio = document.getElementById(`audio_${fileId}`);
    const speedBtn = document.getElementById(`speed_${fileId}`);

    if (!audio || !speedBtn) return;

    const speeds = [1, 1.5, 2];
    const currentSpeed = audio.playbackRate;
    const nextIndex = (speeds.indexOf(currentSpeed) + 1) % speeds.length;
    const nextSpeed = speeds[nextIndex];

    audio.playbackRate = nextSpeed;
    speedBtn.textContent = `${nextSpeed}x`;

    console.log(`🔊 Speed: ${nextSpeed}x`);
}

function formatDuration(seconds) {
    if (!seconds || isNaN(seconds)) return '0:00';

    const mins = Math.floor(seconds / 60);
    const secs = Math.floor(seconds % 60);
    return `${mins}:${secs.toString().padStart(2, '0')}`;
}










function toggleEmojiPicker() {
    let container = document.getElementById('emojiPickerContainer');
    if (!container) {
        container = document.createElement('div');
        container.id = 'emojiPickerContainer';
        document.body.appendChild(container);
    }

    if (!container.innerHTML) {
        container.innerHTML = createMatrixEmojiPickerHTML();
        setupEmojiPickerEvents();
    }

    emojiPickerVisible = !emojiPickerVisible;

    if (emojiPickerVisible) {
        container.style.display = 'block';

        const emojiBtn = document.getElementById('emojiBtn');
        const pickerEl = container.querySelector('.mx_ContextualMenu');

        if (emojiBtn && pickerEl) {
            // ✅ صبر کنید تا picker رندر شود
            setTimeout(() => {
                adjustEmojiPickerPosition(); // ✅ استفاده از تابع مشترک
            }, 50);
        }
    } else {
        container.style.display = 'none';
    }
}

function getRecentEmojis() {
    return ['😂', '❤️', '😍', '👍', '🔥', '🙏', '😊', '😘', '💯', '✨', '🎉', '👏', '💪', '🌹', '☺️', '😭', '🥰', '😁', '🤗', '💕', '🙌', '✅', '👌', '💖'];
}

function getPeopleEmojis() {
    return [
        '😀', '😃', '😄', '😁', '😆', '😅', '😂', '🤣', '😊', '😇', '🙂', '🙃', '😉', '😌', '😍', '🥰',
        '😘', '😗', '😙', '😚', '😋', '😛', '😝', '😜', '🤪', '🤨', '🧐', '🤓', '😎', '🤩', '🥳', '😏',
        '😒', '😞', '😔', '😟', '😕', '🙁', '☹️', '😣', '😖', '😫', '😩', '🥺', '😢', '😭', '😤', '😠',
        '😡', '🤬', '🤯', '😳', '🥵', '🥶', '😱', '😨', '😰', '😥', '😓', '🤗', '🤔', '🤭', '🤫', '🤥',
        '😶', '😐', '😑', '😬', '🙄', '😯', '😦', '😧', '😮', '😲', '🥱', '😴', '🤤', '😪', '😵', '🤐'
    ];
}

function getNatureEmojis() {
    return [
        '🐶', '🐱', '🐭', '🐹', '🐰', '🦊', '🐻', '🐼', '🐨', '🐯', '🦁', '🐮', '🐷', '🐽', '🐸', '🐵',
        '🙈', '🙉', '🙊', '🐒', '🐔', '🐧', '🐦', '🐤', '🐣', '🐥', '🦆', '🦅', '🦉', '🦇', '🐺', '🐗',
        '🐴', '🦄', '🐝', '🐛', '🦋', '🐌', '🐞', '🐜', '🦟', '🦗', '🕷️', '🕸️', '🦂', '🐢', '🐍', '🦎',
        '🦖', '🦕', '🐙', '🦑', '🦐', '🦞', '🦀', '🐡', '🐠', '🐟', '🐬', '🐳', '🐋', '🦈', '🐊', '🐅',
        '🌸', '🌺', '🌻', '🌹', '🥀', '🌷', '🌼', '🌾', '🍀', '☘️', '🍃', '🍂', '🍁', '🌿', '🌱', '🌲',
        '🌳', '🌴', '🌵', '🌾', '🌿', '🍀', '☘️', '🍃', '🍂', '🍁', '🪴', '🌾', '💐', '🏵️', '🌹', '🥀'
    ];
}

function getFoodEmojis() {
    return [
        '🍎', '🍐', '🍊', '🍋', '🍌', '🍉', '🍇', '🍓', '🫐', '🍈', '🍒', '🍑', '🥭', '🍍', '🥥', '🥝',
        '🍅', '🍆', '🥑', '🥦', '🥬', '🥒', '🌶️', '🫑', '🌽', '🥕', '🧄', '🧅', '🥔', '🍠', '🥐', '🥖',
        '🍞', '🥨', '🥯', '🧀', '🥚', '🍳', '🧈', '🥞', '🧇', '🥓', '🥩', '🍗', '🍖', '🦴', '🌭', '🍔',
        '🍟', '🍕', '🫓', '🥪', '🥙', '🧆', '🌮', '🌯', '🫔', '🥗', '🥘', '🫕', '🥫', '🍝', '🍜', '🍲',
        '🍛', '🍣', '🍱', '🥟', '🦪', '🍤', '🍙', '🍚', '🍘', '🍥', '🥠', '🥮', '🍢', '🍡', '🍧', '🍨'
    ];
}

function getActivityEmojis() {
    return [
        '⚽', '🏀', '🏈', '⚾', '🥎', '🎾', '🏐', '🏉', '🥏', '🎱', '🪀', '🏓', '🏸', '🏒', '🏑', '🥍',
        '🏏', '🪃', '🥅', '⛳', '🪁', '🏹', '🎣', '🤿', '🥊', '🥋', '🎽', '🛹', '🛼', '🛷', '⛸️', '🥌',
        '🎿', '⛷️', '🏂', '🪂', '🏋️', '🤼', '🤸', '🤺', '⛹️', '🤾', '🏌️', '🏇', '🧘', '🏊', '🤽', '🚣',
        '🧗', '🚴', '🚵', '🤹', '🎪', '🎭', '🎨', '🎬', '🎤', '🎧', '🎼', '🎹', '🥁', '🪘', '🎷', '🎺'
    ];
}

function getTravelEmojis() {
    return [
        '🚗', '🚕', '🚙', '🚌', '🚎', '🏎️', '🚓', '🚑', '🚒', '🚐', '🛻', '🚚', '🚛', '🚜', '🛴', '🚲',
        '🛵', '🏍️', '🛺', '🚨', '🚔', '🚍', '🚘', '🚖', '🛞', '🚡', '🚠', '🚟', '🚃', '🚋', '🚞', '🚝',
        '🚄', '🚅', '🚈', '🚂', '🚆', '🚇', '🚊', '🚉', '✈️', '🛫', '🛬', '🛩️', '💺', '🛰️', '🚁', '🛸',
        '🚀', '🛶', '⛵', '🚤', '🛥️', '🛳️', '⛴️', '🚢', '⚓', '🪝', '⛽', '🚧', '🚦', '🚥', '🗺️', '🗿',
        '🗽', '🗼', '🏰', '🏯', '🏟️', '🎡', '🎢', '🎠', '⛲', '⛱️', '🏖️', '🏝️', '🏜️', '🌋', '⛰️', '🏔️'
    ];
}

function getObjectsEmojis() {
    return [
        '⌚', '📱', '📲', '💻', '⌨️', '🖥️', '🖨️', '🖱️', '🖲️', '🕹️', '🗜️', '💾', '💿', '📀', '📼', '📷',
        '📸', '📹', '🎥', '📽️', '🎞️', '📞', '☎️', '📟', '📠', '📺', '📻', '🎙️', '🎚️', '🎛️', '🧭', '⏱️',
        '⏲️', '⏰', '🕰️', '⌛', '⏳', '📡', '🔋', '🪫', '🔌', '💡', '🔦', '🕯️', '🪔', '🧯', '🛢️', '💸',
        '💵', '💴', '💶', '💷', '🪙', '💰', '💳', '💎', '⚖️', '🪜', '🧰', '🪛', '🔧', '🔨', '⚒️', '🛠️',
        '⛏️', '🪚', '🔩', '⚙️', '🪤', '🧱', '⛓️', '🧲', '🔫', '💣', '🧨', '🪓', '🔪', '🗡️', '⚔️', '🛡️'
    ];
}

function getSymbolsEmojis() {
    return [
        '❤️', '🧡', '💛', '💚', '💙', '💜', '🖤', '🤍', '🤎', '💔', '❣️', '💕', '💞', '💓', '💗', '💖',
        '💘', '💝', '💟', '☮️', '✝️', '☪️', '🕉️', '☸️', '✡️', '🔯', '🕎', '☯️', '☦️', '🛐', '⛎', '♈',
        '♉', '♊', '♋', '♌', '♍', '♎', '♏', '♐', '♑', '♒', '♓', '🆔', '⚛️', '🉑', '☢️', '☣️', '📴',
        '📳', '🈶', '🈚', '🈸', '🈺', '🈷️', '✴️', '🆚', '💮', '🉐', '㊙️', '㊗️', '🈴', '🈵', '🈹', '🈲',
        '🅰️', '🅱️', '🆎', '🆑', '🅾️', '🆘', '❌', '⭕', '🛑', '⛔', '📛', '🚫', '💯', '💢', '♨️', '🚷'
    ];
}




function createMatrixEmojiPickerHTML() {
    return `
        <div class="mx_ContextualMenu mx_visible">
            <section class="mx_EmojiPicker">
                <nav class="mx_EmojiPicker_header">
                    <button class="mx_EmojiPicker_anchor active" data-category="recent" title="پرکاربرد">🕒</button>
                    <button class="mx_EmojiPicker_anchor" data-category="people" title="افراد">😀</button>
                    <button class="mx_EmojiPicker_anchor" data-category="nature" title="طبیعت">🐱</button>
                    <button class="mx_EmojiPicker_anchor" data-category="food" title="غذا">🍔</button>
                    <button class="mx_EmojiPicker_anchor" data-category="symbols" title="نمادها">❤️</button>
                </nav>
                <div class="mx_EmojiPicker_body">
                    <section class="mx_EmojiPicker_category active" data-category="recent">
                        <h2 class="mx_EmojiPicker_category_label">پرکاربرد</h2>
                        <div class="mx_EmojiPicker_list">
                            ${getRecentEmojis().map(e => `<div class="mx_EmojiPicker_item" data-emoji="${e}">${e}</div>`).join('')}
                        </div>
                    </section>
                    <section class="mx_EmojiPicker_category" data-category="people">
                        <h2 class="mx_EmojiPicker_category_label">افراد</h2>
                        <div class="mx_EmojiPicker_list">
                            ${getPeopleEmojis().map(e => `<div class="mx_EmojiPicker_item" data-emoji="${e}">${e}</div>`).join('')}
                        </div>
                    </section>
                    <section class="mx_EmojiPicker_category" data-category="nature">
                        <h2 class="mx_EmojiPicker_category_label">طبیعت</h2>
                        <div class="mx_EmojiPicker_list">
                            ${getNatureEmojis().map(e => `<div class="mx_EmojiPicker_item" data-emoji="${e}">${e}</div>`).join('')}
                        </div>
                    </section>
                    <section class="mx_EmojiPicker_category" data-category="food">
                        <h2 class="mx_EmojiPicker_category_label">غذا</h2>
                        <div class="mx_EmojiPicker_list">
                            ${getFoodEmojis().map(e => `<div class="mx_EmojiPicker_item" data-emoji="${e}">${e}</div>`).join('')}
                        </div>
                    </section>
                    <section class="mx_EmojiPicker_category" data-category="symbols">
                        <h2 class="mx_EmojiPicker_category_label">نمادها</h2>
                        <div class="mx_EmojiPicker_list">
                            ${getSymbolsEmojis().map(e => `<div class="mx_EmojiPicker_item" data-emoji="${e}">${e}</div>`).join('')}
                        </div>
                    </section>
                </div>
                <section class="mx_EmojiPicker_footer">
                    <h2 class="mx_EmojiPicker_quick_header">واکنش سریع</h2>
                    <div class="mx_EmojiPicker_quick_list">
                        ${['👍', '👎', '😂', '❤️', '🎉', '😢', '🔥', '👀'].map(e => `<div class="mx_EmojiPicker_item" data-emoji="${e}">${e}</div>`).join('')}
                    </div>
                </section>
            </section>
        </div>
    `;
}



function insertEmoji(emoji) {
    const input = document.getElementById('messageInput');
    if (!input) return;

    const start = input.selectionStart;
    const end = input.selectionEnd;
    input.value = input.value.substring(0, start) + emoji + input.value.substring(end);
    input.focus();
    const newPos = start + emoji.length;
    input.setSelectionRange(newPos, newPos);

    hideEmojiPicker();
}

function hideEmojiPicker() {
    const container = document.getElementById('emojiPickerContainer');
    if (container) container.style.display = 'none';
    emojiPickerVisible = false;
}

function setupEmojiPickerEvents() {
    const container = document.getElementById('emojiPickerContainer');
    if (!container) return;

    container.addEventListener('click', function (e) {
        const emojiItem = e.target.closest('.mx_EmojiPicker_item');
        if (emojiItem) {
            insertEmoji(emojiItem.dataset.emoji);
            e.stopPropagation();
            return;
        }

        const categoryBtn = e.target.closest('.mx_EmojiPicker_anchor');
        if (categoryBtn) {
            document.querySelectorAll('.mx_EmojiPicker_anchor').forEach(btn => btn.classList.remove('active'));
            categoryBtn.classList.add('active');

            document.querySelectorAll('.mx_EmojiPicker_category').forEach(cat => cat.classList.remove('active'));
            const targetCategory = container.querySelector(`.mx_EmojiPicker_category[data-category="${categoryBtn.dataset.category}"]`);
            if (targetCategory) targetCategory.classList.add('active');

            // ✅ تنظیم مجدد موقعیت بعد از تغییر دسته
            setTimeout(() => {
                adjustEmojiPickerPosition();
            }, 50);

            e.stopPropagation();
        }
    });
}

// ✅ تابع جدید برای تنظیم موقعیت
function adjustEmojiPickerPosition() {
    const emojiBtn = document.getElementById('emojiBtn');
    const pickerEl = document.querySelector('.mx_ContextualMenu');

    if (!emojiBtn || !pickerEl) return;

    const btnRect = emojiBtn.getBoundingClientRect();
    const pickerRect = pickerEl.getBoundingClientRect();
    const pickerHeight = pickerRect.height;
    const pickerWidth = pickerRect.width;

    const spaceAbove = btnRect.top;
    const spaceBelow = window.innerHeight - btnRect.bottom;

    let top, left;

    // ✅ باز کردن بالای دکمه
    if (spaceAbove > pickerHeight + 20) {
        top = btnRect.top - pickerHeight - 10;
    }
    // ✅ باز کردن پایین دکمه
    else if (spaceBelow > pickerHeight + 20) {
        top = btnRect.bottom + 10;
    }
    // ✅ وسط صفحه
    else {
        top = Math.max(20, (window.innerHeight - pickerHeight) / 2);
    }

    // ✅ تراز راست (RTL)
    left = btnRect.right - pickerWidth;

    // ✅ محدودیت‌ها
    left = Math.max(10, Math.min(left, window.innerWidth - pickerWidth - 10));
    top = Math.max(10, Math.min(top, window.innerHeight - pickerHeight - 10));

    pickerEl.style.top = `${top}px`;
    pickerEl.style.left = `${left}px`;

    console.log('📍 Adjusted Emoji Picker:', { top, left, pickerHeight });
}





function openImagePreview(url) {
    const modal = document.createElement('div');
    modal.className = 'image-preview-modal';
    modal.innerHTML = `
        <div class="image-preview-overlay">
            <button class="close-preview" onclick="closeImagePreview(); event.stopPropagation()">✕</button>
            
            <!-- ✅ دکمه اسکرول به بالا -->
            <button class="scroll-top-btn" id="scrollTopBtn" onclick="scrollToTop(); event.stopPropagation()" style="display: none;">
                <i class="fas fa-arrow-up"></i>
            </button>
            
            <div class="image-preview-container" id="imageContainer">
                <img id="previewImage" src="${url}" alt="Preview">
            </div>
            
            <div class="image-preview-controls">
                <div class="zoom-control">
                    <i class="fas fa-search-minus"></i>
                    <input type="range" 
                           id="zoomSlider" 
                           min="100" 
                           max="300" 
                           value="100" 
                           step="10"
                           oninput="updateZoom(this.value)">
                    <i class="fas fa-search-plus"></i>
                    <span id="zoomValue">100%</span>
                </div>
                <a href="${url}" download class="download-btn" onclick="event.stopPropagation()">
                    <i class="fas fa-download"></i> دانلود
                </a>
                <button class="reset-zoom-btn" onclick="resetZoom(); event.stopPropagation()">
                    <i class="fas fa-undo"></i> بازنشانی
                </button>
            </div>
        </div>
    `;
    document.body.appendChild(modal);
    document.body.style.overflow = 'hidden';

    currentPreviewImage = document.getElementById('previewImage');

    // ✅ نمایش دکمه scroll وقتی اسکرول می‌شود
    const container = document.getElementById('imageContainer');
    const scrollTopBtn = document.getElementById('scrollTopBtn');

    if (container && scrollTopBtn) {
        container.addEventListener('scroll', function () {
            if (this.scrollTop > 100) {
                scrollTopBtn.style.display = 'flex';
            } else {
                scrollTopBtn.style.display = 'none';
            }
        });
    }

    setTimeout(() => {
        modal.classList.add('active');
    }, 10);
}

function scrollToTop() {
    const container = document.getElementById('imageContainer');
    if (container) {
        container.scrollTo({
            top: 0,
            left: 0,
            behavior: 'smooth'
        });
    }
}

function closeImagePreview() {
    const modal = document.querySelector('.image-preview-modal');
    if (modal) {
        modal.classList.remove('active');
        setTimeout(() => {
            modal.remove();
            document.body.style.overflow = 'auto';
            currentPreviewImage = null;
        }, 300);
    }
}

function updateZoom(value) {
    if (!currentPreviewImage) return;

    const scale = value / 100;
    const container = document.getElementById('imageContainer');

    // ✅ ذخیره موقعیت فعلی scroll
    const scrollTopBefore = container ? container.scrollTop : 0;
    const scrollLeftBefore = container ? container.scrollLeft : 0;

    currentPreviewImage.style.transform = `scale(${scale})`;

    const zoomValueEl = document.getElementById('zoomValue');
    if (zoomValueEl) {
        zoomValueEl.textContent = `${value}%`;
    }

    // ✅ اگر Zoom شد، اسکرول را تنظیم کن
    if (container && scale > 1) {
        setTimeout(() => {
            // نگه داشتن نسبت scroll
            container.scrollTop = scrollTopBefore * (scale / (scale - 0.1));
            container.scrollLeft = scrollLeftBefore;
        }, 50);
    }

    console.log('🔍 Zoom:', value + '%');
}

function resetZoom() {
    const slider = document.getElementById('zoomSlider');
    const container = document.getElementById('imageContainer');

    if (slider) {
        slider.value = 100;
        updateZoom(100);
    }

    // ✅ اسکرول به بالا
    if (container) {
        container.scrollTo({
            top: 0,
            left: 0,
            behavior: 'smooth'
        });
    }
}

// ✅ بستن با ESC
document.addEventListener('keydown', function (e) {
    if (e.key === 'Escape') {
        closeImagePreview();
    }
});