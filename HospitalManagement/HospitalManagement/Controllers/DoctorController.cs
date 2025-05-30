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
    public class DoctorController : Controller
    {
        private readonly PasswordHasher<Doctor> _passwordHasher;

        public DoctorController(HospitalManagementContext context)
        {
            _passwordHasher = new PasswordHasher<Doctor>();
        }

        [HttpGet]
        public IActionResult ViewProfile()
        {
            // Load profile data from sesion
            var userJson = HttpContext.Session.GetString("DoctorSession");

            if (string.IsNullOrEmpty(userJson))
            {
                return RedirectToAction("Login", "Auth");
            }

            var user = JsonConvert.DeserializeObject<Doctor >(userJson);
            return View(user);
        }
        [HttpGet]
        public IActionResult UpdateProfile()
        {
            // Load profile data to edit
            var userJson = HttpContext.Session.GetString("DoctorSession");


            if (string.IsNullOrEmpty(userJson))
            {
                return RedirectToAction("Login", "Auth");
            }

            var user = JsonConvert.DeserializeObject<Doctor>(userJson);
            return View(user);
        }


        [HttpGet]
        public IActionResult ChangePassword()
        {
            // Load profile data from sesion
            var userJson = HttpContext.Session.GetString("DoctorSession");

            if (string.IsNullOrEmpty(userJson))
            {
                return RedirectToAction("Login", "Auth");
            }

            var user = JsonConvert.DeserializeObject<Doctor>(userJson);
            return View();
        }


        [HttpPost]
        public IActionResult ChangePassword(ChangePass model)
        {
            var userJson = HttpContext.Session.GetString("DoctorSession");
            if (string.IsNullOrEmpty(userJson)) return RedirectToAction("Login", "Auth");
            var user = JsonConvert.DeserializeObject<Doctor>(userJson);

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
                var dbUser = context.Doctors.FirstOrDefault(u => u.DoctorId == user.DoctorId);
                if (dbUser != null)
                {
                    dbUser.PasswordHash = _passwordHasher.HashPassword(null, model.NewPassword);
                    context.SaveChanges();
                    HttpContext.Session.SetString("DoctorSession", JsonConvert.SerializeObject(dbUser));

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
            var userJson = HttpContext.Session.GetString("DoctorSession");
            if (string.IsNullOrEmpty(userJson)) return RedirectToAction("Login", "Auth");

            // get user from session
            var user = JsonConvert.DeserializeObject<Doctor>(userJson);


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
                    var dbUser = context.Doctors.FirstOrDefault(u => u.DoctorId == user.DoctorId);
                    if (dbUser != null)
                    {
                        dbUser.ProfileImage = user.ProfileImage;
                        context.SaveChanges();
                    }
                }

                // Cập nhật lại session
                HttpContext.Session.SetString("DoctorSession", JsonConvert.SerializeObject(user));
                TempData["success"] = "Update successful!";
                return RedirectToAction("UpdateProfile");

            }

            // do nothing
            TempData["success"] = null;
            return RedirectToAction("UpdateProfile");
        }
        [HttpPost]
        public IActionResult UpdateProfile(Doctor model)
        {
            // check login
            var userJson = HttpContext.Session.GetString("DoctorSession");
            if (string.IsNullOrEmpty(userJson))
            {
                return RedirectToAction("Login", "Auth");
            }

            // get user
            var sessionUser = JsonConvert.DeserializeObject<Doctor>(userJson);

            using (var context = new HospitalManagementContext())
            {
                var curUser = context.Doctors.FirstOrDefault(u => u.DoctorId == sessionUser.DoctorId);
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
                    var phoneOwner = context.Doctors.FirstOrDefault(u => u.PhoneNumber == model.PhoneNumber);

                    if (phoneOwner != null && phoneOwner.DoctorId != curUser.DoctorId)
                    {
                        TempData["error"] = "This phone number was used before.";
                        return RedirectToAction("UpdateProfile");
                    }


                    // update info user
                    curUser.FullName = model.FullName;
                    curUser.Gender = model.Gender;
                    curUser.PhoneNumber = model.PhoneNumber;

                    // luu lai vao database
                    context.SaveChanges();

                    // Cập nhật lại session
                    sessionUser.FullName = curUser.FullName;
                    sessionUser.Gender = curUser.Gender;
                    sessionUser.PhoneNumber = curUser.PhoneNumber;
                    HttpContext.Session.SetString("DoctorSession", JsonConvert.SerializeObject(sessionUser));
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
