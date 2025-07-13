using HospitalManagement.Data;
using HospitalManagement.Models;
using Microsoft.EntityFrameworkCore;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
                .Include(a => a.Service)
                .Include(a => a.Package)
                .Where(a =>
                    (RoleKey == "PatientID" && a.PatientId == UserID) ||
                    (RoleKey == "StaffID" && a.CreatedByStaffId == UserID) ||
                    (RoleKey == "DoctorID" && a.DoctorId == UserID))
                .AsQueryable();

            if (!string.IsNullOrEmpty(Name))
            {
                Name = string.Join(" ", Name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
                query = RoleKey switch
                {
                    "PatientID" => query.Where(a => a.Doctor.FullName.Contains(Name)),
                    _ => query.Where(a => a.Patient.FullName.Contains(Name))
                };
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
            else if (RoleKey == "DoctorID")
            {
                query = query.Where(a => a.Status != "Pending");
            }
            return await query.ToListAsync();
        }
        public async Task<List<Appointment>> FilterForAdmin(string? Name, string? slotId, string? Date, string? Status)
        {
            var query = _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Include(a => a.Slot)
                .Include(a => a.Service)
                .Include(a => a.Package)
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
        public async Task<List<Appointment>> FilterApproveAppointment(string? statusFilter, string? searchName, string? timeFilter, string? dateFilter)
        {
            var query = _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Include(a => a.Service)
                .Include(a => a.Package)
                .Include(a => a.Slot)
                .OrderByDescending(a => a.Date)
                .Where(a => a.Status == "Pending" || a.Status == "Confirmed" || a.Status == "Rejected")
                .AsQueryable();

            // Lọc theo status
            if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "All")
            {
                query = query.Where(a => a.Status == statusFilter);
            }

            // Lọc theo tên bệnh nhân
            if (!string.IsNullOrEmpty(searchName))
            {
                query = query.Where(a => a.Patient.FullName.Contains(searchName));
            }

            // Lọc theo SlotId (dựa trên timeFilter truyền dưới dạng SlotId)
            if (!string.IsNullOrEmpty(timeFilter) && int.TryParse(timeFilter, out int parsedSlotId))
            {
                query = query.Where(a => a.SlotId == parsedSlotId);
            }

            // Lọc theo ngày khám
            if (!string.IsNullOrEmpty(dateFilter) && DateTime.TryParse(dateFilter, out var parsedDate))
            {
                var convertedDate = DateOnly.FromDateTime(parsedDate);
                query = query.Where(a => a.Date == convertedDate);
            }

            return await query.ToListAsync();
        }


        public IQueryable<Appointment> GetAppointmentByDoctorID(int DoctorID)
        {
            return _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Include(a => a.CreatedByStaff)
                .Include(a => a.Slot)
                .Include(a => a.Service)
                .Where(a => a.DoctorId == DoctorID)
                .OrderByDescending(a => a.AppointmentId);
        }

        public IQueryable<Appointment> GetAppointmentByPatientID(int PatientID)
        {
            return _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Include(a => a.CreatedByStaff)
                .Include(a => a.Slot)
                .Include(a => a.Service)
                .Where(a => a.PatientId == PatientID)
                .OrderByDescending(a => a.AppointmentId);
        }

        public IQueryable<Appointment> GetAppointmentBySalesID(int SalesID)
        {
            return _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Include(a => a.CreatedByStaff)
                .Include(a => a.Slot)
                .Include(a => a.Service)
                .Where(a => a.CreatedByStaffId == SalesID)
                .OrderByDescending(a => a.AppointmentId);
        }

        public async Task<Appointment?> GetByIdAsync(int id)
        {
            return await _context.Appointments.FindAsync(id);
        }

        public async Task DeleteAsync(Appointment appointment)
        {
            _context.Appointments.Remove(appointment);
            await _context.SaveChangesAsync();
        }
        public async Task<bool> HasAppointmentAsync(int doctorId, int slotId, DateOnly day)
        {
            return await _context.Appointments.Where(a => a.Status == "Pending" || a.Status == "Confirmed").AnyAsync(a =>
                a.DoctorId == doctorId &&
                a.SlotId == slotId &&
                a.Date == day
            );
        }

        public async Task<List<Appointment>> GetAppointmentsAsync(string phone)
        {
            var query = _context.Appointments
                .Include(a => a.Patient)
                .Where(a => a.Status == "Confirmed");

            if (!string.IsNullOrWhiteSpace(phone))
            {
                phone = string.Join("", phone.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
                query = query.Where(a => a.Patient.PhoneNumber.Contains(phone));
            }

            return await query
                .ToListAsync();
        }
        public async Task<List<Appointment>> GetTodayAppointmentsAsync(string? phone)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);

            var query = _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Slot)
                .Include(a => a.InvoiceDetails)
                .Where(a => a.Status == "Confirmed" && a.Date == today);
           
            var appointments = await query
                .OrderBy(a => a.Slot.StartTime)
                .ToListAsync();

            // Gán giá trị IsServiceOrPackagePaid
            foreach (var appointment in appointments)
            {
                appointment.IsServiceOrPackagePaid = appointment.InvoiceDetails != null &&
                    appointment.InvoiceDetails.Any(i =>
                        (i.ItemType == "Service" || i.ItemType == "Package") &&
                        (i.PaymentStatus == "Success") &&
                        (i.UnitPrice > 0)
                    );
            }

            return appointments;
        }

        public async Task StartAppointmentAsync(int appointmentId)
        {
            var appointment = await _context.Appointments.FindAsync(appointmentId);
            if (appointment != null)
            {
                appointment.Status = "OnGoing";
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<Appointment>> GetOngoingAppointmentsByDoctorIdAsync(int doctorId)
        {
            return await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Slot)
                .Where(a => a.DoctorId == doctorId && a.Status == "Ongoing" && a.Date == DateOnly.FromDateTime(DateTime.Today))
                .OrderBy(a => a.Date).ThenBy(a => a.Slot.StartTime)
                .ToListAsync();
        }

        public async Task<Appointment> GetAppointmentByIdAsync(int appointmentId)
        {
            return await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Trackings)
                .ThenInclude(t => t.Room)
                .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);
        }
    }
}