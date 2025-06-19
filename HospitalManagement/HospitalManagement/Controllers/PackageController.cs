using System.Threading.Tasks;
using HospitalManagement.Data;
using HospitalManagement.Models;
using HospitalManagement.Repositories;
using HospitalManagement.ViewModels;
using HospitalManagement.ViewModels.Package;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using static System.Net.Mime.MediaTypeNames;
using X.PagedList;
using X.PagedList.Mvc.Core;
using X.PagedList.Extensions;
using Microsoft.Extensions.Options;

namespace HospitalManagement.Controllers
{
    public class PackageController : Controller
    {
        private readonly HospitalManagementContext _context;
        private readonly ITestRepository _testRepository;
        private readonly IPackageRepository _packageRepository;

        public PackageController(HospitalManagementContext context, ITestRepository testRepository, IPackageRepository packageRepository)
        {
            _context = context;
            _testRepository = testRepository;
            _packageRepository = packageRepository;
        }

        public async Task<IActionResult> Index(string? CategoryFilter, string? AgeFilter, string? GenderFilter, string? PriceRangeFilter, int? page)
        {
            int pageSize = 6;
            int pageNumber = page ?? 1;
            ViewBag.CategoryFilter = CategoryFilter ?? "";
            ViewBag.AgeFilter = AgeFilter ?? "";
            ViewBag.GenderFilter = GenderFilter ?? "A";
            ViewBag.PriceRangeFilter = PriceRangeFilter ?? "";

            ViewBag.AgeRange = GetAgeRangeOptions(AgeFilter);
            ViewBag.PriceRange = GetPriceRangeOptions(PriceRangeFilter);
            ViewBag.Categories = new SelectList(await _context.PackageCategories.ToListAsync(), "PackageCategoryId", "CategoryName", CategoryFilter);
            ViewBag.GenderFilter = GenderFilter;

            // Truy vấn dữ liệu với Include và phân trang
            var pagedList = await _packageRepository.FilterPackagesAsync(CategoryFilter, AgeFilter, GenderFilter, PriceRangeFilter, pageNumber, pageSize);
            return View(pagedList);
        }

        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            var package = await _context.Packages
                .Include(p => p.PackageCategory)
                .FirstOrDefaultAsync(p => p.PackageId == id);
            if (package == null)
            {
                TempData["error"] = "Không tìm thấy gói khám!";
                return NotFound();
            }

            var tests = await _context.PackageTests
                .Where(pt => pt.PackageId == id)
                .Include(pt => pt.Test)
                .Select(pt => pt.Test)
                .ToListAsync();

            var model = new PackageDetailViewModel
            {
                Package = new PackageViewModel
                {
                    PackageId = package.PackageId,
                    PackageName = package.PackageName,
                    Description = package.Description,
                    TargetGender = package.TargetGender,
                    AgeFrom = package.AgeFrom,
                    AgeTo = package.AgeTo,
                    Thumbnail = package.Thumbnail,
                    DiscountPercent = package.DiscountPercent,
                    FinalPrice = package.FinalPrice,
                    OriginalPrice = package.OriginalPrice,
                    PackageCategory = package.PackageCategory
                },
                Tests = tests,
                BookingCount = 100
            };

            return View(model);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            CreatePackageViewModel model = new CreatePackageViewModel()
            {
                AgeOptions = GetAgeRangeOptions(),
                AvailableTests = (List<Test>)_testRepository.GetAll(),
                Categories = await GetCategoryOptions()
            };
            return View(model);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create(CreatePackageViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.AgeOptions = GetAgeRangeOptions();
                model.AvailableTests = (List<Test>)_testRepository.GetAll();
                return View(model);
            }

            string? base64Image = null;
            if (model.ThumbnailFile != null && model.ThumbnailFile.Length > 0)
            {
                using var ms = new MemoryStream();
                await model.ThumbnailFile.CopyToAsync(ms);
                var bytes = ms.ToArray();
                base64Image = Convert.ToBase64String(bytes);
            }

            int ageFrom = 0, ageTo = 100;

            if (!string.IsNullOrEmpty(model.AgeRange))
            {
                if (model.AgeRange.Contains("+"))
                {
                    ageFrom = int.Parse(model.AgeRange.Replace("+", ""));
                    ageTo = 150;
                }
                else
                {
                    var parts = model.AgeRange.Split("-");
                    ageFrom = int.Parse(parts[0]);
                    ageTo = int.Parse(parts[1]);
                }
            }
            var tests = _context.Tests
                        .Where(t => model.SelectedTestIds.Contains(t.TestId))
                        .ToList();

