using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrganizationalMessenger.Application.Interfaces;
using OrganizationalMessenger.Domain.Entities;
using OrganizationalMessenger.Domain.Enums;
using OrganizationalMessenger.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OrganizationalMessenger.Infrastructure.Services
{
    public class GroupService : IGroupService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<GroupService> _logger;

        public GroupService(ApplicationDbContext context, ILogger<GroupService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Group> CreateGroupAsync(Group group, int creatorId)
        {
            try
            {
                // ایجاد گروه
                _context.Groups.Add(group);
                await _context.SaveChangesAsync();

                // اضافه کردن سازنده به عنوان ادمین
                var creatorMember = new GroupMember
                {
                    GroupId = group.Id,
                    UserId = creatorId,
                    Role = GroupRole.Owner,
                    JoinedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.GroupMembers.Add(creatorMember);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Group {group.Id} created by user {creatorId}");

                return group;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating group: {group.Name}");
                throw;
            }
        }

        public async Task<Group> GetGroupByIdAsync(int groupId)
        {
            return await _context.Groups
                .Include(g => g.Members)
                    .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(g => g.Id == groupId);
        }

        public async Task<List<Group>> GetUserGroupsAsync(int userId)
        {
            return await _context.GroupMembers
                .Where(gm => gm.UserId == userId && gm.IsActive)
                .Include(gm => gm.Group)
                .Select(gm => gm.Group)
                .ToListAsync();
        }

        public async Task<List<Group>> GetPublicGroupsAsync()
        {
            return await _context.Groups
                .Where(g => g.IsPublic && g.IsActive)
                .OrderByDescending(g => g.CreatedAt)
                .ToListAsync();
        }

        public async Task AddMemberAsync(int groupId, int userId)
        {
            var group = await GetGroupByIdAsync(groupId);
            if (group == null)
            {
                throw new Exception("گروه یافت نشد");
            }

            var existingMember = await _context.GroupMembers
                .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == userId);

            if (existingMember != null && existingMember.IsActive)
            {
                throw new Exception("کاربر قبلاً عضو این گروه است");
            }

            var membersCount = await _context.GroupMembers
                .CountAsync(gm => gm.GroupId == groupId && gm.IsActive);

            if (membersCount >= group.MaxMembers)
            {
                throw new Exception("ظرفیت گروه تکمیل است");
            }

            if (existingMember != null)
            {
                // فعال‌سازی مجدد
                existingMember.IsActive = true;
                existingMember.JoinedAt = DateTime.UtcNow;
            }
            else
            {
                var member = new GroupMember
                {
                    GroupId = groupId,
                    UserId = userId,
                    Role = GroupRole.Member,
                    JoinedAt = DateTime.UtcNow,
                    IsActive = true
                };
                _context.GroupMembers.Add(member);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation($"User {userId} joined group {groupId}");
        }

        public async Task RemoveMemberAsync(int groupId, int userId)
        {
            var member = await _context.GroupMembers
                .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == userId && gm.IsActive);

            if (member == null)
            {
                throw new Exception("کاربر عضو این گروه نیست");
            }

            var group = await GetGroupByIdAsync(groupId);
            if (group.CreatorId == userId)
            {
                throw new Exception("سازنده گروه نمی‌تواند از گروه خارج شود");
            }

            member.IsActive = false;
            await _context.SaveChangesAsync();

            _logger.LogInformation($"User {userId} left group {groupId}");
        }

        public async Task<bool> IsMemberAsync(int groupId, int userId)
        {
            return await _context.GroupMembers
                .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == userId && gm.IsActive);
        }

        public async Task<bool> IsAdminAsync(int groupId, int userId)
        {
            var member = await _context.GroupMembers
                .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == userId && gm.IsActive);

            return member?.Role == GroupRole.Admin || member?.Role == GroupRole.Owner;
        }

        public async Task UpdateGroupAsync(Group group)
        {
            group.UpdatedAt = DateTime.UtcNow;
            _context.Groups.Update(group);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteGroupAsync(int groupId)
        {
            var group = await GetGroupByIdAsync(groupId);
            if (group != null)
            {
                group.IsDeleted = true;
                group.DeletedAt = DateTime.UtcNow;
                group.IsActive = false;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<User>> GetGroupMembersAsync(int groupId)
        {
            return await _context.GroupMembers
                .Where(gm => gm.GroupId == groupId && gm.IsActive)
                .Include(gm => gm.User)
                .Select(gm => gm.User)
                .ToListAsync();
        }

        public async Task PromoteToAdminAsync(int groupId, int userId, int promoterId)
        {
            var isPromoterAdmin = await IsAdminAsync(groupId, promoterId);
            if (!isPromoterAdmin)
            {
                throw new Exception("شما مجوز ارتقا عضو را ندارید");
            }

            var member = await _context.GroupMembers
                .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == userId && gm.IsActive);

            if (member == null)
            {
                throw new Exception("کاربر عضو گروه نیست");
            }

            member.Role = GroupRole.Admin;
            await _context.SaveChangesAsync();

            _logger.LogInformation($"User {userId} promoted to admin in group {groupId}");
        }

        public async Task DemoteFromAdminAsync(int groupId, int userId, int demoterId)
        {
            var isDemoterAdmin = await IsAdminAsync(groupId, demoterId);
            if (!isDemoterAdmin)
            {
                throw new Exception("شما مجوز تنزل رتبه را ندارید");
            }

            var member = await _context.GroupMembers
                .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == userId && gm.IsActive);

            if (member == null)
            {
                throw new Exception("کاربر عضو گروه نیست");
            }

            var group = await GetGroupByIdAsync(groupId);
            if (group.CreatorId == userId)
            {
                throw new Exception("نمی‌توان سازنده گروه را تنزل داد");
            }

            member.Role = GroupRole.Member;
            await _context.SaveChangesAsync();

            _logger.LogInformation($"User {userId} demoted from admin in group {groupId}");
        }
    }
}