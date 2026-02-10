using Microsoft.EntityFrameworkCore;
using OrganizationalMessenger.Domain.Entities;

namespace OrganizationalMessenger.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // ==================== DbSets ====================
        public DbSet<User> Users { get; set; }
        public DbSet<AdminUser> AdminUsers { get; set; }
        public DbSet<UserPermission> UserPermissions { get; set; }

        public DbSet<Message> Messages { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<Channel> Channels { get; set; }
        public DbSet<UserGroup> UserGroups { get; set; }
        public DbSet<UserChannel> UserChannels { get; set; }
        public DbSet<GroupMember> GroupMembers { get; set; }
        public DbSet<Call> Calls { get; set; }
        public DbSet<MessageReaction> MessageReactions { get; set; }
        public DbSet<MessageRead> MessageReads { get; set; }
        public DbSet<StarredMessage> StarredMessages { get; set; }
        public DbSet<MessageReport> MessageReports { get; set; }
        public DbSet<ForbiddenWord> ForbiddenWords { get; set; }
        public DbSet<FileUploadSetting> FileUploadSettings { get; set; }
        public DbSet<SystemSetting> SystemSettings { get; set; }
        public DbSet<SystemNotification> SystemNotifications { get; set; }
        public DbSet<ServiceLink> ServiceLinks { get; set; }
        public DbSet<SmsCreditLog> SmsCreditLogs { get; set; }
        public DbSet<Poll> Polls { get; set; }
        public DbSet<PollOption> PollOptions { get; set; }
        public DbSet<PollVote> PollVotes { get; set; }
        public DbSet<LoginLog> LoginLogs { get; set; }
        public DbSet<TelegramMessage> TelegramMessages { get; set; }
        public DbSet<FileAttachment> FileAttachments { get; set; }

        public DbSet<Report> Reports { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ==================== User Configuration ====================
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                entity.Property(e => e.Username)
                    .IsRequired()
                    .HasMaxLength(100);
                entity.HasIndex(e => e.Username).IsUnique();

                entity.Property(e => e.PhoneNumber)
                    .HasMaxLength(20);
                entity.HasIndex(e => e.PhoneNumber).IsUnique();

                entity.Property(e => e.Email).HasMaxLength(200);
                entity.Property(e => e.FirstName).HasMaxLength(100);
                entity.Property(e => e.LastName).HasMaxLength(100);
                entity.Property(e => e.PasswordHash).HasMaxLength(500);
                entity.Property(e => e.AvatarUrl).HasMaxLength(500);
                entity.Property(e => e.ActiveDirectoryId).HasMaxLength(200);
                entity.Property(e => e.ErpUserId).HasMaxLength(200);
                entity.Property(e => e.OtpCode).HasMaxLength(10);
                entity.Property(e => e.VoipExtension).HasMaxLength(50);
            });

            // ==================== AdminUser Configuration ====================
            modelBuilder.Entity<AdminUser>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                entity.Property(e => e.Username)
                    .IsRequired()
                    .HasMaxLength(100);
                entity.HasIndex(e => e.Username).IsUnique();

                entity.Property(e => e.Email).HasMaxLength(200);
                entity.Property(e => e.FirstName).HasMaxLength(100);
                entity.Property(e => e.LastName).HasMaxLength(100);
                entity.Property(e => e.PasswordHash).HasMaxLength(500);

                // Seed Admin User
                entity.HasData(new AdminUser
                {
                    Id = 1,
                    Username = "admin",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                    FirstName = "مدیر",
                    LastName = "سیستم",
                    Email = "admin@company.com",
                    IsActive = true,
                    CreatedAt = DateTime.Now
                });
            });

            // ==================== Message Configuration ====================
            modelBuilder.Entity<Message>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                entity.Property(e => e.Content).HasMaxLength(4000);
                entity.Property(e => e.EncryptedContent).HasMaxLength(8000);
                entity.Property(e => e.AttachmentUrl).HasMaxLength(500);
                entity.Property(e => e.AttachmentName).HasMaxLength(255);
                entity.Property(e => e.AttachmentType).HasMaxLength(100);

                // Sender Relationship
                entity.HasOne(e => e.Sender)
                    .WithMany(u => u.SentMessages)
                    .HasForeignKey(e => e.SenderId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Receiver Relationship (Private Chat)
                entity.HasOne(e => e.Receiver)
                    .WithMany(u => u.ReceivedMessages)
                    .HasForeignKey(e => e.ReceiverId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Group Relationship
                entity.HasOne(e => e.Group)
                    .WithMany(g => g.Messages)
                    .HasForeignKey(e => e.GroupId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Channel Relationship
                entity.HasOne(e => e.Channel)
                    .WithMany(c => c.Messages)
                    .HasForeignKey(e => e.ChannelId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Reply Relationship
                entity.HasOne(e => e.ReplyToMessage)
                    .WithMany(m => m.Replies)
                    .HasForeignKey(e => e.ReplyToMessageId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Forward Relationship
                entity.HasOne(e => e.ForwardedFromMessage)
                    .WithMany()
                    .HasForeignKey(e => e.ForwardedFromMessageId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Call Relationship
                entity.HasOne(e => e.Call)
                    .WithMany(c => c.Messages)
                    .HasForeignKey(e => e.CallId)
                    .OnDelete(DeleteBehavior.SetNull);

                // Indexes
                entity.HasIndex(e => e.SenderId);
                entity.HasIndex(e => e.ReceiverId);
                entity.HasIndex(e => e.GroupId);
                entity.HasIndex(e => e.ChannelId);
                entity.HasIndex(e => e.CreatedAt);
            });

            // ==================== Group Configuration ====================
            modelBuilder.Entity<Group>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.AvatarUrl).HasMaxLength(500);

                // Creator Relationship
                entity.HasOne(e => e.Creator)
                    .WithMany(u => u.CreatedGroups)
                    .HasForeignKey(e => e.CreatorId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.CreatorId);
            });

            // ==================== Channel Configuration ====================
            modelBuilder.Entity<Channel>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.AvatarUrl)
                    .HasMaxLength(500);

                entity.HasOne(e => e.Creator)
                    .WithMany(u => u.CreatedChannels)
                    .HasForeignKey(e => e.CreatorId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Query Filter for Soft Delete
                entity.HasQueryFilter(e => !e.IsDeleted);
            });

            // FileAttachment Configuration
            modelBuilder.Entity<FileAttachment>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasIndex(e => e.FileHash);
                entity.HasIndex(e => e.UploaderId);
                entity.HasIndex(e => e.MessageId);
                entity.HasIndex(e => e.CreatedAt);

                entity.HasOne(e => e.Uploader)
                    .WithMany(u => u.UploadedFiles)
                    .HasForeignKey(e => e.UploaderId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Message)
                    .WithMany(m => m.Attachments)
                    .HasForeignKey(e => e.MessageId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Query Filter for Soft Delete
                entity.HasQueryFilter(e => !e.IsDeleted);
            });


            // ==================== UserGroup Configuration ====================
            modelBuilder.Entity<UserGroup>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                // Unique Index for User-Group combination
                entity.HasIndex(e => new { e.UserId, e.GroupId }).IsUnique();

                // User Relationship
                entity.HasOne(e => e.User)
                    .WithMany(u => u.UserGroups)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Group Relationship
                entity.HasOne(e => e.Group)
                    .WithMany(g => g.UserGroups)
                    .HasForeignKey(e => e.GroupId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ==================== UserChannel Configuration ====================
            // UserChannel Configuration
            modelBuilder.Entity<UserChannel>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasIndex(e => new { e.UserId, e.ChannelId }).IsUnique();

                entity.Property(e => e.Role)
                    .HasConversion<int>();

                entity.HasOne(e => e.User)
                    .WithMany(u => u.UserChannels)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Channel)
                    .WithMany(c => c.UserChannels)
                    .HasForeignKey(e => e.ChannelId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ==================== GroupMember Configuration ====================
            modelBuilder.Entity<GroupMember>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                entity.HasIndex(e => new { e.UserId, e.GroupId }).IsUnique();

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Group)
                    .WithMany(g => g.Members)
                    .HasForeignKey(e => e.GroupId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ==================== Call Configuration ====================
            modelBuilder.Entity<Call>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                entity.Property(e => e.MeetingLink).HasMaxLength(500);
                entity.Property(e => e.MeetingPassword).HasMaxLength(100);

                // Initiator Relationship
                entity.HasOne(e => e.Initiator)
                    .WithMany(u => u.InitiatedCalls)
                    .HasForeignKey(e => e.InitiatorId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Receiver Relationship
                entity.HasOne(e => e.Receiver)
                    .WithMany(u => u.ReceivedCalls)
                    .HasForeignKey(e => e.ReceiverId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Group Relationship
                entity.HasOne(e => e.Group)
                    .WithMany(g => g.Calls)
                    .HasForeignKey(e => e.GroupId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Channel Relationship
                entity.HasOne(e => e.Channel)
                    .WithMany(c => c.Calls)
                    .HasForeignKey(e => e.ChannelId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.InitiatorId);
                entity.HasIndex(e => e.ReceiverId);
                entity.HasIndex(e => e.StartedAt);
            });

            // ==================== MessageReaction Configuration ====================
            modelBuilder.Entity<MessageReaction>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                entity.Property(e => e.Emoji)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.HasIndex(e => new { e.MessageId, e.UserId, e.Emoji }).IsUnique();

                entity.HasOne(e => e.Message)
                    .WithMany(m => m.Reactions)
                    .HasForeignKey(e => e.MessageId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ==================== MessageRead Configuration ====================
            modelBuilder.Entity<MessageRead>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                entity.HasIndex(e => new { e.MessageId, e.UserId }).IsUnique();

                entity.HasOne(e => e.Message)
                    .WithMany(m => m.ReadReceipts)
                    .HasForeignKey(e => e.MessageId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ==================== StarredMessage Configuration ====================
            modelBuilder.Entity<StarredMessage>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                entity.HasIndex(e => new { e.UserId, e.MessageId }).IsUnique();

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Message)
                    .WithMany()
                    .HasForeignKey(e => e.MessageId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ==================== MessageReport Configuration ====================
            modelBuilder.Entity<MessageReport>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                entity.Property(e => e.Description).HasMaxLength(1000);

                entity.HasOne(e => e.Message)
                    .WithMany()
                    .HasForeignKey(e => e.MessageId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Reporter)
                    .WithMany()
                    .HasForeignKey(e => e.ReporterId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.ReviewedByAdmin)
                    .WithMany()
                    .HasForeignKey(e => e.ReviewedByAdminId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ==================== ForbiddenWord Configuration ====================
            modelBuilder.Entity<ForbiddenWord>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                entity.Property(e => e.Word)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.HasIndex(e => e.Word).IsUnique();
            });

            // ==================== FileUploadSetting Configuration ====================
            modelBuilder.Entity<FileUploadSetting>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                entity.Property(e => e.FileType)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Category)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.HasIndex(e => e.FileType).IsUnique();

                // Seed Default Settings
                entity.HasData(
                    new FileUploadSetting { Id = 1, FileType = "jpg", Category = "Image", MaxSize = 10 * 1024 * 1024, IsAllowed = true },
                    new FileUploadSetting { Id = 2, FileType = "jpeg", Category = "Image", MaxSize = 10 * 1024 * 1024, IsAllowed = true },
                    new FileUploadSetting { Id = 3, FileType = "png", Category = "Image", MaxSize = 10 * 1024 * 1024, IsAllowed = true },
                    new FileUploadSetting { Id = 4, FileType = "gif", Category = "Image", MaxSize = 5 * 1024 * 1024, IsAllowed = true },
                    new FileUploadSetting { Id = 5, FileType = "mp4", Category = "Video", MaxSize = 100 * 1024 * 1024, IsAllowed = true },
                    new FileUploadSetting { Id = 6, FileType = "webm", Category = "Video", MaxSize = 100 * 1024 * 1024, IsAllowed = true },
                    new FileUploadSetting { Id = 7, FileType = "mp3", Category = "Audio", MaxSize = 20 * 1024 * 1024, IsAllowed = true },
                    new FileUploadSetting { Id = 8, FileType = "wav", Category = "Audio", MaxSize = 20 * 1024 * 1024, IsAllowed = true },
                    new FileUploadSetting { Id = 9, FileType = "ogg", Category = "Audio", MaxSize = 20 * 1024 * 1024, IsAllowed = true },
                    new FileUploadSetting { Id = 10, FileType = "pdf", Category = "Document", MaxSize = 50 * 1024 * 1024, IsAllowed = true },
                    new FileUploadSetting { Id = 11, FileType = "doc", Category = "Document", MaxSize = 50 * 1024 * 1024, IsAllowed = true },
                    new FileUploadSetting { Id = 12, FileType = "docx", Category = "Document", MaxSize = 50 * 1024 * 1024, IsAllowed = true },
                    new FileUploadSetting { Id = 13, FileType = "xls", Category = "Document", MaxSize = 50 * 1024 * 1024, IsAllowed = true },
                    new FileUploadSetting { Id = 14, FileType = "xlsx", Category = "Document", MaxSize = 50 * 1024 * 1024, IsAllowed = true },
                    new FileUploadSetting { Id = 15, FileType = "zip", Category = "Archive", MaxSize = 100 * 1024 * 1024, IsAllowed = true },
                    new FileUploadSetting { Id = 16, FileType = "rar", Category = "Archive", MaxSize = 100 * 1024 * 1024, IsAllowed = true }
                );
            });

            // ==================== SystemSetting Configuration ====================
            modelBuilder.Entity<SystemSetting>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                entity.Property(e => e.Key)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Value)
                    .IsRequired()
                    .HasMaxLength(1000);

                entity.Property(e => e.Description).HasMaxLength(500);

                entity.HasIndex(e => e.Key).IsUnique();

                // Seed Default Settings
                entity.HasData(
                    new SystemSetting { Id = 1, Key = "AuthenticationType", Value = "Database", Description = "نوع احراز هویت: Database, ActiveDirectory, ERP" },
                    new SystemSetting { Id = 2, Key = "AllowUserGroupCreation", Value = "true", Description = "آیا کاربران می‌توانند گروه ایجاد کنند" },
                    new SystemSetting { Id = 3, Key = "AllowUserChannelCreation", Value = "true", Description = "آیا کاربران می‌توانند کانال ایجاد کنند" },
                    new SystemSetting { Id = 4, Key = "MessageEditTimeLimit", Value = "3600", Description = "زمان مجاز ویرایش پیام (ثانیه)" },
                    new SystemSetting { Id = 5, Key = "MessageDeleteTimeLimit", Value = "7200", Description = "زمان مجاز حذف پیام (ثانیه)" },
                    new SystemSetting { Id = 6, Key = "VoipEnabled", Value = "true", Description = "فعال بودن تماس VoIP" },
                    new SystemSetting { Id = 7, Key = "TelegramIntegrationEnabled", Value = "false", Description = "فعال بودن یکپارچگی تلگرام" },
                    new SystemSetting { Id = 8, Key = "MaxGroupMembers", Value = "200", Description = "حداکثر تعداد اعضای گروه" },
                    new SystemSetting { Id = 9, Key = "OtpExpiryMinutes", Value = "5", Description = "زمان انقضای کد OTP (دقیقه)" },
                    new SystemSetting { Id = 10, Key = "CompanyName", Value = "سازمان", Description = "نام سازمان" },
                    new SystemSetting { Id = 11, Key = "CompanyLogoUrl", Value = "/images/logo.png", Description = "آدرس لوگوی سازمان" }
                );
            });

            // ==================== SystemNotification Configuration ====================
            modelBuilder.Entity<SystemNotification>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Content)
                    .IsRequired()
                    .HasMaxLength(2000);

                entity.HasOne(e => e.Sender)
                    .WithMany()
                    .HasForeignKey(e => e.SenderId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ==================== ServiceLink Configuration ====================
            modelBuilder.Entity<ServiceLink>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Url)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.IconUrl).HasMaxLength(500);
            });

            // ==================== SmsCreditLog Configuration ====================
            modelBuilder.Entity<SmsCreditLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.RelatedMessage)
                    .WithMany()
                    .HasForeignKey(e => e.RelatedMessageId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.CreatedAt);
            });

            // ==================== Poll Configuration ====================
            modelBuilder.Entity<Poll>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                entity.Property(e => e.Question)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.HasOne(e => e.Creator)
                    .WithMany()
                    .HasForeignKey(e => e.CreatorId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Group)
                    .WithMany()
                    .HasForeignKey(e => e.GroupId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Channel)
                    .WithMany()
                    .HasForeignKey(e => e.ChannelId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ==================== PollOption Configuration ====================
            modelBuilder.Entity<PollOption>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                entity.Property(e => e.Text)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.HasOne(e => e.Poll)
                    .WithMany(p => p.Options)
                    .HasForeignKey(e => e.PollId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ==================== PollVote Configuration ====================
            modelBuilder.Entity<PollVote>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                entity.HasIndex(e => new { e.PollOptionId, e.UserId }).IsUnique();

                entity.HasOne(e => e.PollOption)
                    .WithMany(o => o.Votes)
                    .HasForeignKey(e => e.PollOptionId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ==================== LoginLog Configuration ====================
            modelBuilder.Entity<LoginLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                entity.Property(e => e.IpAddress).HasMaxLength(50);
                entity.Property(e => e.UserAgent).HasMaxLength(500);
                entity.Property(e => e.FailureReason).HasMaxLength(500);

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.CreatedAt);
            });

            // ==================== TelegramMessage Configuration ====================
            modelBuilder.Entity<TelegramMessage>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                entity.Property(e => e.TelegramChatId)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.TelegramUsername).HasMaxLength(100);
                entity.Property(e => e.Content).HasMaxLength(4000);
                entity.Property(e => e.AttachmentUrl).HasMaxLength(500);

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.TelegramChatId);
            });
        }
    }
}
