using HospitalManagement.Data;
using HospitalManagement.Models;
using HospitalManagement.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;
using Newtonsoft.Json;
using System.Security.Claims;
using HospitalManagement.Repositories;
using HospitalManagement.Filters;

namespace HospitalManagement.Controllers
{
    [Authorize(Roles = "Doctor, TestDoctor")]
    public class DoctorController : Controller
    {
        private readonly PasswordHasher<Doctor> _passwordHasher;
        private readonly HospitalManagementContext _context;
        private readonly ISlotRepository _slotRepo;
        private readonly IScheduleRepository _scheduleRepo;

        public DoctorController(HospitalManagementContext context, ISlotRepository slotRepo, IScheduleRepository scheduleRepo)
        {
            _passwordHasher = new PasswordHasher<Doctor>();
            _context = context;
            _slotRepo = slotRepo;
            _scheduleRepo = scheduleRepo;
        }

        [HttpGet]
        public IActionResult ViewProfile()
        {
            // Load profile data from sesion
            // Lấy DoctorID từ Claims
            var doctorIdClaim = User.FindFirst("DoctorID")?.Value;
            if (doctorIdClaim == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            int doctorID = int.Parse(doctorIdClaim);

            // Lấy thông tin từ DB
            var context = new HospitalManagementContext();
            var user = context.Doctors.FirstOrDefault(p => p.DoctorId == doctorID);
            if (user == null)
            {
                return RedirectToAction("Login", "Auth");
            }
            return View(user);
        }
        [HttpGet]
        public IActionResult UpdateProfile()
        {


            var doctorIdClaim = User.FindFirst("DoctorID")?.Value;
            if (doctorIdClaim == null)

            {
                return RedirectToAction("Login", "Auth");
            }

            int doctorID = int.Parse(doctorIdClaim);

            // Lấy thông tin từ DB
            var context = new HospitalManagementContext();
            var user = context.Doctors.FirstOrDefault(p => p.DoctorId == doctorID);
            if (user == null)
            {
                return RedirectToAction("Login", "Auth");
            }
            return View(user);
        }
        public async Task<IActionResult> RequestChangeSchedule(string? weekStart)
        {
            ViewBag.Slots = await _slotRepo.GetAllSlotsAsync();
            var user = HttpContext.User;
            string email = user.FindFirstValue(ClaimTypes.Email);
            if (email == null) Unauthorized();

            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Email == email);
            if (doctor == null) return NotFound();

            DateOnly selectedWeekStart;
            if (!string.IsNullOrEmpty(weekStart) &&
                DateOnly.TryParseExact(weekStart, "yyyy-MM-dd", out var parsed))
            {
                selectedWeekStart = parsed;
            }
            else
            {
                selectedWeekStart = GetCurrentWeekStart();
            }

            // Nếu không truyền gì, dùng tuần hiện tại
            int selectedYear = GetYearOfWeek(selectedWeekStart);

            

            DateOnly selectedWeekEnd = selectedWeekStart.AddDays(6);

            var schedules = _context.Schedules
                .Where(s => s.DoctorId == doctor.DoctorId && s.Day >= selectedWeekStart && s.Day <= selectedWeekEnd)
                .Select(s => new ScheduleViewModel
                {
                    ScheduleId = s.ScheduleId,
                    Day = s.Day,
                    SlotIndex = s.SlotId,
                    StartTime = s.Slot.StartTime.ToString(@"hh\:mm"),
                    EndTime = s.Slot.EndTime.ToString(@"hh\:mm"),
                    RoomName = s.Room.RoomName
                })
                .ToList();

            ViewBag.SelectedYear = selectedYear;
            ViewBag.SelectedWeekStart = selectedWeekStart;
            return View(schedules);
        }

        [HttpPost]
        [AllowSpam]
        public async Task<IActionResult> GetAvailableSlotsPopup([FromBody] ViewModels.ScheduleChangeRequest model, string? weekStart)
        {
            int fromScheduleId = model.FromScheduleId;
            DateOnly selectedWeekStart = GetParsedOrCurrentWeekStart(weekStart);

            var fromSchedule = await _context.Schedules
                .Include(s => s.Doctor)
                .FirstOrDefaultAsync(s => s.ScheduleId == fromScheduleId);

            if (fromSchedule == null)
                return NotFound("Không tìm thấy lịch làm việc.");

            int doctorId = fromSchedule.DoctorId;

            var allSlots = await _slotRepo.GetAllSlotsAsync();
            ViewBag.Slots = allSlots;
            ViewBag.WeekStart = selectedWeekStart;
            ViewBag.FirstWeekStart = GetCurrentWeekStart();
            ViewBag.LastWeekStart = GetCurrentWeekStart().AddDays(35);
            ViewBag.FromScheduleId = fromScheduleId;

            var lastDate = selectedWeekStart.AddDays(7 * 5 - 1);
            var doctorSchedules = await _scheduleRepo.GetDoctorSchedulesInRangeAsync(doctorId, selectedWeekStart, lastDate);
            return PartialView("_SchedulePopup", doctorSchedules);
        }

