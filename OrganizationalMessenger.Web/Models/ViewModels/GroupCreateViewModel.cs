using System.ComponentModel.DataAnnotations;

namespace OrganizationalMessenger.Web.Models.ViewModels
{
    /// <summary>
    /// ViewModel برای ایجاد گروه جدید
    /// </summary>
    public class GroupCreateViewModel
    {
        [Required(ErrorMessage = "نام گروه الزامی است.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "نام گروه باید بین 2 تا 100 کاراکتر باشد.")]
        [Display(Name = "نام گروه")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "توضیحات نمی‌تواند بیش از 500 کاراکتر باشد.")]
        [Display(Name = "توضیحات")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "سازنده گروه باید مشخص شود.")]
        [Display(Name = "سازنده")]
        public int CreatorId { get; set; }

        [Range(2, 10000, ErrorMessage = "حداکثر تعداد اعضا باید بین 2 تا 10000 باشد.")]
        [Display(Name = "حداکثر تعداد اعضا")]
        public int MaxMembers { get; set; } = 100;

        [Display(Name = "عمومی")]
        public bool IsPublic { get; set; } = false;

        [Display(Name = "فعال")]
        public bool IsActive { get; set; } = true;
    }
}
