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
        public UserController(HospitalManagementContext context, IDoctorRepository doctorRepo, IPatientRepository patientRepo, IStaffRepository staffRepo, IRoomRepository roomRepo, EmailService emailService)
        {
            _context = context;
            _doctorRepo = doctorRepo;
            _patientRepo = patientRepo;
            _staffRepo = staffRepo;
            _roomRepo = roomRepo;
            _passwordHasher = new PasswordHasher<Patient>();
            _emailService = emailService;
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

            List<Doctor> doctors = await _doctorRepo.SearchAsync(name, department, null, null, null, null, pageNumber, pageSize);
            var totalDoctors = await _doctorRepo.CountAsync(name, department, null, null, null);
            vm.Doctors = new StaticPagedList<Doctor>(doctors, pageNumber, pageSize, totalDoctors);

            List<Staff> staffs = await _staffRepo.SearchAsync(name, roleName, pageNumber, pageSize);
            var totalStaffs = await _staffRepo.CountAsync(name, roleName);
            vm.Staffs = new StaticPagedList<Staff>(staffs, pageNumber, pageSize, totalStaffs);

            vm.AccountType = type;
            // Truyền vào các Department
            var departments = await _doctorRepo.GetDistinctDepartment();
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
        public async Task<IActionResult> ManageRoom(int? page, string? name, string? building, string? floor, string? status)
        {
            name = UserController.NormalizeName(name);
            int pageSize = 10;
            int pageNumber = page ?? 1;

            List<RoomWithDoctorDtoViewModel> rooms = await _roomRepo.SearchAsync(name, building, floor, status, pageNumber, pageSize);
            var totalRooms = await _roomRepo.CountAsync(name, building, floor, status);
            var pagedRooms = new StaticPagedList<RoomWithDoctorDtoViewModel>(rooms, pageNumber, pageSize, totalRooms);
            var allBuildings = await _roomRepo.GetAllDistinctBuildings();
            var allFloors = await _roomRepo.GetAllDistinctFloors();

            ViewBag.Name = name;
            ViewBag.Building = building;
            ViewBag.Floor = floor;
            ViewBag.Status = status;
            ViewBag.AllBuildings = allBuildings;
            ViewBag.AllFloors = allFloors;

            return View(pagedRooms);
        }

        //[HttpGet]
        //[Authorize(Roles = "Admin")]
        //public IActionResult RoomDetail()
        //{
        //    return View();
        //}

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
            TempData["success"] = "Status updated successfully.";
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
                    TempData["error"] = $"Each department can have only one Department Head. Conflict in departments: {deptName}";
                    return RedirectToAction("ManageAccount", new { type = "Doctor" });
                }

                // Không có vi phạm => commit thay đổi
                transaction.Commit();

                TempData["success"] = "Status updated successfully.";
                return RedirectToAction("ManageAccount", new { type = "Doctor" });
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                TempData["error"] = "An error occurred while updating doctor status.";
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
            TempData["success"] = "Status updated successfully.";
            return RedirectToAction("ManageAccount", new { type = "Staff" });
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
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult AddRoom(Room room)
        {
            // Kiểm tra thủ công định dạng RoomName bằng controller
            if (string.IsNullOrWhiteSpace(room.RoomName))
            {
                TempData["error"] = "Tên phòng không được để trống.";
                return View(room);
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(room.RoomName, @"^[A-Z][0-9]{3,4}$"))
            {
                TempData["error"] = "Tên phòng phải có dạng A101 hoặc A1001.";
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
            room.Status = "Hoạt động"; // Hoặc null nếu bạn không cần trạng thái
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
            var phoneOwner = _context.Doctors.FirstOrDefault(u => u.PhoneNumber == model.PhoneNumber);

            if (phoneOwner != null)
            {
                TempData["error"] = "This phone number was used before.";
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
                        <h3>✅ Welcome! Your New Employee Account Details</h3>
                        <p><strong>Email:</strong> {model.Email}</p>
                        <p><strong>Password:</strong> {password}</p>
                        ";

                    await _emailService.SendEmailAsync(
                        toEmail: model.Email,
                        subject: "✅ Your New Account Information",
                        body: emailBody
                    );
                }
                catch (Exception ex)
                {
                    TempData["error"] = $"Failed to send email";
                    return View(model);
                }

                TempData["success"] = "Doctor added successfully!";
                return RedirectToAction("ManageAccount", new { type = "Doctor" });
            }
            catch (DbUpdateException ex)
            {
                if (ex.InnerException != null && ex.InnerException.Message.Contains("Cannot insert duplicate key row"))
                {
                    TempData["error"] = "Email is already registered.";
                    return View(model);
                }

                TempData["error"] = "An unexpected error occurred while saving the doctor account.";
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
            var phoneOwner = _context.Patients.FirstOrDefault(u => u.PhoneNumber == model.PhoneNumber);

            if (phoneOwner != null)
            {
                TempData["error"] = "This phone number was used before.";
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
                    <h3>✅ Welcome! Your New Account Details</h3>
                    <p><strong>Email:</strong> {model.Email}</p>
                    <p><strong>Password:</strong> {password}</p>
                    ";

                    await _emailService.SendEmailAsync(
                        toEmail: model.Email,
                        subject: "✅ Your New Account Information",
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
                    <h3>✅ Welcome! Your New Employee Account Details</h3>
                    <p><strong>Email:</strong> {model.Email}</p>
                    <p><strong>Password:</strong> {password}</p>
                    ";

                    await _emailService.SendEmailAsync(
                        toEmail: model.Email,
                        subject: "✅ Your New Account Information",
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

        //[HttpGet]
        //public async Task<IActionResult> Logout()
        //{
        //    // Đăng xuất người dùng khỏi Identity (cookie authentication)
        //    await HttpContext.SignOutAsync();

        //    TempData["success"] = "Logout successful!";
        //    return RedirectToAction("Index", "Home");
        //}
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
                new ("Ngoại tiêu hóa", "Ngoại tiêu hóa"),
                new ("Mắt", "Mắt"),
                new ("Y học hạt nhân", "Y học hạt nhân"),
                new ("Y học cổ truyền", "Y học cổ truyền"),
                new ("Tâm thần", "Tâm thần"),
                new ("Vật lý trị liệu", "Vật lý trị liệu"),
            };
        }
    }
}
