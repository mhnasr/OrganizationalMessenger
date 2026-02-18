// ============================================
// Group Manager - مدیریت گروه‌ها
// ============================================

import { getCsrfToken } from '../../utils.js';

export class GroupManager {
    constructor() {
        this.searchTimer = null;
        this.canCreateGroup = false; // ✅ مقدار اولیه
        this.init();
    }

    async init() { // ✅ async کنید
        console.log('📦 GroupManager initialized');

        // ✅ اول دسترسی را چک کن و منتظر بمان
        await this.checkCreateGroupPermission();

        // بعد event listener ها را ست کن
        this.setupEventListeners();
    }

    // ✅ چک کردن دسترسی
    async checkCreateGroupPermission() {
        try {
            const response = await fetch('/api/Group/CanCreateGroup');
            const result = await response.json();

            this.canCreateGroup = result.success && result.canCreate;
            console.log('✅ CanCreateGroup permission:', this.canCreateGroup);

            this.toggleCreateGroupButton();

        } catch (error) {
            console.error('❌ Error checking group permission:', error);
            this.canCreateGroup = false;
            this.toggleCreateGroupButton();
        }
    }

    // ✅ نمایش/مخفی کردن دکمه
    toggleCreateGroupButton() {
        const createGroupBtn = document.getElementById('createGroupBtn');
        if (!createGroupBtn) {
            console.warn('⚠️ createGroupBtn not found');
            return;
        }

        console.log('🔧 Toggling button, canCreateGroup:', this.canCreateGroup);

        if (this.canCreateGroup) {
            createGroupBtn.style.display = 'flex'; // ✅ برای menu item
            createGroupBtn.style.visibility = 'visible';
            createGroupBtn.classList.remove('hidden');
        } else {
            createGroupBtn.style.display = 'none';
            createGroupBtn.style.visibility = 'hidden';
            createGroupBtn.classList.add('hidden');
        }
    }

    setupEventListeners() {
        const createGroupBtn = document.getElementById('createGroupBtn');
        console.log('🔍 setupEventListeners - canCreateGroup:', this.canCreateGroup);

        if (createGroupBtn) {
            // ✅ همیشه event listener ست کن، شرط را در click handler بگذار
            createGroupBtn.addEventListener('click', (e) => {
                e.stopPropagation(); // ✅ مهم!
                if (this.canCreateGroup) {
                    console.log('✅ Permission granted, opening dialog');
                    this.showCreateDialog();
                } else {
                    console.log('❌ No permission');
                    // اختیاری: toast یا alert
                }
            });
        } else {
            console.warn('⚠️ createGroupBtn not found in setupEventListeners');
        }
    }

   




    // ============================================
    // ایجاد گروه
    // ============================================
    showCreateDialog() {
        console.log('📝 Opening create group dialog');

        // حذف دیالوگ قبلی
        document.querySelector('.group-dialog-overlay')?.remove();

        const dialog = document.createElement('div');
        dialog.className = 'group-dialog-overlay';
        dialog.innerHTML = `
            <div class="group-dialog">
                <div class="group-dialog-header">
                    <h3><i class="fas fa-users"></i> ایجاد گروه جدید</h3>
                    <button class="close-dialog" onclick="this.closest('.group-dialog-overlay').remove()">✕</button>
                </div>
                <div class="group-dialog-body">
                    <form id="createGroupForm">
                        <div class="form-group">
                            <label>نام گروه <span class="required">*</span></label>
                            <input type="text" id="groupName" class="form-input" required maxlength="100" placeholder="نام گروه را وارد کنید...">
                        </div>
                        <div class="form-group">
                            <label>توضیحات (اختیاری)</label>
                            <textarea id="groupDescription" class="form-input" rows="3" maxlength="500" placeholder="توضیحات گروه..."></textarea>
                        </div>
                        <div class="form-group">
                            <label>تصویر گروه</label>
                            <input type="file" id="groupAvatarInput" class="form-input" accept="image/*">
                            <small class="form-text text-muted">حداکثر 2 مگابایت</small>
                        </div>
                        <div class="form-group checkbox-group">
                            <label>
                                <input type="checkbox" id="groupIsPublic">
                                <span>گروه عمومی (همه می‌توانند پیدا کنند)</span>
                            </label>
                        </div>
                        <div class="form-group">
                            <label>حداکثر تعداد اعضا</label>
                            <input type="number" id="groupMaxMembers" class="form-input" value="200" min="2" max="1000">
                        </div>
                    </form>
                </div>
                <div class="group-dialog-footer">
                    <button class="btn-cancel" onclick="this.closest('.group-dialog-overlay').remove()">انصراف</button>
                    <button class="btn-primary" id="submitCreateGroup">
                        <i class="fas fa-plus"></i> ایجاد گروه
                    </button>
                </div>
            </div>
        `;

        document.body.appendChild(dialog);

        // بستن با کلیک روی overlay
        dialog.addEventListener('click', (e) => {
            if (e.target === dialog) dialog.remove();
        });

        // فوکوس روی نام گروه
        setTimeout(() => {
            document.getElementById('groupName')?.focus();
        }, 100);

        document.getElementById('submitCreateGroup').addEventListener('click', () => {
            this.createGroup();
        });

        // Enter key در فرم
        document.getElementById('groupName')?.addEventListener('keydown', (e) => {
            if (e.key === 'Enter') {
                e.preventDefault();
                this.createGroup();
            }
        });
    }

