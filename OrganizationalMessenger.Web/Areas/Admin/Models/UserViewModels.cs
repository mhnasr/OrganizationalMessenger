using OrganizationalMessenger.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace OrganizationalMessenger.Web.Areas.Admin.Models
{
    // لیست کاربران
    public class UserListViewModel
    {
        public List<User> Users { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public string SearchTerm { get; set; }
    }

    // ایجاد کاربر
    public class UserCreateViewModel
    {
        [Required(ErrorMessage = "نام کاربری الزامی است")]
        [Display(Name = "نام کاربری")]
        public string Username { get; set; }

        [Required(ErrorMessage = "نام الزامی است")]
        [Display(Name = "نام")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "نام خانوادگی الزامی است")]
        [Display(Name = "نام خانوادگی")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "شماره موبایل الزامی است")]
        [Phone(ErrorMessage = "فرمت شماره موبایل صحیح نیست")]
        [Display(Name = "شماره موبایل")]
        public string PhoneNumber { get; set; }

        [EmailAddress(ErrorMessage = "فرمت ایمیل صحیح نیست")]
        [Display(Name = "ایمیل")]
        public string Email { get; set; }

        [Display(Name = "شناسه Active Directory")]
        public string ActiveDirectoryId { get; set; }

        [Display(Name = "شناسه ERP")]
        public string ErpUserId { get; set; }

        [Display(Name = "وضعیت فعال")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "مجوز ایجاد گروه")]
        public bool CanCreateGroup { get; set; } = true;

        [Display(Name = "مجوز ایجاد کانال")]
        public bool CanCreateChannel { get; set; } = true;

        [Display(Name = "مجوز تماس صوتی")]
        public bool CanMakeVoiceCall { get; set; } = true;

        [Display(Name = "مجوز تماس تصویری")]
        public bool CanMakeVideoCall { get; set; } = true;

        [Range(0, int.MaxValue, ErrorMessage = "اعتبار پیامک باید عدد مثبت باشد")]
        [Display(Name = "اعتبار پیامک")]
        public int SmsCredit { get; set; } = 10;
    }

    // ویرایش کاربر
    public class UserEditViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "نام کاربری الزامی است")]
        [Display(Name = "نام کاربری")]
        public string Username { get; set; }

        [Required(ErrorMessage = "نام الزامی است")]
        [Display(Name = "نام")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "نام خانوادگی الزامی است")]
        [Display(Name = "نام خانوادگی")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "شماره موبایل الزامی است")]
        [Phone(ErrorMessage = "فرمت شماره موبایل صحیح نیست")]
        [Display(Name = "شماره موبایل")]
        public string PhoneNumber { get; set; }

        [EmailAddress(ErrorMessage = "فرمت ایمیل صحیح نیست")]
        [Display(Name = "ایمیل")]
        public string Email { get; set; }

        [Display(Name = "شناسه Active Directory")]
        public string ActiveDirectoryId { get; set; }

        [Display(Name = "شناسه ERP")]
        public string ErpUserId { get; set; }

        [Display(Name = "وضعیت فعال")]
        public bool IsActive { get; set; }

        [Display(Name = "مجوز ایجاد گروه")]
        public bool CanCreateGroup { get; set; }

        [Display(Name = "مجوز ایجاد کانال")]
        public bool CanCreateChannel { get; set; }

        [Display(Name = "مجوز تماس صوتی")]
        public bool CanMakeVoiceCall { get; set; }

        [Display(Name = "مجوز تماس تصویری")]
        public bool CanMakeVideoCall { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "اعتبار پیامک باید عدد مثبت باشد")]
        [Display(Name = "اعتبار پیامک")]
        public int SmsCredit { get; set; }
    }
}
