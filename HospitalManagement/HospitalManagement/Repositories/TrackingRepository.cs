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
        public async Task<List<Tracking>> GetTrackingsByAppointmentIdWithDetailsAsync(int appointmentId)
        {
            return await _context.Trackings
                .Include(t => t.TestRecord)
                    .ThenInclude(tl => tl.Test)
                .Include(t => t.Room)
                .Where(t => t.AppointmentId == appointmentId)
                .ToListAsync();
        }
    }
}
