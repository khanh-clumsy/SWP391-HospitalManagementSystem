using HospitalManagement.Data;
using HospitalManagement.Helpers;
using HospitalManagement.Models;
using HospitalManagement.Repositories;
using HospitalManagement.Services;
using HospitalManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Controllers
{
    public class TestPerformController : Controller
    {
        private readonly HospitalManagementContext _context;
        private readonly IRoomRepository _roomRepo;

        public TestPerformController(HospitalManagementContext context, IRoomRepository roomRepo)
        {
            _context = context;
            _roomRepo = roomRepo;
        }

        [Authorize(Roles = "TestDoctor")]
        [HttpGet]
        public async Task<IActionResult> TestInput(int testRecordID)
        {
            var testRecord = await _context.TestRecords
                .Include(t => t.Test)
                .Include(t => t.Appointment).ThenInclude(a => a.Patient)
                .FirstOrDefaultAsync(t => t.TestRecordId == testRecordID);

            if (testRecord == null)
            {
                TempData["error"] = "Không tìm thấy xét nghiệm.";
                return RedirectToAction("ViewOngoingTest", "TestPerform");
            }

            var model = new TestResultInputViewModel
            {
                TestRecordID = testRecord.TestRecordId,
                TestName = testRecord.Test.Name,
                PatientFullName = testRecord.Appointment.Patient.FullName,
                Gender = testRecord.Appointment.Patient.Gender,
                DOB = testRecord.Appointment.Patient.Dob ?? DateTime.MinValue
            };

            return View(model);
        }

        [Authorize(Roles = "TestDoctor")]
        [HttpPost]
        public async Task<IActionResult> InputTestResult(TestResultInputViewModel model)
        {
            int doctorId = int.Parse(User.FindFirst(AppConstants.ClaimTypes.DoctorId)?.Value ?? "0");
            var testRecord = await _context.TestRecords.FindAsync(model.TestRecordID);
            if (testRecord == null)
            {
                TempData["error"] = "Không tìm thấy xét nghiệm.";
                return NotFound();
            }
            if (model.ResultFile == null)
            {
                TempData["Error"] = "Chưa cập nhật kết quả";
                return View(model);
            }

            // Validate file
            if (model.ResultFile != null)
            {
                try
                {
                    var fileName = await FileService.SaveTestFileAsync(model.ResultFile, "TestResult");
                    model.ResultFileName = fileName;
                }
                catch (InvalidOperationException ex)
                {
                    TempData["Error"] = ex.Message;
                    return View(model);
                }
            }
            // Add các trường vào DB
            testRecord.TestNote = model.Note;
            testRecord.Result = model.ResultFileName;
            testRecord.TestStatus = "Completed";
            testRecord.CompletedAt = DateTime.Now;
            testRecord.DoctorId = doctorId;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Lưu kết quả xét nghiệm thành công.";
            return RedirectToAction("TestListForDoctor");
        }


        [HttpGet]
        [Authorize(Roles = "TestDoctor")]
        public async Task<IActionResult> ViewOngoingTest()
        {
            int doctorId = int.Parse(User.FindFirst(AppConstants.ClaimTypes.DoctorId)?.Value ?? "0");
            var today = DateOnly.FromDateTime(DateTime.Today);

            var roomSlotsToday = await _roomRepo.GetRoomSlotInfosByDoctorAndDateAsync(doctorId, today); // nhiều phòng
            ViewBag.RoomSlotsToday = roomSlotsToday;

            var ongoingTestsByRoom = new Dictionary<int, List<TestPatientViewModel>>();
            foreach (var roomSlots in roomSlotsToday)
            {
                var ongoingTestsInRoom = await _context.Trackings
                    .Include(t => t.TestRecord).ThenInclude(tr => tr.Test)
                    .Include(t => t.Appointment).ThenInclude(a => a.Patient)
                    .Where(t => t.RoomId == roomSlots.RoomId &&
                                t.TestRecordId != null && t.TestRecord != null &&
                                t.TestRecord.TestStatus == AppConstants.TestStatus.Ongoing &&
                                t.Time.Date == DateTime.Today &&
                                t.Time.TimeOfDay >= roomSlots.StartTime.ToTimeSpan() &&
                                t.Time.TimeOfDay <= roomSlots.EndTime.ToTimeSpan())
                    .Select(t => new TestPatientViewModel
                    {
                        PatientID = t.Appointment.PatientId,
                        PatientName = t.Appointment.Patient.FullName,
                        TestName = t.TestRecord.Test.Name,
                        TestRecordID = t.TestRecordId ?? 0,
                        TestId = t.TestRecord.TestId,
                        AssignedTime = t.Time
                    })
                    .ToListAsync();
                ongoingTestsByRoom[roomSlots.RoomId] = ongoingTestsInRoom;
            }
            return View(ongoingTestsByRoom);
        }
    }
}
