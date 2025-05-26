using HospitalManagement.Data;
using HospitalManagement.Models;
using HospitalManagement.ViewModels;
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
using HospitalManagement.Services;

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
        private readonly EmailService _emailService;

        public AuthController(HospitalManagementContext context, EmailService emailService)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<Account>();
            _emailService = emailService;

        }


        [HttpPost]
        public async Task<IActionResult> Login(ViewModels.Login LogInfo)
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
        public async Task<IActionResult> Register(ViewModels.Register model)
        {
            if (!ModelState.IsValid)
            {
                TempData["error"] = "Invalid data";

                return View(model); // trả về cùng model để hiện lỗi
            }

            var existingAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.Email == model.Email);
            if (existingAccount != null)
            {

                TempData["error"] = "Email is already registered.";

                return View(model);
            }

            // 1. Tạo mã xác minh
            string code = new Random().Next(100000, 999999).ToString();

            // 2. Gửi email
            await SendVerificationEmail(model.Email, code);

            // 3. Lưu thông tin đăng ký tạm thời + mã vào session
            HttpContext.Session.SetString("PendingRegister", JsonConvert.SerializeObject(model));
            HttpContext.Session.SetString("VerificationCode", code);

            // 4. Chuyển sang trang nhập mã xác minh
            return RedirectToAction("VerifyCode", new { email = model.Email });
        }
        private async Task<bool> SendVerificationEmail(string toEmail, string code)
        {
            string subject = "Your verification code";
            string body = $"<p>Your verification code is: <strong>{code}</strong></p>";

            return await _emailService.SendEmailAsync(toEmail, subject, body);
        }

        [HttpGet]
        public IActionResult VerifyCode(string email)
        {
            return View(new VerifyCodeModel { Email = email });
        }

        [HttpPost]
        public async Task<IActionResult> VerifyCode(VerifyCodeModel model)
        {
            string sampleCode = HttpContext.Session.GetString("VerificationCode");
            string registerData = HttpContext.Session.GetString("PendingRegister");

            if (sampleCode == null || registerData == null)
            {
                TempData["error"] = "Session expired or invalid verification attempt.";
                return RedirectToAction("Register");
            }

            if (model.Code != sampleCode)
            {
                TempData["error"] = "Incorrect verification code.";
                return View(model);
            }

            var registerModel = JsonConvert.DeserializeObject<ViewModels.Register>(registerData);

            var account = new Account
            {
                Email = registerModel.Email,
                PasswordHash = _passwordHasher.HashPassword(null, registerModel.Password),
                FullName = registerModel.FullName,
                PhoneNumber = registerModel.PhoneNumber,
                Gender = registerModel.Gender,
                RoleName = "Patient",
                IsActive = true
            };

            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            HttpContext.Session.Remove("PendingRegister");
            HttpContext.Session.Remove("VerificationCode");

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
        private async Task<bool> RegisterAccountAsync(ViewModels.Register model)
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
                var registerNew = new ViewModels.Register
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

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            var user = await _context.Accounts.FirstOrDefaultAsync(x => x.Email == email);
            if (user == null)
            {
                TempData["error"] = "Email không tồn tại.";
                return View();
            }

            // Tạo token reset
            string token = Guid.NewGuid().ToString();
            var reset = new PasswordReset
            {
                Email = email,
                Token = token,
                ExpireAt = DateTime.Now.AddMinutes(30) // 30 phut
            };
            _context.PasswordResets.Add(reset);
            await _context.SaveChangesAsync();

            // Gửi email
            var resetLink = Url.Action("ResetPassword", "Auth", new { token = token }, Request.Scheme);
            string subject = "Đặt lại mật khẩu";
            string body = $"<p>Nhấn vào liên kết để đặt lại mật khẩu:</p><a href='{resetLink}'>{resetLink}</a>";
            await _emailService.SendEmailAsync(email, subject, body);

            TempData["success"] = "Đã gửi liên kết đặt lại mật khẩu qua email.";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public async Task<IActionResult> ResetPassword(string token)
        {
            var reset = await _context.PasswordResets.FirstOrDefaultAsync(x => x.Token == token && x.ExpireAt > DateTime.Now);
            if (reset == null)
            {
                TempData["error"] = "Token is invalid or expired.";
                return RedirectToAction("ForgotPassword");
            }

            HttpContext.Session.SetString("Token", token);
            return View(); // Hiện form nhập mật khẩu mới
        }

        [HttpPost]
        public async Task<IActionResult> DoResetPassword(ResetPasswordModel model)
        {
            string token = HttpContext.Session.GetString("Token");

            var reset = await _context.PasswordResets.FirstOrDefaultAsync(x => x.Token == token && x.ExpireAt > DateTime.Now);
            if (reset == null)
            {
                TempData["error"] = "Token is invalid or expired."+token+" "+DateTime.Now;
             
                return RedirectToAction("ForgotPassword");
            }

            if(model.NewPassword == null || model.NewPassword != model.ConfirmPassword)
            {
                TempData["error"] = "Two password is not match";

                return RedirectToAction("ForgotPassword");
            }

            var user = await _context.Accounts.FirstOrDefaultAsync(x => x.Email == reset.Email);
            if (user == null)
            {
                TempData["error"] = "Không tìm thấy tài khoản.";
                return RedirectToAction("ForgotPassword");
            }

            var hasher = new PasswordHasher<Account>();
            user.PasswordHash = hasher.HashPassword(user, model.NewPassword);
            _context.PasswordResets.Remove(reset);
            await _context.SaveChangesAsync();

            var userJson = JsonConvert.SerializeObject(user);
            HttpContext.Session.SetString("UserSession", userJson);

            TempData["success"] = "Đổi mật khẩu thành công.";
            return RedirectToAction("ViewDoctors", "Patient");
        }


    }
}
