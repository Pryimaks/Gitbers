using Microsoft.AspNetCore.Mvc;

namespace Gitbers.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
