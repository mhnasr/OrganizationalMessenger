class ChatController {
    constructor() {
        this.chatRoom = new ChatRoom('org-room-1');
        this.socket = null;
        this.init();
    }

    init() {
        this.connectSocket();
        this.loadMessages();
    }

    connectSocket() {
        const script = document.createElement('script');
        script.src = 'https://cdn.socket.io/4.7.5/socket.io.min.js';
        script.onload = () => {
            this.socket = io('http://localhost:3000'); // آدرس سرور خودتون
            this.setupSocketEvents();
        };
        document.head.appendChild(script);
    }

    setupSocketEvents() {
        this.socket.on('message', (data) => {
            const message = new Message(
                data.id, data.userId, data.username,
                data.text, data.timestamp, data.isOwn
            );
            this.chatRoom.addMessage(message);
            this.renderMessages();
        });

        this.socket.on('typing', (data) => {
            if (data.isTyping) {
                this.chatRoom.typingUsers.add(data.username);
            } else {
                this.chatRoom.typingUsers.delete(data.username);
            }
            this.renderTyping();
        });

        this.socket.on('users', (users) => {
            this.chatRoom.users = users;
            this.renderUsers();
        });
    }

    sendMessage(text) {
        if (text.trim() && this.socket) {
            this.socket.emit('message', {
                text: text.trim(),
                userId: 'user123', // از session بگیرید
                username: 'محمد نصر' // از session بگیرید
            });
        }
    }

    startTyping() {
        if (this.socket) this.socket.emit('typing', true);
    }

    stopTyping() {
        if (this.socket) this.socket.emit('typing', false);
    }

    loadMessages() {
        // شبیه‌سازی پیام‌های قبلی
        const sampleMessages = [
            new Message('1', 'user1', 'علی', 'سلام چطوری؟', Date.now() - 60000),
            new Message('2', 'user2', 'سارا', 'خوبم ممنون', Date.now() - 30000)
        ];
        this.chatRoom.messages = sampleMessages;
        this.renderMessages();
    }

    renderMessages() {
        const container = document.querySelector('.messages-container');
        if (!container) return;

        container.innerHTML = this.chatRoom.messages.map(msg => `
            <div class="message ${msg.isOwn ? 'own' : 'other'}">
                <div class="message-header">
                    <span class="username">${msg.username}</span>
                    <span class="time">${msg.formattedTime}</span>
                </div>
                <div class="message-text">${msg.text}</div>
            </div>
        `).join('');

        this.scrollToBottom();
    }

    renderTyping() {
        const typingEl = document.querySelector('.typing-indicator');
        if (this.chatRoom.typingUsers.size > 0) {
            typingEl.innerHTML = `${Array.from(this.chatRoom.typingUsers).join(', ')} در حال تایپ...`;
            typingEl.style.display = 'block';
        } else {
            typingEl.style.display = 'none';
        }
    }

    renderUsers() {
        const usersEl = document.querySelector('.online-users');
        usersEl.innerHTML = this.chatRoom.users.map(user =>
            `<span class="user-tag">${user.name}</span>`
        ).join('');
    }

    scrollToBottom() {
        const container = document.querySelector('.messages-container');
        container.scrollTop = container.scrollHeight;
    }
}
