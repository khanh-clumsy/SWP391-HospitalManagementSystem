using HospitalManagement.Data;
using HospitalManagement.Models;
using HospitalManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace HospitalManagement.Controllers
{
    public class PatientController : Controller
    {
        private readonly HospitalManagementContext _context;

        public PatientController(HospitalManagementContext context)
        {
            _context = context;
        }

        public IActionResult ViewDoctors()
        {
            return View();
        }

        public IActionResult DoctorDetail()
        {
            return View();
        }

        [HttpGet]
        public IActionResult RequestConsultant()
        {
            var userJson = HttpContext.Session.GetString("UserSession");

            if (string.IsNullOrEmpty(userJson))
            {
                return RedirectToAction("Login", "Auth");
            }

            var user = JsonConvert.DeserializeObject<Account>(userJson);
            if (user == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var model = new RequestConsultantViewModel
            {
                Name = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult RequestConsultant(RequestConsultantViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var userJson = HttpContext.Session.GetString("UserSession");
            if (string.IsNullOrEmpty(userJson))
            {
                return RedirectToAction("Login", "Auth");
            }

            var user = JsonConvert.DeserializeObject<Account>(userJson);
            if (user == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var patient = _context.Patients.FirstOrDefault(p => p.AccountId == user.AccountId);

            var consultant = new Consultant
            {
                PatientId = patient.PatientId,
                RequestedPersonType = model.ConsultantType,
                Description = model.Note,
                RequestedDate = DateOnly.FromDateTime(DateTime.Now),
                Status = "Pending",
                ServiceId = model.SelectedServiceId
            };

            _context.Consultants.Add(consultant);
            _context.SaveChanges();
            TempData["SuccessMessage"] = $"Request successfully with Consultant ID = {consultant.ConsultantId}!";
            return RedirectToAction("ViewConsultations");
        }


        [HttpGet]
        public async Task<IActionResult> BookingAppointment()
        {
            var userJson = HttpContext.Session.GetString("UserSession");

            if (string.IsNullOrEmpty(userJson))
            {
                return RedirectToAction("Login", "Auth");
            }

            var user = JsonConvert.DeserializeObject<Account>(userJson);
            if (user == null)
            {
                return RedirectToAction("Login", "Auth");
            }
            var doctors = await _context.Doctors
           .Include(d => d.Account)
           .Select(d => new SelectListItem
           {
               Value = d.DoctorId.ToString(),
               Text = d.Account.FullName
           })
           .ToListAsync();

            // Lấy danh sách slot từ DB
            var slots = await _context.Slots
                .Select(s => new SelectListItem
                {
                    Value = s.SlotId.ToString(),
                    Text = s.StartTime.ToString(@"hh\:mm") + " - " + s.EndTime.ToString(@"hh\:mm")
                })
                .ToListAsync();


            var model = new BookingApointment
            {
                Name = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                DoctorOptions = doctors,
                SlotOptions = slots,
                AppointmentDate = DateTime.Today
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BookingAppointment(BookingApointment model)
        {
            ModelState.Remove(nameof(model.DoctorOptions));
            ModelState.Remove(nameof(model.SlotOptions));
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                foreach (var error in errors)
                {
                    // Ghi log các lỗi
                    Console.WriteLine(error);
                }
                // Nạp lại danh sách dropdown khi trả view để dropdown hiển thị đúng
                model.DoctorOptions = await _context.Doctors
                    .Include(d => d.Account)
                    .Select(d => new SelectListItem
                    {
                        Value = d.DoctorId.ToString(),
                        Text = d.Account.FullName
                    })
                    .ToListAsync();

                model.SlotOptions = await _context.Slots
                    .Select(s => new SelectListItem
                    {
                        Value = s.SlotId.ToString(),
                        Text = s.StartTime.ToString(@"hh\:mm") + " - " + s.EndTime.ToString(@"hh\:mm")
                    })
                    .ToListAsync();
                return View(model);
            }
            var userJson = HttpContext.Session.GetString("UserSession");
            if (string.IsNullOrEmpty(userJson))
            {
                return RedirectToAction("Login", "Auth");
            }

            var user = JsonConvert.DeserializeObject<Account>(userJson);
            if (user == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var patient = _context.Patients.FirstOrDefault(p => p.AccountId == user.AccountId);

            var appointment = new Appointment
            {
                PatientId = patient.PatientId,
                DoctorId = model.SelectedDoctorId,
                Note = model.Note,
                SlotId = model.SelectedSlotId,
                ServiceId = model.SelectedServiceId,
                Date = DateOnly.FromDateTime(model.AppointmentDate),
                Status = "Pending",
            };

            _context.Appointments.Add(appointment);
            _context.SaveChanges();
            return RedirectToAction("ViewBookingAppointment");
        }

        public IActionResult ViewBookingAppointment(string searchName, string timeFilter, DateTime? dateFilter, string statusFilter)
        {
            var appointments = _context.Appointments
            .Include(a => a.Patient).ThenInclude(p => p.Account)
            .Include(a => a.Doctor).ThenInclude(d => d.Account)
            .Include(a => a.Slot)
            .AsQueryable();

            // Lọc theo thời gian slot

            if (!string.IsNullOrEmpty(timeFilter) && TimeOnly.TryParse(timeFilter, out var parsedTime))
            {
                appointments = appointments.Where(a => a.Slot.StartTime == parsedTime);
            }

            // Lọc theo ngày
            if (dateFilter.HasValue)
            {
                var filterDate = DateOnly.FromDateTime(dateFilter.Value);
                appointments = appointments.Where(a => a.Date == filterDate);
            }

            // Lọc theo trạng thái
            if (!string.IsNullOrEmpty(statusFilter))
            {
                appointments = appointments.Where(a => a.Status == statusFilter);
            }

            return View(appointments.ToList());
        }


        [HttpGet]
        public IActionResult ViewConsultations(DateTime? dateFilter, string statusFilter)
        {
            var statusOptions = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "All Status" },
                new SelectListItem { Value = "Confirmed", Text = "Confirmed" },
                new SelectListItem { Value = "Pending", Text = "Pending" },
                new SelectListItem { Value = "Cancelled", Text = "Cancelled" }
            };

            ViewBag.StatusOptions = new SelectList(statusOptions, "Value", "Text", statusFilter);
            var query = _context.Consultants
            .Include(c => c.Patient).ThenInclude(p => p.Account)
            .Include(c => c.Doctor).ThenInclude(d => d.Account)
            .Include(c => c.Service)
            .AsQueryable();

            if (dateFilter.HasValue)
            {
                var dateOnlyFilter = DateOnly.FromDateTime(dateFilter.Value);

                query = query.Where(c => c.RequestedDate.HasValue && c.RequestedDate.Value == dateOnlyFilter);
            }

            if (!string.IsNullOrEmpty(statusFilter))
            {
                query = query.Where(c => c.Status == statusFilter);
            }

            var model = new ViewConsultationsViewModel
            {
                DateFilter = dateFilter,
                StatusFilter = statusFilter,
                Consultants = query.ToList()
            };

            return View(model);
        }

        [Route("Patient/EditConsultant/{consultantId}")]
        [HttpGet]
        public IActionResult EditConsultant(int consultantId)
        {
            var consultant = _context.Consultants
            .Include(c => c.Patient)
            .ThenInclude(p => p.Account)
            .Include(c => c.Service)
            .Include(c => c.Doctor)
            .ThenInclude(d => d.Account)
            .FirstOrDefault(c => c.ConsultantId == consultantId);

            if (consultant == null)
            {
                TempData["ErrorMessage"] = "Err";
                return View("ViewConsultations");
            }
            var model = new EditConsultantViewModel
            {
                ConsultantID = consultantId,
                Name = consultant.Patient?.Account?.FullName,
                Email = consultant.Patient?.Account?.Email,
                PhoneNumber = consultant.Patient?.Account?.PhoneNumber,
                RequestedDate = consultant.RequestedDate,
                Consultants = consultant.RequestedPersonType,
                ServiceID = consultant.Service.ServiceId,
                Description = consultant.Description,
            };
            return View(model);
        }

        [Route("Patient/EditConsultant/{consultantId}")]
        [HttpPost]
        public IActionResult EditConsultant(int consultantId, EditConsultantViewModel model)
        {

            if (consultantId != model.ConsultantID)
            {
                return BadRequest();
            }

            var consultant = _context.Consultants
                .FirstOrDefault(c => c.ConsultantId == consultantId);

            if (consultant == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            consultant.RequestedPersonType = model.Consultants;
            consultant.ServiceId = (int)model.ServiceID;
            consultant.Description = model.Description;

            _context.SaveChanges();
            TempData["SuccessMessage"] = $"Update successfully with ID = {consultantId}";
            return RedirectToAction("ViewConsultations");
        }



        [HttpPost]
        public IActionResult DeleteConsultant(int consultantId)
        {
            var consultant = _context.Consultants.Find(consultantId);
            if (consultant == null)
            {
                return NotFound();
            }

            try
            {
                _context.Consultants.Remove(consultant);
                _context.SaveChanges();
                TempData["SuccessMessage"] = $"Consultant deleted with ID = {consultantId} successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error deleting consultant: " + ex.Message;
            }

            return RedirectToAction("ViewConsultations");
        }


    }

}
