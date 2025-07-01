using System.Security.Claims;
using System.Text.Json;
using HospitalManagement.Data;
using HospitalManagement.Models;
using HospitalManagement.Repositories;
using HospitalManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using X.PagedList.Extensions;

namespace HospitalManagement.Controllers
{
    public class TrackingController : Controller
    {

        private readonly IAppointmentRepository _appointmentRepo;
        private readonly ITrackingRepository _TrackingRepository;
        private readonly IRoomRepository _RoomRepository;
        private readonly HospitalManagementContext _context;

        public TrackingController(IAppointmentRepository appointmentRepo, ITrackingRepository trackingRepository, IRoomRepository roomRepository, HospitalManagementContext context)
        {
            _appointmentRepo = appointmentRepo;
            _TrackingRepository = trackingRepository;
            _RoomRepository = roomRepository;
            _context = context;
        }
        [Authorize(Roles = "Receptionist")]
        //tìm cuộc hẹn theo sd0t
        public async Task<IActionResult> StartMedical(string phone)
        {
            ViewBag.Phone = phone;

            if (string.IsNullOrWhiteSpace(phone))
            {
                return View(null);
            }
            var list = await _TrackingRepository.GetAppointmentsAsync(phone);
            return View(list);
        }
        
        //Bắt đầu cuộc hẹn chuyển status của appoinment sang Ongoing
        [HttpGet]
        [Authorize(Roles = "Receptionist")]
        public async Task<IActionResult> StartAppointment(int id)
        {
            var tracking = await _TrackingRepository.GetAppointmentByIdAsync(id);

            var appointment = tracking.Appointment;
            if (appointment == null)
                return Json(new { success = false, message = "Không tìm thấy cuộc hẹn" });

            await _TrackingRepository.StartAppointmentAsync(id);

            return PartialView("RoomInfo", tracking);
        }

        //Hiện các cuộc hẹn cần khám ontime
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> DoctorTodayAppointment(int? page)
        {
            var doctorClaim = User.FindFirst("DoctorID");
            if (doctorClaim == null)
                return RedirectToAction("Login", "Auth");

            if (!int.TryParse(doctorClaim.Value, out int doctorId))
                return RedirectToAction("Login", "Auth");

            var appointmentList = await _TrackingRepository.GetOngoingAppointmentsByDoctorIdAsync(doctorId);
            int pageNumber = page ?? 1;
            int pageSize = 10;
            var pagedAppointments = appointmentList.ToPagedList(pageNumber, pageSize);

            return View(pagedAppointments);
        }
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> MedicalExam(int id)
        {
            // Lấy thông tin cuộc hẹn + bệnh nhân (nếu cần)
            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                .FirstOrDefaultAsync(a => a.AppointmentId == id);
            var allTestsCompleted = await _context.TestLists
                    .Where(t => t.AppointmentId == id)
                    .AllAsync(t => t.TestStatus == "Completed");

            if (appointment == null)
                return NotFound();

            // Lấy danh sách phòng đã chỉ định (Tracking + Room)
            var assignedRooms = await _context.Trackings
                                .Include(t => t.TestList)
                                    .ThenInclude(tl => tl.Test)
                                .Include(t => t.Room)
                                .Where(t => t.AppointmentId == id)
                                .ToListAsync();


            // Tạo ViewModel
            var viewModel = new ExaminationViewModel
            {
                AppointmentId = appointment.AppointmentId,
                PatientName = appointment.Patient.FullName,
                DateOfBirth = appointment.Patient.Dob,
                Gender = appointment.Patient.Gender,
                TestStatus = appointment.Status,
                AssignedRooms = assignedRooms,
                AvailableTests = _context.Tests
                                       .Select(t => new Test { TestId = t.TestId, Name = t.Name })
                                       .ToList(),
                Symptoms = appointment.Symptoms,
                Diagnosis = appointment.Diagnosis,
                PrescriptionNote = appointment.PrescriptionNote,
                ServiceId = appointment.ServiceId,
                PackageId = appointment.PackageId,
                ServiceName = appointment.Service?.ServiceType,
                PackageName = appointment.Package?.PackageName
            };
            ViewBag.AllTestsCompleted = allTestsCompleted;
            var trackingViewModel = viewModel.AssignedRooms
                .Select(t => new TrackingViewModel
                {
                    TestListId = t.TestListId ?? 0,
                    TestId = t.TestList.Test.TestId,
                    TestName = t.TestList?.Test?.Name,
                    TestStatus = t.TestList?.TestStatus,
                    RoomId = t.Room.RoomId,
                    RoomName = t.Room.RoomName,
                    RoomType = t.Room.RoomType
                }).ToList();
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            ViewBag.AssignedRoom = JsonSerializer.Serialize(trackingViewModel, options);
            return View(viewModel);
        }

