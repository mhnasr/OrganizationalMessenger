// ============================================
// Group Manager - مدیریت اعضای گروه
// ============================================

// ✅ باز کردن پنل مدیریت اعضا
async function openGroupMembersPanel(groupId) {
    console.log('👥 Opening group members panel for group:', groupId);

    try {
        // دریافت اعضای گروه
        const response = await fetch(`/api/Group/${groupId}/Members`, {
            headers: {
                'RequestVerificationToken': getCsrfToken()
            }
        });

        if (!response.ok) {
            alert('خطا در دریافت اعضای گروه');
            return;
        }

        const result = await response.json();

        if (!result.success) {
            alert(result.message || 'خطا در دریافت اعضا');
            return;
        }

        showGroupMembersDialog(groupId, result.members);

    } catch (error) {
        console.error('❌ Error loading group members:', error);
        alert('خطا در بارگذاری اعضای گروه');
    }
}

// ✅ نمایش دیالوگ اعضای گروه
function showGroupMembersDialog(groupId, members) {
    // حذف دیالوگ قبلی
    const existingDialog = document.getElementById('groupMembersDialog');
    if (existingDialog) existingDialog.remove();

    const dialog = document.createElement('div');
    dialog.id = 'groupMembersDialog';
    dialog.className = 'group-members-overlay';

    const currentUserIsAdmin = members.some(m =>
        m.userId === window.currentUserId && m.isAdmin
    );

    dialog.innerHTML = `
        <div class="group-members-dialog">
            <!-- هدر -->
            <div class="group-members-header">
                <h3>
                    <i class="fas fa-users"></i>
                    اعضای گروه
                    <span class="member-count-badge">${members.length} نفر</span>
                </h3>
                <button class="close-dialog" onclick="closeGroupMembersDialog()">✕</button>
            </div>

            <!-- تب‌ها -->
            <div class="group-members-tabs">
                <button class="gm-tab active" data-tab="members" onclick="switchGroupTab('members')">
                    <i class="fas fa-users"></i>
                    اعضا (${members.length})
                </button>
                ${currentUserIsAdmin ? `
                <button class="gm-tab" data-tab="add" onclick="switchGroupTab('add')">
                    <i class="fas fa-user-plus"></i>
                    افزودن عضو
                </button>
                ` : ''}
            </div>

            <!-- محتوای تب اعضا -->
            <div class="gm-tab-content" id="gm-tab-members">
                <div class="group-members-search">
                    <i class="fas fa-search"></i>
                    <input type="text" id="memberSearchInput" 
                           placeholder="جستجوی اعضا..." 
                           oninput="filterGroupMembers(this.value)">
                </div>
                <div class="group-members-list" id="membersList">
                    ${renderMembersList(members, currentUserIsAdmin, groupId)}
                </div>
            </div>

            <!-- محتوای تب افزودن عضو -->
            ${currentUserIsAdmin ? `
            <div class="gm-tab-content" id="gm-tab-add" style="display: none;">
                <div class="group-members-search">
                    <i class="fas fa-search"></i>
                    <input type="text" id="addMemberSearchInput" 
                           placeholder="جستجوی کاربر برای اضافه کردن..."
                           oninput="searchUsersForGroup(${groupId}, this.value)">
                </div>
                <div class="search-hint">
                    <i class="fas fa-info-circle"></i>
                    نام یا نام کاربری فرد مورد نظر را جستجو کنید
                </div>
                <div class="group-members-list" id="addMembersList">
                    <!-- نتایج جستجو اینجا نمایش داده می‌شود -->
                </div>
            </div>
            ` : ''}
        </div>
    `;

    document.body.appendChild(dialog);
    document.body.style.overflow = 'hidden';

    // انیمیشن ورود
    requestAnimationFrame(() => {
        dialog.classList.add('active');
    });
}

