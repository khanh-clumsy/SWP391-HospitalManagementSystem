using Microsoft.AspNetCore.Mvc;

namespace HospitalManagement.Controllers
{
    public class PatientController : Controller
    {
        public IActionResult ViewDoctors()
        {
            return View();
        }
    }
}
