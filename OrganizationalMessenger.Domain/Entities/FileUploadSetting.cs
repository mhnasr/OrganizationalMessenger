namespace OrganizationalMessenger.Domain.Entities
{
    public class FileUploadSetting
    {
        public int Id { get; set; }
        public string FileType { get; set; } = string.Empty; // مثل: jpg, png, pdf, mp4
        public string Category { get; set; } = string.Empty; // Image, Video, Audio, Document
        public long MaxSize { get; set; } // بایت
        public bool IsAllowed { get; set; } = true;
    }
}
