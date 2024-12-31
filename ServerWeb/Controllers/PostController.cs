using Microsoft.AspNetCore.Mvc;

namespace ServerWeb.Controllers
{
    public class PostController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
