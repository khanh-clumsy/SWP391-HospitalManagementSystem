using HospitalManagement.Data;
using HospitalManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;

namespace HospitalManagement.Controllers
{
    public class AuthController : Controller
    {
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }
        [HttpGet]

        public IActionResult Register()
        {
            return View();
        }
        [HttpGet]

        public IActionResult ForgotPassword()
        {
            return View();
        }

        private readonly HospitalManagementContext _context;
        private readonly PasswordHasher<Account> _passwordHasher;

        public AuthController(HospitalManagementContext context)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<Account>();
        }


        [HttpPost]
        public async Task<IActionResult> Login(Models.Login LogInfo)
        {
            if (string.IsNullOrEmpty(LogInfo.Email) || string.IsNullOrEmpty(LogInfo.Password))
            {
                TempData["error"] = "Please enter Email and password.";

                return View(LogInfo);
            }

            var user = _context.Accounts.SingleOrDefault(u => u.Email == LogInfo.Email);
            if (user == null || _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, LogInfo.Password) != PasswordVerificationResult.Success)
            {
                TempData["error"] = "Email or password is invalid.";

                return View(LogInfo);
            }
            var userJson = JsonConvert.SerializeObject(user); // convert Account object to JSON string
            HttpContext.Session.SetString("UserSession", userJson);

            // Đăng nhập thành công
            TempData["success"] = "Login successful!";

            return RedirectToAction("ViewDoctors", "Patient");
        }

        [HttpPost]
        public async Task<IActionResult> Register(Models.Register model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existingAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.Email == model.Email);
            if (existingAccount != null)
            {

                TempData["error"] = "Email is already registered.";

                return View(model);
            }

            var account = new Account
            {
                Email = model.Email,
                PasswordHash = _passwordHasher.HashPassword(null, model.Password),
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber,
                Gender = model.Gender,
                RoleName = model.RoleName,
                IsActive = true
            };

            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            // Lưu session sau khi đăng nhập
            var accJson = JsonConvert.SerializeObject(account);
            HttpContext.Session.SetString("UserSession", accJson);

            TempData["success"] = "Register successful!";
            return RedirectToAction("ViewDoctors", "Patient");
        }

        public async Task LoginGoogle()
        {
            await HttpContext.ChallengeAsync(GoogleDefaults.AuthenticationScheme,
                new AuthenticationProperties
                {
                    RedirectUri = Url.Action("GoogleResponse")
                });
        }
        private async Task<bool> RegisterAccountAsync(Models.Register model)
        {
            if (await _context.Accounts.AnyAsync(a => a.Email == model.Email))
            {
                return false;
            }

            var account = new Account
            {
                Email = model.Email,
                PasswordHash = _passwordHasher.HashPassword(null, model.Password),
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber,
                Gender = model.Gender,
                RoleName = "Patient", // mặc định nếu dùng Google
                IsActive = true
            };

            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<IActionResult> GoogleResponse()
        {
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (!result.Succeeded)
            {
                TempData["error"] = "Login with Google failed.";
                return RedirectToAction("Login");
            }

            var claims = result.Principal.Identities.FirstOrDefault()?.Claims;
            var email = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var name = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(email))
            {
                TempData["error"] = "Email not found in Google response.";
                return RedirectToAction("Login");
            }
            var user = _context.Accounts.SingleOrDefault(u => u.Email == email);

            if (user == null)
            {
                var passwordHash = _passwordHasher.HashPassword(null, "17092005");
                var registerNew = new Models.Register
                {
                    Email = email,
                    Password = "password",
                    ConfirmPassword = "password",
                    FullName = name,
                    PhoneNumber = "0",
                    Gender = "M",
                };

                // register and return result

                bool success = await RegisterAccountAsync(registerNew);
                if (!success)
                {
                    TempData["error"] = "Fail to register new account with Google.";
                    return RedirectToAction("Login");
                }

                user = await _context.Accounts.FirstOrDefaultAsync(u => u.Email == email);

            }
            // Lưu session sau khi đăng nhập
            var userJson = JsonConvert.SerializeObject(user);
            HttpContext.Session.SetString("UserSession", userJson);

            TempData["success"] = "Login successful!";
            return RedirectToAction("ViewDoctors", "Patient");
        }
    }
}
