using HospitalManagement.Data;
using HospitalManagement.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
namespace HospitalManagement.Repositories
{
    public class DoctorRepository : IDoctorRepository
    {
        private readonly HospitalManagementContext _context;

        public DoctorRepository(HospitalManagementContext context)
        {
            _context = context;
        }

        public async Task<int> CountAsync(string? name, string? department, int? exp, bool? isHead, bool? isActive, bool? containTestDoc)
        {
            var query = _context.Doctors.AsQueryable();
            if(!(containTestDoc.HasValue) || containTestDoc == false)
            {
                query = query.Where(d => d.DepartmentName != "Xét nghiệm" && d.DepartmentName != "Chẩn đoán hình ảnh");
            }
            if (isActive.HasValue)
            {
                query = query.Where(d => d.IsActive == isActive);
            }
            if (!string.IsNullOrEmpty(name))
            {
                name = name.Trim();
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

        public async Task<List<string?>> GetDistinctDepartment(bool? containTestDoc)
        {
            if (!(containTestDoc.HasValue) || containTestDoc == false)
            {
                return await _context.Doctors
                    .Where(d => d.DepartmentName != "Xét nghiệm" && d.DepartmentName != "Chẩn đoán hình ảnh")
                    .Select(d => d.DepartmentName)
                    .Distinct().ToListAsync();
            }
            return await _context.Doctors
                .Where(d => d.DepartmentName != null)
                .Select(d => d.DepartmentName)
                .Distinct().ToListAsync();
        }

        public async Task<List<Doctor>> GetAllDoctorsWithDepartment(string dep)
        {
            return await _context.Doctors
                .Where(d => d.DepartmentName != null && d.DepartmentName == dep)
                .Select(d => d)
                .ToListAsync();
        }
        public async Task<List<Doctor>> SearchAsync(string? name, string? department, int? exp, bool? isHead, string? sort, bool? isActive,bool? containTestDoc, int page, int pageSize)
        {
            var query = _context.Doctors.AsQueryable();
            if (!(containTestDoc.HasValue) || containTestDoc == false)
            {
                query = query.Where(d => d.DepartmentName != "Xét nghiệm" && d.DepartmentName != "Chẩn đoán hình ảnh");
            }
            if (isActive.HasValue)
            {
                query = query.Where(d => d.IsActive == isActive);
            }
            if (!string.IsNullOrEmpty(name))
            {
                name = name.Trim();
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
                .Where(d=>d.IsActive == true)
                .Where(d => d.DepartmentName != "Xét nghiệm" && d.DepartmentName != "Chẩn đoán hình ảnh") // Loại bỏ bác sĩ xét nghiệm và chẩn đoán hình ảnh
                .OrderByDescending(d => d.IsSpecial) // Ưu tiên bác sĩ đặc biệt
                .ThenBy(d => d.DoctorId) // Sắp xếp phụ theo tên (hoặc theo ý bạn)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> CountAllActiveDoctorsAsync()
        {
            return await _context.Doctors.Where(d=>d.IsActive == true).Where(d => d.DepartmentName != "Xét nghiệm" && d.DepartmentName != "Chẩn đoán hình ảnh").CountAsync();
        }

        public async Task<List<Doctor>> GetDoctorsBySchedule(List<int> ids)
        {
            var doctors = await _context.Schedules
                .Where(s => ids.Contains(s.ScheduleId))
                .Include(s => s.Doctor).Select(s => s.Doctor)
                .Distinct()
                .ToListAsync();

            return doctors;
        }

        public async Task<List<SelectListItem>> GetAvailableDoctorsAsync(string departmentName, int slotId, DateOnly day, int excludeDoctorId)
        {
            // Danh sách bác sĩ cùng khoa (trừ người hiện tại)
            var doctorIdsInSameDept = await _context.Doctors
                .Where(d => d.DepartmentName == departmentName && d.DoctorId != excludeDoctorId)
                .Select(d => d.DoctorId)
                .ToListAsync();

            // Các bác sĩ có lịch vào slot & ngày đó
            var scheduledDoctorIds = await _context.Schedules
                .Where(s => doctorIdsInSameDept.Contains(s.DoctorId) && s.SlotId == slotId && s.Day == day)
                .Select(s => s.DoctorId)
                .Distinct()
                .ToListAsync();

            // Các bác sĩ KHÔNG có appointment trong slot đó ngày đó
            var busyDoctorIds = await _context.Appointments
                .Where(a => (a.Status == "Pending" || a.Status == "Confirmed") &&
                            a.SlotId == slotId && a.Date == day)
                .Select(a => a.DoctorId)
                .Where(id => id != null)
                .Cast<int>()
                .Distinct()
                .ToListAsync();

            var availableDoctorIds = scheduledDoctorIds.Except(busyDoctorIds).ToList();

            // Truy xuất thông tin bác sĩ
            var doctors = await _context.Doctors
                .Where(d => availableDoctorIds.Contains(d.DoctorId))
                .Select(d => new SelectListItem
                {
                    Value = d.DoctorId.ToString(),
                    Text = d.FullName
                })
                .ToListAsync();

            return doctors;
        }

    }
}
