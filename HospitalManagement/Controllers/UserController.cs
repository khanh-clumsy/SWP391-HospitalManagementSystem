using HospitalManagement.Data;
using HospitalManagement.Models;
using HospitalManagement.Repositories;
using HospitalManagement.Services;
using HospitalManagement.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.DotNet.Scaffolding.Shared.Project;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using X.PagedList;

namespace HospitalManagement.Controllers
{
    public class UserController : Controller
    {
        private readonly HospitalManagementContext _context;
        private readonly IDoctorRepository _doctorRepo;
        private readonly IPatientRepository _patientRepo;
        private readonly IStaffRepository _staffRepo;
        private readonly IRoomRepository _roomRepo;
        private readonly PasswordHasher<Patient> _passwordHasher;
        private readonly EmailService _emailService;
        private readonly IScheduleRepository _scheduleRepo;
        private readonly ISlotRepository _slotRepo;
        private readonly IScheduleChangeRepository _scheduleChangeRepo;
        private readonly IAppointmentRepository _appointmentRepo;
        public UserController(HospitalManagementContext context, IDoctorRepository doctorRepo, IPatientRepository patientRepo, IStaffRepository staffRepo, IRoomRepository roomRepo, EmailService emailService, IScheduleRepository scheduleRepository, ISlotRepository slotRepository, IScheduleChangeRepository scheduleChangeRepo, IAppointmentRepository appointmentRepo)
        {
            _context = context;
            _doctorRepo = doctorRepo;
            _patientRepo = patientRepo;
            _staffRepo = staffRepo;
            _roomRepo = roomRepo;
            _passwordHasher = new PasswordHasher<Patient>();
            _emailService = emailService;
            _scheduleRepo = scheduleRepository;
            _slotRepo = slotRepository;
            _scheduleChangeRepo = scheduleChangeRepo;
            _appointmentRepo = appointmentRepo;
        }


        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ManageAccount(int? page, string? name, string? department, string? gender, string? roleName, string type = "Patient")
        {
            name = UserController.NormalizeName(name);
            int pageSize = 10;
            int pageNumber = page ?? 1;

            var vm = new AccountListViewModel();

            List<Patient> patients = await _patientRepo.SearchAsync(name, gender, pageNumber, pageSize);
            var totalPatients = await _patientRepo.CountAsync(name, gender);
            vm.Patients = new StaticPagedList<Patient>(patients, pageNumber, pageSize, totalPatients);

            List<Doctor> doctors = await _doctorRepo.SearchAsync(name, department, null, null, null, null, true, pageNumber, pageSize);
            var totalDoctors = await _doctorRepo.CountAsync(name, department, null, null, null, true);
            vm.Doctors = new StaticPagedList<Doctor>(doctors, pageNumber, pageSize, totalDoctors);

            List<Staff> staffs = await _staffRepo.SearchAsync(name, roleName, pageNumber, pageSize);
            var totalStaffs = await _staffRepo.CountAsync(name, roleName);
            vm.Staffs = new StaticPagedList<Staff>(staffs, pageNumber, pageSize, totalStaffs);

            vm.AccountType = type;
            // Truyền vào các Department
            var departments = await _doctorRepo.GetDistinctDepartment(true);
            var roles = await _staffRepo.GetDistinctRole();

            // Truyền lại filter cho view
            ViewBag.Name = name;
            ViewBag.Gender = gender;
            ViewBag.Department = department;
            ViewBag.Role = roleName;
            ViewBag.Departments = departments;
            ViewBag.Roles = roles;

            return View(vm);
        }


        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ManageRoom(int? page, string? name, string? building, string? floor, string? status, string? roomType)
        {
            name = UserController.NormalizeName(name);
            int pageSize = 10;
            int pageNumber = page ?? 1;

            List<RoomWithDoctorDtoViewModel> rooms = await _roomRepo.SearchAsync(name, building, floor, status, roomType, pageNumber, pageSize);
            var totalRooms = await _roomRepo.CountAsync(name, building, floor, status, roomType);
            var pagedRooms = new StaticPagedList<RoomWithDoctorDtoViewModel>(rooms, pageNumber, pageSize, totalRooms);
            var allBuildings = await _roomRepo.GetAllDistinctBuildings();
            var allFloors = await _roomRepo.GetAllDistinctFloors();
            var allRoomTypes = await _roomRepo.GetAllDistinctRoomTypes();

            ViewBag.Name = name;
            ViewBag.Building = building;
            ViewBag.Floor = floor;
            ViewBag.Status = status;
            ViewBag.RoomType = roomType;
            ViewBag.AllBuildings = allBuildings;
            ViewBag.AllFloors = allFloors;
            ViewBag.AllRoomTypes = allRoomTypes;

            return View(pagedRooms);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RoomDetail(int id, DateOnly? weekStart)
        {
            ViewBag.Units = GetAllRoomTypes();
            ViewBag.Slots = await _slotRepo.GetAllSlotsAsync();
            var room = await _context.Rooms.FindAsync(id);
            if (room == null) return RedirectToAction("NotFound", "Home");

            DateOnly selectedWeekStart = weekStart ?? GetCurrentWeekStart();

            // Xác định năm theo tuần
            int selectedYear = GetYearOfWeek(selectedWeekStart);

            var schedule = await _scheduleRepo.GetScheduleByRoomAndWeekAsync(id, selectedWeekStart);

            ViewBag.SelectedYear = selectedYear;
            ViewBag.SelectedWeekStart = selectedWeekStart;

            var model = new RoomDetailViewModel
            {
                RoomId = room.RoomId,
                RoomName = room.RoomName,
                RoomType = room.RoomType,
                Status = room.Status,
                Schedule = schedule
            };

            return View(model);
        }



        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult UpdatePatientStatus(List<Patient> patients)
        {
            foreach (var updated in patients)
            {
                var existing = _context.Patients.FirstOrDefault(p => p.PatientId == updated.PatientId);
                if (existing != null)
                {
                    existing.IsActive = updated.IsActive;
                }
            }

            _context.SaveChanges();
            TempData["success"] = "Cập nhật trạng thái thành công";
            return RedirectToAction("ManageAccount", new { type = "Patient" });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult UpdateDoctorStatus(List<Doctor> doctors)
        {

            using var transaction = _context.Database.BeginTransaction();

            try
            {
                foreach (var updated in doctors)
                {
                    var existing = _context.Doctors.FirstOrDefault(d => d.DoctorId == updated.DoctorId);
                    if (existing != null)
                    {
                        existing.IsActive = updated.IsActive;
                        existing.IsDepartmentHead = updated.IsDepartmentHead;
                        existing.IsSpecial = updated.IsSpecial;
                    }
                }

                _context.SaveChanges();

                // Kiểm tra vi phạm
                var violate = _context.Doctors
                    .Where(d => d.IsDepartmentHead == true)
                    .GroupBy(d => d.DepartmentName)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();

                if (violate.Any())
                {
                    // Nếu có vi phạm thì rollback lại
                    transaction.Rollback();

                    var deptName = string.Join(", ", violate);
                    TempData["error"] = $"Mỗi khoa chỉ được phép có một trưởng khoa, Lỗi ở khoa: {deptName}";
                    return RedirectToAction("ManageAccount", new { type = "Doctor" });
                }

                // Không có vi phạm => commit thay đổi
                transaction.Commit();

                TempData["success"] = "Cập nhật trạng thái thành công";
                return RedirectToAction("ManageAccount", new { type = "Doctor" });
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                TempData["error"] = "Lỗi xảy ra khi cập nhật trạng thái";
                return RedirectToAction("ManageAccount", new { type = "Doctor" });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult UpdateStaffStatus(List<Staff> staff)
        {
            foreach (var updated in staff)
            {
                var existing = _context.Staff.FirstOrDefault(s => s.StaffId == updated.StaffId);
                if (existing != null)
                {
                    existing.IsActive = updated.IsActive;
                }
            }

            _context.SaveChanges();
            TempData["success"] = "Cập nhật trạng thái thành công";
            return RedirectToAction("ManageAccount", new { type = "Staff" });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateRoom(RoomDetailViewModel room, DateOnly weekStart)
        {
            // Kiểm tra null hoặc trống
            if (string.IsNullOrWhiteSpace(room.RoomName) ||
                string.IsNullOrWhiteSpace(room.RoomType) ||
                string.IsNullOrWhiteSpace(room.Status))
            {
                return await ReturnRoomDetailWithError(room, "Vui lòng không để trống các trường bắt buộc.", weekStart);
            }

            //// Kiểm tra định dạng RoomName
            //if (!System.Text.RegularExpressions.Regex.IsMatch(room.RoomName, @"^[A-Z][0-9]{3,4}$"))
            //{
            //    return await ReturnRoomDetailWithError(room, "Tên phòng phải có dạng A101 hoặc A1001.", weekStart);
            //}

            // Kiểm tra trùng tên phòng
            //var existingRoom = await _context.Rooms
            //    .FirstOrDefaultAsync(r => r.RoomName == room.RoomName && r.RoomId != room.RoomId);

            //if (existingRoom != null)
            //{
            //    return await ReturnRoomDetailWithError(room, "Tên phòng đã tồn tại.", weekStart);
            //}

            // Tìm phòng hiện tại để cập nhật
            var roomInDb = await _context.Rooms
                .Include(r => r.Schedules)
                .ThenInclude(s => s.Slot)
                .FirstOrDefaultAsync(r => r.RoomId == room.RoomId);

            if (roomInDb == null)
            {
                TempData["error"] = "Phòng không tồn tại.";
                return RedirectToAction("ManageRoom");
            }

            // Nếu muốn chuyển sang "Bảo trì", kiểm tra có lịch trong tương lai không
            if (room.Status == "Maintain")
            {
                var now = DateTime.Now;
                bool hasFutureSchedule = roomInDb.Schedules.Any(s =>
                    s.Day.ToDateTime(s.Slot.EndTime) >= now);


                if (hasFutureSchedule)
                {
                    return await ReturnRoomDetailWithError(room, "Không thể chuyển phòng sang trạng thái 'Bảo trì' vì phòng vẫn còn lịch sử dụng trong tương lai.", weekStart);
                }
            }

            // Cập nhật thông tin
            //roomInDb.RoomName = room.RoomName.Trim();
            roomInDb.RoomType = room.RoomType.Trim();
            roomInDb.Status = room.Status.Trim();

            await _context.SaveChangesAsync();

            TempData["success"] = "Cập nhật phòng thành công!";
            return RedirectToAction("RoomDetail", new { id = room.RoomId, weekStart });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ConfirmRoomChange(string selectedSchedules, int newRoomId)
        {
            var ids = selectedSchedules.Split(',').Select(int.Parse).ToList();
            var doctors = await _doctorRepo.GetDoctorsBySchedule(ids);
            if (doctors != null)
            {
                foreach (var doctor in doctors)
                {
                    try
                    {
                        var emailBody = $@"
                        <h3>Cập nhật lịch làm việc</h3>
                        <p><strong>Kiểm tra chi tiết tại trang lịch làm việc</strong></p>
                        ";

                        await _emailService.SendEmailAsync(
                            toEmail: doctor.Email,
                            subject: "Thông báo thay đổi phòng làm việc",
                            body: emailBody
                        );
                    }
                    catch (Exception ex)
                    {
                        TempData["error"] = $"Failed to send email";
                        return View(selectedSchedules, newRoomId);
                    }
                }
            }
            await _scheduleRepo.ChangeRoomForSchedulesAsync(ids, newRoomId);
            TempData["success"] = "Đổi phòng thành công.";

            return RedirectToAction("ManageRoom");
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAvailableRooms([FromBody] List<int> selectedScheduleIds)
        {
            if (selectedScheduleIds == null || selectedScheduleIds.Count == 0)
                return BadRequest("Không có lịch nào được chọn.");

            var result = await _roomRepo.GetAvailableRoomsForSchedulesAsync(selectedScheduleIds);
            ViewBag.SelectedSchedules = selectedScheduleIds;

            return PartialView("_AvailableRoomsPartial", result);
        }





        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult AddDoctorAccount()
        {
            ViewBag.Units = GetAllDepartmentName();
            return View();
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult AddPatientAccount()
        {
            return View();
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult AddStaffAccount()
        {
            return View();
        }
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult AddRoom()
        {
            ViewBag.Units = GetAllRoomTypes();
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult AddRoom(Room room)
        {
            ViewBag.Units = GetAllRoomTypes();
            // Kiểm tra thủ công định dạng RoomName bằng controller
            if (string.IsNullOrWhiteSpace(room.RoomName) ||
                string.IsNullOrWhiteSpace(room.RoomType))
            {
                TempData["error"] = "Vui lòng không để trống các trường bắt buộc.";
                return View(room);
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(room.RoomName, @"^[A-Z][0-9]{3,4}$"))
            {
                TempData["error"] = "Tên phòng phải có dạng A101 hoặc A1001.";
                return View(room);
            }

            if (string.IsNullOrWhiteSpace(room.RoomType))
            {
                TempData["error"] = "Loại phòng không được để trống.";
                return View(room);
            }

            // Kiểm tra trùng tên phòng 
            var existingRoom = _context.Rooms.FirstOrDefault(r => r.RoomName == room.RoomName);
            if (existingRoom != null)
            {
                TempData["error"] = "Tên phòng này đã tồn tại.";
                return View(room);
            }

            // Lưu phòng mới
            room.Status = "Active"; // Hoặc null nếu bạn không cần trạng thái
            _context.Rooms.Add(room);
            _context.SaveChanges();

            // Thông báo thành công
            TempData["success"] = "Phòng đã được thêm thành công.";
            return RedirectToAction("ManageRoom");
        }


        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddDoctorAccount(ViewModels.Register model)
        {
            ViewBag.Units = GetAllDepartmentName();
            // check if mail is used
            var existingAccount = await _context.Doctors.FirstOrDefaultAsync(a => a.Email == model.Email);
            if (existingAccount != null)
            {

                TempData["error"] = "Email đã được sử dụng rồi!";
                return View(model);
            }

            //check phone valid

            // check if phone start with 0 and 9 digits back
            if (model.PhoneNumber == null)
            {
                TempData["error"] = "Số điện thoại không hợp lệ";
                return View(model);
            }

            if (model.PhoneNumber[0] != '0' || model.PhoneNumber.Length != 10)
            {
                TempData["error"] = "Số điện thoại không hợp lệ";
                return View(model);
            }

            // check if phone is non-number
            foreach (char u in model.PhoneNumber) if (u < '0' || u > '9')
                {
                    TempData["error"] = "Số điện thoại không hợp lệ";
                    return View(model);
                }

            // check phone is used
            var phoneOwner = _context.Doctors.FirstOrDefault(u => u.PhoneNumber == model.PhoneNumber);

            if (phoneOwner != null)
            {
                TempData["error"] = "Số điện thoại đã được sử dụng rồi";
                return View(model);
            }

            string password = UserController.RandomString(10);

            var doctor = new Models.Doctor
            {
                Email = model.Email,
                PasswordHash = _passwordHasher.HashPassword(null, password),
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber,
                Gender = model.Gender,
                IsActive = true,
                DepartmentName = model.DepartmentName,
                IsDepartmentHead = false,
                ExperienceYear = model.ExperienceYear ?? 0,
                Degree = model.Degree,
                IsSpecial = false,
                ProfileImage = model.ProfileImage
            };

            // handle case add email is used exception in sqlserver
            try
            {
                _context.Doctors.Add(doctor);
                await _context.SaveChangesAsync();
                // send account information email
                try
                {
                    var emailBody = $@"
                        <h3>✅ Chào mừng! Thông tin tài khoản bác sĩ của bạn</h3>
                        <p><strong>Email:</strong> {model.Email}</p>
                        <p><strong>Mật khẩu:</strong> {password}</p>
                        ";

                    await _emailService.SendEmailAsync(
                        toEmail: model.Email,
                        subject: "✅ Thông tin tài khoản mới của bạn",
                        body: emailBody
                    );
                }
                catch (Exception ex)
                {
                    TempData["error"] = $"Không gửi được email";
                    return View(model);
                }

                TempData["success"] = "Thêm tài khoản bác sĩ thành công";
                return RedirectToAction("ManageAccount", new { type = "Doctor" });
            }
            catch (DbUpdateException ex)
            {
                if (ex.InnerException != null && ex.InnerException.Message.Contains("Cannot insert duplicate key row"))
                {
                    TempData["error"] = "Email đã được sử dụng rồi!";
                    return View(model);
                }

                TempData["error"] = "Lỗi xảy ra khi lưu thông tin bác sĩ";
                return View(model);
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddPatientAccount(ViewModels.Register model)
        {
            // check if mail is used
            var existingAccount = await _context.Patients.FirstOrDefaultAsync(a => a.Email == model.Email);
            if (existingAccount != null)
            {

                TempData["error"] = "Email đã được sử dụng rồi!";
                return View(model);
            }

            //check phone valid

            // check if phone start with 0 and 9 digits back
            if (model.PhoneNumber == null)
            {
                TempData["error"] = "Số điện thoại không hợp lệ";
                return View(model);
            }

            if (model.PhoneNumber[0] != '0' || model.PhoneNumber.Length != 10)
            {
                TempData["error"] = "Số điện thoại không hợp lệ";
                return View(model);
            }

            // check if phone is non-number
            foreach (char u in model.PhoneNumber) if (u < '0' || u > '9')
                {
                    TempData["error"] = "Số điện thoại không hợp lệ";
                    return View(model);
                }

            // check phone is used(not this user)
            var phoneOwner = _context.Patients.FirstOrDefault(u => u.PhoneNumber == model.PhoneNumber);

            if (phoneOwner != null)
            {
                TempData["error"] = "Số điện thoại đã được sử dụng rồi";
                return View(model);
            }
            string password = UserController.RandomString(10);
            var patient = new Models.Patient
            {
                Email = model.Email,
                PasswordHash = _passwordHasher.HashPassword(null, password),
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber,
                Gender = model.Gender,
                IsActive = true,
                ProfileImage = model.ProfileImage
            };


            // handle case add email is used exception in sqlserver
            try
            {
                _context.Patients.Add(patient);
                await _context.SaveChangesAsync();
                // send account information email
                try
                {
                    var emailBody = $@"
                    <h3>✅ Chào mừng! Thông tin tài khoản nhân viên của bạn</h3>
                    <p><strong>Email:</strong> {model.Email}</p>
                    <p><strong>Mật khẩu:</strong> {password}</p>
                    ";

                    await _emailService.SendEmailAsync(
                        toEmail: model.Email,
                        subject: "✅ Thông tin tài khoản mới của bạn",
                        body: emailBody
                    );
                }
                catch (Exception ex)
                {
                    TempData["error"] = $"Failed to send email";
                    return View(model);
                }


                TempData["success"] = "Patient added successfully!";
                return RedirectToAction("ManageAccount", new { type = "Patient" });
            }
            catch (DbUpdateException ex)
            {
                if (ex.InnerException != null && ex.InnerException.Message.Contains("Cannot insert duplicate key row"))
                {
                    TempData["error"] = "Email is already registered.";
                    return View(model);
                }

                TempData["error"] = "An unexpected error occurred while saving the patient account.";
                return View(model);
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddStaffAccount(ViewModels.Register model)
        {
            // check if mail is used
            var existingAccount = await _context.Staff.FirstOrDefaultAsync(a => a.Email == model.Email);
            if (existingAccount != null)
            {

                TempData["error"] = "Email is already registered.";
                return View(model);
            }

            //check phone valid

            // check if phone start with 0 and 9 digits back
            if (model.PhoneNumber == null)
            {
                TempData["error"] = "Phone number is invalid.";
                return View(model);
            }

            if (model.PhoneNumber[0] != '0' || model.PhoneNumber.Length != 10)
            {
                TempData["error"] = "Phone number is invalid.";
                return View(model);
            }

            // check if phone is non-number
            foreach (char u in model.PhoneNumber) if (u < '0' || u > '9')
                {
                    TempData["error"] = "Phone number is invalid.";
                    return View(model);
                }

            // check phone is used(not this user)
            var phoneOwner = _context.Staff.FirstOrDefault(u => u.PhoneNumber == model.PhoneNumber);

            if (phoneOwner != null)
            {
                TempData["error"] = "This phone number was used before.";
                return View(model);
            }
            string password = UserController.RandomString(10);
            var staff = new Models.Staff
            {
                Email = model.Email,
                PasswordHash = _passwordHasher.HashPassword(null, password),
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber,
                Gender = model.Gender,
                RoleName = model.RoleName,
                IsActive = true,
                ProfileImage = model.ProfileImage
            };


            // handle case add email is used exception in sqlserver
            try
            {
                _context.Staff.Add(staff);
                await _context.SaveChangesAsync();
                // send account information email
                try
                {
                    var emailBody = $@"
                    <h3>✅ Chào mừng! Your New Employee Account Details</h3>
                    <p><strong>Email:</strong> {model.Email}</p>
                    <p><strong>Password:</strong> {password}</p>
                    ";

                    await _emailService.SendEmailAsync(
                        toEmail: model.Email,
                        subject: "✅ Thông tin tài khoản mới của bạn",
                        body: emailBody
                    );
                }
                catch (Exception ex)
                {
                    TempData["error"] = $"Failed to send email";
                    return View(model);
                }

                TempData["success"] = "Staff added successfully!";
                return RedirectToAction("ManageAccount", new { type = "Staff" });
            }
            catch (DbUpdateException ex)
            {
                if (ex.InnerException != null && ex.InnerException.Message.Contains("Cannot insert duplicate key row"))
                {
                    TempData["error"] = "Email is already registered.";
                    return View(model);
                }

                TempData["error"] = "An unexpected error occurred while saving the staff account.";
                return View(model);
            }

        }
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PatientDetail(int id)
        {
            var patient = await _patientRepo.GetByIdAsync(id);
            if (patient == null)
            {
                return RedirectToAction("NotFound", "Home");
            }
            return View(patient);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DoctorDetail(int id)
        {
            var doctor = await _doctorRepo.GetByIdAsync(id);
            if (doctor == null)
            {
                return RedirectToAction("NotFound", "Home");
            }
            return View(doctor);
        }
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> StaffDetail(int id)
        {
            var staff = await _staffRepo.GetByIdAsync(id);
            if (staff == null)
            {
                return RedirectToAction("NotFound", "Home");
            }
            return View(staff);
        }

        [HttpGet]
        [Authorize(Roles = "Doctor,TestDoctor")]
        public async Task<IActionResult> OngoingPatientScreen()
        {
            // 1. Lấy DoctorId từ claims
            int doctorId = int.Parse(User.FindFirst("DoctorID")?.Value ?? "0");

            // 2. Lấy thông tin bác sĩ
            var doctor = await _doctorRepo.GetByIdAsync(doctorId);
            ViewBag.DoctorName = doctor.FullName;
            return View();
        }

        [HttpGet]
        [Authorize(Roles = "Doctor,TestDoctor")]
        public async Task<IActionResult> GetTodayPatients()
        {
            // 1. Lấy DoctorId từ claims
            int doctorId = int.Parse(User.FindFirst("DoctorID")?.Value ?? "0");

            // 2. Lấy thông tin bác sĩ
            var doctor = await _doctorRepo.GetByIdAsync(doctorId); // nên Include Department
            if (doctor == null)
                return RedirectToAction("NotFound", "Home");

            List<Patient> patients;

            if (doctor.DepartmentName == "Xét nghiệm" || doctor.DepartmentName == "Chẩn đoán hình ảnh")
            {
                // 3. Nếu là bác sĩ xét nghiệm, lấy phòng hiện tại
                var roomId = await _scheduleRepo.GetCurrentWorkingRoomId(doctorId);
                if (roomId == null)
                    return Json(new List<object>()); // không có lịch trực hiện tại

                patients = await _patientRepo.GetOngoingLabPatientsByRoom(roomId.Value);
            }
            else
            {
                // 4. Bác sĩ khám thường
                patients = await _patientRepo.GetOngoingPatients(doctorId);
            }

            // 5. Trả JSON danh sách bệnh nhân đơn giản
            var result = patients.Select(p => new
            {
                id = p.PatientId,
                patientName = p.FullName
            }).ToList();

            return Json(result);
        }
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ScheduleRequestList(int? page, string type = "Pending")
        {

            int pageSize = 10;
            int pageNumber = page ?? 1;
            string viewType = type?.ToLower() == "completed" ? "Completed" : "Pending";

            var vm = new ScheduleChangeRequestListViewModel();
            vm.ViewType = viewType;

            // Lấy danh sách request theo trạng thái (Pending hoặc Completed)
            List<ScheduleRequestViewModel> requests = await _scheduleChangeRepo.SearchAsync(viewType, pageNumber, pageSize);
            int total = await _scheduleChangeRepo.CountAsync(viewType);

            vm.Requests = new StaticPagedList<ScheduleRequestViewModel>(requests, pageNumber, pageSize, total);

            return View("ScheduleRequestList", vm);
        }
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> HandleRequest(int requestId, string decision)
        {

            var request = await _context.ScheduleChangeRequests
                .Include(r => r.FromSchedule)
                .ThenInclude(s => s.Doctor)
                .FirstOrDefaultAsync(r => r.RequestId == requestId);

            if (request == null)
            {
                TempData["error"] = "Không tìm thấy yêu cầu.";
                return RedirectToAction("ScheduleRequestList");
            }

            if (request.Status != "Pending")
            {
                TempData["error"] = "Yêu cầu này đã được xử lý.";
                return RedirectToAction("ScheduleRequestList");
            }

            if (decision == "reject")
            {
                request.Status = "Rejected";
                await _context.SaveChangesAsync();
                try
                {
                    var doctor = await _context.Doctors
                        .FirstOrDefaultAsync(d => d.DoctorId == request.DoctorId);

                    if (doctor != null)
                    {
                        var emailBody = $@"
                        <h3>Thông báo về yêu cầu đổi lịch</h3>
                        <p>Xin chào bác sĩ <strong>{doctor.FullName}</strong>,</p>
                        <p>Yêu cầu đổi lịch của bạn từ <strong>Slot {request.FromSchedule.SlotId} - ngày {request.FromSchedule.Day}</strong> 
                        đến <strong>Slot {request.ToSlotId} - ngày {request.ToDay}</strong>  đã bị <strong>từ chối</strong>.</p>
                        <p>Nếu có thắc mắc, vui lòng liên hệ quản trị viên.</p>
                        ";

                        await _emailService.SendEmailAsync(
                            toEmail: doctor.Email,
                            subject: "Yêu cầu đổi lịch bị từ chối",
                            body: emailBody
                        );
                    }
                }
                catch (Exception ex)
                {
                    TempData["error"] = $"Failed to send email";
                    return RedirectToAction("ScheduleRequestList");
                }

                TempData["success"] = "Đã từ chối yêu cầu.";
                return RedirectToAction("ScheduleRequestList");
            }
            else if (decision == "accept")
            {
                // Chuyển hướng sang trang chọn phòng và bác sĩ thay thế
                return RedirectToAction("SelectReplacementInfo", new { requestId = requestId });
            }

            TempData["error"] = "Yêu cầu không hợp lệ.";
            return RedirectToAction("ScheduleRequestList");
        }
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SelectReplacementInfo(int requestId)
        {
            var request = await _context.ScheduleChangeRequests
                    .Include(r => r.FromSchedule)
                        .ThenInclude(s => s.Doctor)
                    .FirstOrDefaultAsync(r => r.RequestId == requestId);


            if (request == null)
            {
                TempData["error"] = "Không tìm thấy yêu cầu.";
                return RedirectToAction("ScheduleRequestList");
            }

            // Kiểm tra xem lịch có appointment không
            bool hasAppointment = await _appointmentRepo.HasAppointmentAsync(
                                            request.FromSchedule.DoctorId,
                                            request.FromSchedule.SlotId,
                                            request.FromSchedule.Day
                                        );


            // Truyền danh sách phòng
            var rooms = await _roomRepo.GetAvailableRoomsAsync(request.ToSlotId, request.ToDay);
            ViewBag.Rooms = rooms;

            // Nếu có appointment thì truyền danh sách bác sĩ cùng khoa (trừ người cũ)
            if (hasAppointment)
            {
                var doctors = await _doctorRepo.GetAvailableDoctorsAsync(
                                    request.FromSchedule.Doctor.DepartmentName,
                                    request.FromSchedule.SlotId,
                                    request.FromSchedule.Day,
                                    request.FromSchedule.DoctorId
                                    );

                ViewBag.Doctors = doctors;
            }

            var vm = new ReplacementInfoViewModel
            {
                RequestId = request.RequestId,
                HasAppointment = hasAppointment
            };

            return View(vm);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SelectReplacementInfo(ReplacementInfoViewModel model)
        {
            // Nếu lịch cũ có appointment thì bắt buộc phải chọn bác sĩ thay thế
            if ((model.HasAppointment == true && model.ReplacementDoctorId == -1) || model.RoomId == -1)
            {
                TempData["error"] = "Vui lòng điền đủ thông tin";
                return RedirectToAction("SelectReplacementInfo", new { requestId = model.RequestId });
            }

            // Kiểm tra lịch đổi có hợp lệ không
            var request = await _context.ScheduleChangeRequests
                .Include(r => r.FromSchedule)
                    .ThenInclude(s => s.Doctor)
                .FirstOrDefaultAsync(r => r.RequestId == model.RequestId);

            if (request == null || request.Status != "Pending")
            {
                TempData["error"] = "Yêu cầu đổi lịch không hợp lệ.";
                return RedirectToAction("ScheduleRequestList");
            }

            var fromSchedule = request.FromSchedule;

            Doctor? replacementDoctor = null;

            // Nếu có bác sĩ thay thế được chọn
            if (model.ReplacementDoctorId.HasValue)
            {
                replacementDoctor = await _context.Doctors.FirstOrDefaultAsync(d => d.DoctorId == model.ReplacementDoctorId.Value);

                // Cập nhật Appointment liên quan
                var appointments = await _context.Appointments
                    .Include(a => a.Patient)
                    .Where(a =>
                        a.DoctorId == fromSchedule.Doctor.DoctorId &&
                        a.SlotId == fromSchedule.SlotId &&
                        a.Date == fromSchedule.Day &&
                        (a.Status == "Pending" || a.Status == "Confirmed"))
                    .ToListAsync();

                foreach (var appt in appointments)
                {
                    appt.DoctorId = replacementDoctor.DoctorId;
                    await _context.SaveChangesAsync();

                    // Gửi email cho bệnh nhân
                    if (appt.Patient != null)
                    {
                        var patientBody = $@"
                        <h4>Lịch khám đã được cập nhật</h4>
                        <p><strong>Ngày:</strong> {appt.Date:dd/MM/yyyy}</p>
                        <p><strong>Giờ:</strong> Slot {appt.SlotId}</p>
                        <p><strong>Bác sĩ mới:</strong> {replacementDoctor.FullName}</p>
                        <p>Vui lòng xem chi tiết trong hệ thống.</p>";

                        await _emailService.SendEmailAsync(
                            toEmail: appt.Patient.Email,
                            subject: "📅 Cập nhật lịch khám",
                            body: patientBody
                        );
                    }
                }

                // Gửi email cho bác sĩ thay thế
                if (replacementDoctor != null)
                {
                    var replaceBody = $@"
                <h4>Bạn được chỉ định thay thế cho lịch khám</h4>
                <p><strong>Ngày:</strong> {fromSchedule.Day:dd/MM/yyyy}</p>
                <p><strong>Giờ:</strong> Slot {fromSchedule.SlotId}</p>
                <p>Vui lòng kiểm tra trong hệ thống.</p>";

                    await _emailService.SendEmailAsync(
                        toEmail: replacementDoctor.Email,
                        subject: "🔄 Lịch trực thay thế",
                        body: replaceBody
                    );
                }
            }

            // Cập nhật lịch cũ với thông tin mới
            fromSchedule.RoomId = model.RoomId;
            fromSchedule.SlotId = request.ToSlotId;
            fromSchedule.Day = request.ToDay;
            var newRoom = await _context.Rooms.FindAsync(model.RoomId);

            // Đánh dấu yêu cầu đã xử lý
            request.Status = "Accepted";

            // Gửi email cho bác sĩ yêu cầu
            var requestingDoctor = fromSchedule.Doctor;
            if (requestingDoctor != null)
            {
                var confirmBody = $@"
                <h4>Yêu cầu đổi lịch đã được chấp thuận</h4>
                <p><strong>Lịch mới:</strong> Slot {fromSchedule.SlotId} - {fromSchedule.Day:dd/MM/yyyy}</p>
                <p><strong>Phòng mới:</strong> {newRoom.RoomName}</p>
                <p>Vui lòng kiểm tra trong hệ thống.</p>";

                await _emailService.SendEmailAsync(
                    toEmail: requestingDoctor.Email,
                    subject: "✅ Đổi lịch thành công",
                    body: confirmBody
                );
            }

            await _context.SaveChangesAsync();

            TempData["success"] = "Xử lý yêu cầu đổi lịch thành công.";
            return RedirectToAction("ScheduleRequestList");
        }



        public static string NormalizeName(string? input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            input = input.Trim();
            var words = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return string.Join(" ", words);
        }
        public static string RandomString(int length)
        {
            const string allChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            const string uppercaseChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string digitChars = "0123456789";

            var random = new Random();

            // Bắt buộc có ít nhất 1 chữ hoa và 1 số
            char upper = uppercaseChars[random.Next(uppercaseChars.Length)];
            char digit = digitChars[random.Next(digitChars.Length)];

            // Sinh các ký tự còn lại
            var remainingChars = Enumerable.Range(0, length - 2)
                .Select(_ => allChars[random.Next(allChars.Length)])
                .ToList();

            // Thêm 2 ký tự bắt buộc vào danh sách
            remainingChars.Add(upper);
            remainingChars.Add(digit);

            // Trộn chuỗi để các ký tự không cố định vị trí
            return new string(remainingChars.OrderBy(_ => random.Next()).ToArray());
        }


        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ViewSchedule(int id, int? year, string? weekStart)
        {
            var doctor = await _doctorRepo.GetByIdAsync(id);
            if (doctor == null) return NotFound();

            ViewBag.Doctor = doctor;

            // Nếu không truyền gì, dùng tuần hiện tại
            int selectedYear = year ?? DateTime.Today.Year;

            DateOnly selectedWeekStart;
            if (!string.IsNullOrEmpty(weekStart) &&
                DateOnly.TryParseExact(weekStart, "yyyy-MM-dd", out var parsed))
            {
                selectedWeekStart = parsed;
            }
            else
            {
                selectedWeekStart = GetStartOfWeek(DateOnly.FromDateTime(DateTime.Today));
            }

            DateOnly selectedWeekEnd = selectedWeekStart.AddDays(6);

            var schedules = _context.Schedules
                .Where(s => s.DoctorId == doctor.DoctorId && s.Day >= selectedWeekStart && s.Day <= selectedWeekEnd)
                .Select(s => new DoctorScheduleViewModel.ScheduleItem
                {
                    ScheduleId = s.ScheduleId,
                    Day = s.Day,
                    SlotId = s.SlotId,
                    StartTime = s.Slot.StartTime.ToString(@"hh\:mm"),
                    EndTime = s.Slot.EndTime.ToString(@"hh\:mm"),
                    RoomName = s.Room.RoomName,
                    RoomId = s.Room.RoomId,
                    DoctorId = doctor.DoctorId,
                    DoctorName = doctor.FullName,
                    Status = s.Status
                })
                .ToList();

            ViewBag.SelectedYear = selectedYear;
            ViewBag.SelectedWeekStart = selectedWeekStart;
            var slots = _context.Slots.ToList();
            ViewBag.SlotsPerDay = slots.Count();
            // TempData["success"] = $"Size: {schedules.Capacity}";
            //return Redirect("Home/NotFound");
            return View(schedules);
        }

        public List<SelectListItem> GetAllDepartmentName()
        {

            return new List<SelectListItem>
            {
                new ("Nội tim mạch", "Nội tim mạch"),
                new ("Dị ứng", "Dị ứng"),
                new ("Truyền nhiễm", "Truyền nhiễm"),
                new ("Thần kinh", "Thần kinh"),
                new ("Phụ sản", "Phụ sản"),
                new ("Nhi", "Nhi"),
                new ("Xét nghiệm", "Xét nghiệm"),
                new ("Chẩn đoán hình ảnh", "Chẩn đoán hình ảnh"),
                new ("Nha khoa", "Nha khoa"),
                new ("Ngoại tiêu hóa", "Ngoại tiêu hóa"),
                new ("Mắt", "Mắt"),
                new ("Y học hạt nhân", "Y học hạt nhân"),
                new ("Y học cổ truyền", "Y học cổ truyền"),
                new ("Tâm thần", "Tâm thần"),
                new ("Vật lý trị liệu", "Vật lý trị liệu"),
            };
        }
        public List<SelectListItem> GetAllRoomTypes()
        {

            return new List<SelectListItem>
            {
                new ("Phòng khám", "Phòng khám"),
                new ("Phòng xét nghiệm máu", "Phòng xét nghiệm máu"),
                new ("Phòng nội soi", "Phòng nội soi"),
                new ("Phòng chẩn đoán hình ảnh", "Phòng chẩn đoán hình ảnh"),
                new ("Phòng siêu âm", "Phòng siêu âm"),
                new ("Khác", "Khác"),
            };
        }

        private DateOnly GetCurrentWeekStart()
        {
            var today = DateTime.Today;
            int offset = (int)today.DayOfWeek == 0 ? 6 : (int)today.DayOfWeek - 1;
            var monday = today.AddDays(-offset);
            return DateOnly.FromDateTime(monday);
        }

        private int GetYearOfWeek(DateOnly weekStart)
        {
            for (int i = 0; i < 7; i++)
            {
                var day = weekStart.AddDays(i);
                if (day.Day == 1 && day.Month == 1)
                    return day.Year;
            }

            // Fallback: lấy ngày giữa tuần
            return weekStart.AddDays(3).Year;
        }

        private async Task<IActionResult> ReturnRoomDetailWithError(RoomDetailViewModel room, string error, DateOnly selectedWeekStart)
        {
            room.Schedule = await _scheduleRepo.GetScheduleByRoomAndWeekAsync(room.RoomId, selectedWeekStart);
            ViewBag.Slots = await _slotRepo.GetAllSlotsAsync();
            ViewBag.Units = GetAllRoomTypes();
            ViewBag.SelectedYear = GetYearOfWeek(selectedWeekStart);
            ViewBag.SelectedWeekStart = selectedWeekStart;
            TempData["error"] = error;

            // Lấy lại đúng thông tin phòng từ DB
            var roomInDb = await _context.Rooms.FindAsync(room.RoomId);
            if (roomInDb != null)
            {
                room.RoomName = roomInDb.RoomName;
                room.RoomType = roomInDb.RoomType;
                room.Status = roomInDb.Status;
            }
            ModelState.Clear();
            return RedirectToAction("RoomDetail", new { id = room.RoomId, weekStart = selectedWeekStart });
        }

        private DateOnly GetStartOfWeek(DateOnly date)
        {
            int diff = ((int)date.DayOfWeek + 6) % 7; // Monday = 0
            return date.AddDays(-diff);
        }
    }
}