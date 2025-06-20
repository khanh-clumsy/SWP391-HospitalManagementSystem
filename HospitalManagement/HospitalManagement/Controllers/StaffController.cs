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
    [Authorize(Roles = "Admin,Sales,Cashier")]
    public class StaffController : Controller
    {
        private readonly PasswordHasher<Staff> _passwordHasher;

        public StaffController(HospitalManagementContext context)
        {
            _passwordHasher = new PasswordHasher<Staff>();
        }

        [HttpGet]
        public IActionResult ViewProfile()
        {
            // Lấy StaffId từ Claims
            var staffIdClaim = User.FindFirst("staffID")?.Value;
            if (staffIdClaim == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            int staffId = int.Parse(staffIdClaim);

            // Lấy thông tin từ DB
            var context = new HospitalManagementContext();
            var user = context.Staff.FirstOrDefault(p => p.StaffId == staffId);
            if (user == null)
            {
                return RedirectToAction("Login", "Auth");
            }
            return View(user);
        }
        [HttpGet]
        public IActionResult UpdateProfile()
        {
            // Lấy staffId từ Claims
            var staffIdClaim = User.FindFirst("StaffID")?.Value;
            if (staffIdClaim == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            int staffId = int.Parse(staffIdClaim);

            // Lấy thông tin từ DB
            var context = new HospitalManagementContext();
            var user = context.Staff.FirstOrDefault(p => p.StaffId == staffId);
            if (user == null)
            {
                return RedirectToAction("Login", "Auth");
            }
            return View(user);
        }


        [HttpGet]
        public IActionResult ChangePassword()
        {
            // Lấy staffId từ Claims
            var staffIdClaim = User.FindFirst("StaffID")?.Value;
            if (staffIdClaim == null)
            {
                return RedirectToAction("Login", "Auth");
            }   

            int StaffId = int.Parse(staffIdClaim);

            // Lấy thông tin từ DB
            var context = new HospitalManagementContext();
            var user = context.Staff.FirstOrDefault(p => p.StaffId == StaffId);
            if (user == null)
            {
                return RedirectToAction("Login", "Auth");
            }
            return View();
        }


        [HttpPost]
        public IActionResult ChangePassword(ChangePass model)
        {
            // Lấy staffId từ Claims
            var staffIdClaim = User.FindFirst("StaffID")?.Value;
            if (staffIdClaim == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            int staffId = int.Parse(staffIdClaim);

            // Lấy thông tin từ DB
            var context = new HospitalManagementContext();
            var user = context.Staff.FirstOrDefault(p => p.StaffId == staffId);
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
            var dbUser = context.Staff.FirstOrDefault(u => u.StaffId == user.StaffId);
            if (dbUser != null)
            {
                dbUser.PasswordHash = _passwordHasher.HashPassword(null, model.NewPassword);
                context.SaveChanges();
                HttpContext.Session.SetString("StaffSession", JsonConvert.SerializeObject(dbUser));

            }

            TempData["success"] = "Change password successful!";
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> UploadPhoto(IFormFile photo)
        {
            // check login
            // Lấy StaffId từ Claims
            var staffIdClaim = User.FindFirst("StaffID")?.Value;
            if (staffIdClaim == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            int staffId = int.Parse(staffIdClaim);

            // Lấy thông tin từ DB
            var context = new HospitalManagementContext();
            var user = context.Staff.FirstOrDefault(p => p.StaffId == staffId);
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

                var dbUser = context.Staff.FirstOrDefault(u => u.StaffId == user.StaffId);
                if (dbUser != null)
                {
                    dbUser.ProfileImage = user.ProfileImage;
                    context.SaveChanges();
                }

                // Cập nhật lại session
                //HttpContext.Session.SetString("StaffSession", JsonConvert.SerializeObject(user));
                TempData["success"] = "Update successful!";
                return RedirectToAction("UpdateProfile");

            }

            // do nothing
            TempData["success"] = "Không có ảnh được tải lên";
            return RedirectToAction("UpdateProfile");
        }
        [HttpPost]
        public IActionResult UpdateProfile(Staff model)
        {
            // check login
            // Lấy StaffId từ Claims
            var staffIdClaim = User.FindFirst("StaffID")?.Value;
            if (staffIdClaim == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            int staffId = int.Parse(staffIdClaim);

            // Lấy thông tin từ DB
            var context = new HospitalManagementContext();
            var user = context.Staff.FirstOrDefault(p => p.StaffId == staffId);
            if (user == null)
            {
                return RedirectToAction("Login", "Auth");
            }


            var curUser = context.Staff.FirstOrDefault(u => u.StaffId == user.StaffId);
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
                var phoneOwner = context.Staff.FirstOrDefault(u => u.PhoneNumber == model.PhoneNumber);

                if (phoneOwner != null && phoneOwner.StaffId != curUser.StaffId)
                {
                    TempData["error"] = "This phone number was used before.";
                    return RedirectToAction("UpdateProfile");
                }


                // update info user and session
                user.FullName = curUser.FullName = model.FullName;
                user.Gender = curUser.Gender = model.Gender;
                user.PhoneNumber = curUser.PhoneNumber = model.PhoneNumber;

                // luu lai user vao database
                context.SaveChanges();

                //// reset session
                //HttpContext.Session.SetString("StaffSession", JsonConvert.SerializeObject(sessionUser));
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
