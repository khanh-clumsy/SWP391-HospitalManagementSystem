using System.Security.Claims;
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
            var appointment = _context.Appointments
                .Include(a => a.Patient)
                .FirstOrDefault(a => a.AppointmentId == id);
            var allTestsCompleted = _context.TestLists
                    .Where(t => t.AppointmentId == id)
                    .All(t => t.TestStatus == "Completed");

            if (appointment == null)
                return NotFound();

            // Lấy danh sách phòng đã chỉ định (Tracking + Room)
            var assignedRooms = _context.Trackings
                                .Include(t => t.TestList)
                                    .ThenInclude(tl => tl.Test)
                                .Include(t => t.Room)
                                .Where(t => t.AppointmentId == id)
                                .ToList();


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
                PrescriptionNote = appointment.PrescriptionNote

            };
            ViewBag.AllTestsCompleted = allTestsCompleted;

            return View(viewModel);
        }
        [HttpPost]
        [Authorize(Roles = "Doctor")]
        public IActionResult AssignRoom(int appointmentId, int roomId, int testId)
        {
            // Kiểm tra testId có tồn tại
            var testExists = _context.Tests.Any(t => t.TestId == testId);
            if (!testExists)
            {
                return BadRequest("Test không tồn tại");
            }

            // Nếu đã tồn tại phòng + test này thì không thêm nữa
            bool exists = _context.Trackings
                .Include(t => t.TestList)
                .ThenInclude(t => t.Test)
                .Any(t =>
                    t.AppointmentId == appointmentId &&
                    t.RoomId == roomId &&
                    t.TestList != null &&
                    t.TestList.TestId == testId);

            if (!exists)
            {
                // Tìm hoặc tạo TestList
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

                // Tạo tracking mới
                var tracking = new Tracking
                {
                    AppointmentId = appointmentId,
                    RoomId = roomId,
                    Time = DateTime.Now,
                    TestListId = testList.TestListId
                };

                _context.Trackings.Add(tracking);
                _context.SaveChanges();

                ViewBag.Message = "Thêm phòng thành công";
                ViewBag.Status = "success";
            }
            else
            {
                ViewBag.Message = "Test này đã được chỉ định";
                ViewBag.Status = "danger";
            }

            // Trả về danh sách phòng đã chỉ định
            var assignedRooms = _context.Trackings
                .Where(t => t.AppointmentId == appointmentId)
                .Include(t => t.TestList)
                .ThenInclude(t => t.Test)
                .Include(t => t.Room)
                .ToList();

            return PartialView("_AssignedRoom", assignedRooms);
        }
        public IActionResult GetRoomTypesByTest(int testId)
        {
            var roomType = _context.Tests
                .Where(t => t.TestId == testId)
                .Select(t => t.RoomType)
                .FirstOrDefault();

            // Tìm tất cả RoomType trùng với roomType của test
            var roomTypes = _context.Rooms
                .Where(r => r.RoomType == roomType)
                .Select(r => r.RoomType)
                .Distinct()
                .ToList();

            return PartialView("_RoomTypeSelector", roomTypes);
        }
        public IActionResult GetRoomsByRoomType(string roomType, int testId, int appointmentId)
        {
            var rooms = _context.Rooms
                .Where(r => r.RoomType == roomType)
                .Select(r => new RoomWithTestCountViewModel
                {
                    RoomId = r.RoomId,
                    RoomName = r.RoomName,
                    TestCount = _context.Trackings
                        .Count(t => t.RoomId == r.RoomId && t.TestList.TestStatus == "Ongoing")
                })
                .ToList();

            return PartialView("_AvailableRoomsList", rooms);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveExamination(ExaminationViewModel model)
        {

            var appointment = _context.Appointments.FirstOrDefault(a => a.AppointmentId == model.AppointmentId);
            if (appointment == null)
            {
                TempData["error"] = "Cuộc hẹn không tồn tại.";
                return RedirectToAction("AppointmentList");
            }

            appointment.Symptoms = model.Symptoms;
            appointment.Diagnosis = model.Diagnosis;
            appointment.PrescriptionNote = model.PrescriptionNote;

            _context.SaveChanges();

            TempData["success"] = "Lưu thông tin thành công!";
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

    }
}
