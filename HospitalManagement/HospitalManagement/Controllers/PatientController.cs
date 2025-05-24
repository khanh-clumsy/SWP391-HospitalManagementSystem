using HospitalManagement.Data;
using HospitalManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using X.PagedList;


namespace HospitalManagement.Controllers
{
    public class PatientController : Controller
    {
        private readonly HospitalManagementContext _context;
        public PatientController(HospitalManagementContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> ViewDoctors(int? page)
        {
            int pageSize = 8;
            int pageNumber = page ?? 1;

            // Lấy danh sách bác sĩ theo trang, async với EF Core
            var doctors = await _context.Doctors
                .Include(d => d.Account)
                .OrderBy(d => d.DoctorId)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Lấy tổng số bác sĩ
            var totalDoctors = await _context.Doctors.CountAsync();

            // Tạo IPagedList từ danh sách đã lấy
            var pagedDoctors = new StaticPagedList<Doctor>(doctors, pageNumber, pageSize, totalDoctors);

            return View(pagedDoctors);
        }
        public IActionResult DoctorDetail()
        {
            return View();
        }
    }
}
