using HospitalManagement.Data;
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
            test.CompletedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Lưu kết quả xét nghiệm thành công.";
            return RedirectToAction("TestListForDoctor");
        }

        [HttpGet]
        [Authorize(Roles = "TestDoctor")]
        public async Task<IActionResult> ViewOngoingTest()
        {
            int doctorId = int.Parse(User.FindFirst("DoctorID")?.Value ?? "0");

            // Lấy RoomId hiện tại từ lịch trực
            var now = DateTime.Now;
            var today = DateOnly.FromDateTime(now);
            var currentTime = TimeOnly.FromDateTime(now);

            var roomId = await _context.Schedules
                            .Include(s => s.Slot)
                            .Where(s => s.DoctorId == doctorId &&
                                        s.Day == today &&
                                        s.Slot.StartTime <= currentTime)
                            .OrderByDescending(s => s.Slot.StartTime)
                            .Select(s => s.RoomId)
                            .FirstOrDefaultAsync();


            if (roomId == 0)
            {
                TempData["error"] = "Bạn không có lịch làm xét nghiệm trong ngày này.";
                return RedirectToAction("Index", "Home");
            }

            // Lấy các test đang ở trạng thái Ongoing trong phòng
            var ongoingTests = await _context.Trackings
                .Include(t => t.TestRecord).ThenInclude(tl => tl.Test)
                .Include(t => t.Appointment).ThenInclude(a => a.Patient)
                .Where(t => t.RoomId == roomId &&
                            t.TestRecordId != null &&
                            t.TestRecord.TestStatus == "Ongoing")
                .Select(t => new TestPatientViewModel
                {
                    PatientID = t.Appointment.PatientId,
                    PatientName = t.Appointment.Patient.FullName,
                    TestName = t.TestRecord.Test.Name,
                    TestRecordID = t.TestRecordId ?? 0,
                    TestId = t.TestRecord.TestId
                })
                .ToListAsync();
            Room room = await _roomRepo.GetRoomById(roomId);
            ViewBag.RoomNow = room?.RoomName ?? "Không xác định";
            return View(ongoingTests);
        }
    }
}
