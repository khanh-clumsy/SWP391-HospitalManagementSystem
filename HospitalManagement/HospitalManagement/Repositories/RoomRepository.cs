using HospitalManagement.Models;
using HospitalManagement.Data;
using HospitalManagement.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Repositories
{
    public class RoomRepository : IRoomRepository
    {
        private readonly HospitalManagementContext _context;
        public RoomRepository(HospitalManagementContext context)
        {
            _context = context;
        }




        public async Task<int> CountAsync(string? name, string? building, string? floor, string? status)
        {
            var roomsQuery = _context.Rooms.AsQueryable();

            if (!string.IsNullOrEmpty(name))
                roomsQuery = roomsQuery.Where(r => r.RoomName.Contains(name));

            if (!string.IsNullOrEmpty(building))
                roomsQuery = roomsQuery.Where(r => r.RoomName.StartsWith(building));

            if (!string.IsNullOrEmpty(status))
                roomsQuery = roomsQuery.Where(r => r.Status == status);

            var list = await roomsQuery.ToListAsync();

            // Lọc theo tầng sau khi đã lấy về bộ nhớ
            if (!string.IsNullOrEmpty(floor) && int.TryParse(floor, out int parsedFloor))
            {
                list = list.Where(r => ExtractFloor(r.RoomName) == parsedFloor).ToList();
            }

            return list.Count;
        }


        public async Task<List<string>> GetAllDistinctBuildings()
        {
            return await _context.Rooms
                .Where(r => !string.IsNullOrEmpty(r.RoomName))
                .Select(r => r.RoomName.Substring(0, 1))
                .Distinct()
                .OrderBy(b => b)
                .ToListAsync();
        }

        public async Task<List<string>> GetAllDistinctFloors()
        {
            var floors = await _context.Rooms
                .Where(r => !string.IsNullOrEmpty(r.RoomName) && r.RoomName.Length >= 3)
                .Select(r => r.RoomName.Length >= 5
                    ? r.RoomName.Substring(1, 2)
                    : r.RoomName.Substring(1, 1))
                .Distinct()
                .ToListAsync(); // lấy từ DB trước

            return floors
                .Where(f => int.TryParse(f, out _))
                .OrderBy(f => int.Parse(f))
                .ToList();
        }



        public async Task<RoomWithDoctorDtoViewModel> GetByIdAsync(int id)
        {
            var now = TimeOnly.FromDateTime(DateTime.Now);
            var today = DateOnly.FromDateTime(DateTime.Today);

            var query = from room in _context.Rooms
                        where room.RoomId == id

                        join schedule in _context.Schedules
                            on room.RoomId equals schedule.RoomId into scheduleJoin
                        from schedule in scheduleJoin
                            .Where(s => s.Day == today)
                            .DefaultIfEmpty()

                        join slot in _context.Slots
                            on schedule.SlotId equals slot.SlotId into slotJoin
                        from slot in slotJoin
                            .Where(sl => sl.StartTime <= now && sl.EndTime >= now)
                            .DefaultIfEmpty()

                        join doctor in _context.Doctors
                            on schedule.DoctorId equals doctor.DoctorId into doctorJoin
                        from doctor in doctorJoin.DefaultIfEmpty()

                        select new RoomWithDoctorDtoViewModel
                        {
                            RoomId = room.RoomId,
                            RoomName = room.RoomName,
                            Status = room.Status,
                            DoctorID = doctor != null ? doctor.DoctorId : (int?)null,
                            DoctorName = doctor != null ? doctor.FullName : null
                        };

            // Group lại nếu có nhiều lịch nhưng chỉ muốn lấy 1 bản ghi duy nhất
            var result = await query
                .GroupBy(r => new { r.RoomId, r.RoomName, r.Status })
                .Select(g => g.First())
                .FirstOrDefaultAsync();

            return result!;
        }





        public async Task<List<RoomWithDoctorDtoViewModel>> SearchAsync(string? name,
                                                                string? building,
                                                                string? floor,
                                                                string? status,
                                                                int page,
                                                                int pageSize)
        {
            var now = TimeOnly.FromDateTime(DateTime.Now);
            var today = DateOnly.FromDateTime(DateTime.Today);

            // Lấy toàn bộ rooms
            var rooms = await _context.Rooms.ToListAsync();

            // Lấy tất cả schedule của ngày hôm nay kèm slot và doctor
            var schedules = await (from s in _context.Schedules
                                   join sl in _context.Slots on s.SlotId equals sl.SlotId
                                   join d in _context.Doctors on s.DoctorId equals d.DoctorId
                                   where s.Day == today
                                   select new
                                   {
                                       s.RoomId,
                                       s.DoctorId,
                                       d.FullName,
                                       sl.StartTime,
                                       sl.EndTime
                                   }).ToListAsync();

            // Ánh xạ phòng + lịch (nếu có trong giờ hiện tại)
            var result = rooms.Select(room =>
            {
                var activeSchedule = schedules.FirstOrDefault(s =>
                    s.RoomId == room.RoomId &&
                    s.StartTime <= now &&
                    s.EndTime >= now
                );

                return new RoomWithDoctorDtoViewModel
                {
                    RoomId = room.RoomId,
                    RoomName = room.RoomName,
                    Status = room.Status,
                    DoctorID = activeSchedule?.DoctorId,
                    DoctorName = activeSchedule?.FullName ?? "Trống"
                };
            }).AsQueryable();

            // Lọc thêm các tiêu chí
            if (!string.IsNullOrEmpty(name))
                result = result.Where(r => r.RoomName.Contains(name));

            if (!string.IsNullOrEmpty(building))
                result = result.Where(r => r.RoomName.StartsWith(building));

            if (!string.IsNullOrEmpty(status))
                result = result.Where(r => r.Status == status);

            if (!string.IsNullOrEmpty(floor) && int.TryParse(floor, out int parsedFloor))
                result = result.Where(r => ExtractFloor(r.RoomName) == parsedFloor);

            // Phân trang
            var paged = result
                .OrderBy(r => r.RoomName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return paged;
        }






        public static int ExtractFloor(string roomName)
        {
            if (roomName.Length == 4) // A101
                return int.Parse(roomName.Substring(1, 1));
            else if (roomName.Length == 5) // A1101
                return int.Parse(roomName.Substring(1, 2));
            else
                throw new FormatException("Tên phòng không hợp lệ: " + roomName);
        }


    }
}
