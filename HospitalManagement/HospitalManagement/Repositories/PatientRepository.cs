using HospitalManagement.Data;
using HospitalManagement.Models;
using Microsoft.EntityFrameworkCore;
using System.Drawing.Printing;

namespace HospitalManagement.Repositories
{
    public class PatientRepository : IPatientRepository
    {
        private readonly HospitalManagementContext _context;
        public PatientRepository(HospitalManagementContext context)
        {
            _context = context;
        }

        public async Task<int> CountAsync(string? name, string? gender)
        {
            var query = _context.Patients.AsQueryable();

            if (!string.IsNullOrEmpty(name))
            {
                name = name.Trim();
                query = query.Where(p => p.FullName.Contains(name));
            }
            if (!string.IsNullOrEmpty(gender))
            {
                query = query.Where(p => p.Gender == gender);
            }

            return await query.CountAsync();
        }

        public async Task<Patient?> GetByIdAsync(int id)
        {
            return await _context.Patients.FirstOrDefaultAsync(p => p.PatientId == id);
        }
        public async Task<List<Patient>> SearchAsync(string? name, string? gender, int page, int pageSize)
        {
            var query = _context.Patients.AsQueryable();

            if (!string.IsNullOrEmpty(name))
            {
                name = name.Trim();
                query = query.Where(p => p.FullName.Contains(name));
            }
            if (!string.IsNullOrEmpty(gender))
            {
                query = query.Where(p => p.Gender == gender);
            }

            return await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        }
    }
}
