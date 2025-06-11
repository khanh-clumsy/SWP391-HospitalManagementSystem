using HospitalManagement.Data;
using HospitalManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Repositories
{
    public class MedicineRepository : IMedicineRepository
    {
        private readonly HospitalManagementContext _context;

        public MedicineRepository(HospitalManagementContext context)
        {
            _context = context;
        }

        public async Task<List<Medicine>> Filter(string? searchName, string? typeFilter, string? unitFilter)
        {
            var query = _context.Medicines.AsQueryable();

            if (!string.IsNullOrEmpty(searchName))
                query = query.Where(m => m.Name.Contains(searchName));

            if (!string.IsNullOrEmpty(typeFilter))
                query = query.Where(m => m.MedicineType == typeFilter);

            if (!string.IsNullOrEmpty(unitFilter))
                query = query.Where(m => m.Unit == unitFilter);

            return await query.ToListAsync();
        }
    }

}
