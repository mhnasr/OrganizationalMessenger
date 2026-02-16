namespace OrganizationalMessenger.Domain.Enums
{
    public enum GroupRole
    {
        Member = 0,
        Admin = 1,
        Owner = 2
    }

  
    public enum ChannelRole
    {
        Subscriber = 0,  // فقط خواندن
        Publisher = 1,   // ارسال پست
        Admin = 2,       // مدیریت
        Owner = 3        // مالک
    }


    public enum AuthenticationType
    {
        Database = 0,      // شماره موبایل از دیتابیس
        ActiveDirectory = 1,
        ERP = 2,
        SMS = 3           // اضافه شد - OTP پیامکی
    }
    public enum MessageType
    {
        Text = 0,
        Image = 1,
        Video = 2,
        Audio = 3,
        Voice = 4,
        File = 5,
        Location = 6,
        Contact = 7,
        Poll = 8,
        System = 9
    }

  
    public enum CallType
    {
        Voice = 0,
        Video = 1
    }

    public enum CallDestinationType
    {
        Private = 0,
        Group = 1,
        Channel = 2
    }

    public enum CallStatus
    {
        Initiated = 0,
        Ringing = 1,
        InProgress = 2,
        Ended = 3,
        Missed = 4,
        Rejected = 5,
        Failed = 6
    }


    public enum ReportStatus
    {
        Pending = 0,
        Reviewed = 1,
        Resolved = 2,
        Dismissed = 3,
        ActionTaken = 4
    }
    

    public enum MessageDestinationType
    {
        Direct = 1,
        Group = 2,
        Channel = 3
    }


   
    // === Enum های جدید ===

    public enum AlertType
    {
        Info = 0,
        Warning = 1,
        Error = 2,
        Success = 3
    }

    public enum AlertDeliveryType
    {
        Immediate = 0,      // فقط کاربران آنلاین
        Persistent = 1      // همه کاربران (ذخیره در پیام‌های سیستم)
    }

    public enum SmsCreditRequestStatus
    {
        Pending = 0,
        Approved = 1,
        Rejected = 2,
        PartiallyApproved = 3
    }

    public enum SettingCategory
    {
        Authentication = 0,
        VoIP = 1,
        Telegram = 2,
        FileUpload = 3,
        Message = 4,
        GroupChannel = 5,
        General = 6
    }
    public enum ReportItemType
    {
        User = 1,
        Message = 2,
        Group = 3,
        Channel = 4
    }






}
