using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrganizationalMessenger.Application.Interfaces;
using OrganizationalMessenger.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// ✅ استفاده از alias
using DomainChannel = OrganizationalMessenger.Domain.Entities.Channel;
using DomainUser = OrganizationalMessenger.Domain.Entities.User;
using DomainUserChannel = OrganizationalMessenger.Domain.Entities.UserChannel;
using DomainChannelRole = OrganizationalMessenger.Domain.Enums.ChannelRole;

namespace OrganizationalMessenger.Infrastructure.Services
{
    public class ChannelService : IChannelService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ChannelService> _logger;

        public ChannelService(ApplicationDbContext context, ILogger<ChannelService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<DomainChannel> CreateChannelAsync(DomainChannel channel, int creatorId)
        {
            try
            {
                _context.Channels.Add(channel);
                await _context.SaveChangesAsync();

                var creatorSubscriber = new DomainUserChannel
                {
                    ChannelId = channel.Id,
                    UserId = creatorId,
                    Role = DomainChannelRole.Owner,
                    IsOwner = true,
                    IsAdmin = true,
                    CanPost = true,
                    CanDeleteMessages = true,
                    CanManageMembers = true,
                    JoinedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.UserChannels.Add(creatorSubscriber);
                channel.MemberCount = 1;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Channel {channel.Id} created by user {creatorId}");
                return channel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating channel: {channel.Name}");
                throw;
            }
        }

        public async Task<DomainChannel> GetChannelByIdAsync(int channelId)
        {
            return await _context.Channels
                .Include(c => c.UserChannels)
                    .ThenInclude(uc => uc.User)
                .FirstOrDefaultAsync(c => c.Id == channelId);
        }

        public async Task<List<DomainChannel>> GetUserChannelsAsync(int userId)
        {
            return await _context.UserChannels
                .Where(uc => uc.UserId == userId && uc.IsActive)
                .Include(uc => uc.Channel)
                .Select(uc => uc.Channel)
                .ToListAsync();
        }

        public async Task<List<DomainChannel>> GetPublicChannelsAsync()
        {
            return await _context.Channels
                .Where(c => c.IsPublic && c.IsActive && !c.IsDeleted)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task SubscribeAsync(int channelId, int userId)
        {
            var channel = await GetChannelByIdAsync(channelId);
            if (channel == null) throw new Exception("کانال یافت نشد");

            var existing = await _context.UserChannels
                .FirstOrDefaultAsync(uc => uc.ChannelId == channelId && uc.UserId == userId);

            if (existing != null)
            {
                if (existing.IsActive) throw new Exception("شما قبلاً عضو این کانال هستید");
                existing.Rejoin();
                channel.MemberCount++;
                await _context.SaveChangesAsync();
                return;
            }

            var subscriber = new DomainUserChannel
            {
                ChannelId = channelId,
                UserId = userId,
                Role = DomainChannelRole.Subscriber,
                IsActive = true,
                JoinedAt = DateTime.UtcNow
            };

            _context.UserChannels.Add(subscriber);
            channel.MemberCount++;
            await _context.SaveChangesAsync();
            _logger.LogInformation($"User {userId} subscribed to channel {channelId}");
        }

        public async Task UnsubscribeAsync(int channelId, int userId)
        {
            var subscriber = await _context.UserChannels
                .FirstOrDefaultAsync(uc => uc.ChannelId == channelId && uc.UserId == userId);

            if (subscriber == null || !subscriber.IsActive)
                throw new Exception("شما عضو این کانال نیستید");

            if (subscriber.IsOwner)
                throw new Exception("مالک کانال نمی‌تواند اشتراک را لغو کند");

            subscriber.Leave();
            var channel = await GetChannelByIdAsync(channelId);
            if (channel != null && channel.MemberCount > 0) channel.MemberCount--;
            await _context.SaveChangesAsync();
            _logger.LogInformation($"User {userId} unsubscribed from channel {channelId}");
        }

        public async Task<bool> IsSubscriberAsync(int channelId, int userId)
        {
            return await _context.UserChannels
                .AnyAsync(uc => uc.ChannelId == channelId && uc.UserId == userId && uc.IsActive);
        }

        public async Task<bool> IsAdminAsync(int channelId, int userId)
        {
            var subscriber = await _context.UserChannels
                .FirstOrDefaultAsync(uc => uc.ChannelId == channelId && uc.UserId == userId && uc.IsActive);
            return subscriber?.IsAdmin == true || subscriber?.IsOwner == true;
        }

        public async Task UpdateChannelAsync(DomainChannel channel)
        {
            channel.UpdatedAt = DateTime.UtcNow;
            _context.Channels.Update(channel);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteChannelAsync(int channelId)
        {
            var channel = await GetChannelByIdAsync(channelId);
            if (channel != null)
            {
                channel.MarkAsDeleted();
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<DomainUser>> GetSubscribersAsync(int channelId)
        {
            return await _context.UserChannels
                .Where(uc => uc.ChannelId == channelId && uc.IsActive)
                .Include(uc => uc.User)
                .Select(uc => uc.User)
                .ToListAsync();
        }

        public async Task AddAdminAsync(int channelId, int userId, int promoterId)
        {
            if (!await IsAdminAsync(channelId, promoterId))
                throw new Exception("شما مجوز افزودن ادمین را ندارید");

            var subscriber = await _context.UserChannels
                .FirstOrDefaultAsync(uc => uc.ChannelId == channelId && uc.UserId == userId && uc.IsActive);

            if (subscriber == null) throw new Exception("کاربر عضو کانال نیست");
            subscriber.PromoteToAdmin();
            await _context.SaveChangesAsync();
            _logger.LogInformation($"User {userId} promoted to admin in channel {channelId}");
        }

        public async Task RemoveAdminAsync(int channelId, int userId, int demoterId)
        {
            if (!await IsAdminAsync(channelId, demoterId))
                throw new Exception("شما مجوز حذف ادمین را ندارید");

            var subscriber = await _context.UserChannels
                .FirstOrDefaultAsync(uc => uc.ChannelId == channelId && uc.UserId == userId && uc.IsActive);

            if (subscriber == null) throw new Exception("کاربر عضو کانال نیست");
            if (subscriber.IsOwner) throw new Exception("نمی‌توان مالک کانال را حذف کرد");

            subscriber.DemoteFromAdmin();
            await _context.SaveChangesAsync();
            _logger.LogInformation($"User {userId} demoted from admin in channel {channelId}");
        }
    }
}