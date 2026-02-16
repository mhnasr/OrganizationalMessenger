using System.Collections.Generic;
using System.Threading.Tasks;

// ✅ استفاده از alias
using DomainChannel = OrganizationalMessenger.Domain.Entities.Channel;
using DomainUser = OrganizationalMessenger.Domain.Entities.User;

namespace OrganizationalMessenger.Application.Interfaces
{
    public interface IChannelService
    {
        Task<DomainChannel> CreateChannelAsync(DomainChannel channel, int creatorId);
        Task<DomainChannel> GetChannelByIdAsync(int channelId);
        Task<List<DomainChannel>> GetUserChannelsAsync(int userId);
        Task<List<DomainChannel>> GetPublicChannelsAsync();
        Task SubscribeAsync(int channelId, int userId);
        Task UnsubscribeAsync(int channelId, int userId);
        Task<bool> IsSubscriberAsync(int channelId, int userId);
        Task<bool> IsAdminAsync(int channelId, int userId);
        Task UpdateChannelAsync(DomainChannel channel);
        Task DeleteChannelAsync(int channelId);
        Task<List<DomainUser>> GetSubscribersAsync(int channelId);
        Task AddAdminAsync(int channelId, int userId, int promoterId);
        Task RemoveAdminAsync(int channelId, int userId, int demoterId);
    }
}