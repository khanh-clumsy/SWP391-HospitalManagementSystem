using HospitalManagement.Data;
using HospitalManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Repositories
{
    public class AppointmentRepository : IAppointmentRepository
    {
        private readonly HospitalManagementContext _context;

        public AppointmentRepository(HospitalManagementContext context)
        {
            _context = context;
        }

        public async Task<List<Appointment>> Filter(string RoleKey, int UserID, string? Name, string? slotId, string? Date, string? Status)
        {
            var query = _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Include(a => a.Slot)
                .Where(a =>
                    (RoleKey == "PatientID" && a.PatientId == UserID) ||
                    (RoleKey == "StaffID" && a.StaffId == UserID) ||
                    (RoleKey == "DoctorID" && a.DoctorId == UserID))
                .AsQueryable();

            if (!string.IsNullOrEmpty(Name))
            {
                Name = Name.Trim();
                query = query.Where(a => a.Patient.FullName.Contains(Name));
            }

            if (!string.IsNullOrEmpty(slotId) && int.TryParse(slotId, out int parsedSlotId))
            {
                query = query.Where(a => a.SlotId == parsedSlotId);
            }

            if (!string.IsNullOrEmpty(Date) && DateTime.TryParse(Date, out var parsedDate))
            {
                var convertedDate = DateOnly.FromDateTime(parsedDate);
                query = query.Where(a => a.Date == convertedDate);
            }

            if (!string.IsNullOrEmpty(Status))
            {
                query = query.Where(a => a.Status == Status);
            }
            return await query.ToListAsync();
        }
        public async Task<List<Appointment>> FilterForAdmin(string? Name, string? slotId, string? Date, string? Status)
        {
            var query = _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Include(a => a.Slot)
                .AsQueryable();

            if (!string.IsNullOrEmpty(Name))
            {
                Name = Name.Trim();
                query = query.Where(a => a.Patient.FullName.Contains(Name));
            }

            if (!string.IsNullOrEmpty(slotId) && int.TryParse(slotId, out int parsedSlotId))
            {
                query = query.Where(a => a.SlotId == parsedSlotId);
            }

            if (!string.IsNullOrEmpty(Date) && DateTime.TryParse(Date, out var parsedDate))
            {
                var convertedDate = DateOnly.FromDateTime(parsedDate);
                query = query.Where(a => a.Date == convertedDate);
            }

            if (!string.IsNullOrEmpty(Status))
            {
                query = query.Where(a => a.Status == Status);
            }

            return await query.ToListAsync();
        }

       
        public async Task<List<Appointment>> GetAppointmentByDoctorIDAsync(int DoctorID)
        {
            return await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Include(a => a.Staff)
                .Include(a => a.Slot)
                .Include(a => a.Service)
                .Where(a => a.DoctorId == DoctorID)
                .ToListAsync();
        }

        public async Task<List<Appointment>> GetAppointmentByPatientIDAsync(int PatientID)
        {
            return await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Include(a => a.Staff)
                .Include(a => a.Slot)
                .Include(a => a.Service)
                .Where(a => a.PatientId == PatientID)
                .ToListAsync();
        }

        public async Task<List<Appointment>> GetAppointmentBySalesIDAsync(int SalesID)
        {
            return await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Include(a => a.Staff)
                .Include(a => a.Slot)
                .Include(a => a.Service)
                .Where(a => a.StaffId == SalesID)
                .ToListAsync();
        }
        public async Task<Appointment> GetByIdAsync(int id)
        {
            return await _context.Appointments.FindAsync(id);
        }

        public async Task DeleteAsync(Appointment appointment)
        {
            _context.Appointments.Remove(appointment);
            await _context.SaveChangesAsync();
        }
    }
}