            var originalPrice = tests.Sum(t => t.Price);
            var discountPercent = model.DiscountPercent;
            var finalPrice = originalPrice - (originalPrice * discountPercent / 100);

            Package package = new Package()
            {
                PackageName = model.PackageName ?? "N/A",
                PackageCategoryId = model.SelectedCategoryId,
                TargetGender = model.TargetGender,
                AgeFrom = ageFrom,
                AgeTo = ageTo,
                Thumbnail = base64Image,
                Description = model.Description,
                DiscountPercent = model.DiscountPercent,
                CreatedAt = DateTime.Now,
                OriginalPrice = originalPrice,
                FinalPrice = finalPrice
            };

            _context.Packages.Add(package);
            await _context.SaveChangesAsync();

            var packageTests = tests.Select(test => new PackageTest
            {
                PackageId = package.PackageId,
                TestId = test.TestId
            }).ToList();
            _context.PackageTests.AddRange(packageTests);

            await _context.SaveChangesAsync();

            TempData["success"] = "Tạo gói khám thành công!";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var package = await _context.Packages
                            .Include(p => p.PackageCategory)
                            .Include(p => p.PackageTests)
                            .ThenInclude(pt => pt.Test)
                            .FirstOrDefaultAsync(p => p.PackageId == id);

            if (package == null)
            {
                TempData["error"] = "Không tìm thấy gói khám!";
                return NotFound();
            }

            var ageRange = package.AgeTo == 150
                        ? $"{package.AgeFrom}+"
                        : $"{package.AgeFrom}-{package.AgeTo}";

            var selectedTestIds = package.PackageTests.Select(pt => pt.TestId).ToList();
            var tests = _context.Tests
                         .Where(t => selectedTestIds.Contains(t.TestId))
                         .ToList();

            var originalPrice = tests.Sum(t => t.Price);
            var discountPercent = package.DiscountPercent;
            var finalPrice = originalPrice - (originalPrice * discountPercent / 100);

            EditPackageViewModel model = new EditPackageViewModel()
            {
                PackageId = package.PackageId,
                PackageName = package.PackageName,
                Description = package.Description,
                SelectedCategoryId = package.PackageCategoryId,
                Categories = await GetCategoryOptions(),
                TargetGender = package.TargetGender,
                AgeRange = ageRange,
                AgeOptions = GetAgeRangeOptions(),
                DiscountPercent = package.DiscountPercent ?? 0,
                CurrentThumbnail = package.Thumbnail,
                AvailableTests = (List<Test>)_testRepository.GetAll(),
                SelectedTestIds = package.PackageTests.Select(pt => pt.TestId).ToList(),
                OriginalPrice = originalPrice,
                FinalPrice = finalPrice
            };
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditPackageViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.AgeOptions = GetAgeRangeOptions();
                model.AvailableTests = (List<Test>)_testRepository.GetAll();
                return View(model);
            }

            string? base64Image = null;
            if (model.ThumbnailFile != null && model.ThumbnailFile.Length > 0)
            {
                using var ms = new MemoryStream();
                await model.ThumbnailFile.CopyToAsync(ms);
                var bytes = ms.ToArray();
                base64Image = Convert.ToBase64String(bytes);
            }

            var package = await _context.Packages
                        .Include(p => p.PackageTests)
                        .FirstOrDefaultAsync(p => p.PackageId == model.PackageId);

            if (package == null)
            {
                TempData["error"] = "Không tìm thấy gói khám!";
                return NotFound();
            }
            int ageFrom = 0, ageTo = 100;

            if (!string.IsNullOrEmpty(model.AgeRange))
            {
                if (model.AgeRange.Contains("+"))
                {
                    ageFrom = int.Parse(model.AgeRange.Replace("+", ""));
                    ageTo = 150;
                }
                else
                {
                    var parts = model.AgeRange.Split("-");
                    ageFrom = int.Parse(parts[0]);
                    ageTo = int.Parse(parts[1]);
                }
            }

            // Cập nhật lại danh sách xét nghiệm
            var existingTests = package.PackageTests.ToList();
            _context.PackageTests.RemoveRange(existingTests);

            if (model.SelectedTestIds != null && model.SelectedTestIds.Any())
            {
                foreach (var testId in model.SelectedTestIds)
                {
                    package.PackageTests.Add(new PackageTest
                    {
                        PackageId = package.PackageId,
                        TestId = testId
                    });
                }
            }

