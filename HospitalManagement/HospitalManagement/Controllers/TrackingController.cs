using System.Security.Claims;
using System.Text.Json;
using HospitalManagement.Data;
using HospitalManagement.Helpers;
using HospitalManagement.Models;
using HospitalManagement.Repositories;
using HospitalManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using X.PagedList;
using X.PagedList.EF;
using X.PagedList.Extensions;
using static HospitalManagement.Helpers.AppConstants.Messages;

namespace HospitalManagement.Controllers
{
    public class TrackingController : Controller
    {

        private readonly IAppointmentRepository _appointmentRepo;
        private readonly ITrackingRepository _trackingRepo;
        private readonly IRoomRepository _roomRepo;
        private readonly ITestRepository _testRepo;
        private readonly HospitalManagementContext _context;
        private readonly IScheduleRepository _scheduleRepo;

        public TrackingController(IAppointmentRepository appointmentRepo, ITrackingRepository trackingRepository, IRoomRepository roomRepository, ITestRepository testRepository, HospitalManagementContext context, IScheduleRepository scheduleRepository)
        {
            _appointmentRepo = appointmentRepo;
            _trackingRepo = trackingRepository;
            _roomRepo = roomRepository;
            _testRepo = testRepository;
            _context = context;
            _scheduleRepo = scheduleRepository;
        }

        [Authorize(Roles = AppConstants.Roles.Receptionist + ", " + AppConstants.Roles.Admin)]
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
        [Authorize(Roles = AppConstants.Roles.Receptionist + ", " + AppConstants.Roles.Admin)]
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
                var roomId = await _scheduleRepo.GetRoomIdByDoctorSlotAndDayAsync(doctorId, slotId, day);
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

