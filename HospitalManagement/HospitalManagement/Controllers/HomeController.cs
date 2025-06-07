using HospitalManagement.Models;
using HospitalManagement.Repositories;
using HospitalManagement.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;
using X.PagedList;

namespace HospitalFETemplate.Controllers
{

    public class HomeController : Controller
    {
        private readonly IDoctorRepository _doctorRepo;
        public HomeController(IDoctorRepository doctorRepo)
        {
            _doctorRepo = doctorRepo;
        }

        public IActionResult Index()
        {
            return View();
        }
        public IActionResult About()
        {
            return View();
        }

        public IActionResult Service()
        {
            return View();
        }

        public IActionResult NotFound()
        {
            return View();
        }
        public IActionResult AccessDenied()
        {
            return View();
        }
        /**
         * Controller for ViewDoctors page, get name, department, exp year, isHead,
         * sort type, to filter out doctor, handle pagination
         */
        public async Task<IActionResult> ViewDoctors(int? page, string? name, string? department, int? exp, bool? isHead, string? sort)
        {
            name = HomeController.NormalizeName(name);
            int pageSize = 8;
            int pageNumber = page ?? 1;

            bool isDefaultView = string.IsNullOrEmpty(name)
                                 && string.IsNullOrEmpty(department)
                                 && !exp.HasValue
                                 && !isHead.HasValue
                                 && string.IsNullOrEmpty(sort); 

            List<Doctor> doctors;
            int totalDoctors;

            if (isDefaultView)
            {
                // Lấy tất cả bác sĩ, ưu tiên isSpecial = 1 lên trước
                doctors = await _doctorRepo.GetAllDoctorsWithSpecialFirstAsync(pageNumber, pageSize);
                totalDoctors = await _doctorRepo.CountAllActiveDoctorsAsync();
            }
            else
            {
                doctors = await _doctorRepo.SearchAsync(name, department, exp, isHead, sort, true , pageNumber, pageSize);
                totalDoctors = await _doctorRepo.CountAsync(name, department, exp, isHead, true);
            }

            var pagedDoctors = new StaticPagedList<Doctor>(doctors, pageNumber, pageSize, totalDoctors);
            var departments = await _doctorRepo.GetDistinctDepartment();

            // Truyền lại filter cho view
            ViewBag.Name = name;
            ViewBag.Department = department;
            ViewBag.Experience = exp;
            ViewBag.Type = isHead;
            ViewBag.Sort = sort;
            ViewBag.Departments = departments;

            return View(pagedDoctors);
        }

        public async Task<IActionResult> DoctorDetail(int id)
        {
            var doctor = await _doctorRepo.GetByIdAsync(id);
            if (doctor == null)
            {
                return NotFound();
            }
            return View(doctor);
        }
        public static string NormalizeName(string? input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            input = input.Trim();
            var words = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return string.Join(" ", words);
        }
    }
}
