using HospitalManagement.Models;
using HospitalManagement.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalManagement.Controllers
{
    public class TestController : Controller
    {
        private readonly ITestRepository _testRepository;

        public TestController(ITestRepository testRepository)
        {
            _testRepository = testRepository;
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
    }

}

