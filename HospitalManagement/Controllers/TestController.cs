using HospitalManagement.Data;
using HospitalManagement.Helpers;
using HospitalManagement.Models;
using HospitalManagement.Repositories;
using HospitalManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using X.PagedList;
using X.PagedList.Extensions;

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

        public IActionResult Index(string searchName, string sortOrder, decimal? minPrice, decimal? maxPrice, int? page)
        {
            int pageSize = 10;
            int pageNumber = page ?? 1;
            var tests = _testRepository.Search(searchName, sortOrder, minPrice, maxPrice).ToPagedList(pageNumber, pageSize);

            ViewBag.SearchName = searchName;
            ViewBag.SortOrder = sortOrder;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;

            ViewBag.CurrentSearchName = searchName;
            ViewBag.CurrentSortOrder = sortOrder;
            ViewBag.CurrentMinPrice = minPrice;
            ViewBag.CurrentMaxPrice = maxPrice;

            return View(tests);
        }

        [Authorize(Roles = AppConstants.Roles.Admin)]
        public IActionResult Create()
        {
            ViewBag.RoomTypes = GetAvailableRoomTypes();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppConstants.Roles.Admin)]
        public IActionResult Create(Test test)
        {
            if (ModelState.IsValid)
            {
                if (test.Price < 0)
                {
                    ModelState.AddModelError("Price", "Giá tiền phải lớn hơn hoặc bằng 0");
                    ViewBag.RoomTypes = GetAvailableRoomTypes();

                    return View(test);
                }

                _testRepository.Add(test);
                _testRepository.Save();
                TempData["success"] = "Thêm xét nghiệm thành công!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.RoomTypes = GetAvailableRoomTypes();

            return View(test);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Update(int id)
        {
            var test = _testRepository.GetById(id);

            if (test == null)
                return NotFound();

            ViewBag.RoomTypes = GetAvailableRoomTypes();


            return View(test);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppConstants.Roles.Admin)]
        public IActionResult Update(Test test)
        {
            if (ModelState.IsValid)
            {
                if (test.Price < 0)
                {
                    ModelState.AddModelError("Price", "Giá tiền phải lớn hơn hoặc bằng 0");
                    ViewBag.RoomTypes = GetAvailableRoomTypes();

                    return View(test);
                }

                _testRepository.Update(test);
                _testRepository.Save();
                TempData["success"] = "Cập nhật xét nghiệm thành công!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.RoomTypes = GetAvailableRoomTypes();

            return View(test);
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
                DOB = testRecord.Appointment?.Patient?.Dob,
                TestName = testRecord.Test?.Name,
                Note = testRecord.TestNote,
                ResultFileName = testRecord.Result,
                CompletedAt = testRecord.CompletedAt
            };

            return View(viewModel);
        }
        // Get available room types for dropdown


        [Authorize(Roles = AppConstants.Roles.Admin)]
        public List<SelectListItem> GetAvailableRoomTypes()
        {
            return new List<SelectListItem>
                {
                    new SelectListItem { Value = AppConstants.RoomTypes.Lab, Text = AppConstants.RoomTypes.Lab },
                    new SelectListItem { Value = AppConstants.RoomTypes.Imaging, Text = AppConstants.RoomTypes.Imaging }
                    
                };
        }
    }
}

