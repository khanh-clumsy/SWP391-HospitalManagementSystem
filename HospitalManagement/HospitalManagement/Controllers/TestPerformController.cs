using HospitalManagement.Data;
using HospitalManagement.Models;
using HospitalManagement.Repositories;
using HospitalManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Controllers
{
    public class 
        TestPerformController : Controller
    {
        private readonly HospitalManagementContext _context;
     

        public TestPerformController(HospitalManagementContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> InputTestResult(int id)
        {
            var testList = await _context.TestLists
                .Include(t => t.Test)
                .Include(t => t.Appointment).ThenInclude(a => a.Patient)
                .FirstOrDefaultAsync(t => t.TestListId == id);

            if (testList == null) return NotFound();

            var vm = new TestResultInputViewModel
            {
                TestListID = testList.TestListId,
                TestName = testList.Test.Name,
                PatientFullName = testList.Appointment.Patient.FullName,
                Gender = testList.Appointment.Patient.Gender,
                DOB = testList.Appointment.Patient.Dob ?? DateTime.MinValue
            };

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> InputTestResult(TestResultInputViewModel model)
        {

            var test = await _context.TestLists.FindAsync(model.TestListID);
            if (test == null)
            {
                TempData["Error"] = "Không tìm thấy xét nghiệm.";
                return NotFound();
            }
            if (model.ResultFile == null)
            {
                TempData["Error"] = "Chưa cập nhật kết quả";
                return View(model);
            }
            // Ghi chú bác sĩ
            test.TestNote = model.Note;

            // Validate file
            if (model.ResultFile != null)
            {
                var allowedTypes = new[] { "application/pdf", "image/jpeg", "image/png", "image/jpg" };
                var maxSize = 5 * 1024 * 1024; // 5MB

                if (!allowedTypes.Contains(model.ResultFile.ContentType.ToLower()))
                {
                    TempData["Error"] = "File không hợp lệ. Chỉ cho phép PDF và ảnh.";
                    return View(model);
                }

                if (model.ResultFile.Length > maxSize)
                {
                    TempData["Error"] = "Kích thước file vượt quá giới hạn (tối đa 5MB).";
                    return View(model);
                }

                var fileName = Guid.NewGuid() + Path.GetExtension(model.ResultFile.FileName);
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "testresults", fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ResultFile.CopyToAsync(stream);
                }

                test.Result = fileName;
            }

            test.TestStatus = "Completed";
            test.CreatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Lưu kết quả xét nghiệm thành công.";
            return RedirectToAction("TestListForDoctor"); 
        }
        [HttpGet]
        [Authorize(Roles = "TestDoctor")]
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
                            s.Slot.StartTime <= currentTime &&
                            s.Slot.EndTime >= currentTime)
                .Select(s => s.RoomId)
                .FirstOrDefaultAsync();

            if (roomId == null)
                return View(new List<TestPatientViewModel>()); 

            // Lấy các test đang ở trạng thái Ongoing trong phòng
            var patients = await _context.Trackings
                .Include(t => t.TestList).ThenInclude(tl => tl.Test)
                .Include(t => t.Appointment).ThenInclude(a => a.Patient)
                .Where(t => t.RoomId == roomId &&
                            t.TestList != null &&
                            t.TestList.TestStatus == "Ongoing")
                .Select(t => new TestPatientViewModel
                {
                    PatientId = t.Appointment.PatientId,
                    PatientName = t.Appointment.Patient.FullName,
                    TestName = t.TestList.Test.Name,
                    TestListId = t.TestListId.Value
                })
                .ToListAsync();

            return View(patients);
        }



    }
}
