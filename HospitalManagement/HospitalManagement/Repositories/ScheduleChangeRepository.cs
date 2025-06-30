using HospitalManagement.Models;
using HospitalManagement.ViewModels;
using HospitalManagement.Data;
using Microsoft.EntityFrameworkCore;
namespace HospitalManagement.Repositories
{
    public class ScheduleChangeRepository : IScheduleChangeRepository
    {
        private readonly HospitalManagementContext _context;
        public ScheduleChangeRepository(HospitalManagementContext context)
        {
            _context = context;
        }
        public async Task<List<ScheduleRequestViewModel>> SearchAsync(string type, int page, int pageSize)
        {
            var query = _context.ScheduleChangeRequests
                .Include(r => r.FromSchedule).ThenInclude(s => s.Room)
                .Include(r => r.FromSchedule).ThenInclude(s => s.Slot)
                .Include(r => r.Doctor)
                .AsQueryable();

            if (type.ToLower() == "completed")
            {
                query = query.Where(r => r.Status == "Accepted" || r.Status == "Rejected");
            }
            else
            {
                query = query.Where(r => r.Status == "Pending");
            }

            return await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new ScheduleRequestViewModel
                {
                    RequestId = r.RequestId,
                    DoctorName = r.Doctor,
                    FromSlotTime = r.FromSchedule.SlotId,
                    CurrentRoom = r.FromSchedule.Room,
                    FromDay = r.FromSchedule.Day,
                    ToSlotTime = r.ToSlotId,
                    ToDay = r.ToDay,
                    Status = r.Status,
                    Reason = r.Reason,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<int> CountAsync(string type)
        {
            if (type.ToLower() == "completed")
            {
                return await _context.ScheduleChangeRequests
                    .CountAsync(r => r.Status == "Accepted" || r.Status == "Rejected");
            }
            else
            {
                return await _context.ScheduleChangeRequests
                    .CountAsync(r => r.Status == "Pending");
            }
        }

    }
}
