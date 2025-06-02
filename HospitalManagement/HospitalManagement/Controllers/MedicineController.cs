using HospitalManagement.Data;
using HospitalManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.Blazor;

namespace HospitalManagement.Controllers
{
    public class MedicineController : Controller
    {
        private readonly HospitalManagementContext _context;
        public MedicineController(HospitalManagementContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "Patient, Sales, Doctor, Admin")]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Xác định role người dùng
            string role = "";
            if (User.IsInRole("Patient")) role = "Patient";
            else if (User.IsInRole("Sales")) role = "Sales";
            else if (User.IsInRole("Doctor")) role = "Doctor";
            else if (User.IsInRole("Admin")) role = "Admin";

            // Lấy danh sách thuốc
            var medicines = await _context.Medicines.ToListAsync();

            // Đưa thông tin role cho View để ẩn/hiện nút sửa xóa
            ViewBag.IsAdmin = (role == "Admin");

            return View(medicines);
        }

        [Authorize(Roles = "Patient, Sales, Doctor, Admin")]
        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            var medicine = await _context.Medicines.FindAsync(id);
            if (medicine == null) return NotFound();

            return View(medicine);
        }
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var medicine = await _context.Medicines.FindAsync(id);
            if (medicine == null) return NotFound();
            // Xóa thuốc
            _context.Medicines.Remove(medicine);
            await _context.SaveChangesAsync();
            TempData["success"] = "Medicine deleted successfully!";
            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var medicine = await _context.Medicines.FindAsync(id);
            if (medicine == null) return NotFound();
            return View(medicine);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Medicine model)
        {
            var medicine = await _context.Medicines.FindAsync(model.MedicineId);

            if (model.Price <= 0)
            {
                TempData["error"] = "Price can't be negative!";
                return RedirectToAction("Edit", "Medicine");
            }

            if (medicine != null)
            {

                medicine.Name = model.Name;
                medicine.MedicineType = model.MedicineType;
                medicine.Price = model.Price;
                medicine.Description = model.Description;
                medicine.Unit = model.Unit;

                await _context.SaveChangesAsync();
                return RedirectToAction("Index", "Medicine");
            }
            else
            {
                return View(model);
            }
        }
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> UploadPhoto(IFormFile photo, Medicine model)
        {
            if (photo != null && photo.Length > 0)
            {
                // Convert img -> Byte ->  Base64String
                using var ms = new MemoryStream();
                await photo.CopyToAsync(ms);
                var imageBytes = ms.ToArray();
                model.Image = Convert.ToBase64String(imageBytes);

                // Add in database
                var medicine = await _context.Medicines.FindAsync(model.MedicineId);

                if (medicine != null)
                {
                    medicine.Image = model.Image;
                    await _context.SaveChangesAsync();
                }

                TempData["success"] = "Update photo successful!";
                return RedirectToAction("Index", "Medicine");
            }

            // Do nothing
            TempData["error"] = "Photo is invalid!";
            return RedirectToAction("Index", "Medicine");
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Create()
        {
            return View(new Medicine());
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Medicine model, IFormFile photo)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Nếu có ảnh thì convert sang base64 và gán vào model.Image
            if (photo != null && photo.Length > 0)
            {
                using var ms = new MemoryStream();
                await photo.CopyToAsync(ms);
                model.Image = Convert.ToBase64String(ms.ToArray());
            }

            // Thêm mới thuốc vào CSDL
            _context.Medicines.Add(model);
            await _context.SaveChangesAsync();

            TempData["success"] = "New medicine has been created successfully!";
            return RedirectToAction("Index");
        }

    }
}

