using HospitalManagement.Data;
using HospitalManagement.Models;
using HospitalManagement.Repositories;
using HospitalManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        public IActionResult Index(string searchName, string sortOrder, decimal? minPrice, decimal? maxPrice)
        {
            var tests = _testRepository.Search(searchName, sortOrder, minPrice, maxPrice);

            ViewBag.SearchName = searchName;
            ViewBag.SortOrder = sortOrder;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;

            return View(tests);
        }
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public IActionResult Create(Test test)
        {
            if (ModelState.IsValid)
            {
                _testRepository.Add(test);
                _testRepository.Save();
                return RedirectToAction(nameof(Index));
            }
            return View(test);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Update(int id)
        {
            
            var test = _testRepository.GetById(id);
         
            if (test == null)
                return NotFound();
          
            return View(test);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public IActionResult Update(Test test)
        {
            if (ModelState.IsValid)
            {              
                if (test.Price < 0)
                {
                    TempData["error"] = "Price must equal or greater than o";
                    return View(test);
                }
                _testRepository.Update(test);
                _testRepository.Save();
                return RedirectToAction(nameof(Index));
            }
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
                DOB = testRecord.Appointment?.Patient?.Dob ?? DateTime.MinValue,
                TestName = testRecord.Test?.Name,
                Note = testRecord.TestNote,
                ResultFileName = testRecord.Result,
                CompletedAt = testRecord.CompletedAt
            };

            return View(viewModel);
        }
    }

}

