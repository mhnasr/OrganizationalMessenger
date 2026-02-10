using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrganizationalMessenger.Domain.Entities
{
    /// <summary>
    /// Entity برای مدیریت فایل‌های ضمیمه شده به پیام‌ها
    /// </summary>
    public class FileAttachment
    {
        public int Id { get; set; }

        // ==================== اطلاعات فایل ====================

        [Required]
        [StringLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string OriginalFileName { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string FilePath { get; set; } = string.Empty;

        [StringLength(500)]
        public string? FileUrl { get; set; }

        [StringLength(100)]
        public string? ContentType { get; set; }

        [StringLength(10)]
        public string? Extension { get; set; }

        public long FileSize { get; set; }

        // ==================== نوع فایل ====================

        [StringLength(50)]
        public string FileType { get; set; } = "File";

        // ==================== Thumbnail ====================

        [StringLength(500)]
        public string? ThumbnailPath { get; set; }

        [StringLength(500)]
        public string? ThumbnailUrl { get; set; }

        public int? Width { get; set; }
        public int? Height { get; set; }

        // ==================== برای فایل‌های صوتی/ویدیویی ====================

        public int? Duration { get; set; }

        // ==================== آپلودکننده ====================

        public int UploaderId { get; set; }

        [ForeignKey(nameof(UploaderId))]
        public virtual User Uploader { get; set; } = null!;

        // ==================== پیام مرتبط ====================

        /// <summary>
        /// ⚠️ نوع باید با Message.Id یکسان باشد (int)
        /// </summary>
        public int? MessageId { get; set; }  // ✅ تغییر از long? به int?

        [ForeignKey(nameof(MessageId))]
        public virtual Message? Message { get; set; }

        // ==================== تاریخ‌ها ====================

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? DeletedAt { get; set; }
        public bool IsDeleted { get; set; } = false;

        // ==================== Hash ====================

        [StringLength(64)]
        public string? FileHash { get; set; }

        // ==================== اسکن آنتی‌ویروس ====================

        public bool IsScanned { get; set; } = false;
        public bool IsSafe { get; set; } = true;
        public DateTime? ScannedAt { get; set; }

        [StringLength(500)]
        public string? ScanResult { get; set; }

        // ==================== آمار دانلود ====================

        public int DownloadCount { get; set; } = 0;
        public DateTime? LastDownloadAt { get; set; }

        // ==================== متدهای کمکی ====================

        [NotMapped]
        public string ReadableFileSize
        {
            get
            {
                string[] sizes = { "B", "KB", "MB", "GB", "TB" };
                double len = FileSize;
                int order = 0;
                while (len >= 1024 && order < sizes.Length - 1)
                {
                    order++;
                    len = len / 1024;
                }
                return $"{len:0.##} {sizes[order]}";
            }
        }

        [NotMapped]
        public string? ReadableDuration
        {
            get
            {
                if (!Duration.HasValue) return null;
                var ts = TimeSpan.FromSeconds(Duration.Value);
                if (ts.Hours > 0)
                    return $"{ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
                return $"{ts.Minutes:D2}:{ts.Seconds:D2}";
            }
        }

        [NotMapped]
        public bool IsImage => FileType?.ToLower() == "image" ||
            new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".svg" }
            .Contains(Extension?.ToLower());

        [NotMapped]
        public bool IsVideo => FileType?.ToLower() == "video" ||
            new[] { ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".webm" }
            .Contains(Extension?.ToLower());

        [NotMapped]
        public bool IsAudio => FileType?.ToLower() == "audio" ||
            new[] { ".mp3", ".wav", ".ogg", ".m4a", ".aac", ".flac" }
            .Contains(Extension?.ToLower());

        [NotMapped]
        public bool IsDocument => FileType?.ToLower() == "document" ||
            new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".rtf" }
            .Contains(Extension?.ToLower());

        public void IncrementDownloadCount()
        {
            DownloadCount++;
            LastDownloadAt = DateTime.Now;
        }

        public void MarkAsDeleted()
        {
            IsDeleted = true;
            DeletedAt = DateTime.Now;
        }

        public void SetScanResult(bool isSafe, string? result = null)
        {
            IsScanned = true;
            IsSafe = isSafe;
            ScannedAt = DateTime.Now;
            ScanResult = result;
        }
    }
}
