using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using HospitalManagement.Models; // namespace chứa Account
using HospitalManagement.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;
using HospitalManagement.ViewModels;

namespace HospitalManagement.Controllers
{
    public class UserController : Controller
    {
        private readonly PasswordHasher<Account> _passwordHasher;

        public UserController(HospitalManagementContext context)
        {
            _passwordHasher = new PasswordHasher<Account>();
        }

        [HttpGet]

        public IActionResult ViewProfile()
        {
            // Load profile data from sesion
            var userJson = HttpContext.Session.GetString("UserSession");

            if (string.IsNullOrEmpty(userJson))
            {
                return RedirectToAction("Login", "Auth");
            }

            var user = JsonConvert.DeserializeObject<Account>(userJson);
            return View(user);
        }
        [HttpGet]
        public IActionResult UpdateProfile()
        {
            // Load profile data to edit
            var userJson = HttpContext.Session.GetString("UserSession");

            if (string.IsNullOrEmpty(userJson))
            {
                return RedirectToAction("Login", "Auth");
            }

            var user = JsonConvert.DeserializeObject<Account>(userJson);
            return View(user);
        }


        [HttpGet]
        public IActionResult ChangePassword()
        {
            // Load profile data from sesion
            var userJson = HttpContext.Session.GetString("UserSession");

            if (string.IsNullOrEmpty(userJson))
            {
                return RedirectToAction("Login", "Auth");
            }

            var user = JsonConvert.DeserializeObject<Account>(userJson);
            return View();
        }


        [HttpPost]
        public IActionResult ChangePassword(ChangePass model)
        {
            var userJson = HttpContext.Session.GetString("UserSession");
            if (string.IsNullOrEmpty(userJson)) return RedirectToAction("Login", "Auth");
            var user = JsonConvert.DeserializeObject<Account>(userJson);

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
                var dbUser = context.Accounts.FirstOrDefault(u => u.AccountId == user.AccountId);
                if (dbUser != null)
                {
                    dbUser.PasswordHash = _passwordHasher.HashPassword(null, model.NewPassword);
                    context.SaveChanges();
                    HttpContext.Session.SetString("UserSession", JsonConvert.SerializeObject(dbUser));

                }
            }

            // Cập nhật lại session
            TempData["success"] = "Change password successful!";
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> UploadPhoto(IFormFile photo)
        {
            var userJson = HttpContext.Session.GetString("UserSession");
            if (string.IsNullOrEmpty(userJson)) return RedirectToAction("Login", "Auth");

            var user = JsonConvert.DeserializeObject<Account>(userJson);


            if (photo != null && photo.Length > 0)
            {
                using var ms = new MemoryStream();
                await photo.CopyToAsync(ms);
                var imageBytes = ms.ToArray();
                user.ProfileImagePath = Convert.ToBase64String(imageBytes);

                // Cập nhật trong DB
                using (var context = new HospitalManagementContext())
                {
                    var dbUser = context.Accounts.FirstOrDefault(u => u.AccountId == user.AccountId);
                    if (dbUser != null)
                    {
                        dbUser.ProfileImagePath = user.ProfileImagePath;
                        context.SaveChanges();
                    }
                }

                // Cập nhật lại session
                HttpContext.Session.SetString("UserSession", JsonConvert.SerializeObject(user));
            }
            TempData["success"] = "Update successful!";


            return RedirectToAction("UpdateProfile");
        }
        [HttpPost]
        public IActionResult UpdateProfile(Account model)
        {
            var userJson = HttpContext.Session.GetString("UserSession");
            if (string.IsNullOrEmpty(userJson))
            {
                return RedirectToAction("Login", "Auth");
            }

            var sessionUser = JsonConvert.DeserializeObject<Account>(userJson);

            using (var context = new HospitalManagementContext())
            {
                var dbUser = context.Accounts.FirstOrDefault(u => u.AccountId == sessionUser.AccountId);
                if (dbUser != null)
                {
                    dbUser.FullName = model.FullName;
                    dbUser.Gender = model.Gender;
                    dbUser.PhoneNumber = model.PhoneNumber;

                    context.SaveChanges();

                    // Cập nhật lại session
                    sessionUser.FullName = model.FullName;
                    sessionUser.Gender = model.Gender;
                    sessionUser.PhoneNumber = model.PhoneNumber;
                    HttpContext.Session.SetString("UserSession", JsonConvert.SerializeObject(sessionUser));
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

            // Chuyển hướng đến trang danh sách bác sĩ
            return RedirectToAction("Index", "Home");
        }
    }
}
