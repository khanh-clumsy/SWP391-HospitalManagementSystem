using HospitalManagement.Data;
using HospitalManagement.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace HospitalManagement.Repositories
{
    public class TrackingRepository : ITrackingRepository
    {
        private readonly HospitalManagementContext _context;

        public TrackingRepository(HospitalManagementContext context)
            {
                _context = context;
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
                .Where(a => a.DoctorId == doctorId && a.Status == "Ongoing")
                .OrderBy(a => a.Date).ThenBy(a => a.Slot.StartTime)
                .ToListAsync();
        } 

        public async Task<Tracking> GetAppointmentByIdAsync(int appointmentId)
        {
            return await _context.Trackings
            .Include(t => t.Room)
            .Include(t => t.Appointment)
            .ThenInclude(a => a.Patient)
            .FirstOrDefaultAsync(t => t.AppointmentId == appointmentId);
        }

        public async Task<List<Tracking>> GetRoomByAppointmentIdAsync(int appointmentId)
        {
            return await _context.Trackings
                .Include(t => t.Room)
                .Where(t => t.AppointmentId == appointmentId)
                .ToListAsync();
        }
        public async Task<List<Room>> GetTestRoomsAsync()
        {
            return await _context.Rooms
                .Where(r => r.RoomType != "Phòng khám" && r.RoomType != "Khác")
                .ToListAsync();
        }
    }
}
