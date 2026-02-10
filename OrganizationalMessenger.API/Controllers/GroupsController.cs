using Microsoft.AspNetCore.Mvc;

namespace OrganizationalMessenger.API.Controllers
{
    public class GroupsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
