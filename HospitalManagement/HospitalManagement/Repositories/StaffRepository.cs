using HospitalManagement.Data;
using HospitalManagement.Models;
using Microsoft.EntityFrameworkCore;


namespace HospitalManagement.Repositories
{
    public class StaffRepository : IStaffRepository
    {
        private readonly HospitalManagementContext _context;
        public StaffRepository(HospitalManagementContext context)
        {
            _context = context;
        }

        public async Task<int> CountAsync(string? name, string? roleName)
        {
            var query = _context.Staff.AsQueryable();

            if (!string.IsNullOrEmpty(name))
            {
                query = query.Where(s => s.FullName.Contains(name));
            }
            if (!string.IsNullOrEmpty(roleName))
            {
                query = query.Where(s => s.RoleName == roleName);
            }

            return await query.CountAsync();
        }

        public async Task<List<string>> GetDistinctRole()
        {
            return await _context.Staff
                .Where(s => s.RoleName != null)
                .Select(s => s.RoleName)
                .Distinct().ToListAsync();
        }

        public async Task<List<Staff>> SearchAsync(string? name, string? roleName, int page, int pageSize)
        {
            var query = _context.Staff.AsQueryable();

            if (!string.IsNullOrEmpty(name))
            {
                query = query.Where(s => s.FullName.Contains(name));
            }
            if (!string.IsNullOrEmpty(roleName))
            {
                query = query.Where(s => s.RoleName == roleName);
            }

            return await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        }
    }
}
