using Microsoft.AspNetCore.Mvc;

namespace HospitalManagement.Controllers
{
    public class UserController : Controller
    {
        public IActionResult ViewProfile()
        {
            // Load profile data from database
            return View();
        }

        public IActionResult UpdateProfile()
        {
            // Load profile data to edit
            return View();
        }

        public IActionResult ChangePassword()
        {
            return View();
        }
    }
}