    async createGroup() {
        const submitBtn = document.getElementById('submitCreateGroup');
        const name = document.getElementById('groupName')?.value.trim();
        const description = document.getElementById('groupDescription')?.value.trim();
        const isPublic = document.getElementById('groupIsPublic')?.checked || false;
        const maxMembers = parseInt(document.getElementById('groupMaxMembers')?.value) || 200;
        const avatarFile = document.getElementById('groupAvatarInput')?.files[0];

        if (!name) {
            alert('نام گروه الزامی است');
            document.getElementById('groupName')?.focus();
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
        formData.append('MaxMembers', maxMembers.toString());
        if (avatarFile) {
            formData.append('AvatarFile', avatarFile);
        }

        try {
            const response = await fetch('/api/Group/Create', {
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
            console.log('📥 Create group response:', result);

            if (result.success) {
                // بستن دیالوگ ایجاد
                document.querySelector('.group-dialog-overlay')?.remove();

                // ✅ بلافاصله مودال "افزودن اعضا" را باز کن
                this.showPostCreateDialog(result.groupId, result.group?.name || name);

                // بروزرسانی لیست چت‌ها
                try {
                    const { loadChats } = await import('../../chats.js');
                    await loadChats('groups');
                } catch (e) {
                    console.warn('⚠️ Could not refresh chat list:', e);
                }
            } else {
                alert(result.message || 'خطا در ایجاد گروه');
            }
        } catch (error) {
            console.error('❌ Create group error:', error);
            alert(`خطا در ایجاد گروه: ${error.message}`);
        } finally {
            if (submitBtn) {
                submitBtn.disabled = false;
                submitBtn.innerHTML = '<i class="fas fa-plus"></i> ایجاد گروه';
            }
        }
    }

    // ============================================
    // ✅ دیالوگ بعد از ایجاد گروه - پیشنهاد افزودن اعضا
    // ============================================
    showPostCreateDialog(groupId, groupName) {
        const dialog = document.createElement('div');
        dialog.className = 'group-dialog-overlay';
        dialog.innerHTML = `
            <div class="group-dialog post-create-dialog">
                <div class="group-dialog-header success-header">
                    <h3><i class="fas fa-check-circle"></i> گروه ایجاد شد!</h3>
                    <button class="close-dialog" onclick="this.closest('.group-dialog-overlay').remove()">✕</button>
                </div>
                <div class="group-dialog-body text-center">
                    <div class="success-icon">
                        <i class="fas fa-users"></i>
                    </div>
                    <p class="success-message">گروه <strong>${this.escapeHtml(groupName)}</strong> با موفقیت ایجاد شد.</p>
                    <p class="success-sub">آیا می‌خواهید اعضا اضافه کنید؟</p>
                </div>
                <div class="group-dialog-footer">
                    <button class="btn-cancel" onclick="this.closest('.group-dialog-overlay').remove()">بعداً</button>
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
            this.showMembersDialog(groupId);
        });
    }

    // ============================================
    // نمایش لیست اعضا
    // ============================================
    async showMembersDialog(groupId) {
        console.log('👥 Opening members dialog for group:', groupId);

        try {
            const response = await fetch(`/api/Group/${groupId}/Members`);
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
                        <h3><i class="fas fa-users"></i> اعضای گروه <span class="member-count-badge">${result.members.length} نفر</span></h3>
                        <button class="close-dialog" onclick="this.closest('.members-dialog-overlay').remove()">✕</button>
                    </div>
                    <div class="members-dialog-body">
                        <div class="members-actions">
                            <button class="btn-add-member" id="addMemberBtn">
                                <i class="fas fa-user-plus"></i> افزودن عضو جدید
                            </button>
                        </div>
                        <div class="members-list" id="membersList">
                            ${result.members.map(m => this.renderMemberItem(m, groupId)).join('')}
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
                this.showAddMemberDialog(groupId);
            });

        } catch (error) {
            console.error('❌ Error loading members:', error);
            alert('خطا در بارگذاری اعضا');
        }
    }

    // ✅ رندر هر آیتم عضو
    renderMemberItem(member, groupId) {
        const roleClass = member.role === 'Owner' ? 'role-owner' : (member.isAdmin ? 'role-admin' : 'role-member');
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
                    </div>
                </div>
                ${member.role !== 'Owner' && !member.isAdmin ? `
                    <button class="btn-remove-member" onclick="window.groupManager.removeMember(${groupId}, ${member.userId})" title="حذف عضو">
                        <i class="fas fa-times"></i>
                    </button>
                ` : ''}
            </div>
        `;
    }

    // ============================================
    // دیالوگ افزودن عضو
    // ============================================
    async showAddMemberDialog(groupId) {
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

        // ✅ debounce جستجو - 300 میلی‌ثانیه
        searchInput.addEventListener('input', () => {
            if (this.searchTimer) clearTimeout(this.searchTimer);
            this.searchTimer = setTimeout(() => {
                this.searchUsers(groupId, searchInput.value);
            }, 300);
        });

        // فوکوس
        setTimeout(() => searchInput?.focus(), 100);

        // جستجوی اولیه (نمایش همه کاربران)
        this.searchUsers(groupId, '');
    }

    // ============================================
    // جستجوی کاربران
    // ============================================
    async searchUsers(groupId, query) {
        const listEl = document.getElementById('searchResultsList');
        if (!listEl) return;

        // نمایش loading
        listEl.innerHTML = '<div class="loading-users"><i class="fas fa-spinner fa-spin"></i> در حال جستجو...</div>';

        try {
            const response = await fetch(`/api/Group/${groupId}/SearchUsers?query=${encodeURIComponent(query)}`);
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
                    <button class="btn-add-user" onclick="window.groupManager.addMember(${groupId}, ${u.id}, this)" title="افزودن">
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
    async addMember(groupId, userId, buttonEl = null) {
        // ✅ غیرفعال کردن دکمه فوری
        if (buttonEl) {
            buttonEl.disabled = true;
            buttonEl.innerHTML = '<i class="fas fa-spinner fa-spin"></i>';
        }

        try {
            const response = await fetch(`/api/Group/${groupId}/AddMember`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': getCsrfToken()
                },
                body: JSON.stringify({ userId })
            });

            const result = await response.json();

            if (result.success) {
                // ✅ حذف کاربر از لیست جستجو (بدون بستن دیالوگ)
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

                // ✅ نمایش toast بجای alert
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
    async removeMember(groupId, userId) {
        if (!confirm('آیا از حذف این عضو اطمینان دارید؟')) return;

        try {
            const response = await fetch(`/api/Group/${groupId}/RemoveMember`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': getCsrfToken()
                },
                body: JSON.stringify({ userId })
            });

            const result = await response.json();

            if (result.success) {
                // ✅ حذف آیتم از لیست با انیمیشن
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
        document.querySelector('.gm-toast')?.remove();

        const toast = document.createElement('div');
        toast.className = `gm-toast gm-toast-${type}`;

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
            'Admin': 'مدیر',
            'Member': 'عضو'
        };
        return roles[role] || role;
    }

    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }
}

const groupManager = new GroupManager();
window.groupManager = groupManager;

console.log('✅ group-manager.js loaded');