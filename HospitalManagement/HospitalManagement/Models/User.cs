using Microsoft.AspNetCore.Mvc;

namespace HospitalManagement.Models
{
    public class User : Controller
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
