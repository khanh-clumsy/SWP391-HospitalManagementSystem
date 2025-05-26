using HospitalManagement.Data;
using HospitalManagement.Models;
using HospitalManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;

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
            int affected = _context.SaveChanges();
            return RedirectToAction("ViewConsultations");
        }

        public IActionResult Appointment()

        [HttpGet]
        public async Task<IActionResult> BookingAppoinment()
        {
            var userJson = HttpContext.Session.GetString("UserSession");

            //if (string.IsNullOrEmpty(userJson))
            //{
            //    return RedirectToAction("Login", "Auth");
            //}

             var user = JsonConvert.DeserializeObject<Account>(userJson);
            //if (user == null)
            //{
            //    return RedirectToAction("Login", "Auth");
            //}
            var doctors = await _context.Doctors
            .Include(d => d.Account)
             
            .Select(d => new
            {
                Id = d.DoctorId,
                Name = d.Account.FullName
            })
            .ToListAsync();

            // Tạo SelectList cho dropdown
            ViewBag.DoctorList = new SelectList(doctors, "Id", "Name");
            var model = new BookingApointment
            {
                Name = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult BookingAppointment(BookingApointment model)
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

            var appointment = new Appointment
            {
                PatientId = patient.PatientId,
               
               
                Status = "Pending",
                ServiceId = model.SelectedServiceId
            };

            _context.Appointments.Add(appointment);
            _context.SaveChanges();

            return RedirectToAction("BookingSuccess");
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