        [Authorize(Roles = AppConstants.Roles.Receptionist + ", " + AppConstants.Roles.Admin)]
        [HttpPost]
        public async Task<IActionResult> StartDiagnosis(int appointmentId)
        {
            var appointment = await _context.Appointments
                    .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

            if (appointment == null)
            {
                TempData["error"] = AppConstants.Messages.Appointment.NotFound;
                return RedirectToAction("Index", "Home");
            }

            // Kiểm tra xem bệnh nhân đã có cuộc hẹn Ongoing nào khác chưa
            var existingOngoingAppointment = await _context.Appointments
                .Where(a => a.PatientId == appointment.PatientId &&
                           a.AppointmentId != appointmentId &&
                           a.Status == AppConstants.AppointmentStatus.Ongoing)
                .FirstOrDefaultAsync();

            if (existingOngoingAppointment != null)
            {
                TempData["error"] = AppConstants.Messages.Appointment.PatientHasOngoingAppointment;
                return RedirectToAction("StartAppointmentProcess", "Tracking");
            }

            // Cập nhật trạng thái bắt đầu khám
            if (appointment.Status == AppConstants.AppointmentStatus.Completed ||
                appointment.Status == AppConstants.AppointmentStatus.Rejected)
            {
                TempData["error"] = AppConstants.Messages.Appointment.CanNotStartMedicalExam;
                return RedirectToAction("Index", "Home");
            }
            appointment.Status = AppConstants.AppointmentStatus.Ongoing;
            var schedule = await _scheduleRepo.GetScheduleWithRoomAsync(
                                   appointment.DoctorId ?? 0,
                                   appointment.SlotId ?? 0,
                                   appointment.Date);
            if (schedule == null)
            {
                TempData["error"] = AppConstants.Messages.Appointment.CanNotFindDoctorSchedule;
                return RedirectToAction("Index", "Home");
            }
            if (schedule?.Room != null)
            {
                bool trackingExists = await _context.Trackings
                    .AnyAsync(t => t.AppointmentId == appointment.AppointmentId);

                if (!trackingExists)
                {
                    var tracking = new Models.Tracking
                    {
                        AppointmentId = appointment.AppointmentId,
                        RoomId = schedule.Room.RoomId,
                        Time = DateTime.Now,
                        TrackingBatch = 1
                    };
                    _context.Trackings.Add(tracking);
                }
            }
            await _context.SaveChangesAsync();
            TempData["success"] = AppConstants.Messages.Tracking.StartAppointmentProcessSuccess;
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
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .FirstOrDefaultAsync(a => a.AppointmentId == id);
            var doctorIdClaim = User.FindFirst(AppConstants.ClaimTypes.DoctorId)?.Value;
            if (doctorIdClaim == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            int currentDoctorId = int.Parse(doctorIdClaim);
            var allTestsCompleted = await _context.TestRecords
                    .Where(t => t.AppointmentId == id)
                    .AllAsync(t => t.TestStatus == AppConstants.TestStatus.Completed);
            ViewBag.AllTestsCompleted = allTestsCompleted;

            if (appointment == null)
            {
                TempData["error"] = AppConstants.Messages.Appointment.NotFound;
                return RedirectToAction("Index", "Home");
            }
            if (appointment.DoctorId != currentDoctorId)
            {
                TempData["error"] = AppConstants.Messages.Appointment.NoPermission;
                return RedirectToAction("Index", "Home");
            }
            if (appointment.Status == AppConstants.AppointmentStatus.Rejected)
            {
                TempData["error"] = AppConstants.Messages.Appointment.Failed;
                return RedirectToAction("Index", "Home");
            }
            if (appointment.Status == AppConstants.AppointmentStatus.Completed)
            {
                TempData["error"] = "Cuộc hẹn này đã hoàn thành khám, không thể tiếp tục.";
                return RedirectToAction("Detail", "Appointment", new { appId = appointment.AppointmentId });
            }

            // Lấy danh sách phòng đã chỉ định (Tracking + Room)
            var trackings = await _trackingRepo.GetTrackingsByAppointmentIdWithDetailsAsync(appointment.AppointmentId);
            //Lấy danh sách các xét nghiệm đang khả dụng trong hệ thống
            var availableTests = await _testRepo.GetAvailableTestsAsync();
            //Lấy ra các test từ gói khám
            List<int> packageTestIds = new List<int>();

            if (appointment.PackageId != null)
            {
                // Lấy danh sách TestId trong gói khám
                packageTestIds = await _context.PackageTests
                    .Where(pt => pt.PackageId == appointment.PackageId)
                    .Select(pt => pt.TestId)
                    .ToListAsync();
            }

            // Chỉ lấy TestRecord từ gói (không lấy ngoài gói)
            var testRecordsFromPackage = appointment.PackageId != null
                ? await _context.TestRecords
                .Include(tr => tr.Test)
                .Where(tr => tr.AppointmentId == appointment.AppointmentId && packageTestIds.Contains(tr.TestId))
                .ToListAsync()
                : new List<TestRecord>();

            //Map theo ViewModel
            var testRecordViewModels = testRecordsFromPackage.Select(tr =>
            {
                var assignedTracking = trackings.FirstOrDefault(t => t.TestRecordId == tr.TestRecordId);
                return new TestRecordViewModel
                {
                    TestRecordId = tr.TestRecordId,
                    TestId = tr.TestId,
                    TestName = tr.Test.Name,
                    TestStatus = tr.TestStatus,
                    RoomId = assignedTracking?.Room?.RoomId,
                    RoomName = assignedTracking?.Room?.RoomName,
                    RoomType = assignedTracking?.Room?.RoomType,
                };
            }).ToList();

            var examViewModel = new ExaminationViewModel
            {
                AppointmentId = appointment.AppointmentId,
                PatientID = appointment.PatientId,
                PatientName = appointment.Patient.FullName,
                DateOfBirth = appointment.Patient.Dob,
                Gender = appointment.Patient.Gender,
                TestStatus = appointment.Status,
                AssignedRooms = trackings,
                AvailableTests = availableTests,
                Symptoms = appointment.Symptoms,
                Diagnosis = appointment.Diagnosis,
                PrescriptionNote = appointment.PrescriptionNote,
                ServiceId = appointment.ServiceId,
                PackageId = appointment.PackageId,
                ServiceName = appointment.Service?.ServiceType,
                PackageName = appointment.Package?.PackageName,
                TestRecords = testRecordViewModels
            };

            //Lấy ra những phòng phù hợp với Test
            if (appointment.PackageId != null)
            {
                var roomDict = new Dictionary<int, List<Models.Room>>();

                foreach (var testRecord in testRecordsFromPackage)
                {
                    int testId = testRecord.TestId;

                    if (!roomDict.ContainsKey(testId))
                    {
                        var rooms = await _context.Rooms
                            .Where(r => r.RoomType == testRecord.Test.RoomType)
                            .OrderBy(r => r.RoomName)
                            .ToListAsync();

                        roomDict[testId] = rooms;
                    }
                }
                // Gán vào ViewModel
                examViewModel.AvailableRoomsPerTest = roomDict;
            }

            // Lọc chỉ lấy các phòng không phải là phòng khám để trả ra View
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

            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);
           
            if (test == null)  return BadRequest(new { message = AppConstants.Messages.Test.NotFound });

            if (appointment == null) return BadRequest(new { message = AppConstants.Messages.Appointment.NotFound });

            var trackings = await _context.Trackings
                .Include(t => t.TestRecord)
                .Where(t => t.AppointmentId == appointmentId)
                .ToListAsync();

            bool hasOngoingPaidTest = trackings.Any(t =>
                t.TestRecord != null &&
                t.TestRecord.TestStatus == AppConstants.TestStatus.Ongoing &&
                _context.InvoiceDetails.Any(i =>
                    i.AppointmentId == appointmentId &&
                    i.ItemType == "Test" &&
                    i.ItemId == t.TestRecord.TestRecordId &&
                    i.PaymentStatus == AppConstants.PaymentStatus.Paid));

            if (hasOngoingPaidTest)
            {
                return BadRequest(new { message = "Bệnh nhân đang thực hiện xét nghiệm (đã thanh toán). Không thể chỉ định thêm." });
            }

            // 2. Kiểm tra xem đã có test này với phòng này chưa
            bool exists = await _context.Trackings
                .Include(t => t.TestRecord)
                .AnyAsync(t =>
                    t.AppointmentId == appointmentId &&
                    t.RoomId == roomId &&
                    t.TestRecord != null &&
                    t.TestRecord.TestId == testId);

            if (exists)
            {
                return BadRequest(new { message = AppConstants.Messages.Test.AlreadyAssigned });
            }
            
            if (appointment.Status == AppConstants.AppointmentStatus.Completed)
            {
                return BadRequest(new { message = AppConstants.Messages.Appointment.AppointmentAlreadyCompletdCanNotAssignTest });
            }

            // 3. Tìm hoặc tạo mới TestRecord
            var testRecord = await _context.TestRecords
                .FirstOrDefaultAsync(t => t.AppointmentId == appointmentId && t.TestId == testId);

            bool isNewTestRecord = false;
            if (testRecord == null)
            {
                testRecord = new TestRecord
                {
                    AppointmentId = appointmentId,
                    TestId = testId,
                    TestStatus = AppConstants.TestStatus.WaitingForPayment
                };
                await _context.TestRecords.AddAsync(testRecord);
                await _context.SaveChangesAsync();
                isNewTestRecord = true;

                // Tạo hóa đơn cho test
                var testInvoice = new InvoiceDetail
                {
                    AppointmentId = appointmentId,
                    ItemType = "Test",
                    ItemId = testRecord.TestRecordId,
                    ItemName = test.Name,
                    UnitPrice = test.Price,
                    PaymentStatus = AppConstants.PaymentStatus.Unpaid,
                    CreatedAt = DateTime.Now
                };
                await _context.InvoiceDetails.AddAsync(testInvoice);
            }
            else
            {
                if (testRecord.TestStatus != AppConstants.TestStatus.WaitingForPayment)
                {
                    testRecord.TestStatus = AppConstants.TestStatus.Ongoing;
                    _context.TestRecords.Update(testRecord);
                }
            }

            // Tìm batch lớn nhất mà tất cả test trong batch đó đều chưa hoàn thành
            int? openBatch = await BatchHelper.GetOpenBatchAsync(_context, appointmentId);
            int batch = openBatch ?? await BatchHelper.GetOpenOrNewBatchAsync(_context, appointmentId);

            // 5. Tracking test
            var testTracking = new Models.Tracking
            {
                AppointmentId = appointmentId,
                RoomId = roomId,
                Time = DateTime.Now,
                TestRecordId = testRecord.TestRecordId,
                TrackingBatch = batch
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

            // Debug information
            Console.WriteLine("=== ASSIGN TEST DEBUG ===");
            Console.WriteLine($"AppointmentId: {appointmentId}");
            Console.WriteLine($"TestId: {testId}");
            Console.WriteLine($"RoomId: {roomId}");
            Console.WriteLine($"IsNewTestRecord: {isNewTestRecord}");
            Console.WriteLine($"TestRecordId: {testRecord.TestRecordId}");
            Console.WriteLine($"TestRecordStatus: {testRecord.TestStatus}");
            Console.WriteLine($"TestName: {test.Name}");
            Console.WriteLine($"RoomName: {room.RoomName}");
            Console.WriteLine($"RoomType: {room.RoomType}");
            Console.WriteLine($"Tracking Batch: {batch}");
            Console.WriteLine($"Open Batch: {batch}");
            Console.WriteLine("=== END DEBUG ===");
            return Json(response);
        }

        [HttpGet]
        [Authorize(Roles = "Doctor, Patient")]
        public async Task<IActionResult> GetNextRoom(int appointmentId)
        {
            var currentBatch = await _context.Trackings
                .Where(t => t.AppointmentId == appointmentId)
                .MaxAsync(t => (int?)t.TrackingBatch) ?? 0;

            var trackings = await _context.Trackings
                .Include(t => t.Room)
                .Include(t => t.TestRecord)
                .Where(t => t.AppointmentId == appointmentId && t.TrackingBatch == currentBatch)
                .OrderBy(t => t.Time)
                .ToListAsync();

            if (!trackings.Any())
                return NotFound(new { message = "No tracking data found." });

            // Kiểm tra test cần thanh toán
            var unpaidTests = trackings
                .Where(t => t.TestRecord != null &&
                           t.TestRecord.TestStatus == AppConstants.TestStatus.WaitingForPayment)
                .ToList();

            if (unpaidTests.Any())
            {
                return Json(new
                {
                    message = "Bạn hãy tới lễ tân để thanh toán"
                });
            }

            // Kiểm tra test đang diễn ra (theo thứ tự thời gian)
            var ongoingTests = trackings
                .Where(t => t.TestRecord != null &&
                           t.TestRecord.TestStatus != AppConstants.TestStatus.Completed &&
                           t.TestRecord.TestStatus != AppConstants.TestStatus.WaitingForPayment)
                .OrderBy(t => t.Time)
                .ToList();

            if (ongoingTests.Any())
            {
                var nextTest = ongoingTests.First();
                return Json(new
                {
                    roomId = nextTest.RoomId,
                    roomName = nextTest.Room.RoomName,
                    roomType = nextTest.Room.RoomType,
                    message = "Please proceed to the test room"
                });
            }

            // Tất cả test đã hoàn thành
            return Json(new
            {
                message = "All steps completed in current batch."
            });
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
                // Kiểm tra tất cả các xét nghiệm đã được hoàn thành và đã được chỉ định phòng
                bool allTestsCompletedAndAssigned = await _context.TestRecords
                    .Where(t => t.AppointmentId == model.AppointmentId)
                    .AllAsync(t => t.TestStatus == AppConstants.TestStatus.Completed &&
                                   _context.Trackings.Any(tr => tr.TestRecordId == t.TestRecordId));

                if (!allTestsCompletedAndAssigned)
                {
                    TempData["error"] = "Cần hoàn thành và chỉ định phòng cho tất cả xét nghiệm trước khi kết thúc khám bệnh.";
                    // Gán lại ViewBag và model để quay lại đúng trang
                    var assignedTrackings = await _trackingRepo.GetTrackingsByAppointmentIdWithDetailsAsync(model.AppointmentId);
                    model.PatientName = appointment.Patient.FullName;
                    model.DateOfBirth = appointment.Patient.Dob;
                    model.Gender = appointment.Patient.Gender;
                    model.AvailableTests = await _testRepo.GetAvailableTestsAsync();
                    model.AssignedRooms = assignedTrackings;
                    if (appointment.PackageId != null)
                    {
                        model.TestRecords = await GetTestRecordViewModelsAsync(model.AppointmentId);
                    }
                    else
                    {
                        model.TestRecords = new List<TestRecordViewModel>();
                    }
                    ViewBag.AllTestsCompleted = false;
                    var assignedRooms = assignedTrackings
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
                    ViewBag.AssignedRoom = JsonSerializer.Serialize(assignedRooms, jsonOptions);
                    return View("MedicalExam", model);
                }
                // Cập nhật trạng thái hoàn thành
                appointment.Symptoms = model.Symptoms?.Trim() ?? "";
                appointment.Diagnosis = model.Diagnosis?.Trim() ?? "";
                appointment.PrescriptionNote = model.PrescriptionNote?.Trim() ?? "";
                appointment.RecordCreatedAt = DateTime.Now;
                var invoiceDetails = await _context.InvoiceDetails
                    .Where(i => i.AppointmentId == appointment.AppointmentId)
                    .ToListAsync();

                if (invoiceDetails.Count == 0)
                {
                    appointment.PaymentStatus = AppConstants.PaymentStatus.Unpaid;
                }
                else if (invoiceDetails.All(i => i.PaymentStatus == AppConstants.PaymentStatus.Paid))
                {
                    appointment.PaymentStatus = AppConstants.PaymentStatus.Paid;
                }
                else
                {
                    appointment.PaymentStatus = AppConstants.PaymentStatus.Unpaid;
                }
                appointment.TotalPrice = invoiceDetails.Sum(i => i.UnitPrice);
                appointment.Status = AppConstants.AppointmentStatus.Completed;
                TempData["success"] = AppConstants.Messages.Tracking.SubmitExaminationSuccess;
            }
            else if (actionType.Equals("save"))
            {
                // Chỉ lưu thông tin, không kiểm tra test chưa chỉ định phòng
                appointment.Symptoms = model.Symptoms?.Trim() ?? "";
                appointment.Diagnosis = model.Diagnosis?.Trim() ?? "";
                appointment.PrescriptionNote = model.PrescriptionNote?.Trim() ?? "";
                TempData["success"] = AppConstants.Messages.Tracking.SaveExaminationSuccess;

                // Kiểm tra nếu còn test chưa chỉ định phòng thì cảnh báo khi quay lại trang
                var unassignedTestRecords = await _testRepo.GetUnassignedTestRecordsAsync(appointment.AppointmentId);
                if (unassignedTestRecords.Any())
                {
                    TempData["warning"] = $"Còn {unassignedTestRecords.Count} xét nghiệm từ gói khám chưa được chỉ định phòng!";
                }
            }
            await _context.SaveChangesAsync();
            return RedirectToAction("MedicalExam", new { id = model.AppointmentId });
        }
        private async Task<List<TestRecordViewModel>> GetTestRecordViewModelsAsync(int appointmentId)
        {
            var appointment = await _context.Appointments
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

            if (appointment?.PackageId == null)
                return new List<TestRecordViewModel>();

            var testRecordsFromDb = await _context.TestRecords
                .Include(tr => tr.Test)
                .Where(tr => tr.AppointmentId == appointment.AppointmentId)
                .ToListAsync();

            var trackings = await _context.Trackings
                .Include(t => t.Room)
                .Where(t => t.AppointmentId == appointment.AppointmentId && t.TestRecordId != null)
                .ToListAsync();

            var testRecordViewModels = testRecordsFromDb.Select(tr =>
            {
                var tracking = trackings.FirstOrDefault(t => t.TestRecordId == tr.TestRecordId);
                return new TestRecordViewModel
                {
                    TestRecordId = tr.TestRecordId,
                    TestId = tr.TestId,
                    TestName = tr.Test?.Name,
                    TestStatus = tr.TestStatus,
                    RoomId = tracking?.Room?.RoomId,
                    RoomName = tracking?.Room?.RoomName,
                    RoomType = tracking?.Room?.RoomType
                };
            }).ToList();
            return testRecordViewModels;
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

        [HttpGet]
        public async Task<IActionResult> ViewInvoiceList(string status = "Unpaid", string? phone = null)
        {
            var query = _context.InvoiceDetails
                .Include(i => i.Appointment)
                    .ThenInclude(a => a.Patient) // cần include để lấy được PhoneNumber
                .AsQueryable();

            // Filter trạng thái
            if (status == "Paid")
            {
                query = query.Where(i => i.PaymentStatus == "Paid");
            }
            else
            {
                query = query.Where(i => i.PaymentStatus == null || i.PaymentStatus == "Unpaid");
            }

            // Filter số điện thoại nếu có
            if (!string.IsNullOrEmpty(phone))
            {
                query = query.Where(i => i.Appointment.Patient.PhoneNumber.Contains(phone));
            }

            var invoices = await query
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            ViewBag.CurrentStatus = status;
            ViewBag.CurrentPhone = phone;
            return View(invoices);
        }

    }
}
