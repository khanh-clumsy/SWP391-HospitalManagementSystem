using HospitalManagement.Data;
using HospitalManagement.ViewModels.Package;
using Microsoft.EntityFrameworkCore;
using X.PagedList;
using X.PagedList.Extensions;
using X.PagedList.EF;
using System.Threading.Tasks;

namespace HospitalManagement.Repositories
{
    public class PackageRepository : IPackageRepository
    {
        private readonly HospitalManagementContext _context;

        public PackageRepository(HospitalManagementContext context)
        {
            _context = context;
        }

        public async Task<IPagedList<PackageViewModel>> FilterPackagesAsync(string? categoryFilter, string? ageFilter, string? genderFilter, string? priceRangeFilter, int pageNumber, int pageSize)
        {
            var query = _context.Packages.AsQueryable();

            if (!string.IsNullOrEmpty(categoryFilter))
            {
                query = query.Where(p => p.PackageCategory.PackageCategoryId.ToString() == categoryFilter);
            }

            if (!string.IsNullOrEmpty(genderFilter) && (genderFilter != "A"))
            {
                query = query.Where(p => p.TargetGender == genderFilter);
            }

            if (!string.IsNullOrEmpty(ageFilter))
            {
                if (ageFilter.Contains("+") && int.TryParse(ageFilter.Replace("+", ""), out int minAge))
                {
                    query = query.Where(p => p.AgeFrom >= minAge); // Trên X tuổi
                }
                else
                {
                    var parts = ageFilter.Split('-');
                    if (parts.Length == 2 &&
                        int.TryParse(parts[0], out int fromAge) &&
                        int.TryParse(parts[1], out int toAge))
                    {
                        query = query.Where(p => p.AgeFrom == fromAge && p.AgeTo == toAge);
                    }
                }
            }

            if (!string.IsNullOrEmpty(priceRangeFilter))
            {
                if (priceRangeFilter.Contains("+") && decimal.TryParse(priceRangeFilter.Replace("+", ""), out decimal minPrice))
                {
                    query = query.Where(p => p.FinalPrice >= minPrice); // Trên X đồng
                }
                else
                {
                    var parts = priceRangeFilter.Split('-');
                    if (parts.Length == 2 &&
                        decimal.TryParse(parts[0], out decimal min) &&
                        decimal.TryParse(parts[1], out decimal max))
                    {
                        query = query.Where(p => p.FinalPrice >= min && p.FinalPrice <= max);
                    }
                }
            }

            return await query
                .OrderBy(p => p.PackageId)
                .Select(p => new PackageViewModel
                {
                    PackageId = p.PackageId,
                    PackageName = p.PackageName,
                    FinalPrice = p.FinalPrice,
                    OriginalPrice = p.OriginalPrice,
                    TargetGender = p.TargetGender,
                    AgeFrom = p.AgeFrom,
                    AgeTo = p.AgeTo,
                    Thumbnail = p.Thumbnail,
                    PackageCategory = new Models.PackageCategory
                    {
                        CategoryName = p.PackageCategory.CategoryName
                    }
                })
                .ToPagedListAsync(pageNumber, pageSize);
        }

        // CRUD operations
        public async Task<Package?> GetByIdAsync(int id)
        {
            return await _context.Packages
                .Include(p => p.PackageCategory)
                .Include(p => p.PackageTests)
                    .ThenInclude(pt => pt.Test)
                .FirstOrDefaultAsync(p => p.PackageId == id);
        }

        public async Task<List<Package>> GetAllAsync()
        {
            return await _context.Packages
                .Include(p => p.PackageCategory)
                .Include(p => p.PackageTests)
                    .ThenInclude(pt => pt.Test)
                .ToListAsync();
        }

        public async Task<Package> CreateAsync(Package package)
        {
            _context.Packages.Add(package);
            await _context.SaveChangesAsync();
            return package;
        }

        public async Task<Package> UpdateAsync(Package package)
        {
            _context.Packages.Update(package);
            await _context.SaveChangesAsync();
            return package;
        }

        public async Task DeleteAsync(int id)
        {
            var package = await _context.Packages.FindAsync(id);
            if (package != null)
            {
                _context.Packages.Remove(package);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Packages.AnyAsync(p => p.PackageId == id);
        }

        // Package-Test relationships
        public async Task<List<Test>> GetTestsByPackageIdAsync(int packageId)
        {
            return await _context.PackageTests
                .Where(pt => pt.PackageId == packageId)
                .Include(pt => pt.Test)
                .Select(pt => pt.Test)
                .ToListAsync();
        }

        public async Task AddTestToPackageAsync(int packageId, int testId)
        {
            var existingPackageTest = await _context.PackageTests
                .FirstOrDefaultAsync(pt => pt.PackageId == packageId && pt.TestId == testId);

            if (existingPackageTest == null)
            {
                var packageTest = new PackageTest
                {
                    PackageId = packageId,
                    TestId = testId
                };
                _context.PackageTests.Add(packageTest);
                await _context.SaveChangesAsync();
            }
        }

        public async Task RemoveTestFromPackageAsync(int packageId, int testId)
        {
            var packageTest = await _context.PackageTests
                .FirstOrDefaultAsync(pt => pt.PackageId == packageId && pt.TestId == testId);

            if (packageTest != null)
            {
                _context.PackageTests.Remove(packageTest);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<Package>> GetPackagesByTestIdAsync(int testId)
        {
            return await _context.PackageTests
                .Where(pt => pt.TestId == testId)
                .Include(pt => pt.Package)
                .Select(pt => pt.Package)
                .ToListAsync();
        }

        // Package categories
        public async Task<List<PackageCategory>> GetAllCategoriesAsync()
        {
            return await _context.PackageCategories.ToListAsync();
        }

        public async Task<PackageCategory?> GetCategoryByIdAsync(int id)
        {
            return await _context.PackageCategories.FindAsync(id);
        }
    }
}
