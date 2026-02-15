// ============================================
// File Upload & Rendering
// ============================================

import { getCsrfToken, escapeHtml, formatFileSize } from './utils.js';
import { currentChat } from './variables.js';
import { scrollToBottom } from './utils.js';

export async function handleFileSelect(e) {
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
                <button class="close-dialog" onclick="window.closeCaptionDialog()">✕</button>
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
                <button class="btn-cancel" onclick="window.closeCaptionDialog()">انصراف</button>
                <button class="btn-send" onclick="window.sendFileWithCaption()">
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

export function closeCaptionDialog() {
    const dialog = document.getElementById('captionDialog');
    if (dialog) {
        dialog.remove();
        window.pendingFile = null;
        document.body.style.overflow = 'auto';
    }
}

export async function sendFileWithCaption() {
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
    if (!currentChat || !window.connection) return;

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
            if (window.connection?.state === signalR.HubConnectionState.Connected) {
                if (currentChat.type === 'private') {
                    await window.connection.invoke(
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

export function renderFileAttachment(file) {
    const fileType = file.fileType || 'Document';
    console.log('🔍 Rendering file:', file.originalFileName, 'Type:', fileType);

    if (fileType === 'Image') {
        return `
            <div class="message-file image-file">
                <img src="${file.thumbnailUrl || file.fileUrl}" 
                     alt="${file.originalFileName}" 
                     onclick="window.openImagePreview('${file.fileUrl}')"
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
        return window.renderAudioPlayer(file);
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

// Export to window
window.closeCaptionDialog = closeCaptionDialog;
window.sendFileWithCaption = sendFileWithCaption;