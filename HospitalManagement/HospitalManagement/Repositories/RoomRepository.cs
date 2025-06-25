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




        public async Task<int> CountAsync(
                        string? name,
                        string? building,
                        string? floor,
                        string? status,
                        string? roomType) 
        {
            var roomsQuery = _context.Rooms.AsQueryable();

            if (!string.IsNullOrEmpty(name))
                roomsQuery = roomsQuery.Where(r => r.RoomName.Contains(name));

            if (!string.IsNullOrEmpty(building))
                roomsQuery = roomsQuery.Where(r => r.RoomName.StartsWith(building));

            if (!string.IsNullOrEmpty(status))
                roomsQuery = roomsQuery.Where(r => r.Status == status);

            if (!string.IsNullOrEmpty(roomType)) 
                roomsQuery = roomsQuery.Where(r => r.RoomType == roomType);

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
        public async Task<List<string>> GetAllDistinctRoomTypes()
        {
            return await _context.Rooms
                .Where(r => !string.IsNullOrEmpty(r.RoomType))
                .Select(r => r.RoomType)
                .Distinct()
                .OrderBy(rt => rt)
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



        public async Task<Room?> GetByIdAsync(int id)
        {
            return await _context.Rooms
                .Where(r => r.RoomId == id)
                .Select(r => new Room
                {
                    RoomId = r.RoomId,
                    RoomName = r.RoomName,
                    RoomType = r.RoomType,
                    Status = r.Status
                })
                .FirstOrDefaultAsync();
        }



        public async Task<List<RoomWithDoctorDtoViewModel>> SearchAsync(
                                    string? name,
                                    string? building,
                                    string? floor,
                                    string? status,
                                    string? roomType, 
                                    int page,
                                    int pageSize)
        {
            var now = TimeOnly.FromDateTime(DateTime.Now);
            var today = DateOnly.FromDateTime(DateTime.Today);

            var rooms = await _context.Rooms.ToListAsync();

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
                    RoomType = room.RoomType, // ✅ Lấy RoomType
                    DoctorID = activeSchedule?.DoctorId,
                    DoctorName = activeSchedule?.FullName ?? "Trống"
                };
            }).AsQueryable();

            // Các điều kiện lọc
            if (!string.IsNullOrEmpty(name))
                result = result.Where(r => r.RoomName.Contains(name));

            if (!string.IsNullOrEmpty(building))
                result = result.Where(r => r.RoomName.StartsWith(building));

            if (!string.IsNullOrEmpty(status))
                result = result.Where(r => r.Status == status);

            if (!string.IsNullOrEmpty(roomType)) 
                result = result.Where(r => r.RoomType == roomType);

            if (!string.IsNullOrEmpty(floor) && int.TryParse(floor, out int parsedFloor))
                result = result.Where(r => ExtractFloor(r.RoomName) == parsedFloor);

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

        public async Task<List<Room>> GetAvailableRoomsForSchedulesAsync(List<int> selectedScheduleIds)
        {

            // Lấy danh sách lịch đã chọn
            var selectedSchedules = await _context.Schedules
                .Where(s => selectedScheduleIds.Contains(s.ScheduleId))
                .Include(s => s.Slot)
                .ToListAsync();

            // Nếu không có lịch nào, trả về danh sách rỗng
            if (!selectedSchedules.Any())
                return new List<Room>();

            // Loại bỏ chính phòng đang cần chuyển lịch đi
            var currentRoomId = selectedSchedules.First().RoomId;


            // Lấy danh sách các phòng đang hoạt động
            var activeRooms = await _context.Rooms
                .Where(r => r.Status == "Hoạt động")
                .ToListAsync();

            // Tạo danh sách để chứa các phòng thỏa điều kiện
            var availableRooms = new List<Room>();

            foreach (var room in activeRooms)
            {
                bool conflict = false;

                foreach (var schedule in selectedSchedules)
                {
                    // Kiểm tra nếu phòng này đã có lịch trùng ngày và slot thì không hợp lệ
                    bool hasConflict = await _context.Schedules.AnyAsync(s =>
                        s.RoomId == room.RoomId &&
                        s.Day == schedule.Day &&
                        s.SlotId == schedule.SlotId &&
                        !selectedScheduleIds.Contains(s.ScheduleId) // Tránh tự trùng với chính lịch đã chọn
                    );

                    if (hasConflict)
                    {
                        conflict = true;
                        break;
                    }
                }

                if (!conflict && room.RoomId != currentRoomId)
                {
                    availableRooms.Add(room);
                }

            }

            return availableRooms;
        }
    }
}
