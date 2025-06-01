using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using HospitalManagement.Models;
using HospitalManagement.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;
using HospitalManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;

namespace HospitalManagement.Controllers
{
    [Authorize(Roles = "Doctor")]
    public class DoctorController : Controller
    {
        private readonly PasswordHasher<Doctor> _passwordHasher;
        private readonly HospitalManagementContext _context;

        public DoctorController(HospitalManagementContext context)
        {
            _passwordHasher = new PasswordHasher<Doctor>();
            _context = context;
        }

        [HttpGet]
        public IActionResult ViewProfile()
        {
            // Load profile data from sesion
            // Lấy DoctorID từ Claims
            var doctorIdClaim = User.FindFirst("DoctorID")?.Value;
            if (doctorIdClaim == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            int doctorID = int.Parse(doctorIdClaim);

            // Lấy thông tin từ DB
            var context = new HospitalManagementContext();
            var user = context.Doctors.FirstOrDefault(p => p.DoctorId == doctorID);
            if (user == null)
            {
                return RedirectToAction("Login", "Auth");
            }
            return View(user);
        }
        [HttpGet]
        public IActionResult UpdateProfile()
        {


            var doctorIdClaim = User.FindFirst("DoctorID")?.Value;
            if (doctorIdClaim == null)

            {
                return RedirectToAction("Login", "Auth");
            }

            int doctorID = int.Parse(doctorIdClaim);

            // Lấy thông tin từ DB
            var context = new HospitalManagementContext();
            var user = context.Doctors.FirstOrDefault(p => p.DoctorId == doctorID);
            if (user == null)
            {
                return RedirectToAction("Login", "Auth");
            }
            return View(user);
        }
        public IActionResult Dashboard()
        {
            return View();
        }
        public IActionResult Appointments()
        {
            return View();
        }
        public IActionResult Schedule()
        {
            return View();
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            // Lấy DoctorID từ Claims
            var doctorIdClaim = User.FindFirst("DoctorID")?.Value;
            if (doctorIdClaim == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            int doctorID = int.Parse(doctorIdClaim);

            // Lấy thông tin từ DB
            var context = new HospitalManagementContext();
            var user = context.Doctors.FirstOrDefault(p => p.DoctorId == doctorID);
            if (user == null)
            {
                return RedirectToAction("Login", "Auth");
            }
            return View();
        }


        [HttpPost]
        public IActionResult ChangePassword(ChangePass model)
        {
            // Lấy DoctorID từ Claims
            var doctorIdClaim = User.FindFirst("DoctorID")?.Value;
            if (doctorIdClaim == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            int doctorID = int.Parse(doctorIdClaim);

            // Lấy thông tin từ DB
            var context = new HospitalManagementContext();
            var user = context.Doctors.FirstOrDefault(p => p.DoctorId == doctorID);
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

            var dbUser = _context.Doctors.FirstOrDefault(u => u.DoctorId == user.DoctorId);
            if (dbUser != null)
            {
                dbUser.PasswordHash = _passwordHasher.HashPassword(null, model.NewPassword);
                _context.SaveChanges();
                HttpContext.Session.SetString("DoctorSession", JsonConvert.SerializeObject(dbUser));

            }


            // Cập nhật lại session
            TempData["success"] = "Change password successful!";
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> UploadPhoto(IFormFile photo)
        {
            // check login
            // Lấy DoctorID từ Claims
            var doctorIdClaim = User.FindFirst("DoctorID")?.Value;
            if (doctorIdClaim == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            int doctorID = int.Parse(doctorIdClaim);

            // Lấy thông tin từ DB
            var user = _context.Doctors.FirstOrDefault(p => p.DoctorId == doctorID);
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

                var dbUser = _context.Doctors.FirstOrDefault(u => u.DoctorId == user.DoctorId);
                if (dbUser != null)
                {
                    dbUser.ProfileImage = user.ProfileImage;
                    _context.SaveChanges();
                }


                // Cập nhật lại session
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
            var doctorIdClaim = User.FindFirst("DoctorID")?.Value;
            if (doctorIdClaim == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            int doctorID = int.Parse(doctorIdClaim);

            // Lấy thông tin từ DB
            var user = _context.Doctors.FirstOrDefault(p => p.DoctorId == doctorID);
            if (user == null)
            {
                return RedirectToAction("Login", "Auth");
            }



            var curUser = _context.Doctors.FirstOrDefault(u => u.DoctorId == user.DoctorId);
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
                var phoneOwner = _context.Doctors.FirstOrDefault(u => u.PhoneNumber == model.PhoneNumber);

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
                _context.SaveChanges();

                //// Cập nhật lại session
                //user.FullName = curUser.FullName;
                //user.Gender = curUser.Gender;
                //user.PhoneNumber = curUser.PhoneNumber;
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
    }
}
