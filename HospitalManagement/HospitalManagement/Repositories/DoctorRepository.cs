using HospitalManagement.Models;
using Microsoft.EntityFrameworkCore;
using HospitalManagement.Data;
namespace HospitalManagement.Repositories
{
    public class DoctorRepository : IDoctorRepository
    {
        private readonly HospitalManagementContext _context;

        public DoctorRepository(HospitalManagementContext context)
        {
            _context = context;
        }

        public async Task<int> CountAsync(string? name, string? department, int? exp, bool? isHead)
        {
            var query = _context.Doctors.AsQueryable();

            if (!string.IsNullOrEmpty(name))
            {
                query = query.Where(d => d.FullName.Contains(name));
            }
            if (!string.IsNullOrEmpty(department))
            {
                query = query.Where(d => d.DepartmentName == department);
            }
            if (exp.HasValue)
            {
                query = query.Where(d => d.ExperienceYear >= exp.Value);
            }
            if (isHead.HasValue)
            {
                query = query.Where(d => d.IsDepartmentHead == isHead.Value);
            }

            return await query.CountAsync();
        }

        public async Task<List<Doctor>> GetAllAsync()
        {
            return await _context.Doctors.ToListAsync();
        }

        public async Task<Doctor?> GetByIdAsync(int id)
        {
            return await _context.Doctors.FirstOrDefaultAsync(d => d.DoctorId == id);
        }

        public async Task<List<string?>> GetDistinctDepartment()
        {
            return await _context.Doctors
                .Where(d => d.DepartmentName != null)
                .Select(d => d.DepartmentName)
                .Distinct().ToListAsync();
        }

        public async Task<List<Doctor>> SearchAsync(string? name, string? department, int? exp, bool? isHead, string? sort, int page, int pageSize)
        {
            var query = _context.Doctors.AsQueryable();

            if (!string.IsNullOrEmpty(name))
            {
                query = query.Where(d => d.FullName.Contains(name));
            }
            if (!string.IsNullOrEmpty(department))
            {
                query = query.Where(d => d.DepartmentName == department);
            }
            if (exp.HasValue)
            {
                query = query.Where(d => d.ExperienceYear >= exp);
            }
            if (isHead.HasValue)
            {
                query = query.Where(d => d.IsDepartmentHead == isHead);
            }
            if(sort == "asc")
            {
                query = query.OrderBy(d => d.ExperienceYear);
            }
            else if(sort == "desc")
            {
                query = query.OrderByDescending(d => d.ExperienceYear);
            }
            else
            {
                query = query.OrderBy(d => d.DoctorId);
            }

            return await query.Skip((page-1)*pageSize).Take(pageSize).ToListAsync();
        }
        public async Task<List<Doctor>> GetAllDoctorsWithSpecialFirstAsync(int pageNumber, int pageSize)
        {
            return await _context.Doctors
                .OrderByDescending(d => d.IsSpecial) // Ưu tiên bác sĩ đặc biệt
                .ThenBy(d => d.DoctorId) // Sắp xếp phụ theo tên (hoặc theo ý bạn)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> CountAllDoctorsAsync()
        {
            return await _context.Doctors.CountAsync();
        }
    }
}
