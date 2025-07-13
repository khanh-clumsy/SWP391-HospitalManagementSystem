using HospitalManagement.Data;
using HospitalManagement.Helpers;
using HospitalManagement.Models;
using HospitalManagement.Repositories;
using HospitalManagement.Services;
using HospitalManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static HospitalManagement.Helpers.AppConstants.Messages;

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
                TempData["error"] = AppConstants.Messages.Test.TestRecordNotFound;
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
            // Debug: Log the start of the method
            Console.WriteLine($"[DEBUG] InputTestResult started - TestRecordID: {model.TestRecordID}");
            
            int doctorId = int.Parse(User.FindFirst(AppConstants.ClaimTypes.DoctorId)?.Value ?? "0");
            Console.WriteLine($"[DEBUG] DoctorId: {doctorId}");
            
            var testRecord = await _context.TestRecords.FindAsync(model.TestRecordID);
            if (testRecord == null)
            {
                Console.WriteLine($"[DEBUG] TestRecord not found for ID: {model.TestRecordID}");
                TempData["error"] = AppConstants.Messages.Test.TestRecordNotFound;
                return NotFound();
            }
            
            Console.WriteLine($"[DEBUG] TestRecord found - Status: {testRecord.TestStatus}, AppointmentId: {testRecord.AppointmentId}");
            
            if (model.ResultFile == null)
            {
                Console.WriteLine($"[DEBUG] ResultFile is null");
                TempData["Error"] = AppConstants.Messages.Test.ResultFileRequired;
                return View(model);
            }

            // Validate file
            if (model.ResultFile != null)
            {
                try
                {
                    Console.WriteLine($"[DEBUG] Saving file: {model.ResultFile.FileName}, Size: {model.ResultFile.Length}");
                    var fileName = await FileService.SaveTestFileAsync(model.ResultFile, "TestResult");
                    model.ResultFileName = fileName;
                    Console.WriteLine($"[DEBUG] File saved successfully: {fileName}");
                }
                catch (InvalidOperationException ex)
                {
                    Console.WriteLine($"[DEBUG] File save error: {ex.Message}");
                    TempData["Error"] = ex.Message;
                    return View(model);
                }
            }
            
            // Add các trường vào DB
            Console.WriteLine($"[DEBUG] Updating TestRecord in database");
            testRecord.TestNote = model.Note;
            testRecord.Result = model.ResultFileName;
            testRecord.TestStatus = "Completed";
            testRecord.CompletedAt = DateTime.Now;
            testRecord.DoctorId = doctorId;
            
            await _context.SaveChangesAsync();
            Console.WriteLine($"[DEBUG] TestRecord updated successfully");
            
            // Kiểm tra nếu tất cả test trong batch đã Completed
            Console.WriteLine($"[DEBUG] Starting batch completion check");

            _ = Task.Run(async () =>
            {
                try
                {
                    var appointmentId = testRecord.AppointmentId;
                    Console.WriteLine($"[DEBUG] Checking batch for AppointmentId: {appointmentId}");

                    int? currentBatch = await BatchHelper.GetOpenBatchAsync(_context, appointmentId);
                    Console.WriteLine($"[DEBUG] Current batch: {currentBatch}");

                    if (currentBatch == null) 
                    {
                        Console.WriteLine($"[DEBUG] No open batch found");
                        return;
                    }

                    bool isBatchComplete = await _context.Trackings
                        .Where(t => t.TestRecord != null && t.AppointmentId == appointmentId && t.TrackingBatch == currentBatch && t.TestRecordId != null)
                        .AllAsync(t => t.TestRecord.TestStatus == AppConstants.TestStatus.Completed);

                    Console.WriteLine($"[DEBUG] Is batch complete: {isBatchComplete}");

                    if (isBatchComplete)
                    {
                        Console.WriteLine($"[DEBUG] Batch is complete, creating clinic tracking");
                        var appointment = await _context.Appointments
                            .Include(a => a.Doctor)
                            .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

                        if (appointment != null)
                        {
                            var doctor = appointment.Doctor;
                            Console.WriteLine($"[DEBUG] Found appointment with doctor: {doctor?.FullName}");

                            // Tìm phòng khám bác sĩ của cuộc hẹn đó
                            var clinicRoom = await _context.Trackings
                                .Where(t => t.Room.RoomType == AppConstants.RoomTypes.Clinic && t.Room.Status == "Active" && t.AppointmentId == appointmentId)
                                .Select(t => t.Room)
                                .Distinct()
                                .FirstOrDefaultAsync();

                            Console.WriteLine($"[DEBUG] Clinic room found: {clinicRoom?.RoomId}");

                            if (clinicRoom != null) 
                            {
                                int newBatch = await BatchHelper.GetOpenOrNewBatchAsync(_context, appointmentId);
                                Console.WriteLine($"[DEBUG] New batch created: {newBatch}");

                                var clinicTracking = new Models.Tracking
                                {
                                    AppointmentId = appointmentId,
                                    RoomId = clinicRoom.RoomId,
                                    Time = DateTime.Now,
                                    TestRecordId = null,
                                    TrackingBatch = newBatch
                                };

                                _context.Trackings.Add(clinicTracking);
                                await _context.SaveChangesAsync();
                                Console.WriteLine($"[DEBUG] Clinic tracking created successfully");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DEBUG] Error in batch completion task: {ex.Message}");
                }
            });

            TempData["Success"] = AppConstants.Messages.Test.SaveResultSuccess;
            Console.WriteLine($"[DEBUG] InputTestResult completed successfully");
            return RedirectToAction("ViewTestResult", "Test", new { id = testRecord.TestRecordId });
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
                                t.Time.Date == DateTime.Today
                                // && t.Time.TimeOfDay >= roomSlots.StartTime.ToTimeSpan() &&
                                //t.Time.TimeOfDay <= roomSlots.EndTime.ToTimeSpan()
                                )
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
