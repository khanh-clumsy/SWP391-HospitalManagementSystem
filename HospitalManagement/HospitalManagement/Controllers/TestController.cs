using System.Threading.Tasks;
using HospitalManagement.Models;
using HospitalManagement.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Controllers
{
    public class TestController : Controller
    {
        private readonly ITestRepository _testRepository;

        public TestController(ITestRepository testRepository)
        {
            _testRepository = testRepository;
        }

        public async Task<IActionResult> Index(string searchName, string sortOrder, decimal? minPrice, decimal? maxPrice)
        {
            var tests = await _testRepository.SearchAsync(searchName, sortOrder, minPrice, maxPrice);

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
        public async Task<IActionResult> Create(Test test)
        {
            if (test.Price <= 0)
            {
                ModelState.AddModelError("Price", "Price must be greater than 0.");
            }

            if (ModelState.IsValid)
            {
                await _testRepository.AddAsync(test);
                return RedirectToAction(nameof(Index));
            }

            return View(test);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id)
        {
            var test = await _testRepository.GetByIdAsync(id);
            if (test == null)
                return NotFound();

            return View(test);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(Test test)
        {
            if (ModelState.IsValid)
            {
                await _testRepository.UpdateAsync(test);
                return RedirectToAction(nameof(Index));
            }
            return View(test);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var test = await _testRepository.GetByIdAsync(id);
            if (test == null)
            {
                return NotFound();
            }
            await _testRepository.DeleteAsync(test);
            return RedirectToAction(nameof(Index));
        }
    }
}
