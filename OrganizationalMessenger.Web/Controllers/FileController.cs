using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrganizationalMessenger.Domain.Entities;
using OrganizationalMessenger.Infrastructure.Data;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using System.Security.Claims;
using static System.Net.Mime.MediaTypeNames;

namespace OrganizationalMessenger.Web.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class FileController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public FileController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // ✅ آپلود فایل
        // ✅ آپلود فایل - با کپشن

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(
    IFormFile file,
    [FromForm] int? messageId = null,
    [FromForm] string? caption = null,
    [FromForm] int? duration = null)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            if (file == null || file.Length == 0)
                return BadRequest(new { success = false, message = "فایلی انتخاب نشده است" });

            var extension = Path.GetExtension(file.FileName).ToLower().TrimStart('.');
            var contentType = file.ContentType;

            // ✅ اگر duration داریم → حتماً Audio است
            string category = null;
            if (duration.HasValue && duration.Value > 0)
            {
                category = "Audio";
                Console.WriteLine($"🎤 Detected voice message: duration={duration}s");
            }

            var uploadSetting = await _context.FileUploadSettings
                .FirstOrDefaultAsync(f => f.FileType.ToLower() == extension && f.IsAllowed);

            if (uploadSetting == null)
                return BadRequest(new { success = false, message = $"فرمت .{extension} مجاز نیست" });

            // ✅ اگر duration داریم، category را Override کن
            if (category == "Audio")
            {
                uploadSetting.Category = "Audio";
            }

            if (file.Length > uploadSetting.MaxSize)
            {
                var maxSizeMB = uploadSetting.MaxSize / (1024.0 * 1024.0);
                return BadRequest(new { success = false, message = $"حجم فایل نباید بیشتر از {maxSizeMB:0.##} MB باشد" });
            }

            try
            {
                var uploadsPath = Path.Combine(_env.WebRootPath, "uploads", uploadSetting.Category.ToLower());
                if (!Directory.Exists(uploadsPath))
                    Directory.CreateDirectory(uploadsPath);

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                var filePath = Path.Combine(uploadsPath, fileName);
                var fileUrl = $"/uploads/{uploadSetting.Category.ToLower()}/{fileName}";

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                string? thumbnailUrl = null;
                string? thumbnailPath = null;
                int? width = null, height = null;

                if (uploadSetting.Category.ToLower() == "image")
                {
                    try
                    {
                        using var image = await SixLabors.ImageSharp.Image.LoadAsync(filePath);
                        width = image.Width;
                        height = image.Height;

                        var thumbDir = Path.Combine(uploadsPath, "thumbs");
                        if (!Directory.Exists(thumbDir))
                            Directory.CreateDirectory(thumbDir);

                        var thumbFileName = $"thumb_{fileName}";
                        thumbnailPath = Path.Combine(thumbDir, thumbFileName);

                        var clone = image.Clone(x => x.Resize(new ResizeOptions
                        {
                            Size = new Size(300, 300),
                            Mode = ResizeMode.Max
                        }));

                        await clone.SaveAsJpegAsync(thumbnailPath, new JpegEncoder { Quality = 80 });
                        thumbnailUrl = $"/uploads/{uploadSetting.Category.ToLower()}/thumbs/{thumbFileName}";
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Thumbnail error: {ex.Message}");
                    }
                }

                string? fileHash = null;
                try
                {
                    using var sha256 = System.Security.Cryptography.SHA256.Create();
                    using var fileStream = System.IO.File.OpenRead(filePath);
                    var hashBytes = await sha256.ComputeHashAsync(fileStream);
                    fileHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
                }
                catch { }

                var fileAttachment = new FileAttachment
                {
                    FileName = fileName,
                    OriginalFileName = file.FileName,
                    FilePath = filePath,
                    FileUrl = fileUrl,
                    ContentType = contentType,
                    Extension = extension,
                    FileSize = file.Length,
                    FileType = uploadSetting.Category, // ✅ Audio یا Video
                    ThumbnailPath = thumbnailPath,
                    ThumbnailUrl = thumbnailUrl,
                    Width = width,
                    Height = height,
                    Duration = duration, // ✅
                    UploaderId = userId.Value,
                    MessageId = messageId,
                    CreatedAt = DateTime.Now,
                    FileHash = fileHash,
                    IsSafe = true,
                    IsScanned = false
                };

                _context.FileAttachments.Add(fileAttachment);
                await _context.SaveChangesAsync();

                Console.WriteLine($"✅ File uploaded: ID={fileAttachment.Id}, Type={fileAttachment.FileType}, Duration={duration}s");

                return Ok(new
                {
                    success = true,
                    file = new
                    {
                        id = fileAttachment.Id,
                        fileName = fileAttachment.FileName,
                        originalFileName = fileAttachment.OriginalFileName,
                        fileUrl = fileAttachment.FileUrl,
                        thumbnailUrl = fileAttachment.ThumbnailUrl,
                        fileType = fileAttachment.FileType, // ✅ Audio
                        fileSize = fileAttachment.FileSize,
                        width = fileAttachment.Width,
                        height = fileAttachment.Height,
                        extension = fileAttachment.Extension,
                        duration = fileAttachment.Duration,
                        readableSize = fileAttachment.ReadableFileSize,
                        readableDuration = fileAttachment.ReadableDuration
                    },
                    caption
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Upload error: {ex.Message}");
                return StatusCode(500, new { success = false, message = $"خطا در آپلود فایل: {ex.Message}" });
            }
        }










        // ✅ دانلود فایل
        [HttpGet("download/{id}")]
        public async Task<IActionResult> DownloadFile(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var file = await _context.FileAttachments
                .FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted);

            if (file == null)
                return NotFound(new { success = false, message = "فایل یافت نشد" });

            if (!file.IsSafe)
                return BadRequest(new { success = false, message = "این فایل امن نیست و قابل دانلود نمی‌باشد" });

            if (!System.IO.File.Exists(file.FilePath))
                return NotFound(new { success = false, message = "فایل فیزیکی در سرور یافت نشد" });

            // ✅ افزایش شمارنده دانلود
            file.IncrementDownloadCount();
            await _context.SaveChangesAsync();

            var memory = new MemoryStream();
            using (var stream = new FileStream(file.FilePath, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;

            return File(memory, file.ContentType ?? "application/octet-stream", file.OriginalFileName);
        }

        // ✅ حذف فایل (soft delete)
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteFile(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var file = await _context.FileAttachments
                .FirstOrDefaultAsync(f => f.Id == id && f.UploaderId == userId.Value && !f.IsDeleted);

            if (file == null)
                return NotFound(new { success = false, message = "فایل یافت نشد یا دسترسی ندارید" });

            file.MarkAsDeleted();
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "فایل با موفقیت حذف شد" });
        }

        // ✅ دریافت لیست فایل‌های یک پیام
        [HttpGet("message/{messageId}")]
        public async Task<IActionResult> GetMessageFiles(int messageId)
        {
            var files = await _context.FileAttachments
                .Where(f => f.MessageId == messageId && !f.IsDeleted)
                .Select(f => new
                {
                    f.Id,
                    f.OriginalFileName,
                    f.FileUrl,
                    f.ThumbnailUrl,
                    f.FileType,
                    f.FileSize,
                    ReadableSize = f.ReadableFileSize,
                    f.Width,
                    f.Height,
                    f.Extension,
                    f.CreatedAt
                })
                .ToListAsync();

            return Ok(new { success = true, files });
        }

        // ✅ دریافت تنظیمات آپلود فایل
        [HttpGet("settings")]
        public async Task<IActionResult> GetUploadSettings()
        {
            var settings = await _context.FileUploadSettings
                .Where(f => f.IsAllowed)
                .Select(f => new
                {
                    f.FileType,
                    f.Category,
                    f.MaxSize,
                    MaxSizeMB = f.MaxSize / (1024.0 * 1024.0)
                })
                .ToListAsync();

            return Ok(new { success = true, settings });
        }

        // ========== متدهای کمکی ==========

        private int? GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null) return null;
            if (int.TryParse(claim.Value, out var id)) return id;
            return null;
        }
    }
}