using HospitalManagement.Data;
using HospitalManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Controllers
{
    public class ServiceController : Controller
    {
        private readonly HospitalManagementContext _context;
        public ServiceController(HospitalManagementContext context)
        {
            _context = context;
        }

        // Cho phép tất cả các role đều xem được danh sách dịch vụ
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var services = await _context.Services.OrderByDescending(s => s.ServiceId).ToListAsync();
            return View(services);
        }

        // Chỉ Admin mới được thêm, sửa, xóa, khôi phục
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Service service)
        {
            if (ModelState.IsValid)
            {
                service.IsDeleted = false;
                _context.Services.Add(service);
                await _context.SaveChangesAsync();
                TempData["success"] = "Thêm dịch vụ thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(service);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id)
        {
            var service = await _context.Services.FindAsync(id);
            if (service == null)
                return NotFound();
            return View(service);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(Service service)
        {
            if (ModelState.IsValid)
            {
                _context.Services.Update(service);
                await _context.SaveChangesAsync();
                TempData["success"] = "Cập nhật dịch vụ thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(service);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SoftDelete(int id)
        {
            var service = await _context.Services.FindAsync(id);
            if (service == null)
                return NotFound();
            service.IsDeleted = true;
            _context.Services.Update(service);
            await _context.SaveChangesAsync();
            TempData["success"] = "Đã ẩn dịch vụ.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Restore(int id)
        {
            var service = await _context.Services.FindAsync(id);
            if (service == null)
                return NotFound();
            service.IsDeleted = false;
            _context.Services.Update(service);
            await _context.SaveChangesAsync();
            TempData["success"] = "Khôi phục dịch vụ thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
} 