        [HttpPost]
        public async Task<IActionResult> SubmitScheduleChangeRequest(ScheduleChangeRequestViewModel model)
        {
            if (!ModelState.IsValid)
            {
                if (model.Reason.Length > 500)
                {
                    TempData["error"] = "Lý do phải ít hơn 500 ký tự!";
                    return RedirectToAction("RequestChangeSchedule");
                }
                TempData["error"] = "Vui lòng điền đầy đủ thông tin yêu cầu đổi lịch.";
                return RedirectToAction("RequestChangeSchedule");
            }
            var fromSchedule = await _context.Schedules
                .Include(s => s.Doctor)
                .FirstOrDefaultAsync(s => s.ScheduleId == model.FromScheduleId);

            if (fromSchedule == null)
            {
                TempData["error"] = "Vui lòng điền đầy đủ thông tin yêu cầu đổi lịch.";
                return RedirectToAction("RequestChangeSchedule");
            }

            int doctorId = fromSchedule.DoctorId;

            // Kiểm tra xem đã tồn tại request nào của bác sĩ này với cùng FromScheduleId chưa
            var existingRequest = await _context.ScheduleChangeRequests
                .FirstOrDefaultAsync(r => r.DoctorId == doctorId && r.FromScheduleId == model.FromScheduleId);

            if (existingRequest != null)
            {
                // Nếu ToSlot và ToDay giống nhau thì báo lỗi
                if (existingRequest.ToSlotId == model.SelectedSlot && existingRequest.ToDay == model.SelectedDay)
                {
                    TempData["error"] = "Yêu cầu đổi lịch này đã tồn tại.";
                    return RedirectToAction("RequestChangeSchedule");
                }

                // Nếu khác thì cập nhật lại
                existingRequest.ToSlotId = model.SelectedSlot;
                existingRequest.ToDay = model.SelectedDay;
                existingRequest.Reason = model.Reason;
                existingRequest.Status = "Pending";
                existingRequest.CreatedAt = DateTime.Now;

                _context.ScheduleChangeRequests.Update(existingRequest);
                await _context.SaveChangesAsync();

                TempData["success"] = "Yêu cầu đổi lịch đã được cập nhật.";
                return RedirectToAction("RequestChangeSchedule");
            }
            // Nếu chưa có request nào thì tạo mới
            var newRequest = new Models.ScheduleChangeRequest
            {
                DoctorId = doctorId,
                FromScheduleId = model.FromScheduleId,
                ToSlotId = model.SelectedSlot,
                ToDay = model.SelectedDay,
                ToRoomId = null, // để null nếu chưa chọn
                Reason = model.Reason,
                Status = "Pending",
                CreatedAt = DateTime.Now
            };

            _context.ScheduleChangeRequests.Add(newRequest);
            await _context.SaveChangesAsync();

            TempData["success"] = "Yêu cầu đổi lịch đã được gửi.";
            return RedirectToAction("RequestChangeSchedule");
        }