            var tests = _context.Tests
                .Where(t => model.SelectedTestIds != null && model.SelectedTestIds.Contains(t.TestId))
                .ToList();

            var originalPrice = tests.Sum(t => t.Price);
            var discountPercent = model.DiscountPercent;
            var finalPrice = originalPrice - (originalPrice * discountPercent / 100);

            // Cập nhật thông tin
            package.PackageName = model.PackageName ?? "N/A";
            package.Description = model.Description;
            package.PackageCategoryId = model.SelectedCategoryId;
            package.TargetGender = model.TargetGender;
            package.AgeFrom = ageFrom;
            package.AgeTo = ageTo;
            package.DiscountPercent = model.DiscountPercent;
            package.Thumbnail = base64Image ?? package.Thumbnail;
            package.FinalPrice = finalPrice;
            package.OriginalPrice = originalPrice;

            Console.WriteLine($"Original Price: {package.OriginalPrice}");
            Console.WriteLine($"Final Price: {package.FinalPrice}");
            Console.WriteLine($"Discount Percent: {package.DiscountPercent}");

            _context.Packages.Update(package);
            await _context.SaveChangesAsync();
            TempData["success"] = "Cập nhật gói khám thành công!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var package = _context.Packages
                .Include(p => p.PackageTests)
                .FirstOrDefault(p => p.PackageId == id);
            if (package == null)
            {
                TempData["error"] = $"Không thể tìm gói khám với ID là {id}!";
                return RedirectToAction("Index");
            }

            _context.PackageTests.RemoveRange(package.PackageTests);

            _context.Packages.Remove(package);
            await _context.SaveChangesAsync();

            TempData["success"] = $"Xóa thành công gói khám với ID là {id}!";
            return RedirectToAction("Index");
        }

        private async Task<List<SelectListItem>> GetCategoryOptions()
        {
            return await _context.PackageCategories
                                .Select(pc => new SelectListItem
                                {
                                    Value = pc.PackageCategoryId.ToString(),
                                    Text = pc.CategoryName
                                })
                                .ToListAsync();
        }
        private List<SelectListItem> GetAgeRangeOptions(string? selectedValue = null)
        {
            var options = new List<SelectListItem>
            {
                new SelectListItem { Text = "Tất cả độ tuổi", Value = "" },
                new SelectListItem { Text = "0 - 2 tuổi", Value = "0-2" },
                new SelectListItem { Text = "0 - 12 tuổi", Value = "0-12" },
                new SelectListItem { Text = "0 - 16 tuổi", Value = "0-16" },
                new SelectListItem { Text = "0 - 18 tuổi", Value = "0-18" },
                new SelectListItem { Text = "0 - 40 tuổi", Value = "0-40" },
                new SelectListItem { Text = "0 - 55 tuổi", Value = "0-55" },
                new SelectListItem { Text = "13 - 17 tuổi", Value = "13-17" },
                new SelectListItem { Text = "18 - 30 tuổi", Value = "18-30" },
                new SelectListItem { Text = "18 - 45 tuổi", Value = "18-45" },
                new SelectListItem { Text = "18 - 55 tuổi", Value = "18-55" },
                new SelectListItem { Text = "31 - 45 tuổi", Value = "31-45" },
                new SelectListItem { Text = "46 - 60 tuổi", Value = "46-60" },
                new SelectListItem { Text = "Trên 60 tuổi", Value = "60+" }
            };

            if (!string.IsNullOrEmpty(selectedValue))
            {
                var selected = options.FirstOrDefault(x => x.Value == selectedValue);
                if (selected != null) selected.Selected = true;
            }
            return options;
        }

        private List<SelectListItem> GetPriceRangeOptions(string? selectedValue = null)
        {
            var options = new List<SelectListItem>{
                new SelectListItem { Text = "Chọn khoảng giá", Value = "" },
                new SelectListItem { Text = "Dưới 1 triệu", Value = "0-1000000" },
                new SelectListItem { Text = "1 - 5 triệu", Value = "1000000-5000000" },
                new SelectListItem { Text = "5 - 10 triệu", Value = "5000000-10000000" },
                new SelectListItem { Text = "Trên 10 triệu", Value = "10000000+" }
            };

            if (!string.IsNullOrEmpty(selectedValue))
            {
                var selected = options.FirstOrDefault(x => x.Value == selectedValue);
                if (selected != null) selected.Selected = true;
            }
            return options;
        }

    }
}
