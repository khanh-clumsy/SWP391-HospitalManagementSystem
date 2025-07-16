using HospitalManagement.Data;
using HospitalManagement.Models;
using HospitalManagement.ViewModels;
using Microsoft.EntityFrameworkCore;    
namespace HospitalManagement.Repositories
{
    public class FeedbackRepository : IFeedbackRepository
    {
        private readonly HospitalManagementContext _context;

        public FeedbackRepository(HospitalManagementContext context)
        {
            _context = context;
        }

        public Task<bool> PackageExistsAsync(int packageId)
        {
            return _context.Packages.AnyAsync(p => p.PackageId == packageId);
        }

        public Task<bool> ServiceExistsAsync(int serviceId)
        {
            return _context.Services.AnyAsync(s => s.ServiceId == serviceId);
        }

        public Task<bool> HasCompletedAppointmentWithPackageAsync(int patientId, int packageId)
        {
            return _context.Appointments.AnyAsync(a =>
                a.PatientId == patientId &&
                a.PackageId == packageId &&
                a.Status == "Completed");
        }

        public Task<bool> HasCompletedAppointmentWithServiceAsync(int patientId, int serviceId)
        {
            return _context.Appointments.AnyAsync(a =>
                a.PatientId == patientId &&
                a.ServiceId == serviceId &&
                a.Status == "Completed");
        }
        public async Task<int> CountFeedbackAsync(string? name, int? rating, DateOnly? date)
        {
            var query = _context.Feedbacks
                .Include(f => f.Patient)
                .AsQueryable();

            if (!string.IsNullOrEmpty(name))
                query = query.Where(f => f.Patient.FullName.Contains(name.Trim()));

            if (rating.HasValue)
                query = query.Where(f => f.Rating == rating.Value);

            if (date.HasValue)
                query = query.Where(f => DateOnly.FromDateTime(f.CreatedAt) == date.Value);

            return await query.CountAsync();
        }

        public async Task<List<FeedbackManageViewModel>> SearchFeedbackAsync(string? name, int? rating, DateOnly? date, int page, int pageSize)
        {
            var query = _context.Feedbacks
                .Include(f => f.Patient)
                .Include(f => f.Service)
                .Include(f => f.Package)
                .AsQueryable();

            if (!string.IsNullOrEmpty(name))
                query = query.Where(f => f.Patient.FullName.Contains(name.Trim()));

            if (rating.HasValue)
                query = query.Where(f => f.Rating == rating.Value);

            if (date.HasValue)
                query = query.Where(f => DateOnly.FromDateTime(f.CreatedAt) == date.Value);

            return await query
                .OrderByDescending(f => f.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(f => new FeedbackManageViewModel
                {
                    FeedbackId = f.FeedbackId,
                    PatientName = f.Patient.FullName,
                    ItemName = f.Package != null ? f.Package.PackageName : f.Service!.ServiceType,
                    Rating = f.Rating,
                    Comment = f.Comment,
                    CreatedAt = f.CreatedAt,
                    IsSpecial = f.IsSpecial
                })
                .ToListAsync();
        }

        public async Task<List<Feedback>> GetSpecialFeedbacksAsync()
        {
            return await _context.Feedbacks
                .Include(f => f.Patient)
                .Include(f => f.Service)
                .Include(f => f.Package)
                .Where(f => f.IsSpecial)
                .ToListAsync();
        }

    }
}
