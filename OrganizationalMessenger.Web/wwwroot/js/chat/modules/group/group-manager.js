// ============================================
// Group Manager - مدیریت گروه‌ها
// ============================================

import { getCsrfToken } from '../../utils.js';

export class GroupManager {
    constructor() {
        this.init();
    }

    init() {
        console.log('📦 GroupManager initialized');
        this.setupEventListeners();
    }

    setupEventListeners() {
        const createGroupBtn = document.getElementById('createGroupBtn');
        if (createGroupBtn) {
            createGroupBtn.addEventListener('click', () => this.showCreateDialog());
        }
    }

    showCreateDialog() {
        console.log('📝 Opening create group dialog');

        const dialog = document.createElement('div');
        dialog.className = 'group-dialog-overlay';
        dialog.innerHTML = `
            <div class="group-dialog">
                <div class="group-dialog-header">
                    <h3>ایجاد گروه جدید</h3>
                    <button class="close-dialog" onclick="this.closest('.group-dialog-overlay').remove()">✕</button>
                </div>
                <div class="group-dialog-body">
                    <form id="createGroupForm">
                        <div class="form-group">
                            <label>نام گروه *</label>
                            <input type="text" id="groupName" class="form-input" required maxlength="100">
                        </div>
                        <div class="form-group">
                            <label>توضیحات (اختیاری)</label>
                            <textarea id="groupDescription" class="form-input" rows="3" maxlength="500"></textarea>
                        </div>
                        <div class="form-group">
                            <label>تصویر گروه</label>
                            <input type="file"
                                   id="groupAvatarInput"
                                   class="form-input"
                                   accept="image/*">
                            <small class="form-text text-muted">حداکثر 2 مگابایت</small>
                        </div>
                        <div class="form-group">
                            <label>
                                <input type="checkbox" id="groupIsPublic">
                                گروه عمومی (همه می‌توانند پیدا کنند)
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
                    <button class="btn-primary" id="submitCreateGroup">ایجاد گروه</button>
                </div>
            </div>
        `;

        document.body.appendChild(dialog);

        document.getElementById('submitCreateGroup').addEventListener('click', () => {
            this.createGroup();
        });
    }

