using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using HospitalManagement.Models; // namespace chứa Account
using HospitalManagement.Data;
using Microsoft.AspNetCore.Identity;

namespace HospitalManagement.Controllers
{
    public class UserController : Controller
    {
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
            return View(user);
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

            return RedirectToAction("UpdateProfile");
        }

        [HttpGet]
        public IActionResult Logout()
        {
            // Xóa toàn bộ session
            HttpContext.Session.Clear();

            // Chuyển hướng đến trang danh sách bác sĩ
            return RedirectToAction("ViewDoctors", "Patient");
        }
    }
}
