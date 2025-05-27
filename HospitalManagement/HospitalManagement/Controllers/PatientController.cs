using System.Threading.Tasks;
using HospitalManagement.Data;
using HospitalManagement.Models;
using HospitalManagement.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using X.PagedList;


namespace HospitalManagement.Controllers
{
    public class PatientController : Controller
    {
        private readonly IDoctorRepository _doctorRepo;
        public PatientController(IDoctorRepository doctorRepo)
        {
            _doctorRepo = doctorRepo;
        }
        /**
         * Controller for ViewDoctors page, get name, department, exp year, isHead,
         * sort type, to filter out doctor, handle pagination
         */
        public async Task<IActionResult> ViewDoctors(int? page, string? name, string? department, int? exp, bool? isHead, string? sort)
        {
            int pageSize = 8;
            int pageNumber = page ?? 1;

            // Lấy danh sách bác sĩ theo trang, async với EF Core
            var doctors = await _doctorRepo.SearchAsync(name, department, exp, isHead, sort, pageNumber, pageSize);
            // Lấy tổng số bác sĩ
            var totalDoctors = await _doctorRepo.CountAsync(name, department, exp, isHead);

            // Tạo IPagedList từ danh sách đã lấy
            var pagedDoctors = new StaticPagedList<Doctor>(doctors, pageNumber, pageSize, totalDoctors);
            var departments = await _doctorRepo.GetDistinctDepartment();


            // Truyền lại dữ liệu vào cshtml để khi reload trang filter vẫn hiển thị nội dung filter đã chọn
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
    }
}