    async createGroup() {
        const name = document.getElementById('groupName')?.value.trim();
        const description = document.getElementById('groupDescription')?.value.trim();
        const isPublic = document.getElementById('groupIsPublic')?.checked || false;
        const maxMembers = parseInt(document.getElementById('groupMaxMembers')?.value) || 200;
        const avatarFile = document.getElementById('groupAvatarInput')?.files[0];

        if (!name) {
            alert('نام گروه الزامی است');
            return;
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
                alert('گروه با موفقیت ایجاد شد');

                document.querySelector('.group-dialog-overlay')?.remove();

                const { loadChats } = await import('../../chats.js');
                await loadChats('groups');
            } else {
                alert(result.message || 'خطا در ایجاد گروه');
            }
        } catch (error) {
            console.error('❌ Create group error:', error);
            alert(`خطا در ایجاد گروه: ${error.message}`);
        }
    }

    // ✅ نمایش لیست اعضا و افزودن عضو
    async showMembersDialog(groupId) {
        console.log('👥 Opening members dialog for group:', groupId);

        try {
            const response = await fetch(`/api/Group/${groupId}/Members`);
            const result = await response.json();

            if (!result.success) {
                alert(result.message);
                return;
            }

            const dialog = document.createElement('div');
            dialog.className = 'members-dialog-overlay';
            dialog.innerHTML = `
                <div class="members-dialog">
                    <div class="members-dialog-header">
                        <h3>اعضای گروه</h3>
                        <button class="close-dialog" onclick="this.closest('.members-dialog-overlay').remove()">✕</button>
                    </div>
                    <div class="members-dialog-body">
                        <div class="members-actions">
                            <button class="btn-primary" id="addMemberBtn">
                                <i class="fas fa-user-plus"></i> افزودن عضو
                            </button>
                        </div>
                        <div class="members-list" id="membersList">
                            ${result.members.map(m => `
                                <div class="member-item">
                                    <img src="${m.avatar}" class="member-avatar">
                                    <div class="member-info">
                                        <div class="member-name">${m.name}</div>
                                        <div class="member-role">${this.getRoleName(m.role)}</div>
                                    </div>
                                    ${!m.isAdmin ? `
                                        <button class="btn-danger btn-sm" onclick="window.groupManager.removeMember(${groupId}, ${m.userId})">
                                            <i class="fas fa-times"></i>
                                        </button>
                                    ` : ''}
                                </div>
                            `).join('')}
                        </div>
                    </div>
                </div>
            `;

            document.body.appendChild(dialog);

            document.getElementById('addMemberBtn').addEventListener('click', () => {
                this.showAddMemberDialog(groupId);
            });

        } catch (error) {
            console.error('❌ Error loading members:', error);
            alert('خطا در بارگذاری اعضا');
        }
    }

    // ✅ دیالوگ افزودن عضو
    async showAddMemberDialog(groupId) {
        const dialog = document.createElement('div');
        dialog.className = 'add-member-dialog-overlay';
        dialog.innerHTML = `
            <div class="add-member-dialog">
                <div class="dialog-header">
                    <h3>افزودن عضو</h3>
                    <button class="close-dialog" onclick="this.closest('.add-member-dialog-overlay').remove()">✕</button>
                </div>
                <div class="dialog-body">
                    <div class="form-group">
                        <input type="text" id="searchUsersInput" class="form-input" placeholder="جستجوی کاربر...">
                    </div>
                    <div class="users-list" id="searchResultsList">
                        <!-- نتایج جستجو -->
                    </div>
                </div>
            </div>
        `;

        document.body.appendChild(dialog);

        const searchInput = document.getElementById('searchUsersInput');
        searchInput.addEventListener('input', () => {
            this.searchUsers(groupId, searchInput.value);
        });

        // جستجوی اولیه
        this.searchUsers(groupId, '');
    }

    // ✅ جستجوی کاربران
    async searchUsers(groupId, query) {
        try {
            const response = await fetch(`/api/Group/${groupId}/SearchUsers?query=${encodeURIComponent(query)}`);
            const result = await response.json();

            if (!result.success) {
                alert(result.message);
                return;
            }

            const listEl = document.getElementById('searchResultsList');
            if (result.users.length === 0) {
                listEl.innerHTML = '<p class="text-muted text-center">کاربری یافت نشد</p>';
                return;
            }

            listEl.innerHTML = result.users.map(u => `
                <div class="user-item">
                    <img src="${u.avatar}" class="user-avatar">
                    <div class="user-info">
                        <div class="user-name">${u.name}</div>
                        <div class="user-username">@${u.username}</div>
                    </div>
                    <button class="btn-primary btn-sm" onclick="window.groupManager.addMember(${groupId}, ${u.id})">
                        افزودن
                    </button>
                </div>
            `).join('');
        } catch (error) {
            console.error('❌ Search error:', error);
        }
    }

    // ✅ افزودن عضو
    async addMember(groupId, userId) {
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
                alert('عضو با موفقیت اضافه شد');
                document.querySelector('.add-member-dialog-overlay')?.remove();
                document.querySelector('.members-dialog-overlay')?.remove();
                this.showMembersDialog(groupId);
            } else {
                alert(result.message);
            }
        } catch (error) {
            console.error('❌ Add member error:', error);
            alert('خطا در افزودن عضو');
        }
    }

    // ✅ حذف عضو
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
                alert('عضو حذف شد');
                document.querySelector('.members-dialog-overlay')?.remove();
                this.showMembersDialog(groupId);
            } else {
                alert(result.message);
            }
        } catch (error) {
            console.error('❌ Remove member error:', error);
            alert('خطا در حذف عضو');
        }
    }

    getRoleName(role) {
        const roles = {
            'Owner': 'مالک',
            'Admin': 'مدیر',
            'Member': 'عضو'
        };
        return roles[role] || role;
    }
}

const groupManager = new GroupManager();
window.groupManager = groupManager; // برای دسترسی از onclick

console.log('✅ group-manager.js loaded');