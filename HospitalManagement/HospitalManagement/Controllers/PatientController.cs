
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using HospitalManagement.Models;
using HospitalManagement.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;
using HospitalManagement.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using HospitalManagement.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
namespace HospitalManagement.Controllers
{
    [Authorize(Roles = "Patient")]
    public class PatientController : Controller
    {
        private readonly PasswordHasher<Patient> _passwordHasher;
        private readonly IBookingAppointmentRepository _doctorRepo;
        private readonly IBookingAppointmentRepository _slotRepo;
        private readonly IBookingAppointmentRepository _patientRepo;
        private readonly IBookingAppointmentRepository _appointmentRepo;
        private readonly HospitalManagementContext _context;



        public PatientController(HospitalManagementContext context, IBookingAppointmentRepository doctorRepo,
            IBookingAppointmentRepository slotRepo,
            IBookingAppointmentRepository patientRepo,
            IBookingAppointmentRepository appointmentRepo)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<Patient>();
            _doctorRepo = doctorRepo;
            _slotRepo = slotRepo;
            _patientRepo = patientRepo;
            _appointmentRepo = appointmentRepo;
        }


        [HttpGet]
        public IActionResult ViewProfile()
        {
            // Lấy PatientId từ Claims
            var patientIdClaim = User.FindFirst("PatientID")?.Value;
            if (patientIdClaim == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            int patientId = int.Parse(patientIdClaim);

            // Lấy thông tin từ DB
            var context = new HospitalManagementContext();
            var user = context.Patients.FirstOrDefault(p => p.PatientId == patientId);
            if (user == null)
            {
                return RedirectToAction("Login", "Auth");
            }
            return View(user);
        }
        [HttpGet]
        public IActionResult UpdateProfile()
        {
            // Lấy PatientId từ Claims
            var patientIdClaim = User.FindFirst("PatientID")?.Value;
            if (patientIdClaim == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            int patientId = int.Parse(patientIdClaim);

            // Lấy thông tin từ DB
            var context = new HospitalManagementContext();
            var user = context.Patients.FirstOrDefault(p => p.PatientId == patientId);
            if (user == null)
            {
                return RedirectToAction("Login", "Auth");
            }
            return View(user);
        }


        [HttpGet]
        public IActionResult ChangePassword()
        {
            // Lấy PatientId từ Claims
            var patientIdClaim = User.FindFirst("PatientID")?.Value;
            if (patientIdClaim == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            int patientId = int.Parse(patientIdClaim);

            // Lấy thông tin từ DB
            var context = new HospitalManagementContext();
            var user = context.Patients.FirstOrDefault(p => p.PatientId == patientId);
            if (user == null)
            {
                return RedirectToAction("Login", "Auth");
            }
            return View();
        }


        [HttpPost]
        public IActionResult ChangePassword(ChangePass model)
        {
            // Lấy PatientId từ Claims
            var patientIdClaim = User.FindFirst("PatientID")?.Value;
            if (patientIdClaim == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            int patientId = int.Parse(patientIdClaim);

            // Lấy thông tin từ DB
            var context = new HospitalManagementContext();
            var user = context.Patients.FirstOrDefault(p => p.PatientId == patientId);
            if (user == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (_passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.OldPassword) != PasswordVerificationResult.Success)
            {
                TempData["error"] = "Current password not match";
                return View();
            }

            if (model.NewPassword != model.ConfirmPassword)
            {
                TempData["error"] = "2 new passwords not match";
                return View();
            }

            // Cập nhật trong DB
            var dbUser = context.Patients.FirstOrDefault(u => u.PatientId == user.PatientId);
            if (dbUser != null)
            {
                dbUser.PasswordHash = _passwordHasher.HashPassword(null, model.NewPassword);
                context.SaveChanges();
                HttpContext.Session.SetString("PatientSession", JsonConvert.SerializeObject(dbUser));

            }

            TempData["success"] = "Change password successful!";
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> UploadPhoto(IFormFile photo)
        {
            // check login
            // Lấy PatientId từ Claims
            var patientIdClaim = User.FindFirst("PatientID")?.Value;
            if (patientIdClaim == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            int patientId = int.Parse(patientIdClaim);

            // Lấy thông tin từ DB
            var context = new HospitalManagementContext();
            var user = context.Patients.FirstOrDefault(p => p.PatientId == patientId);
            if (user == null)
            {
                return RedirectToAction("Login", "Auth");
            }


            if (photo != null && photo.Length > 0)
            {
                // convert img -> Byte ->  Base64String
                using var ms = new MemoryStream();
                await photo.CopyToAsync(ms);
                var imageBytes = ms.ToArray();
                user.ProfileImage = Convert.ToBase64String(imageBytes);

                // add in database

                var dbUser = context.Patients.FirstOrDefault(u => u.PatientId == user.PatientId);
                if (dbUser != null)
                {
                    dbUser.ProfileImage = user.ProfileImage;
                    context.SaveChanges();
                }

                // Cập nhật lại session
                //HttpContext.Session.SetString("PatientSession", JsonConvert.SerializeObject(user));
                TempData["success"] = "Update successful!";
                return RedirectToAction("UpdateProfile");

            }

            // do nothing
            TempData["success"] = null;
            return RedirectToAction("UpdateProfile");
        }
        [HttpPost]
        public IActionResult UpdateProfile(Patient model)
        {
            // check login
            // Lấy PatientId từ Claims
            var patientIdClaim = User.FindFirst("PatientID")?.Value;
            if (patientIdClaim == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            int patientId = int.Parse(patientIdClaim);

            // Lấy thông tin từ DB
            var context = new HospitalManagementContext();
            var user = context.Patients.FirstOrDefault(p => p.PatientId == patientId);
            if (user == null)
            {
                return RedirectToAction("Login", "Auth");
            }


            var curUser = context.Patients.FirstOrDefault(u => u.PatientId == user.PatientId);
            if (curUser != null)
            {
                // check if phone start with 0 and 9 digits back
                if (model.PhoneNumber == null)
                {
                    TempData["error"] = "Phone number is invalid.";
                    return RedirectToAction("UpdateProfile");
                }

                if (model.PhoneNumber[0] != '0' || model.PhoneNumber.Length != 10)
                {
                    TempData["error"] = "Phone number is invalid.";
                    return RedirectToAction("UpdateProfile");
                }

                //check if phone is non - number
                foreach (char u in model.PhoneNumber) if (u < '0' || u > '9')
                    {
                        TempData["error"] = "Phone number is invalid.";
                        return RedirectToAction("UpdateProfile");
                    }

                // check phone is used(not this user)
                var phoneOwner = context.Patients.FirstOrDefault(u => u.PhoneNumber == model.PhoneNumber);

                if (phoneOwner != null && phoneOwner.PatientId != curUser.PatientId)
                {
                    TempData["error"] = "This phone number was used before.";
                    return RedirectToAction("UpdateProfile");
                }


                // update info user and session
                user.FullName = curUser.FullName = model.FullName;
                user.Gender = curUser.Gender = model.Gender;
                user.PhoneNumber = curUser.PhoneNumber = model.PhoneNumber;
                user.Dob = curUser.Dob = model.Dob;
                user.Address = curUser.Address = model.Address;
                user.HealthInsurance = curUser.HealthInsurance = model.HealthInsurance;
                user.BloodGroup = curUser.BloodGroup = model.BloodGroup;

                // luu lai user vao database
                context.SaveChanges();

                //// reset session
                //HttpContext.Session.SetString("PatientSession", JsonConvert.SerializeObject(sessionUser));
            }

            TempData["success"] = "Update successful!";

            return RedirectToAction("UpdateProfile");
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            // Đăng xuất người dùng khỏi Identity (cookie authentication)
            await HttpContext.SignOutAsync();

            TempData["success"] = "Logout successful!";
            return RedirectToAction("Index", "Home");
        }
        [HttpGet]
        public async Task<IActionResult> BookingAppointment(int? doctorId)
        {
            var userJson = HttpContext.Session.GetString("PatientSession");
            if (string.IsNullOrEmpty(userJson)) return RedirectToAction("Login", "Auth");

            var user = JsonConvert.DeserializeObject<Patient>(userJson);
            if (user == null) return RedirectToAction("Login", "Auth");

            var model = new BookingApointment
            {
                Name = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                DoctorOptions = await _doctorRepo.GetDoctorSelectListAsync(),
                SlotOptions = await _slotRepo.GetSlotSelectListAsync(),
                AppointmentDate = DateTime.Today,
                SelectedDoctorId = doctorId ?? 0
            };

            return View(model);
        }

        [Authorize(Roles = "Patient")]
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
                    Console.WriteLine(error);

                model.DoctorOptions = await _doctorRepo.GetDoctorSelectListAsync();
                model.SlotOptions = await _slotRepo.GetSlotSelectListAsync();
                return View(model);
            }

            // Lấy PatientId từ Claim
            var patientIdClaim = User.Claims.FirstOrDefault(c => c.Type == "PatientId")?.Value;

            if (string.IsNullOrEmpty(patientIdClaim))
                return RedirectToAction("Login", "Auth");

            if (!int.TryParse(patientIdClaim, out int patientId))
                return BadRequest("Invalid patient ID");

            var patient = await _patientRepo.GetPatientByPatientIdAsync(patientId);
            if (patient == null)
                return BadRequest("Patient not found");

            var appointment = new Appointment
            {
                PatientId = patient.PatientId,
                DoctorId = model.SelectedDoctorId,
                Note = model.Note,
                SlotId = model.SelectedSlotId,
                ServiceId = model.SelectedServiceId,
                Date = DateOnly.FromDateTime(model.AppointmentDate),
                Status = "Pending"
            };

            await _appointmentRepo.AddAppointmentAsync(appointment);
            await _appointmentRepo.SaveChangesAsync();

            return RedirectToAction("ViewBookingAppointment");
        }
    }
}