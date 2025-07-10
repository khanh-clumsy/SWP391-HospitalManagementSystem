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
using X.PagedList;
using X.PagedList.Extensions;
using X.PagedList.EF;
using System.Threading.Tasks;
using HospitalManagement.Helpers;

namespace HospitalManagement.Controllers
{
    public class TrackingController : Controller
    {

        private readonly IAppointmentRepository _appointmentRepo;
        private readonly ITrackingRepository _TrackingRepository;
        private readonly IRoomRepository _RoomRepository;
        private readonly HospitalManagementContext _context;
        private readonly IScheduleRepository _scheduleRepository;

        public TrackingController(IAppointmentRepository appointmentRepo, ITrackingRepository trackingRepository, IRoomRepository roomRepository, HospitalManagementContext context, IScheduleRepository scheduleRepository)
        {
            _appointmentRepo = appointmentRepo;
            _TrackingRepository = trackingRepository;
            _RoomRepository = roomRepository;
            _context = context;
            _scheduleRepository = scheduleRepository;
        }

        [Authorize(Roles = "Receptionist")]
        //tìm cuộc hẹn theo sd0t
        public async Task<IActionResult> StartAppointmentProcess(string? phone, int? page)
        {
            int pageSize = 12;
            int pageNumber = page ?? 1;
            ViewBag.Phone = phone;
            var appointments = await _appointmentRepo.GetTodayAppointmentsAsync(phone);
            var pagedAppointments = appointments
                .OrderBy(a => a.Slot.StartTime)
                .ToPagedList(pageNumber, pageSize);

            return View(pagedAppointments);
        }

        //Bắt đầu cuộc hẹn chuyển status của appoinment sang Ongoing
        public async Task<IActionResult> StartAppointment(int id)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                    .ThenInclude(d => d.Schedules)
                .Include(a => a.Slot)
                .FirstOrDefaultAsync(a => a.AppointmentId == id);

            if (appointment == null)
                return NotFound();

            // Lấy phòng làm việc của bác sĩ tại slot và ngày của appointment
            int doctorId = appointment.DoctorId ?? 0;
            int slotId = appointment.SlotId ?? 0;
            var day = appointment.Date;
            Console.WriteLine($"doctorId={doctorId}, slotId={slotId}, day={day}");
            string doctorRoomName = null;
            if (doctorId > 0 && slotId > 0)
            {
                var roomId = await _scheduleRepository.GetRoomIdByDoctorSlotAndDayAsync(doctorId, slotId, day);
                if (roomId.HasValue)
                {
                    var room = await _context.Rooms.FirstOrDefaultAsync(r => r.RoomId == roomId.Value);
                    doctorRoomName = room?.RoomName;
                }

            }
            ViewBag.DoctorRoomName = doctorRoomName;

            // Trả về một partial view chứa quy trình khám
            return PartialView("_StartAppointmentPartial", appointment);
        }

        [HttpPost]
        public async Task<IActionResult> StartDiagnosis(int appointmentId)
        {
            var appointment = _context.Appointments
                .FirstOrDefault(a => a.AppointmentId == appointmentId);

            if (appointment == null)
            {
                TempData["error"] = AppConstants.Messages.Appointment.NotFound;
                return RedirectToAction("Index", "Home");
            }

            // Cập nhật trạng thái bắt đầu khám nếu cần
            appointment.Status = AppConstants.AppointmentStatus.Ongoing;
            var schedule = await _scheduleRepository.GetScheduleWithRoomAsync(appointment.DoctorId ?? 0, appointment.SlotId ?? 0, appointment.Date);

            if (schedule != null && schedule.Room != null)
            {
                // Lưu bản ghi mới vào bảng Trackings
                var tracking = new Tracking
                {
                    AppointmentId = appointment.AppointmentId,
                    RoomId = schedule.Room.RoomId,
                    Time = DateTime.Now
                };
                _context.Trackings.Add(tracking);
            }
            if (appointment.PackageId != null)
            {
                var package = await _context.Packages
                    .Include(p => p.PackageTests).ThenInclude(pt => pt.Test) 
                    .FirstOrDefaultAsync(p => p.PackageId == appointment.PackageId);

                if (package?.PackageTests != null)
                {
                    foreach (var test in package.PackageTests)
                    {
                        var testRecord = new TestRecord
                        {
                            AppointmentId = appointment.AppointmentId,
                            TestId = test.TestId,
                            TestStatus = AppConstants.TestStatus.Pending,
                            CreatedAt = DateTime.Now
                        };
                        _context.TestRecords.Add(testRecord);
                    }
                }
            }
            // Nếu không tìm thấy lịch hoặc phòng thì không lưu tracking
            await _context.SaveChangesAsync();

            // ✅ Gán thông báo thành công
            TempData["success"] = AppConstants.Messages.Tracking.StartAppointmentProcessSuccess;

            // ✅ Chuyển hướng sang trang quy trình
            return RedirectToAction("StartAppointmentProcess", "Tracking");
        }