        [HttpPost]
        [Authorize(Roles = "Doctor")]
        public IActionResult AssignRoom(int appointmentId, int roomId, int testId)
        {
            // 1. Kiểm tra test có tồn tại
            var test = _context.Tests.FirstOrDefault(t => t.TestId == testId);
            if (test == null)
            {
                return BadRequest(new { message = "Xét nghiệm không tồn tại" });
            }

            // 2. Kiểm tra xem đã có test này với phòng này chưa
            bool exists = _context.Trackings
                .Include(t => t.TestList)
                .Any(t =>
                    t.AppointmentId == appointmentId &&
                    t.RoomId == roomId &&
                    t.TestList != null &&
                    t.TestList.TestId == testId);

            if (exists)
            {
                return BadRequest(new { message = "Test này đã được chỉ định cho phòng này." });
            }

            // 3. Tìm hoặc tạo mới TestList
            var testList = _context.TestLists
                .FirstOrDefault(t => t.AppointmentId == appointmentId && t.TestId == testId);

            if (testList == null)
            {
                testList = new TestList
                {
                    AppointmentId = appointmentId,
                    TestId = testId,
                    CreatedAt = DateTime.Now,
                    TestStatus = "Ongoing"
                };
                _context.TestLists.Add(testList);
                _context.SaveChanges();
            }

            // 4. Tạo mới Tracking
            var tracking = new Tracking
            {
                AppointmentId = appointmentId,
                RoomId = roomId,
                Time = DateTime.Now,
                TestListId = testList.TestListId
            };
            _context.Trackings.Add(tracking);
            _context.SaveChanges();

            // 5. Lấy thông tin Room
            var room = _context.Rooms.FirstOrDefault(r => r.RoomId == roomId);
            if (room == null)
            {
                return BadRequest(new { message = "Phòng không tồn tại." });
            }

            // 6. Trả về DTO cho JS
            var dto = new
            {
                testListId = testList.TestListId,
                testId = testList.TestId,
                testName = test.Name,
                testStatus = testList.TestStatus,
                roomId = room.RoomId,
                roomName = room.RoomName,
                roomType = room.RoomType,
                status = room.Status
            };
            return Json(dto);
        }


