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
            // Lấy danh sách đơn vị thuốc và loại thuốc để hiển thị trong dropdown
            List<SelectListItem> UnitList = GetUnitList();
            List<SelectListItem> TypeList = GetMedicineTypeList();
            ViewBag.Units = UnitList;
            ViewBag.Types = TypeList;

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
        public async Task<IActionResult> Edit(int id)
        {
            List<SelectListItem> UnitList = GetUnitList();
            List<SelectListItem> TypeList = GetMedicineTypeList();
            ViewBag.Units = UnitList;
            ViewBag.Types = TypeList;
            var medicine = await _context.Medicines.FindAsync(id);
            if (medicine == null) return NotFound();
            return View(medicine);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Medicine model, IFormFile photo)
        {
            List<SelectListItem> UnitList = GetUnitList();
            List<SelectListItem> TypeList = GetMedicineTypeList();
            ViewBag.Units = UnitList;
            ViewBag.Types = TypeList;
            
            //Nếu giá âm hoặc = 0
            if (model.Price <= 0)
            {
                TempData["error"] = "Price can't be negative!";
                return RedirectToAction("Edit", "Medicine");
            }

            //Validate model
            ModelState.Remove("photo");
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                TempData["error"] = "Validation failed: " + string.Join(", ", errors);
                return View(model);
            }
            var medicine = await _context.Medicines.FindAsync(model.MedicineId);
            if (medicine == null) { return View(new Medicine()); }

            medicine.Name = model.Name;
            medicine.MedicineType = model.MedicineType;
            medicine.Price = model.Price;
            medicine.Description = model.Description;
            medicine.Unit = model.Unit;

            if (photo != null && photo.Length > 0)
            {
                // Supported formats
                var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/jpg" };
                var maxSize = 5 * 1024 * 1024; // 5MB

                // Check if the file type is supported
                if (!allowedTypes.Contains(photo.ContentType.ToLower()))
                {
                    // Handle invalid file type
                    TempData["Error"] = "Unsupported file type. Only JPG, JPEG, PNG, and GIF are allowed.";
                    return RedirectToAction("Edit", model);
                }

                // Check if the file size exceeds the limit
                if (photo.Length > maxSize)
                {
                    // Handle file size exceeds
                    TempData["Error"] = "File size exceeds the 5MB limit.";
                    return RedirectToAction("Edit", model);
                }

                // Convert the image to Base64
                using (var ms = new MemoryStream())
                {
                    await photo.CopyToAsync(ms);
                    medicine.Image = Convert.ToBase64String(ms.ToArray());
                }
            }

            await _context.SaveChangesAsync();
            TempData["success"] = $"Updated {medicine.Name} successfully!";
            return RedirectToAction("Index", "Medicine");

        }
      
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Create()
        {
            List<SelectListItem> UnitList = GetUnitList();
            List<SelectListItem> TypeList = GetMedicineTypeList();
            ViewBag.Units = UnitList;
            ViewBag.Types = TypeList;
            return View(new Medicine());
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Medicine model, IFormFile photo)
        {
            List<SelectListItem> UnitList = GetUnitList();
            List<SelectListItem> TypeList = GetMedicineTypeList();
            ViewBag.Units = UnitList;
            ViewBag.Types = TypeList;

            ModelState.Remove("photo");

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                TempData["error"] = "Validation failed: " + string.Join(", ", errors);
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
                // Supported formats
                var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/jpg" };
                var maxSize = 5 * 1024 * 1024; // 5MB

                // Check if the file type is supported
                if (!allowedTypes.Contains(photo.ContentType.ToLower()))
                {
                    // Handle invalid file type
                    TempData["Error"] = "Unsupported file type. Only JPG, JPEG, PNG, and GIF are allowed.";
                    return RedirectToAction("Create", model);
                }

                // Check if the file size exceeds the limit
                if (photo.Length > maxSize)
                {
                    // Handle file size exceeds
                    TempData["Error"] = "File size exceeds the 5MB limit.";
                    return RedirectToAction("Create", model);

                }

                // Convert the image to Base64
                using (var ms = new MemoryStream())
                {
                    await photo.CopyToAsync(ms);
                    model.Image = Convert.ToBase64String(ms.ToArray());
                }
            }

            // Thêm mới thuốc vào CSDL
            _context.Medicines.Add(model);
            await _context.SaveChangesAsync();

            TempData["success"] = "New medicine has been created successfully!";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Filter(string? SearchName, string? TypeFilter, string? UnitFilter)
        {
            //Lấy ra danh sách đơn vị thuốc và loại thuốc để hiển thị trong dropdown
            List<SelectListItem> UnitList = GetUnitList();
            List<SelectListItem> TypeList = GetMedicineTypeList();

            // Gán giá trị cho ViewBag để sử dụng trong View, xử lý lưu lại những lựa chọn đã chọn sau khi filter
            ViewBag.Units = new SelectList(UnitList, "Value", "Text", UnitFilter);
            ViewBag.Types = new SelectList(TypeList, "Value", "Text", TypeFilter);
            ViewBag.SearchName = SearchName;

            var result = await _medicineRepository.Filter(SearchName, TypeFilter, UnitFilter);
            return View("Index", result);
        }

       [HttpPost]
        public async Task<IActionResult> Delete(int medicineId)
        {
            var medicine = _context.Medicines.FirstOrDefault(m => m.MedicineId == medicineId);
            if (medicine == null)
            {
                TempData["error"] = $"Can't find medicine with ID = {medicineId}";
                return RedirectToAction("Index");
            }
            _context.Medicines.Remove(medicine);
            await _context.SaveChangesAsync();
            TempData["success"] = "Medicine deleted successfully!";
            return RedirectToAction("Index");  
        }

        private List<SelectListItem> GetUnitList()
        {
            return new List<SelectListItem>
            {
                new SelectListItem("Viên", "Viên"),
                new SelectListItem("Lọ", "Lọ"),
                new SelectListItem("Hộp", "Hộp"),
                new SelectListItem("Tuýp", "Tuýp"),
                new SelectListItem("Ống", "Ống"),
                new SelectListItem("Vỉ", "Vỉ"),
            };
        }

        private List<SelectListItem> GetMedicineTypeList()
        {
            return new List<SelectListItem>
            {
                new SelectListItem("Thuốc uống", "Thuốc uống"),
                new SelectListItem("Thuốc tiêm", "Thuốc tiêm"),
                new SelectListItem("Thuốc bôi", "Thuốc bôi"),
                new SelectListItem("Thuốc nhỏ", "Thuốc nhỏ"),
                new SelectListItem("Thuốc xịt", "Thuốc xịt"),
                new SelectListItem("Khác", "Khác"),
            };
        }

    }
}

