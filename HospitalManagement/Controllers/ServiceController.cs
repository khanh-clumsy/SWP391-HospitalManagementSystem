using HospitalManagement.Data;
using HospitalManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HospitalManagement.Helpers;
using X.PagedList;
using X.PagedList.Extensions;
using X.PagedList.EF;

namespace HospitalManagement.Controllers
{
    public class ServiceController : Controller
    {
        private readonly HospitalManagementContext _context;

        public ServiceController(HospitalManagementContext context)
        {
            _context = context;
        }

        //  Danh sách dịch vụ (mọi role đều xem được)
        [AllowAnonymous]
        public async Task<IActionResult> Index(int? page)
        {
            int pageSize = 15;
            int pageNumber = page ?? 1;
            var services = User.IsInRole("Admin")
                ? await _context.Services
                    .IgnoreQueryFilters()
                    .OrderBy(s => s.ServiceId)
                    .ToPagedListAsync(pageNumber, pageSize)
                : await _context.Services
                    .OrderBy(s => s.ServiceId)
                    .ToPagedListAsync(pageNumber, pageSize);
            return View(services);
        }

        //  GET: Tạo dịch vụ (Admin-only)
        [Authorize(Roles = AppConstants.Roles.Admin)]
        public IActionResult Create() => View();

        //  POST: Tạo dịch vụ
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Service service)
        {
            if (!ModelState.IsValid)
                return View(service);

            service.IsDeleted = false;
            _context.Services.Add(service);
            await _context.SaveChangesAsync();

            TempData["success"] = AppConstants.Messages.Service.CreateSuccess;
            return RedirectToAction(nameof(Index));
        }

        //  GET: Cập nhật
        [Authorize(Roles = AppConstants.Roles.Admin)]
        public async Task<IActionResult> Update(int id)
        {
            var service = await _context.Services
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(s => s.ServiceId == id);

            if (service == null)
                return NotFound();

            return View(service);
        }

        // POST: Cập nhật
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppConstants.Roles.Admin)]
        public async Task<IActionResult> Update(Service service)
        {
            if (!ModelState.IsValid)
                return View(service);

            _context.Services.Update(service);
            await _context.SaveChangesAsync();

            TempData["success"] = AppConstants.Messages.Service.UpdateSuccess;
            return RedirectToAction(nameof(Index));
        }

        // Ẩn dịch vụ (Soft Delete)
        [Authorize(Roles = AppConstants.Roles.Admin)]
        public async Task<IActionResult> SoftDelete(int id)
        {
            var service = await _context.Services.FindAsync(id);
            if (service == null)
                return NotFound();

            service.IsDeleted = true;
            _context.Services.Update(service);
            await _context.SaveChangesAsync();

            TempData["success"] = AppConstants.Messages.Service.SoftDeleteSuccess;
            return RedirectToAction(nameof(Index));
        }

        //  Khôi phục dịch vụ
        [Authorize(Roles = AppConstants.Roles.Admin)]
        public async Task<IActionResult> Restore(int id)
        {
            var service = await _context.Services
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(s => s.ServiceId == id);

            if (service == null)
                return NotFound();

            service.IsDeleted = false;
            _context.Services.Update(service);
            await _context.SaveChangesAsync();

            TempData["success"] = AppConstants.Messages.Service.RestoreSuccess;
            return RedirectToAction(nameof(Index));
        }
    }
}