// ✅ رندر لیست اعضا
function renderMembersList(members, isAdmin, groupId) {
    if (members.length === 0) {
        return '<div class="no-members">هیچ عضوی یافت نشد</div>';
    }

    return members.map(member => {
        const roleLabel = getRoleLabel(member.role);
        const roleBadge = member.isAdmin
            ? `<span class="role-badge admin">${roleLabel}</span>`
            : `<span class="role-badge member">${roleLabel}</span>`;

        const isCurrentUser = member.userId === window.currentUserId;
        const isOwner = member.role === 'Owner';

        // دکمه حذف فقط برای ادمین‌ها و نه برای سازنده
        let actionButtons = '';
        if (isAdmin && !isCurrentUser && !isOwner) {
            actionButtons = `
                <button class="member-action-btn remove" 
                        onclick="removeMemberFromGroup(${groupId}, ${member.userId}, '${escapeHtml(member.name)}')"
                        title="حذف عضو">
                    <i class="fas fa-user-minus"></i>
                </button>
            `;
        }

        return `
            <div class="group-member-item" data-member-name="${escapeHtml(member.name).toLowerCase()}">
                <div class="member-avatar ${member.isOnline ? 'online' : ''}">
                    <img src="${member.avatar}" alt="${escapeHtml(member.name)}">
                </div>
                <div class="member-info">
                    <div class="member-name-row">
                        <span class="member-name">
                            ${escapeHtml(member.name)}
                            ${isCurrentUser ? '<span class="you-badge">(شما)</span>' : ''}
                        </span>
                        ${roleBadge}
                    </div>
                    <span class="member-username">@${escapeHtml(member.username)}</span>
                </div>
                <div class="member-actions">
                    ${actionButtons}
                </div>
            </div>
        `;
    }).join('');
}

// ✅ تبدیل نقش به فارسی
function getRoleLabel(role) {
    const roles = {
        'Owner': 'سازنده',
        'Admin': 'مدیر',
        'Member': 'عضو'
    };
    return roles[role] || 'عضو';
}

// ✅ فیلتر اعضا
function filterGroupMembers(query) {
    const items = document.querySelectorAll('#membersList .group-member-item');
    const lowerQuery = query.toLowerCase();

    items.forEach(item => {
        const name = item.dataset.memberName || '';
        item.style.display = name.includes(lowerQuery) ? 'flex' : 'none';
    });
}

// ✅ تغییر تب
function switchGroupTab(tab) {
    // تغییر تب فعال
    document.querySelectorAll('.gm-tab').forEach(t => t.classList.remove('active'));
    document.querySelector(`.gm-tab[data-tab="${tab}"]`).classList.add('active');

    // نمایش محتوا
    document.querySelectorAll('.gm-tab-content').forEach(c => c.style.display = 'none');
    document.getElementById(`gm-tab-${tab}`).style.display = 'block';

    // اگر تب افزودن عضو باز شد، فوکوس روی input
    if (tab === 'add') {
        setTimeout(() => {
            document.getElementById('addMemberSearchInput')?.focus();
        }, 100);
    }
}

// ✅ جستجوی کاربران برای اضافه کردن
let searchTimeout = null;
async function searchUsersForGroup(groupId, query) {
    if (searchTimeout) clearTimeout(searchTimeout);

    const container = document.getElementById('addMembersList');

    if (query.length < 1) {
        container.innerHTML = `
            <div class="search-empty">
                <i class="fas fa-search fa-2x"></i>
                <p>نام کاربر را جستجو کنید</p>
            </div>
        `;
        return;
    }

    // دبانس
    searchTimeout = setTimeout(async () => {
        container.innerHTML = `
            <div class="search-loading">
                <div class="spinner"></div>
                <span>در حال جستجو...</span>
            </div>
        `;

        try {
            const response = await fetch(
                `/api/Group/${groupId}/SearchUsers?query=${encodeURIComponent(query)}`,
                {
                    headers: {
                        'RequestVerificationToken': getCsrfToken()
                    }
                }
            );

            const result = await response.json();

            if (!result.success) {
                container.innerHTML = `<div class="search-empty"><p>${result.message}</p></div>`;
                return;
            }

            if (result.users.length === 0) {
                container.innerHTML = `
                    <div class="search-empty">
                        <i class="fas fa-user-slash fa-2x"></i>
                        <p>کاربری یافت نشد</p>
                    </div>
                `;
                return;
            }

            container.innerHTML = result.users.map(user => `
                <div class="group-member-item add-mode" id="add-user-${user.id}">
                    <div class="member-avatar ${user.isOnline ? 'online' : ''}">
                        <img src="${user.avatar}" alt="${escapeHtml(user.name)}">
                    </div>
                    <div class="member-info">
                        <span class="member-name">${escapeHtml(user.name)}</span>
                        <span class="member-username">@${escapeHtml(user.username)}</span>
                    </div>
                    <button class="member-action-btn add" 
                            onclick="addMemberToGroup(${groupId}, ${user.id}, '${escapeHtml(user.name)}')"
                            title="افزودن به گروه">
                        <i class="fas fa-user-plus"></i>
                        افزودن
                    </button>
                </div>
            `).join('');

        } catch (error) {
            console.error('❌ Search error:', error);
            container.innerHTML = `<div class="search-empty"><p>خطا در جستجو</p></div>`;
        }
    }, 300);
}

