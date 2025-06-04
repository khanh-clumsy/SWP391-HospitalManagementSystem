using HospitalManagement.Data;
using HospitalManagement.Models;
using HospitalManagement.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.Blazor;

namespace HospitalManagement.Controllers
{
    public class MedicineController : Controller
    {
        private readonly HospitalManagementContext _context;
        private readonly IMedicineRepository _medicineRepository;

        public MedicineController(HospitalManagementContext context, IMedicineRepository medicineRepository)
        {
            _context = context;
            _medicineRepository = medicineRepository;
        }


        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Lấy danh sách thuốc
            var medicines = await _context.Medicines.ToListAsync();

            // Lấy danh sách MedicineType không trùng
            var medicineTypes = await _context.Medicines
                .Select(m => m.MedicineType)
                .Distinct()
                .ToListAsync();

            // Đưa thông tin loại thuốc cho dropdown lọc
            ViewBag.MedicineTypes = medicineTypes;

            return View(medicines);
        }


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

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var medicine = await _context.Medicines.FindAsync(id);
            if (medicine == null) return NotFound();
            return View(medicine);
        }

        [Authorize]
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

            if (!User.IsInRole("Admin"))
            {
                TempData["error"] = "You do not have permission to edit this information.";
                return RedirectToAction("Edit", new { id = model.MedicineId });
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

            if (model.Price <= 0)
            {
                TempData["error"] = "Price can't be negative!";
                return RedirectToAction("Edit", "Medicine");
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

        [HttpGet]
        public async Task<IActionResult> Filter(string? SearchName, string? TypeFilter)
        {
            var types = await _context.Medicines
                .Select(m => m.MedicineType)
                .Distinct()
                .ToListAsync();

            ViewBag.MedicineTypes = types;
            ViewBag.TypeFilter = TypeFilter;
            ViewBag.SearchName = SearchName;

            var result = await _medicineRepository.Filter(SearchName, TypeFilter);
            return View("Index", result);
        }

        public List<SelectListItem> GetUnitList()
        {
            return new List<SelectListItem>
            {
                new SelectListItem("Viên", "pill"),
                new SelectListItem("Lọ", "bottle"),
                new SelectListItem("Hộp", "box"),
                new SelectListItem("Tuýp", "tube"),
                new SelectListItem("Ống", "vial"),
                new SelectListItem("Vỉ", "pack"),
            };
        }

        public List<SelectListItem> GetMedicineTypeList()
        {
            return new List<SelectListItem>
            {
                new SelectListItem("Thuốc uống", "oral"),
                new SelectListItem("Thuốc tiêm", "injection"),
                new SelectListItem("Thuốc bôi", "topical"),
                new SelectListItem("Thuốc nhỏ", "drops"),
                new SelectListItem("Thuốc xịt", "spray"),
                new SelectListItem("Khác", "other"),
            };
        }

    }
}

