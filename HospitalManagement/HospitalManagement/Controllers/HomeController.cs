using System.Diagnostics;
using HospitalManagement.Models;
using Microsoft.AspNetCore.Mvc;

namespace HospitalFETemplate.Controllers
{
    public class HomeController : Controller
    {
        
        public IActionResult ViewDoctors()
        {
            return View();
        }

        // Nếu bạn muốn thêm các trang khác (about, service, contact...), có thể khai báo thêm action tương ứng
    }
}
