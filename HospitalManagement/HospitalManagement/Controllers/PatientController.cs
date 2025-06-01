using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using HospitalManagement.Models;
using HospitalManagement.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;
using HospitalManagement.ViewModels;

namespace HospitalManagement.Controllers
{
    public class PatientController : Controller
    {
        private readonly PasswordHasher<Patient> _passwordHasher;

        public PatientController(HospitalManagementContext context)
        {
            _passwordHasher = new PasswordHasher<Patient>();
        }


        [HttpGet]

        public IActionResult ViewProfile()
        {
            // Load profile data from sesion
            var userJson = HttpContext.Session.GetString("PatientSession");

            if (string.IsNullOrEmpty(userJson))
            {
                return RedirectToAction("Login", "Auth");
            }

            var doctors = await _context.Doctors
           .Select(d => new SelectListItem
           {
               Value = d.DoctorId.ToString(),
               Text = d.FullName
           })
           .ToListAsync();

            // Lấy danh sách slot từ DB
            var slots = await _context.Slots
                .Select(s => new SelectListItem
            var user = JsonConvert.DeserializeObject<Patient>(userJson);
            return View(user);
        }
        [HttpGet]
        public IActionResult UpdateProfile()
        {
            // Load profile data to edit
            var userJson = HttpContext.Session.GetString("PatientSession");

            if (string.IsNullOrEmpty(userJson))
            {
                return RedirectToAction("Login", "Auth");
            }

            var user = JsonConvert.DeserializeObject<Patient>(userJson);
            return View(user);
        }


        [HttpGet]
        public IActionResult ChangePassword()
        {
            // Load profile data from sesion
            var userJson = HttpContext.Session.GetString("PatientSession");

            if (string.IsNullOrEmpty(userJson))
            {
                return RedirectToAction("Login", "Auth");
            }

            var user = JsonConvert.DeserializeObject<Patient>(userJson);
            return View();
        }


        [HttpPost]
        public IActionResult ChangePassword(ChangePass model)
        {
            var userJson = HttpContext.Session.GetString("PatientSession");
            if (string.IsNullOrEmpty(userJson)) return RedirectToAction("Login", "Auth");
            var user = JsonConvert.DeserializeObject<Patient>(userJson);

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
            using (var context = new HospitalManagementContext())
            {
                var dbUser = context.Patients.FirstOrDefault(u => u.PatientId == user.PatientId);
                if (dbUser != null)
                {
                    dbUser.PasswordHash = _passwordHasher.HashPassword(null, model.NewPassword);
                    context.SaveChanges();
                    HttpContext.Session.SetString("PatientSession", JsonConvert.SerializeObject(dbUser));

                }
            }

            // Cập nhật lại session
            TempData["success"] = "Change password successful!";
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> UploadPhoto(IFormFile photo)
        {
            // check login
            var userJson = HttpContext.Session.GetString("PatientSession");
            if (string.IsNullOrEmpty(userJson)) return RedirectToAction("Login", "Auth");

            // get user from session
            var user = JsonConvert.DeserializeObject<Patient>(userJson);


            if (photo != null && photo.Length > 0)
            {
                // convert img -> Byte ->  Base64String
                using var ms = new MemoryStream();
                await photo.CopyToAsync(ms);
                var imageBytes = ms.ToArray();
                user.ProfileImage = Convert.ToBase64String(imageBytes);

                // add in database
                using (var context = new HospitalManagementContext())
                {
                    var dbUser = context.Patients.FirstOrDefault(u => u.PatientId == user.PatientId);
                    if (dbUser != null)
                    {
                        dbUser.ProfileImage = user.ProfileImage;
                        context.SaveChanges();
                    }
                }

                // Cập nhật lại session
                HttpContext.Session.SetString("PatientSession", JsonConvert.SerializeObject(user));
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
            var userJson = HttpContext.Session.GetString("PatientSession");
            if (string.IsNullOrEmpty(userJson))
            {
                return RedirectToAction("Login", "Auth");
            }

            // get user
            var sessionUser = JsonConvert.DeserializeObject<Patient>(userJson);

            using (var context = new HospitalManagementContext())
            {
                var curUser = context.Patients.FirstOrDefault(u => u.PatientId == sessionUser.PatientId);
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
                    sessionUser.FullName = curUser.FullName = model.FullName;
                    sessionUser.Gender =  curUser.Gender = model.Gender;
                    sessionUser.PhoneNumber = curUser.PhoneNumber = model.PhoneNumber;
                    sessionUser.Dob = curUser.Dob = model.Dob;
                    sessionUser.Address = curUser.Address = model.Address;
                    sessionUser.HealthInsurance = curUser.HealthInsurance = model.HealthInsurance;
                    sessionUser.BloodGroup = curUser.BloodGroup = model.BloodGroup;

                    // luu lai user vao database
                    context.SaveChanges();

                    // reset session
                    HttpContext.Session.SetString("PatientSession", JsonConvert.SerializeObject(sessionUser));
                }
            }
            TempData["success"] = "Update successful!";

            return RedirectToAction("UpdateProfile");
        }

        [HttpGet]
        public IActionResult Logout()
        {
            // Xóa toàn bộ session
            HttpContext.Session.Clear();
            TempData["success"] = "Logout successful!";

            return RedirectToAction("Index", "Home");
        }
    }
}
