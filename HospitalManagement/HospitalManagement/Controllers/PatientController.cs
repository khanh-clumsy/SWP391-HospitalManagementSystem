
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
        private readonly HospitalManagementContext _context;



        public PatientController(HospitalManagementContext context)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<Patient>();
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
                var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/jpg", "image/webp" };
                if (!allowedTypes.Contains(photo.ContentType))
                {
                    TempData["error"] = "Không đúng địng dạng ảnh cho phép";
                    return RedirectToAction("UpdateProfile");
                }
                if (photo.Length > 2 * 1024 * 1024)
                {
                    TempData["error"] = "Kích thước file quá 2MB";
                    return RedirectToAction("UpdateProfile");

                }
                // Tạo tên file duy nhất để tránh trùng
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(photo.FileName);
                var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", fileName);

                // Lưu file vào wwwroot/uploads
                using (var stream = new FileStream(uploadPath, FileMode.Create))
                {
                    await photo.CopyToAsync(stream);
                }

                user.ProfileImage = fileName;

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
            TempData["success"] = "Không có ảnh được tải lên";
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
            var returnUrl = TempData["ReturnUrl"] as string;
            if (!string.IsNullOrEmpty(returnUrl))
            {
                return Redirect(returnUrl);
            }

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