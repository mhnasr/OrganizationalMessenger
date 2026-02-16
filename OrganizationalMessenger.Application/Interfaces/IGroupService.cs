using System.Collections.Generic;
using System.Threading.Tasks;

// ✅ استفاده از alias
using DomainGroup = OrganizationalMessenger.Domain.Entities.Group;
using DomainUser = OrganizationalMessenger.Domain.Entities.User;

namespace OrganizationalMessenger.Application.Interfaces
{
    public interface IGroupService
    {
        Task<DomainGroup> CreateGroupAsync(DomainGroup group, int creatorId);
        Task<DomainGroup> GetGroupByIdAsync(int groupId);
        Task<List<DomainGroup>> GetUserGroupsAsync(int userId);
        Task<List<DomainGroup>> GetPublicGroupsAsync();
        Task AddMemberAsync(int groupId, int userId);
        Task RemoveMemberAsync(int groupId, int userId);
        Task<bool> IsMemberAsync(int groupId, int userId);
        Task<bool> IsAdminAsync(int groupId, int userId);
        Task UpdateGroupAsync(DomainGroup group);
        Task DeleteGroupAsync(int groupId);
        Task<List<DomainUser>> GetGroupMembersAsync(int groupId);
        Task PromoteToAdminAsync(int groupId, int userId, int promoterId);
        Task DemoteFromAdminAsync(int groupId, int userId, int demoterId);
    }
}