        [HttpGet]
        public IActionResult ChangePassword()
        {
            // Lấy DoctorID từ Claims
            var doctorIdClaim = User.FindFirst("DoctorID")?.Value;
            if (doctorIdClaim == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            int doctorID = int.Parse(doctorIdClaim);

            // Lấy thông tin từ DB
            var context = new HospitalManagementContext();
            var user = context.Doctors.FirstOrDefault(p => p.DoctorId == doctorID);
            if (user == null)
            {
                return RedirectToAction("Login", "Auth");
            }
            return View();
        }


        [HttpPost]
        public IActionResult ChangePassword(ChangePass model)
        {
            // Lấy DoctorID từ Claims
            var doctorIdClaim = User.FindFirst("DoctorID")?.Value;
            if (doctorIdClaim == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            int doctorID = int.Parse(doctorIdClaim);

            // Lấy thông tin từ DB
            var context = new HospitalManagementContext();
            var user = context.Doctors.FirstOrDefault(p => p.DoctorId == doctorID);
            if (user == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (_passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.OldPassword) != PasswordVerificationResult.Success)
            {
                TempData["error"] = "Mật khẩu hiện tại không đúng.";
                return View();
            }

            if (model.NewPassword != model.ConfirmPassword)
            {
                TempData["error"] = "Hai mật khẩu mới không khớp.";
                return View();
            }

            // Cập nhật trong DB

            var dbUser = _context.Doctors.FirstOrDefault(u => u.DoctorId == user.DoctorId);
            if (dbUser != null)
            {
                dbUser.PasswordHash = _passwordHasher.HashPassword(null, model.NewPassword);
                _context.SaveChanges();
                HttpContext.Session.SetString("DoctorSession", JsonConvert.SerializeObject(dbUser));

            }


            // Cập nhật lại session
            TempData["success"] = "Đổi mật khẩu thành công!";
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> UploadPhoto(IFormFile photo)
        {
            // check login
            // Lấy DoctorId từ Claims
            var doctorIdClaim = User.FindFirst("DoctorId")?.Value;
            if (doctorIdClaim == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            int doctorId = int.Parse(doctorIdClaim);

            // Lấy thông tin từ DB
            var context = new HospitalManagementContext();
            var user = context.Doctors.FirstOrDefault(p => p.DoctorId == doctorId);
            if (user == null)
            {
                return RedirectToAction("Login", "Auth");
            }


            if (photo != null && photo.Length > 0)
            {
                var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/jpg", "image/webp" };
                if (!allowedTypes.Contains(photo.ContentType))
                {
                    TempData["error"] = "Định dạng ảnh không được phép.";
                    return RedirectToAction("UpdateProfile");
                }
                if (photo.Length > 2 * 1024 * 1024)
                {
                    TempData["error"] = "Kích thước file quá 2MB.";
                    return RedirectToAction("UpdateProfile");

                }
                // Tạo tên file duy nhất để tránh trùng
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(photo.FileName);
                var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", fileName);

                // Lưu file vào wwwroot/uploads
                using (var stream = new FileStream(uploadPath, FileMode.Create))
                {
                    await photo.CopyToAsync(stream);
                }

                user.ProfileImage = fileName;

                // add in database
                var dbUser = context.Doctors.FirstOrDefault(u => u.DoctorId == user.DoctorId);
                if (dbUser != null)
                {
                    dbUser.ProfileImage = user.ProfileImage;
                    context.SaveChanges();
                }

                // Cập nhật lại session
                //HttpContext.Session.SetString("PatientSession", JsonConvert.SerializeObject(user));
                TempData["success"] = "Cập nhật thành công!";
                return RedirectToAction("UpdateProfile");
            }
            // do nothing
            TempData["success"] = "Không có ảnh được tải lên";
            return RedirectToAction("UpdateProfile");
        }

        [HttpPost]
        public IActionResult UpdateProfile(Doctor model)
        {
            var doctorIdClaim = User.FindFirst("DoctorID")?.Value;
            if (doctorIdClaim == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            int doctorID = int.Parse(doctorIdClaim);

            // Lấy thông tin từ DB
            var user = _context.Doctors.FirstOrDefault(p => p.DoctorId == doctorID);
            if (user == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var curUser = _context.Doctors.FirstOrDefault(u => u.DoctorId == user.DoctorId);
            if (curUser != null)
            {
                // check if phone start with 0 and 9 digits back
                if (model.PhoneNumber == null)
                {
                    TempData["error"] = "Số điện thoại không hợp lệ.";
                    return RedirectToAction("UpdateProfile");
                }

                if (model.PhoneNumber[0] != '0' || model.PhoneNumber.Length != 10)
                {
                    TempData["error"] = "Số điện thoại không hợp lệ.";
                    return RedirectToAction("UpdateProfile");
                }

                //check if phone is non - number
                foreach (char u in model.PhoneNumber) if (u < '0' || u > '9')
                    {
                        TempData["error"] = "Số điện thoại không hợp lệ.";
                        return RedirectToAction("UpdateProfile");
                    }

                // check phone is used(not this user)
                var phoneOwner = _context.Doctors.FirstOrDefault(u => u.PhoneNumber == model.PhoneNumber);

                if (phoneOwner != null && phoneOwner.DoctorId != curUser.DoctorId)
                {
                    TempData["error"] = "Số điện thoại này đã được sử dụng trước đó.";
                    return RedirectToAction("UpdateProfile");
                }


                // update info user
                curUser.FullName = model.FullName;
                curUser.Gender = model.Gender;
                curUser.PhoneNumber = model.PhoneNumber;

                // luu lai vao database
                _context.SaveChanges();

                //// Cập nhật lại session
                //user.FullName = curUser.FullName;
                //user.Gender = curUser.Gender;
                //user.PhoneNumber = curUser.PhoneNumber;
            }

            TempData["success"] = "Cập nhật thành công!";
            return RedirectToAction("UpdateProfile");
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            // Đăng xuất người dùng khỏi Identity (cookie authentication)
            await HttpContext.SignOutAsync();

            TempData["success"] = "Đăng xuất thành công!";
            return RedirectToAction("Index", "Home");
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
        private DateOnly GetParsedOrCurrentWeekStart(string? weekStart)
        {
            if (!string.IsNullOrEmpty(weekStart) &&
                DateOnly.TryParseExact(weekStart, "yyyy-MM-dd", out var parsed))
            {
                return parsed;
            }
            return GetCurrentWeekStart();
        }
    }
}