        //Hiện các cuộc hẹn cần khám ontime
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> DoctorTodayAppointment(int? page)
        {
            var doctorClaim = User.FindFirst(AppConstants.ClaimTypes.DoctorId);
            if (doctorClaim == null)
                return RedirectToAction("Login", "Auth");

            if (!int.TryParse(doctorClaim.Value, out int doctorId))
                return RedirectToAction("Login", "Auth");

            var appointmentList = await _appointmentRepo.GetOngoingAppointmentsByDoctorIdAsync(doctorId);
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

            var allTestsCompleted = await _context.TestRecords
                    .Where(t => t.AppointmentId == id)
                    .AllAsync(t => t.TestStatus == AppConstants.TestStatus.Completed);
            ViewBag.AllTestsCompleted = allTestsCompleted;

            if (appointment == null)
            {
                TempData["error"] = AppConstants.Messages.Appointment.NotFound;
                return RedirectToAction("Index", "Home");
            }

            // Lấy danh sách phòng đã chỉ định (Tracking + Room)
            var trackings = await _context.Trackings
                                .Include(t => t.TestRecord)
                                    .ThenInclude(tl => tl.Test)
                                .Include(t => t.Room)
                                .Where(t => t.AppointmentId == id)
                                .ToListAsync();

            // Tạo ViewModel
            var examViewModel = new ExaminationViewModel
            {
                AppointmentId = appointment.AppointmentId,
                PatientID = appointment.PatientId,
                PatientName = appointment.Patient.FullName,
                DateOfBirth = appointment.Patient.Dob,
                Gender = appointment.Patient.Gender,
                TestStatus = appointment.Status,
                AssignedRooms = trackings,
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

            // Lọc chỉ lấy các phòng không phải là phòng khám
            var assignedRooms = examViewModel.AssignedRooms
                .Where(tracking => tracking.Room?.RoomType != AppConstants.RoomTypes.Clinic && tracking.Room?.RoomType != AppConstants.RoomTypes.Cashier)
                .Select(tracking => new TrackingViewModel
                {
                    TestRecordID = tracking.TestRecordId ?? 0,
                    TestID = tracking.TestRecord?.Test?.TestId ?? 0,
                    TestName = tracking.TestRecord?.Test?.Name,
                    TestStatus = tracking.TestRecord?.TestStatus,
                    RoomID = tracking.Room?.RoomId ?? 0,
                    RoomName = tracking.Room?.RoomName ?? AppConstants.Messages.General.Undefined,
                    RoomType = tracking.Room?.RoomType ?? AppConstants.Messages.General.Undefined
                })
                .ToList();

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            ViewBag.AssignedRoom = JsonSerializer.Serialize(assignedRooms, jsonOptions);
            return View(examViewModel);
        }

        [HttpPost]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> AssignTest(int appointmentId, int roomId, int testId)
        {
            // 1. Kiểm tra test có tồn tại
            var test = await _context.Tests.FirstOrDefaultAsync(t => t.TestId == testId);
            if (test == null)
                return BadRequest(new { message = AppConstants.Messages.Test.NotFound });

            // 2. Kiểm tra xem đã có test này với phòng này chưa
            bool exists = await _context.Trackings
                .Include(t => t.TestRecord)
                .AnyAsync(t =>
                    t.AppointmentId == appointmentId &&
                    t.RoomId == roomId &&
                    t.TestRecord != null &&
                    t.TestRecord.TestId == testId);

            if (exists)
                return BadRequest(new { message = AppConstants.Messages.Test.AlreadyAssigned });

            // 3. Tìm hoặc tạo mới TestRecord
            var testRecord = await _context.TestRecords
                .FirstOrDefaultAsync(t => t.AppointmentId == appointmentId && t.TestId == testId);

            if (testRecord == null)
            {
                testRecord = new TestRecord
                {
                    AppointmentId = appointmentId,
                    TestId = testId,
                    CreatedAt = DateTime.Now,
                    TestStatus = AppConstants.TestStatus.WaitingForPayment
                };
                await _context.TestRecords.AddAsync(testRecord);

                // Ghi chú: SaveChangesAsync ngay để lấy TestRecordId
                await _context.SaveChangesAsync();

                // Tạo hóa đơn cho test
                var testInvoice = new InvoiceDetail
                {
                    AppointmentId = appointmentId,
                    ItemType = "Test",
                    ItemId = test.TestId,
                    ItemName = test.Name,
                    UnitPrice = test.Price,
                    PaymentStatus = AppConstants.PaymentStatus.Unpaid,
                    CreatedAt = DateTime.Now
                };
                await _context.InvoiceDetails.AddAsync(testInvoice);
            }

            // 4. Tracking tới phòng thanh toán (nếu chưa có)
            var paymentRoom = await _context.Rooms
                .Where(r => r.RoomType == AppConstants.RoomTypes.Cashier)
                .OrderBy(r => Guid.NewGuid())
                .FirstOrDefaultAsync();

            if (paymentRoom != null)
            {
                var paymentTracking = new Tracking
                {
                    AppointmentId = appointmentId,
                    RoomId = paymentRoom.RoomId,
                    Time = DateTime.Now,
                    TestRecordId = null
                };
                await _context.Trackings.AddAsync(paymentTracking);
            }

            // 5. Tracking test
            var testTracking = new Tracking
            {
                AppointmentId = appointmentId,
                RoomId = roomId,
                Time = DateTime.Now,
                TestRecordId = testRecord.TestRecordId
            };
            await _context.Trackings.AddAsync(testTracking);

            // 6. Save toàn bộ
            await _context.SaveChangesAsync();

            // 7. Trả về room
            var room = await _context.Rooms.FirstOrDefaultAsync(r => r.RoomId == roomId);
            if (room == null)
                return BadRequest(new { message = AppConstants.Messages.Room.NotFound });

            var response = new
            {
                testRecordId = testRecord.TestRecordId,
                testId = testRecord.TestId,
                testName = test.Name,
                testStatus = testRecord.TestStatus,
                roomId = room.RoomId,
                roomName = room.RoomName,
                roomType = room.RoomType,
                status = room.Status
            };

            return Json(response);
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
                TempData["error"] = AppConstants.Messages.Appointment.NotFound;
                return RedirectToAction("AppointmentList");
            }

            var actionType = Request.Form["action"];

            if (actionType.Equals("submit"))
            {
                // Kiểm tra tất cả các xét nghiệm đã được hoàn thành
                bool allTestsCompleted = await _context.TestRecords
                    .Where(t => t.AppointmentId == model.AppointmentId)
                    .AllAsync(t => t.TestStatus == AppConstants.AppointmentStatus.Completed);

                if (!allTestsCompleted)
                {
                    TempData["error"] = AppConstants.Messages.Test.NotCompleted;

                    // Gán lại ViewBag và model để quay lại đúng trang
                    var assignedTrackings = await _context.Trackings
                                        .Include(t => t.TestRecord)
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
                    model.AssignedRooms = assignedTrackings;

                    ViewBag.AllTestsCompleted = false;
                    var trackingDtoList = assignedTrackings
                        .Select(t => new TrackingViewModel
                        {
                            TestRecordID = t.TestRecordId ?? 0,
                            TestID = t.TestRecord.Test.TestId,
                            TestName = t.TestRecord?.Test?.Name,
                            TestStatus = t.TestRecord?.TestStatus,
                            RoomID = t.Room.RoomId,
                            RoomName = t.Room.RoomName,
                            RoomType = t.Room.RoomType
                        }).ToList();
                    var jsonOptions = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };
                    ViewBag.AssignedRoom = JsonSerializer.Serialize(trackingDtoList, jsonOptions);

                    return View("MedicalExam", model);
                }

                // Cập nhật trạng thái hoàn thành
                appointment.Symptoms = model.Symptoms?.Trim() ?? "";
                appointment.Diagnosis = model.Diagnosis?.Trim() ?? "";
                appointment.PrescriptionNote = model.PrescriptionNote?.Trim() ?? "";
                appointment.Status = AppConstants.AppointmentStatus.Completed;
                TempData["success"] = AppConstants.Messages.Tracking.SubmitExaminationSuccess;
            }
            else if (actionType.Equals("save"))
            {
                appointment.Symptoms = model.Symptoms?.Trim() ?? "";
                appointment.Diagnosis = model.Diagnosis?.Trim() ?? "";
                appointment.PrescriptionNote = model.PrescriptionNote?.Trim() ?? "";
                TempData["success"] = AppConstants.Messages.Tracking.SaveExaminationSuccess;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("MedicalExam", new { id = model.AppointmentId });
        }


