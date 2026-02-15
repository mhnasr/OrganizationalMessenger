// ============================================
// Utility Functions
// ============================================

export function formatPersianTime(dateStr) {
    const date = new Date(dateStr);
    return date.toLocaleTimeString('fa-IR', {
        hour: 'numeric',
        minute: '2-digit'
    });
}

export function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

export function getCsrfToken() {
    return document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
}

export function scrollToBottom() {
    const container = document.getElementById('messagesContainer');
    if (container) {
        container.scrollTop = container.scrollHeight;
    }
}

export function getInitials(name) {
    return name.split(' ').slice(0, 2).map(n => n[0]).join('').toUpperCase();
}

export function formatFileSize(bytes) {
    const sizes = ['B', 'KB', 'MB', 'GB'];
    if (bytes === 0) return '0 B';
    const i = Math.floor(Math.log(bytes) / Math.log(1024));
    return Math.round(bytes / Math.pow(1024, i) * 100) / 100 + ' ' + sizes[i];
}

export function formatDuration(seconds) {
    if (!seconds || isNaN(seconds)) return '0:00';

    const mins = Math.floor(seconds / 60);
    const secs = Math.floor(seconds % 60);
    return `${mins}:${secs.toString().padStart(2, '0')}`;
}