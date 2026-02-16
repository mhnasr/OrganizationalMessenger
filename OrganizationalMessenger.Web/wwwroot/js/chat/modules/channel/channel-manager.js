// ============================================
// Channel Manager - مدیریت کانال‌ها
// ============================================

import { getCsrfToken } from '../../utils.js';

export class ChannelManager {
    constructor() {
        this.init();
    }

    init() {
        console.log('📡 ChannelManager initialized');
        this.setupEventListeners();
    }

    setupEventListeners() {
        const createChannelBtn = document.getElementById('createChannelBtn');
        if (createChannelBtn) {
            createChannelBtn.addEventListener('click', () => this.showCreateDialog());
        }
    }

    showCreateDialog() {
        console.log('📝 Opening create channel dialog');

        const dialog = document.createElement('div');
        dialog.className = 'channel-dialog-overlay';
        dialog.innerHTML = `
            <div class="channel-dialog">
                <div class="channel-dialog-header">
                    <h3>ایجاد کانال جدید</h3>
                    <button class="close-dialog" onclick="this.closest('.channel-dialog-overlay').remove()">✕</button>
                </div>
                <div class="channel-dialog-body">
                    <form id="createChannelForm">
                        <div class="form-group">
                            <label>نام کانال *</label>
                            <input type="text" id="channelName" class="form-input" required maxlength="100">
                        </div>
                        <div class="form-group">
                            <label>توضیحات (اختیاری)</label>
                            <textarea id="channelDescription" class="form-input" rows="3" maxlength="500"></textarea>
                        </div>
                        <div class="form-group">
                            <label>تصویر کانال</label>
                            <input type="file"
                                   id="channelAvatarInput"
                                   class="form-input"
                                   accept="image/*">
                            <small class="form-text text-muted">حداکثر 2 مگابایت</small>
                        </div>
                        <div class="form-group">
                            <label>
                                <input type="checkbox" id="channelIsPublic" checked>
                                کانال عمومی
                            </label>
                        </div>
                        <div class="form-group">
                            <label>
                                <input type="checkbox" id="channelOnlyAdminsCanPost" checked>
                                فقط ادمین‌ها می‌توانند پست بگذارند
                            </label>
                        </div>
                        <div class="form-group">
                            <label>
                                <input type="checkbox" id="channelAllowComments">
                                اجازه کامنت‌گذاری
                            </label>
                        </div>
                    </form>
                </div>
                <div class="channel-dialog-footer">
                    <button class="btn-cancel" onclick="this.closest('.channel-dialog-overlay').remove()">انصراف</button>
                    <button class="btn-primary" id="submitCreateChannel">ایجاد کانال</button>
                </div>
            </div>
        `;

        document.body.appendChild(dialog);

        document.getElementById('submitCreateChannel').addEventListener('click', () => {
            this.createChannel();
        });
    }

    async createChannel() {
        const name = document.getElementById('channelName')?.value.trim();
        const description = document.getElementById('channelDescription')?.value.trim();
        const isPublic = document.getElementById('channelIsPublic')?.checked || false;
        const onlyAdminsCanPost = document.getElementById('channelOnlyAdminsCanPost')?.checked || true;
        const allowComments = document.getElementById('channelAllowComments')?.checked || false;
        const avatarFile = document.getElementById('channelAvatarInput')?.files[0];

        if (!name) {
            alert('نام کانال الزامی است');
            return;
        }

        const formData = new FormData();
        formData.append('Name', name);
        formData.append('Description', description || '');
        formData.append('IsPublic', isPublic.toString());
        formData.append('OnlyAdminsCanPost', onlyAdminsCanPost.toString());
        formData.append('AllowComments', allowComments.toString());
        if (avatarFile) {
            formData.append('AvatarFile', avatarFile);
        }

        try {
            const response = await fetch('/api/Channel/Create', {
                method: 'POST',
                headers: {
                    'RequestVerificationToken': getCsrfToken()
                },
                body: formData
            });

            if (!response.ok) {
                throw new Error(`HTTP ${response.status}: ${response.statusText}`);
            }

            const result = await response.json();
            console.log('📥 Create channel response:', result);

            if (result.success) {
                alert('کانال با موفقیت ایجاد شد');

                document.querySelector('.channel-dialog-overlay')?.remove();

                // ✅ تغییر مسیر import
                const { loadChats } = await import('../../chats.js');
                await loadChats('channels');
            } else {
                alert(result.message || 'خطا در ایجاد کانال');
            }
        } catch (error) {
            console.error('❌ Create channel error:', error);
            alert(`خطا در ایجاد کانال: ${error.message}`);
        }
    }
}

const channelManager = new ChannelManager();

console.log('✅ channel-manager.js loaded');