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
            var testList = await _context.TestRecords
                .Include(t => t.Test)
                .Include(t => t.Appointment).ThenInclude(a => a.Patient)
                .FirstOrDefaultAsync(t => t.TestRecordId == testRecordID);

            if (testList == null)
            {
                TempData["error"] = "Không tìm thấy xét nghiệm.";
                return RedirectToAction("ViewOngoingTest", "TestPerform");
            }

            var model = new TestResultInputViewModel
            {
                TestRecordID = testList.TestRecordId,
                TestName = testList.Test.Name,
                PatientFullName = testList.Appointment.Patient.FullName,
                Gender = testList.Appointment.Patient.Gender,
                DOB = testList.Appointment.Patient.Dob ?? DateTime.MinValue
            };

            return View(model);
        }

        [Authorize(Roles = "TestDoctor")]
        [HttpPost]
        public async Task<IActionResult> InputTestResult(TestResultInputViewModel model)
        {
            int doctorId = int.Parse(User.FindFirst(AppConstants.ClaimTypes.DoctorId)?.Value ?? "0");
            var test = await _context.TestRecords.FindAsync(model.TestRecordID);
            if (test == null)
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
            test.TestNote = model.Note;
            test.Result = model.ResultFileName;
            test.TestStatus = "Completed";
            test.CreatedAt = DateTime.Now;
            test.DoctorId = doctorId;
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

            var roomList = await _roomRepo.GetRoomSlotInfosByDoctorAndDateAsync(doctorId, today); // nhiều phòng
            ViewBag.RoomSlotsToday = roomList;

            var allOngoingTests = new Dictionary<int, List<TestPatientViewModel>>();
            foreach (var room in roomList)
            {
                var tests = await _context.Trackings
                    .Include(t => t.TestRecord).ThenInclude(tr => tr.Test)
                    .Include(t => t.Appointment).ThenInclude(a => a.Patient)
                    .Where(t => t.RoomId == room.RoomId &&
                                t.TestRecordId != null && t.TestRecord != null &&
                                t.TestRecord.TestStatus == AppConstants.TestStatus.Ongoing &&
                                t.Time.Date == DateTime.Today &&
                                t.Time.TimeOfDay >= room.StartTime.ToTimeSpan() &&
                                t.Time.TimeOfDay <= room.EndTime.ToTimeSpan())
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
                allOngoingTests[room.RoomId] = tests;
            }
            return View(allOngoingTests);
        }
    }
}
