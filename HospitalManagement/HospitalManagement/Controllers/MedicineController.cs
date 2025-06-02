using HospitalManagement.Data;
using Microsoft.AspNetCore.Mvc;

namespace HospitalManagement.Controllers
{
    public class MedicineController : Controller
    {
        private readonly HospitalManagementContext _context;
        public MedicineController(HospitalManagementContext context) {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}
