using HospitalManagement.Data;
using HospitalManagement.Helpers;
using HospitalManagement.Models;
using HospitalManagement.Repositories;
using HospitalManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Controllers
{
    public class TestController : Controller
    {
        private readonly ITestRepository _testRepository;
        private readonly HospitalManagementContext _context;
        public TestController(ITestRepository testRepository, HospitalManagementContext context)
        {
            _testRepository = testRepository;
            _context = context;
        }

        public async Task<IActionResult> Index(string searchName, string sortOrder, decimal? minPrice, decimal? maxPrice)
        {
            var isAdmin = User.IsInRole("Admin");
            var tests = await _testRepository.SearchAsync(searchName, sortOrder, minPrice, maxPrice, isAdmin);

            ViewBag.SearchName = searchName;
            ViewBag.SortOrder = sortOrder;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;

            return View(tests);
        }
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewBag.RoomTypes = GetRoomTypeOptions();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Test test)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra thêm validation tùy chỉnh
                if (string.IsNullOrWhiteSpace(test.RoomType))
                {
                    ModelState.AddModelError("RoomType", "Vui lòng chọn loại phòng cho xét nghiệm");
                    ViewBag.RoomTypes = GetRoomTypeOptions();
                    return View(test);
                }

                await _testRepository.AddAsync(test);
                await _testRepository.SaveAsync();
                TempData["success"] = "Tạo xét nghiệm thành công!";
                return RedirectToAction(nameof(Index));
            }
            ViewBag.RoomTypes = GetRoomTypeOptions();
            return View(test);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id)
        {

            var test = await _testRepository.GetByIdAsync(id);

            if (test == null)
                return NotFound();

            ViewBag.RoomTypes = GetRoomTypeOptions();
            return View(test);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(Test test)
        {
            // 1. Validate thủ công trước khi kiểm tra ModelState
            if (string.IsNullOrWhiteSpace(test.RoomType))
            {
                ModelState.AddModelError("RoomType", "Vui lòng chọn loại phòng cho xét nghiệm!");
            }

            if (test.Price < 0)
            {
                ModelState.AddModelError("Price", "Giá tiền phải lớn hơn hoặc bằng 0");
            }

            // 2. Nếu có lỗi thì return lại View cùng dữ liệu
            if (!ModelState.IsValid)
            {
                ViewBag.RoomTypes = GetRoomTypeOptions();
                return View(test);
            }

            // 3. Cập nhật và lưu
            await _testRepository.UpdateAsync(test);
            await _testRepository.SaveAsync();

            TempData["success"] = "Cập nhật xét nghiệm thành công!";
            return RedirectToAction(nameof(Index));
        }


        [Authorize]
        public async Task<IActionResult> ViewTestResult(int id)
        {
            var testRecord = await _context.TestRecords
                .Where(tr => tr.Appointment != null && tr.Appointment.Patient != null && tr.Test != null)
                .Include(tr => tr.Appointment)
                    .ThenInclude(a => a.Patient)
                .Include(tr => tr.Test)
                .FirstOrDefaultAsync(tr => tr.TestRecordId == id);

            if (testRecord == null)
            {
                return NotFound();
            }

            var viewModel = new TestResultViewModel
            {
                PatientFullName = testRecord.Appointment?.Patient?.FullName,
                Gender = testRecord.Appointment?.Patient?.Gender,
                DOB = testRecord.Appointment?.Patient?.Dob ?? DateTime.MinValue,
                TestName = testRecord.Test?.Name,
                Note = testRecord.TestNote,
                ResultFileName = testRecord.Result,
                CompletedAt = testRecord.CompletedAt
            };

            return View(viewModel);
        }

        private List<SelectListItem> GetRoomTypeOptions()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Text = "-- Chọn loại phòng cho xét nghiệm --", Value = "" },
                new SelectListItem { Text = AppConstants.RoomTypes.Lab, Value = AppConstants.RoomTypes.Lab },
                new SelectListItem { Text = AppConstants.RoomTypes.Imaging, Value = AppConstants.RoomTypes.Imaging },
                new SelectListItem { Text = AppConstants.RoomTypes.Endoscopy, Value = AppConstants.RoomTypes.Endoscopy },
                new SelectListItem { Text = AppConstants.RoomTypes.Ultrasound, Value = AppConstants.RoomTypes.Ultrasound }

            };
        }
    }

}