        public IActionResult PerformTest(int id)
        {
            var testList = _context.TestRecords
                .Include(t => t.Test)
                .Include(t => t.Appointment)
                    .ThenInclude(a => a.Patient)
                .FirstOrDefault(t => t.TestRecordId == id);

            if (testList == null)
                return NotFound();

            var model = new PerformTestViewModel
            {
                TestRecordID = testList.TestRecordId,
                TestName = testList.Test.Name,
                ResultDescription = testList.Result,
                PatientName = testList.Appointment.Patient.FullName,
                DateOfBirth = testList.Appointment.Patient.Dob,
                Gender = testList.Appointment.Patient.Gender
            };

            return View(model);
        }

        // private (string RoleKey, int? UserId) GetUserRoleAndId(ClaimsPrincipal user)
        // {

        //     if (user.IsInRole("Receptionist"))
        //         return ("StaffID", GetUserIdFromClaim(user, "StaffID"));

        //     if (user.IsInRole("Doctor"))
        //         return ("DoctorID", GetUserIdFromClaim(user, "DoctorID"));

        //     if (user.IsInRole("LabTechnician"))
        //         return ("StaffID", GetUserIdFromClaim(user, "StaffID"));
        //     return default;
        // }

        // private int? GetUserIdFromClaim(ClaimsPrincipal user, string claimType)
        // {
        //     var claim = user.FindFirst(claimType);
        //     return claim != null && int.TryParse(claim.Value, out var id) ? id : null;
        // }
        [HttpPost]
        [Authorize(Roles = "Receptionist, Doctor, Patient")]
        public async Task<IActionResult> UpdateDOB(int patientId, DateTime dob)
        {
            // Kiểm tra ngày sinh hợp lệ
            if (dob == default || dob > DateTime.Now)
            {
                TempData["error"] = AppConstants.Messages.General.InvalidDate;
                return RedirectToAction("MedicalExam", new { id = patientId });
            }

            var patient = await _context.Patients.FindAsync(patientId);
            if (patient == null)
            {
                TempData["error"] = AppConstants.Messages.User.PatientNotFound;
                return RedirectToAction("MedicalExam", new { id = patientId });
            }

            // Cập nhật ngày sinh
            patient.Dob = dob;
            await _context.SaveChangesAsync();

            // Tìm appointment đang diễn ra
            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.PatientId == patientId && a.Status == AppConstants.AppointmentStatus.Ongoing);

            TempData["success"] = AppConstants.Messages.User.UpdateDOBSuccess;

            if (appointment != null)
            {
                return RedirectToAction("MedicalExam", "Tracking", new { id = appointment.AppointmentId });
            }

            // Nếu không có appointment, về lại trang chủ hoặc trang phù hợp
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> GetRoomsByTest(int testId)
        {
            var roomType = await _context.Tests
                                         .Where(t => t.TestId == testId)
                                         .Select(t => t.RoomType)
                                         .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(roomType))
            {
                return Json(new { success = false, message = AppConstants.Messages.Room.InvalidRoomType });
            }

            var rooms = await _context.Rooms
                                      .Where(r => r.RoomType != null && r.RoomType.Equals(roomType) && r.Status != AppConstants.RoomStatus.Maintain) // Add null check for RoomType
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
