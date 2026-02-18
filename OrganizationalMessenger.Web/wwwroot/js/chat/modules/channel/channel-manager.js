// ============================================
// Channel Manager - مدیریت کانال‌ها (با Permission)
// ============================================

import { getCsrfToken } from '../../utils.js';

export class ChannelManager {
    constructor() {
        this.searchTimer = null;
        this.canCreateChannel = false; // ✅ مقدار اولیه
        this.init();
    }

    async init() { // ✅ async
        console.log('📡 ChannelManager initialized');

        // ✅ اول دسترسی را چک کن و منتظر بمان
        await this.checkCreateChannelPermission();

        // بعد event listener ها را ست کن
        this.setupEventListeners();
    }

    // ✅ چک کردن دسترسی کانال
    async checkCreateChannelPermission() {
        try {
            const response = await fetch('/api/Channel/CanCreateChannel');
            const result = await response.json();

            this.canCreateChannel = result.success && result.canCreate;
            console.log('✅ CanCreateChannel permission:', this.canCreateChannel);

            this.toggleCreateChannelButton();

        } catch (error) {
            console.error('❌ Error checking channel permission:', error);
            this.canCreateChannel = false;
            this.toggleCreateChannelButton();
        }
    }

    // ✅ نمایش/مخفی کردن دکمه کانال
    toggleCreateChannelButton() {
        const createChannelBtn = document.getElementById('createChannelBtn');
        if (!createChannelBtn) {
            console.warn('⚠️ createChannelBtn not found');
            return;
        }

        console.log('🔧 Toggling channel button, canCreateChannel:', this.canCreateChannel);

        if (this.canCreateChannel) {
            createChannelBtn.style.display = 'flex';
            createChannelBtn.style.visibility = 'visible';
            createChannelBtn.classList.remove('hidden');
        } else {
            createChannelBtn.style.display = 'none';
            createChannelBtn.style.visibility = 'hidden';
            createChannelBtn.classList.add('hidden');
        }
    }

    setupEventListeners() {
        const createChannelBtn = document.getElementById('createChannelBtn');
        console.log('🔍 Channel setupEventListeners - canCreateChannel:', this.canCreateChannel);

        if (createChannelBtn) {
            createChannelBtn.addEventListener('click', (e) => {
                e.stopPropagation();
                if (this.canCreateChannel) {
                    console.log('✅ Channel permission granted, opening dialog');
                    this.showCreateDialog();
                } else {
                    console.log('❌ No channel permission');
                }
            });
        }
    }

