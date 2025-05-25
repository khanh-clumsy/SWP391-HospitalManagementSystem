using Microsoft.AspNetCore.Mvc;

namespace HospitalManagement.Controllers
{
    public class PatientController : Controller
    {
        public IActionResult ViewDoctors()
        {
            return View();
        }

        public IActionResult DoctorDetail()
        {
            return View();
        }

        public IActionResult RequestConsultant()
        {

        }

        public IActionResult Appointment()

        {
            return View();
        }
        public IActionResult AppointmentList()
        {
            return View();
        }
    }
    
}
