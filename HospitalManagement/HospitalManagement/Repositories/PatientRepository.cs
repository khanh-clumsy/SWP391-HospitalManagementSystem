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

        public async Task<List<Patient>> GetOngoingPatients(int? doctorId)
        {
            return await _context.Appointments
                .OrderBy(a => a.SlotId)
                .Where(a => a.Status == "Ongoing" && a.DoctorId == doctorId)
                .Include(a => a.Patient)
                .Select(a => a.Patient)
                .ToListAsync();
        }

        public async Task<List<Patient>> GetOngoingLabPatientsByRoom(int roomId)
        {
            var ongoingTrackings = await _context.Trackings
                .Include(t => t.TestList)
                .Include(t => t.Appointment).ThenInclude(a => a.Patient)
                .Where(t =>
                    t.RoomId == roomId &&
                    t.TestList != null &&
                    t.TestList.TestStatus == "Ongoing")
                .ToListAsync();

            var validTrackings = new List<Tracking>();

            foreach (var tracking in ongoingTrackings)
            {
                var appointmentId = tracking.AppointmentId;
                var currentTestListId = tracking.TestListId!.Value;

                // Lấy tất cả TestList có ID nhỏ hơn của cùng AppointmentID
                var previousTestLists = await _context.TestLists
                    .Where(tl =>
                        tl.AppointmentId == appointmentId &&
                        tl.TestListId < currentTestListId)
                    .ToListAsync();

                // Kiểm tra nếu tất cả TestList nhỏ hơn đã "Done"
                if (previousTestLists.All(tl => tl.TestStatus == "Done"))
                {
                    validTrackings.Add(tracking);
                }
            }

            // Trả về danh sách bệnh nhân duy nhất
            return validTrackings
                .Select(t => t.Appointment.Patient)
                .Distinct()
                .ToList();
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
