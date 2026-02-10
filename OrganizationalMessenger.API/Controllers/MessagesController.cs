using Microsoft.AspNetCore.Mvc;

namespace OrganizationalMessenger.API.Controllers
{
    public class MessagesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
