using Microsoft.AspNetCore.Mvc;

namespace HospitalManagement.Controllers
{
    public class SalesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
