using Microsoft.AspNetCore.Mvc;

namespace ServerWeb.Controllers
{
    public class CommentController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
