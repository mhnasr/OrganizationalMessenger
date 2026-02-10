using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrganizationalMessenger.Infrastructure.Services
{
    public interface ISmsSender
    {
        Task<string> SendSmsAsync(string messageBody, string phoneNumber, int? memberId = null, string smsType = "");
    }
}