    // ============================================
    // ایجاد کانال
    // ============================================
    showCreateDialog() {
        console.log('📝 Opening create channel dialog');

        // حذف دیالوگ قبلی
        document.querySelector('.channel-dialog-overlay')?.remove();

        const dialog = document.createElement('div');
        dialog.className = 'channel-dialog-overlay';
        dialog.innerHTML = `
            <div class="channel-dialog">
                <div class="channel-dialog-header">
                    <h3><i class="fas fa-bullhorn"></i> ایجاد کانال جدید</h3>
                    <button class="close-dialog" onclick="this.closest('.channel-dialog-overlay').remove()">✕</button>
                </div>
                <div class="channel-dialog-body">
                    <form id="createChannelForm">
                        <div class="form-group">
                            <label>نام کانال <span class="required">*</span></label>
                            <input type="text" id="channelName" class="form-input" required maxlength="100" placeholder="نام کانال را وارد کنید...">
                        </div>
                        <div class="form-group">
                            <label>توضیحات (اختیاری)</label>
                            <textarea id="channelDescription" class="form-input" rows="3" maxlength="500" placeholder="توضیحات کانال..."></textarea>
                        </div>
                        <div class="form-group">
                            <label>تصویر کانال</label>
                            <input type="file" id="channelAvatarInput" class="form-input" accept="image/*">
                            <small class="form-text text-muted">حداکثر 2 مگابایت</small>
                        </div>
                        <div class="form-group checkbox-group">
                            <label>
                                <input type="checkbox" id="channelIsPublic" checked>
                                <span>کانال عمومی (همه می‌توانند پیدا کنند)</span>
                            </label>
                        </div>
                        <div class="form-group checkbox-group">
                            <label>
                                <input type="checkbox" id="channelOnlyAdminsCanPost" checked>
                                <span>فقط ادمین‌ها می‌توانند پست بگذارند</span>
                            </label>
                        </div>
                        <div class="form-group checkbox-group">
                            <label>
                                <input type="checkbox" id="channelAllowComments">
                                <span>اجازه کامنت‌گذاری</span>
                            </label>
                        </div>
                    </form>
                </div>
                <div class="channel-dialog-footer">
                    <button class="btn-cancel" onclick="this.closest('.channel-dialog-overlay').remove()">انصراف</button>
                    <button class="btn-primary" id="submitCreateChannel">
                        <i class="fas fa-plus"></i> ایجاد کانال
                    </button>
                </div>
            </div>
        `;

        document.body.appendChild(dialog);

        // بستن با کلیک روی overlay
        dialog.addEventListener('click', (e) => {
            if (e.target === dialog) dialog.remove();
        });

        // فوکوس روی نام کانال
        setTimeout(() => {
            document.getElementById('channelName')?.focus();
        }, 100);

        document.getElementById('submitCreateChannel').addEventListener('click', () => {
            this.createChannel();
        });

        // Enter key در فرم
        document.getElementById('channelName')?.addEventListener('keydown', (e) => {
            if (e.key === 'Enter') {
                e.preventDefault();
                this.createChannel();
            }
        });
    }