        public List<TrackingViewModel> ConvertToDto(List<Tracking> trackings)
        {
            return trackings.Select(t => new TrackingViewModel
            {
                TestListId = t.TestListId ?? 0,
                TestId = t.TestList.Test.TestId,
                TestName = t.TestList?.Test?.Name,
                TestStatus = t.TestList?.TestStatus,
                RoomId = t.Room.RoomId,
                RoomName = t.Room.RoomName,
                RoomType = t.Room.RoomType,
                Status = t.Room.Status
            }).ToList();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> SaveExamination(ExaminationViewModel model)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                .FirstOrDefaultAsync(a => a.AppointmentId == model.AppointmentId);

            if (appointment == null)
            {
                TempData["error"] = "Cuộc hẹn không tồn tại.";
                return RedirectToAction("AppointmentList");
            }

            var actionType = Request.Form["action"];

            if (actionType.Equals("submit"))
            {
                // Kiểm tra tất cả các xét nghiệm đã được hoàn thành
                bool allTestsCompleted = await _context.TestLists
                    .Where(t => t.AppointmentId == model.AppointmentId)
                    .AllAsync(t => t.TestStatus == "Completed");

                if (!allTestsCompleted)
                {
                    TempData["error"] = "Vui lòng hoàn thành tất cả xét nghiệm trước khi kết thúc khám bệnh.";

                    // Gán lại ViewBag và model để quay lại đúng trang
                    var assignedRooms = await _context.Trackings
                                        .Include(t => t.TestList)
                                            .ThenInclude(tl => tl.Test)
                                        .Include(t => t.Room)
                                        .Where(t => t.AppointmentId == model.AppointmentId)
                                        .ToListAsync();

                    model.PatientName = appointment.Patient.FullName;
                    model.DateOfBirth = appointment.Patient.Dob;
                    model.Gender = appointment.Patient.Gender;
                    model.AvailableTests = _context.Tests
                       .Select(t => new Test { TestId = t.TestId, Name = t.Name })
                       .ToList();
                    model.AssignedRooms = assignedRooms;

                    ViewBag.AllTestsCompleted = false;
                    var trackingViewModel = assignedRooms
                        .Select(t => new TrackingViewModel
                        {
                            TestListId = t.TestListId ?? 0,
                            TestId = t.TestList.Test.TestId,
                            TestName = t.TestList?.Test?.Name,
                            TestStatus = t.TestList?.TestStatus,
                            RoomId = t.Room.RoomId,
                            RoomName = t.Room.RoomName,
                            RoomType = t.Room.RoomType
                        }).ToList();
                    var options = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };
                    ViewBag.AssignedRoom = JsonSerializer.Serialize(trackingViewModel, options);

                    return View("MedicalExam", model);
                }

                // Cập nhật trạng thái hoàn thành
                appointment.Symptoms = model.Symptoms?.Trim() ?? "";
                appointment.Diagnosis = model.Diagnosis?.Trim() ?? "";
                appointment.PrescriptionNote = model.PrescriptionNote?.Trim() ?? "";
                appointment.Status = "Completed";
                TempData["success"] = "Đã hoàn thành khám bệnh!";
            }
            else if (actionType.Equals("save"))
            {
                appointment.Symptoms = model.Symptoms?.Trim() ?? "";
                appointment.Diagnosis = model.Diagnosis?.Trim() ?? "";
                appointment.PrescriptionNote = model.PrescriptionNote?.Trim() ?? "";
                TempData["success"] = "Lưu thông tin thành công!";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("MedicalExam", new { id = model.AppointmentId });
        }


        public IActionResult PerformTest(int id)
        {
            var testList = _context.TestLists
                .Include(t => t.Test)
                .Include(t => t.Appointment)
                    .ThenInclude(a => a.Patient)
                .FirstOrDefault(t => t.TestListId == id);

            if (testList == null)
                return NotFound();

            var model = new PerformTestViewModel
            {
                TestListId = testList.TestListId,
                TestName = testList.Test.Name,
                ResultDescription = testList.Result,
                PatientName = testList.Appointment.Patient.FullName,
                DateOfBirth = testList.Appointment.Patient.Dob,
                Gender = testList.Appointment.Patient.Gender
            };

            return View(model);
        }


        private (string RoleKey, int? UserId) GetUserRoleAndId(ClaimsPrincipal user)
        {

            if (user.IsInRole("Receptionist"))
                return ("StaffID", GetUserIdFromClaim(user, "StaffID"));

            if (user.IsInRole("Doctor"))
                return ("DoctorID", GetUserIdFromClaim(user, "DoctorID"));

            if (user.IsInRole("LabTechnician"))
                return ("StaffID", GetUserIdFromClaim(user, "StaffID"));
            return default;
        }

        private int? GetUserIdFromClaim(ClaimsPrincipal user, string claimType)
        {
            var claim = user.FindFirst(claimType);
            return claim != null && int.TryParse(claim.Value, out var id) ? id : null;
        }

        [HttpGet]
        public async Task<IActionResult> GetRoomsByTest(int testId)
        {
            var roomType = await _context.Tests
                                         .Where(t => t.TestId == testId)
                                         .Select(t => t.RoomType)
                                         .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(roomType)) // Ensure roomType is not null or empty
            {
                return Json(new { success = false, message = "Không tìm thấy loại phòng cho xét nghiệm này." });
            }

            var rooms = await _context.Rooms
                                      .Where(r => r.RoomType != null && r.RoomType.Equals(roomType) && r.Status != "Maintain") // Add null check for RoomType
                                      .Select(r => new
                                      {
                                          RoomId = r.RoomId,
                                          RoomName = r.RoomName,
                                          RoomType = r.RoomType
                                      })
                                      .ToListAsync();

            return Json(rooms);
        }
    }
}
