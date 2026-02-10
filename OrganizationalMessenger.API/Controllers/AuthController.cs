using Microsoft.AspNetCore.Mvc;

namespace OrganizationalMessenger.API.Controllers
{
    public class AuthController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
