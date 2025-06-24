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

        public async Task<List<Room>> GetAllRoom()
        {
            return await _context.Rooms
                .Select(r => r)
                .OrderBy(r => r.RoomName)
                .ToListAsync();
        }
        public async Task<Room> GetRoomById(int id)
        {
            return await _context.Rooms.FirstOrDefaultAsync(r => r.RoomId == id);
        }
        public async Task<List<Room>> GetAllActiveRoom()
        {
            return await _context.Rooms
                .Where(r => r.Status == "Active")
                .Select(r => r)
                .OrderBy(r => r.RoomName)
                .ToListAsync();
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

            var query = from room in _context.Rooms

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

            if (!string.IsNullOrEmpty(name))
                query = query.Where(r => r.RoomName.Contains(name));

            if (!string.IsNullOrEmpty(building))
                query = query.Where(r => r.RoomName.StartsWith(building));

            if (!string.IsNullOrEmpty(status))
                query = query.Where(r => r.Status == status);

            // Gọi ToListAsync trước để đưa dữ liệu vào bộ nhớ
            var rawData = await query.ToListAsync();

            // Bổ sung lọc theo floor sau khi đưa vào bộ nhớ
            if (!string.IsNullOrEmpty(floor) && int.TryParse(floor, out int parsedFloor))
            {
                rawData = rawData
                    .Where(r => ExtractFloor(r.RoomName) == parsedFloor)
                    .ToList();
            }

            // Group và phân trang
            var grouped = rawData
                .GroupBy(r => new { r.RoomId, r.RoomName, r.Status })
                .Select(g => g.First())
                .OrderBy(r => r.RoomName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return grouped;
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
