using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace OrganizationalMessenger.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class BaseAdminController : Controller
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // Check if admin is logged in
            var adminId = HttpContext.Session.GetInt32("AdminId");
            if (adminId == null)
            {
                context.Result = new RedirectToActionResult("Login", "Account", new { area = "Admin" });
                return;
            }

            // Pass admin info to ViewBag
            ViewBag.AdminUsername = HttpContext.Session.GetString("AdminUsername");
            ViewBag.AdminFullName = HttpContext.Session.GetString("AdminFullName");
            ViewBag.IsSuperAdmin = HttpContext.Session.GetInt32("IsSuperAdmin") == 1;

            base.OnActionExecuting(context);
        }
    }
}
