using HospitalManagement.Data;
using HospitalManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

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
        public async Task<IActionResult> Login(Login LogInfo)
        {
            if (string.IsNullOrEmpty(LogInfo.Email) || string.IsNullOrEmpty(LogInfo.Password))
            {
                ModelState.AddModelError(string.Empty, "!Please enter Email and password.");
                return View(LogInfo);
            }

            var user = _context.Accounts.SingleOrDefault(u => u.Email == LogInfo.Email);
            if (user == null || _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, LogInfo.Password) != PasswordVerificationResult.Success)
            {
                ModelState.AddModelError(string.Empty, "!Email or password is invalid.");
                return View(LogInfo);
            }
            var userJson = JsonConvert.SerializeObject(user); // convert Account object to JSON string
            HttpContext.Session.SetString("UserSession", userJson);

            // Đăng nhập thành công
            return RedirectToAction("ViewDoctors", "Patient");
        }

        [HttpPost]
        public async Task<IActionResult> Register(Register model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existingAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.Email == model.Email);
            if (existingAccount != null)
            {
                ModelState.AddModelError("Email", "Email is already registered.");
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

            return RedirectToAction("Login");
        }


    }
}