    async createChannel() {
        const submitBtn = document.getElementById('submitCreateChannel');
        const name = document.getElementById('channelName')?.value.trim();
        const description = document.getElementById('channelDescription')?.value.trim();
        const isPublic = document.getElementById('channelIsPublic')?.checked || false;
        const onlyAdminsCanPost = document.getElementById('channelOnlyAdminsCanPost')?.checked || true;
        const allowComments = document.getElementById('channelAllowComments')?.checked || false;
        const avatarFile = document.getElementById('channelAvatarInput')?.files[0];

        if (!name) {
            alert('نام کانال الزامی است');
            document.getElementById('channelName')?.focus();
            return;
        }

        // غیرفعال کردن دکمه
        if (submitBtn) {
            submitBtn.disabled = true;
            submitBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> در حال ایجاد...';
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
                // بستن دیالوگ ایجاد
                document.querySelector('.channel-dialog-overlay')?.remove();

                // ✅ نمایش موفقیت
                this.showPostCreateDialog(result.channelId, result.channel?.name || name);

                // بروزرسانی لیست چت‌ها
                try {
                    const { loadChats } = await import('../../chats.js');
                    await loadChats('channels');
                } catch (e) {
                    console.warn('⚠️ Could not refresh chat list:', e);
                }
            } else {
                alert(result.message || 'خطا در ایجاد کانال');
            }
        } catch (error) {
            console.error('❌ Create channel error:', error);
            alert(`خطا در ایجاد کانال: ${error.message}`);
        } finally {
            if (submitBtn) {
                submitBtn.disabled = false;
                submitBtn.innerHTML = '<i class="fas fa-plus"></i> ایجاد کانال';
            }
        }
    }

    // ✅ دیالوگ بعد از ایجاد (مشابه گروه)
    showPostCreateDialog(channelId, channelName) {
        const dialog = document.createElement('div');
        dialog.className = 'channel-dialog-overlay';
        dialog.innerHTML = `
            <div class="channel-dialog post-create-dialog">
                <div class="channel-dialog-header success-header">
                    <h3><i class="fas fa-check-circle"></i> کانال ایجاد شد!</h3>
                    <button class="close-dialog" onclick="this.closest('.channel-dialog-overlay').remove()">✕</button>
                </div>
                <div class="channel-dialog-body text-center">
                    <div class="success-icon">
                        <i class="fas fa-bullhorn"></i>
                    </div>
                    <p class="success-message">کانال <strong>${this.escapeHtml(channelName)}</strong> با موفقیت ایجاد شد.</p>
                    <p class="success-sub">آیا می‌خواهید اعضا اضافه کنید؟</p>
                </div>
                <div class="channel-dialog-footer">
                    <button class="btn-cancel" onclick="this.closest('.channel-dialog-overlay').remove()">بعداً</button>
                    <button class="btn-primary" id="goToAddMembers">
                        <i class="fas fa-user-plus"></i> افزودن اعضا
                    </button>
                </div>
            </div>
        `;

        document.body.appendChild(dialog);

        dialog.addEventListener('click', (e) => {
            if (e.target === dialog) dialog.remove();
        });

        document.getElementById('goToAddMembers').addEventListener('click', () => {
            dialog.remove();
            this.showMembersDialog(channelId);
        });
    }

    // Helpers
    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }


   
    // ============================================
    // نمایش لیست اعضا
    // ============================================
    async showMembersDialog(channelId) {
        console.log('👥 Opening members dialog for channel:', channelId);

        try {
            const response = await fetch(`/api/Channel/${channelId}/Members`);
            const result = await response.json();

            if (!result.success) {
                alert(result.message);
                return;
            }

            // حذف دیالوگ قبلی
            document.querySelector('.members-dialog-overlay')?.remove();

            const dialog = document.createElement('div');
            dialog.className = 'members-dialog-overlay';
            dialog.innerHTML = `
            <div class="members-dialog">
                <div class="members-dialog-header">
                    <h3><i class="fas fa-users"></i> اعضای کانال <span class="member-count-badge">${result.members.length} نفر</span></h3>
                    <button class="close-dialog" onclick="this.closest('.members-dialog-overlay').remove()">✕</button>
                </div>
                <div class="members-dialog-body">
                    <div class="members-actions">
                        <button class="btn-add-member" id="addMemberBtn">
                            <i class="fas fa-user-plus"></i> افزودن عضو جدید
                        </button>
                    </div>
                    <div class="members-list" id="membersList">
                        ${result.members.map(m => this.renderMemberItem(m, channelId)).join('')}
                    </div>
                </div>
            </div>
        `;

            document.body.appendChild(dialog);

            // بستن با کلیک روی overlay
            dialog.addEventListener('click', (e) => {
                if (e.target === dialog) dialog.remove();
            });

            document.getElementById('addMemberBtn').addEventListener('click', () => {
                this.showAddMemberDialog(channelId);
            });

        } catch (error) {
            console.error('❌ Error loading members:', error);
            alert('خطا در بارگذاری اعضا');
        }
    }

    // ✅ رندر هر آیتم عضو
    renderMemberItem(member, channelId) {
        const roleClass = member.role === 'Owner' ? 'role-owner' :
            (member.isAdmin ? 'role-admin' : 'role-member');
        const onlineClass = member.isOnline ? 'online' : '';

        return `
        <div class="member-item" data-user-id="${member.userId}">
            <div class="member-avatar-wrapper ${onlineClass}">
                <img src="${member.avatar}" class="member-avatar" alt="${member.name}">
            </div>
            <div class="member-info">
                <div class="member-name">${member.name}</div>
                <div class="member-role ${roleClass}">
                    ${member.role === 'Owner' ? '<i class="fas fa-crown"></i> ' : ''}
                    ${member.isAdmin && member.role !== 'Owner' ? '<i class="fas fa-shield-alt"></i> ' : ''}
                    ${this.getRoleName(member.role)}
                    ${member.canPost ? '<span class="post-badge">📝</span>' : ''}
                </div>
            </div>
            ${member.role !== 'Owner' && !member.isAdmin ? `
                <button class="btn-remove-member" onclick="window.channelManager.removeMember(${channelId}, ${member.userId})" title="حذف عضو">
                    <i class="fas fa-times"></i>
                </button>
            ` : ''}
        </div>
    `;
    }

    // ============================================
    // دیالوگ افزودن عضو
    // ============================================
    async showAddMemberDialog(channelId) {
        // حذف دیالوگ قبلی
        document.querySelector('.add-member-dialog-overlay')?.remove();

        const dialog = document.createElement('div');
        dialog.className = 'add-member-dialog-overlay';
        dialog.innerHTML = `
        <div class="add-member-dialog">
            <div class="add-member-dialog-header">
                <h3><i class="fas fa-user-plus"></i> افزودن عضو</h3>
                <button class="close-dialog" onclick="this.closest('.add-member-dialog-overlay').remove()">✕</button>
            </div>
            <div class="add-member-dialog-body">
                <div class="search-wrapper">
                    <i class="fas fa-search search-icon"></i>
                    <input type="text" id="searchUsersInput" class="search-input" placeholder="نام یا نام کاربری را جستجو کنید..." autofocus>
                </div>
                <div class="users-list" id="searchResultsList">
                    <div class="loading-users">
                        <i class="fas fa-spinner fa-spin"></i> در حال بارگذاری...
                    </div>
                </div>
            </div>
        </div>
    `;

        document.body.appendChild(dialog);

        // بستن با کلیک روی overlay
        dialog.addEventListener('click', (e) => {
            if (e.target === dialog) dialog.remove();
        });

        const searchInput = document.getElementById('searchUsersInput');

        // debounce جستجو - 300 میلی‌ثانیه
        searchInput.addEventListener('input', () => {
            if (this.searchTimer) clearTimeout(this.searchTimer);
            this.searchTimer = setTimeout(() => {
                this.searchUsers(channelId, searchInput.value);
            }, 300);
        });

        // فوکوس
        setTimeout(() => searchInput?.focus(), 100);

        // جستجوی اولیه (نمایش همه کاربران)
        this.searchUsers(channelId, '');
    }

    // ============================================
    // جستجوی کاربران
    // ============================================
    async searchUsers(channelId, query) {
        const listEl = document.getElementById('searchResultsList');
        if (!listEl) return;

        // نمایش loading
        listEl.innerHTML = '<div class="loading-users"><i class="fas fa-spinner fa-spin"></i> در حال جستجو...</div>';

        try {
            const response = await fetch(`/api/Channel/${channelId}/SearchUsers?query=${encodeURIComponent(query)}`);
            const result = await response.json();

            if (!result.success) {
                listEl.innerHTML = `<div class="no-results"><i class="fas fa-exclamation-circle"></i> ${result.message}</div>`;
                return;
            }

            if (result.users.length === 0) {
                listEl.innerHTML = `
                <div class="no-results">
                    <i class="fas fa-user-slash"></i>
                    <p>کاربری یافت نشد</p>
                    ${query ? '<small>عبارت دیگری را امتحان کنید</small>' : '<small>همه کاربران عضو هستند</small>'}
                </div>
            `;
                return;
            }

            listEl.innerHTML = result.users.map(u => `
            <div class="user-item" data-user-id="${u.id}">
                <div class="user-avatar-wrapper ${u.isOnline ? 'online' : ''}">
                    <img src="${u.avatar}" class="user-avatar" alt="${u.name}">
                </div>
                <div class="user-info">
                    <div class="user-name">${u.name}</div>
                    <div class="user-username">@${u.username}</div>
                </div>
                <button class="btn-add-user" onclick="window.channelManager.addMember(${channelId}, ${u.id}, this)" title="افزودن">
                    <i class="fas fa-plus"></i> افزودن
                </button>
            </div>
        `).join('');

        } catch (error) {
            console.error('❌ Search error:', error);
            listEl.innerHTML = '<div class="no-results"><i class="fas fa-exclamation-triangle"></i> خطا در جستجو</div>';
        }
    }

    // ============================================
    // افزودن عضو
    // ============================================
    async addMember(channelId, userId, buttonEl = null) {
        // غیرفعال کردن دکمه فوری
        if (buttonEl) {
            buttonEl.disabled = true;
            buttonEl.innerHTML = '<i class="fas fa-spinner fa-spin"></i>';
        }

        try {
            const response = await fetch(`/api/Channel/${channelId}/AddMember`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': getCsrfToken()
                },
                body: JSON.stringify({ userId })
            });

            const result = await response.json();

            if (result.success) {
                // حذف کاربر از لیست جستجو (بدون بستن دیالوگ)
                const userItem = buttonEl?.closest('.user-item');
                if (userItem) {
                    userItem.style.transition = 'all 0.3s ease';
                    userItem.style.opacity = '0';
                    userItem.style.transform = 'translateX(-20px)';
                    setTimeout(() => {
                        userItem.remove();

                        // اگر لیست خالی شد
                        const listEl = document.getElementById('searchResultsList');
                        if (listEl && listEl.children.length === 0) {
                            listEl.innerHTML = `
                            <div class="no-results">
                                <i class="fas fa-check-circle" style="color: #4caf50;"></i>
                                <p>همه کاربران اضافه شدند!</p>
                            </div>
                        `;
                        }
                    }, 300);
                }

                this.showToast(`${result.member?.name || 'عضو'} اضافه شد`, 'success');

            } else {
                this.showToast(result.message || 'خطا', 'error');
                if (buttonEl) {
                    buttonEl.disabled = false;
                    buttonEl.innerHTML = '<i class="fas fa-plus"></i> افزودن';
                }
            }
        } catch (error) {
            console.error('❌ Add member error:', error);
            this.showToast('خطا در افزودن عضو', 'error');
            if (buttonEl) {
                buttonEl.disabled = false;
                buttonEl.innerHTML = '<i class="fas fa-plus"></i> افزودن';
            }
        }
    }

    // ============================================
    // حذف عضو
    // ============================================
    async removeMember(channelId, userId) {
        if (!confirm('آیا از حذف این عضو اطمینان دارید؟')) return;

        try {
            const response = await fetch(`/api/Channel/${channelId}/RemoveMember`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': getCsrfToken()
                },
                body: JSON.stringify({ userId })
            });

            const result = await response.json();

            if (result.success) {
                // حذف آیتم از لیست با انیمیشن
                const memberItem = document.querySelector(`.member-item[data-user-id="${userId}"]`);
                if (memberItem) {
                    memberItem.style.transition = 'all 0.3s ease';
                    memberItem.style.opacity = '0';
                    memberItem.style.transform = 'translateX(-20px)';
                    setTimeout(() => {
                        memberItem.remove();
                        // بروزرسانی تعداد اعضا
                        const badge = document.querySelector('.member-count-badge');
                        if (badge) {
                            const count = document.querySelectorAll('.member-item').length;
                            badge.textContent = `${count} نفر`;
                        }
                    }, 300);
                }
                this.showToast('عضو حذف شد', 'success');
            } else {
                this.showToast(result.message, 'error');
            }
        } catch (error) {
            console.error('❌ Remove member error:', error);
            this.showToast('خطا در حذف عضو', 'error');
        }
    }

    // ============================================
    // Toast Notification
    // ============================================
    showToast(message, type = 'info') {
        // حذف toast قبلی
        document.querySelector('.cm-toast')?.remove();

        const toast = document.createElement('div');
        toast.className = `cm-toast cm-toast-${type}`;

        const icon = type === 'success' ? 'fa-check-circle' :
            type === 'error' ? 'fa-exclamation-circle' : 'fa-info-circle';

        toast.innerHTML = `<i class="fas ${icon}"></i> ${this.escapeHtml(message)}`;
        document.body.appendChild(toast);

        // نمایش
        requestAnimationFrame(() => {
            toast.classList.add('show');
        });

        // حذف بعد از 3 ثانیه
        setTimeout(() => {
            toast.classList.remove('show');
            setTimeout(() => toast.remove(), 300);
        }, 3000);
    }

    // ============================================
    // Helpers
    // ============================================
    getRoleName(role) {
        const roles = {
            'Owner': 'مالک',
            'Admin': 'ادمین',
            'Moderator': 'مدیر',
            'Subscriber': 'مشترک'
        };
        return roles[role] || role;
    }

    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }




}





const channelManager = new ChannelManager();
window.channelManager = channelManager;

console.log('✅ channel-manager.js loaded');
