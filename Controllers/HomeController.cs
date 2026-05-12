using Microsoft.AspNetCore.Mvc;

namespace EduPsych_Web.Controllers
{
    public class HomeController : Controller
    {
        // هذه هي الدالة التي سيعرض من خلالها صفحة Index
        public IActionResult Index()
        {
            return View();
        }
    }
}
