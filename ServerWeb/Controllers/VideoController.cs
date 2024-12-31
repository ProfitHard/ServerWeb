using Microsoft.AspNetCore.Mvc;

namespace ServerWeb.Controllers
{
    public class VideoController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