// ✅ افزودن عضو به گروه
async function addMemberToGroup(groupId, userId, userName) {
    try {
        const btn = document.querySelector(`#add-user-${userId} .member-action-btn`);
        if (btn) {
            btn.disabled = true;
            btn.innerHTML = '<div class="spinner-small"></div>';
        }

        const response = await fetch(`/api/Group/${groupId}/AddMember`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getCsrfToken()
            },
            body: JSON.stringify({ userId: userId })
        });

        const result = await response.json();

        if (result.success) {
            // تغییر دکمه به تیک سبز
            if (btn) {
                btn.innerHTML = '<i class="fas fa-check"></i> اضافه شد';
                btn.classList.remove('add');
                btn.classList.add('added');
                btn.disabled = true;
            }

            // نمایش پیام موفقیت
            showToast(`${userName} به گروه اضافه شد`, 'success');

            // به‌روزرسانی تعداد اعضا
            const countBadge = document.querySelector('.member-count-badge');
            if (countBadge) {
                const currentCount = parseInt(countBadge.textContent) || 0;
                countBadge.textContent = `${currentCount + 1} نفر`;
            }

            console.log('✅ Member added:', userName);
        } else {
            alert(result.message || 'خطا در افزودن عضو');
            if (btn) {
                btn.disabled = false;
                btn.innerHTML = '<i class="fas fa-user-plus"></i> افزودن';
            }
        }
    } catch (error) {
        console.error('❌ Add member error:', error);
        alert('خطا در افزودن عضو');
    }
}

// ✅ حذف عضو از گروه
async function removeMemberFromGroup(groupId, userId, userName) {
    // تأیید حذف
    showConfirmDialog(
        'حذف عضو',
        `آیا از حذف "${userName}" از گروه اطمینان دارید؟`,
        async () => {
            try {
                const response = await fetch(`/api/Group/${groupId}/RemoveMember`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': getCsrfToken()
                    },
                    body: JSON.stringify({ userId: userId })
                });

                const result = await response.json();

                if (result.success) {
                    // حذف عنصر از لیست با انیمیشن
                    const memberItems = document.querySelectorAll('.group-member-item');
                    memberItems.forEach(item => {
                        const removeBtn = item.querySelector(`.member-action-btn.remove[onclick*="${userId}"]`);
                        if (removeBtn) {
                            item.style.animation = 'fadeOut 0.3s ease';
                            setTimeout(() => item.remove(), 300);
                        }
                    });

                    // به‌روزرسانی تعداد
                    const countBadge = document.querySelector('.member-count-badge');
                    if (countBadge) {
                        const currentCount = parseInt(countBadge.textContent) || 0;
                        countBadge.textContent = `${currentCount - 1} نفر`;
                    }

                    showToast(`${userName} از گروه حذف شد`, 'info');
                    console.log('✅ Member removed:', userName);
                } else {
                    alert(result.message || 'خطا در حذف عضو');
                }
            } catch (error) {
                console.error('❌ Remove member error:', error);
                alert('خطا در حذف عضو');
            }
        }
    );
}

// ✅ بستن دیالوگ
function closeGroupMembersDialog() {
    const dialog = document.getElementById('groupMembersDialog');
    if (dialog) {
        dialog.classList.remove('active');
        setTimeout(() => {
            dialog.remove();
            document.body.style.overflow = 'auto';
        }, 300);
    }
}

// ✅ Toast notification ساده
function showToast(message, type = 'info') {
    const existingToast = document.querySelector('.toast-notification');
    if (existingToast) existingToast.remove();

    const toast = document.createElement('div');
    toast.className = `toast-notification toast-${type}`;

    const icons = {
        success: 'fas fa-check-circle',
        error: 'fas fa-exclamation-circle',
        info: 'fas fa-info-circle',
        warning: 'fas fa-exclamation-triangle'
    };

    toast.innerHTML = `
        <i class="${icons[type] || icons.info}"></i>
        <span>${message}</span>
    `;

    document.body.appendChild(toast);

    requestAnimationFrame(() => toast.classList.add('show'));

    setTimeout(() => {
        toast.classList.remove('show');
        setTimeout(() => toast.remove(), 300);
    }, 3000);
}