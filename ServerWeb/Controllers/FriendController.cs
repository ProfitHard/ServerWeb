using Microsoft.AspNetCore.Mvc;

namespace ServerWeb.Controllers
{
    public class FriendController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
