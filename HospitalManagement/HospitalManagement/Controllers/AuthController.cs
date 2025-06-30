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
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Org.BouncyCastle.Crypto.Generators;

namespace HospitalManagement.Controllers
{
    public class AuthController : Controller
    {
        [HttpGet]
        public IActionResult Login(string? error)
        {
            if (!string.IsNullOrEmpty(error)) TempData["error"] = error;
            return View(new ViewModels.Login());
        }
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View(new ViewModels.ResetPasswordModel());
        }



        private readonly HospitalManagementContext _context;
        private readonly PasswordHasher<Patient> _patientHasher;
        private readonly PasswordHasher<Doctor> _doctorHasher;
        private readonly PasswordHasher<Staff> _staffHasher;

        private readonly EmailService _emailService;

        public AuthController(HospitalManagementContext context, EmailService emailService)
        {
            _context = context;

            _patientHasher = new PasswordHasher<Patient>();
            _doctorHasher = new PasswordHasher<Doctor>();
            _staffHasher = new PasswordHasher<Staff>();

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
            // Staff
            if (LogInfo.Role == "Staff")
            {
                var user = _context.Staff.SingleOrDefault(u => u.Email == LogInfo.Email);
                PasswordHasher<Staff> localHasher = new PasswordHasher<Staff>();

                if (user == null || localHasher.VerifyHashedPassword(user, user.PasswordHash, LogInfo.Password) != PasswordVerificationResult.Success)
                {
                    TempData["error"] = "Email or password is invalid.";
                    return View(LogInfo);
                }

                var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Email, user.Email),

                        new Claim(ClaimTypes.Role, user.RoleName),

                        new Claim("StaffID", user.StaffId.ToString()),
                    };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                TempData["success"] = "Login successful!";
                return RedirectToAction("Index", "Home");
            }

            //Patient
            else if (LogInfo.Role == "Patient")
            {
                var user = _context.Patients.SingleOrDefault(u => u.Email == LogInfo.Email);

                if (user == null || _patientHasher.VerifyHashedPassword(user, user.PasswordHash, LogInfo.Password) != PasswordVerificationResult.Success)
                {
                    TempData["error"] = "Email or password is invalid.";

                    return View(LogInfo);
                }
                if (user.IsActive == false)
                {
                    TempData["error"] = "Account is inactive";

                    return View(LogInfo);
                }
                var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Email, user.Email),
                        new Claim(ClaimTypes.Role, "Patient"),
                        new Claim("PatientID", user.PatientId.ToString())
                    };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                // Đăng nhập thành công
                TempData["success"] = "Login successful!";
                return RedirectToAction("Index", "Home");
            }
            else if (LogInfo.Role == "Doctor")// Doctor
            {
                var user = _context.Doctors.SingleOrDefault(u => u.Email == LogInfo.Email);
                if (user == null || _doctorHasher.VerifyHashedPassword(user, user.PasswordHash, LogInfo.Password) != PasswordVerificationResult.Success)
                {
                    TempData["error"] = "Email or password is invalid.";
                    return View(LogInfo);
                }
                if (user.IsActive == false)
                {
                    TempData["error"] = "Account is inactive";

                    return View(LogInfo);
                }
                string roleName = "Doctor";
                if (user.DepartmentName == "Xét nghiệm" || user.DepartmentName == "Chẩn đoán hình ảnh")
                {
                    roleName = "TestDoctor";
                }
                var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Email, user.Email),
                        new Claim(ClaimTypes.Role, roleName),
                        new Claim("DoctorID", user.DoctorId.ToString()),
                        new Claim("IsDepartmentHead", user.IsDepartmentHead.ToString()),
                    };
                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                // Đăng nhập thành công
                TempData["success"] = "Doctor Login successful!";
                return RedirectToAction("Index", "Home");
            }
            else // Staff
            {
                var user = _context.Staff.SingleOrDefault(u => u.Email == LogInfo.Email);

                if (user == null || _staffHasher.VerifyHashedPassword(user, user.PasswordHash, LogInfo.Password) != PasswordVerificationResult.Success)
                {
                    TempData["error"] = "Email or password is invalid.";
                    return View(LogInfo);
                }
                if (user.IsActive == false)
                {
                    TempData["error"] = "Account is inactive";

                    return View(LogInfo);
                }
                var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Email, user.Email),

                        new Claim(ClaimTypes.Role, user.RoleName),

                        new Claim("StaffID", user.StaffId.ToString()),
                    };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                TempData["success"] = user.RoleName + " Login successful!";
                return RedirectToAction("Index", "Home");
            }

        }



        [HttpPost]
        public async Task<IActionResult> Register(ViewModels.Register model)
        {

            // check if mail is used
            var existingAccount = await _context.Patients.FirstOrDefaultAsync(a => a.Email == model.Email);
            if (existingAccount != null)
            {

                TempData["error"] = "Email is already registered.";

                return View(model);
            }

            //check phone valid

            // check if phone start with 0 and 9 digits back
            if (model.PhoneNumber == null)
            {
                TempData["error"] = "Phone number is invalid.";
                return View(model);
            }

            if (model.PhoneNumber[0] != '0' || model.PhoneNumber.Length != 10)
            {
                TempData["error"] = "Phone number is invalid.";
                return View(model);
            }

            // check if phone is non-number
            foreach (char u in model.PhoneNumber) if (u < '0' || u > '9')
                {
                    TempData["error"] = "Phone number is invalid.";
                    return View(model);
                }

            // check phone is used(not this user)
            var phoneOwner = _context.Patients.FirstOrDefault(u => u.PhoneNumber == model.PhoneNumber);

            if (phoneOwner != null)
            {
                TempData["error"] = "This phone number was used before.";
                return View(model);
            }

            // not check confirm password, valid in View


            // 1. gen random code 6 digit
            string code = new Random().Next(100000, 999999).ToString();

            // 2. send email
            await SendVerificationEmail(model.Email, code);

            // 3. put temp model and code into session
            HttpContext.Session.SetString("PendingRegister", JsonConvert.SerializeObject(model));
            HttpContext.Session.SetString("VerificationCode", code);

            // 4. move to verify code page
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

            var patient = new Patient
            {
                Email = registerModel.Email,
                PasswordHash = _patientHasher.HashPassword(null, registerModel.Password),
                FullName = registerModel.FullName,
                PhoneNumber = registerModel.PhoneNumber,
                Gender = registerModel.Gender,
                IsActive = true
            };

            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();

            HttpContext.Session.Remove("PendingRegister");
            HttpContext.Session.Remove("VerificationCode");

            var accJson = JsonConvert.SerializeObject(patient, new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            });
            HttpContext.Session.SetString("PatientSession", accJson);


            TempData["success"] = "Register successful!";
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> LoginGoogle(string role)
        {

            await HttpContext.ChallengeAsync(GoogleDefaults.AuthenticationScheme,
                new AuthenticationProperties
                {
                    RedirectUri = Url.Action("GoogleResponse", "Auth", new { role = role })
                    // phai dung thu tu nhu nay => Auth/GoogleResponse/role
                });

            return new EmptyResult(); // Thêm return để tránh lỗi
        }

        // create new account for patient gmail login
        private async Task<bool> RegisterAccountAsync(ViewModels.Register model)
        {
            if (await _context.Patients.AnyAsync(a => a.Email == model.Email))
            {
                return false;
            }

            var patient = new Patient
            {
                Email = model.Email,
                PasswordHash = _patientHasher.HashPassword(null, model.Password),
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber,
                Gender = model.Gender,
                IsActive = true
            };

            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<IActionResult> GoogleResponse(string role)
        {
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (!result.Succeeded)
            {
                TempData["error"] = "Login with Google failed.";
                return RedirectToAction("Login");
            }

            var googleClaims = result.Principal.Identities.FirstOrDefault()?.Claims;
            var email = googleClaims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var name = googleClaims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(email))
            {
                TempData["error"] = "Email not found in Google response.";
                return RedirectToAction("Login");
            }

            if (role == "Patient") // null => can register for them
            {
                var user = _context.Patients.SingleOrDefault(u => u.Email == email);

                if (user == null)
                {
                    var registerNew = new ViewModels.Register
                    {
                        Email = email,
                        Password = "password",
                        ConfirmPassword = "password",
                        FullName = name,
                        PhoneNumber = "", // null
                        Gender = "M",
                    };

                    // register and return result
                    bool success = await RegisterAccountAsync(registerNew);
                    if (!success)
                    {
                        TempData["error"] = "Fail to register new account with Google.";
                        return RedirectToAction("Login");
                    }
                    user = await _context.Patients.FirstOrDefaultAsync(u => u.Email == email);

                }
                if (user.IsActive == false)
                {
                    TempData["error"] = "Account is inactive";

                    return RedirectToAction("Login");
                }
                // Tạo Claim và Identity cho Patient
                var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Email, user.Email),
                        new Claim(ClaimTypes.Role, "Patient"),
                        new Claim("PatientID", user.PatientId.ToString())
                    };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);


                TempData["success"] = "Login successful!";
                return RedirectToAction("Index", "Home");
            }
            else if (role == "Doctor") // doctor: null => not register for them like patient
            {
                var user = _context.Doctors.SingleOrDefault(u => u.Email == email);

                if (user == null)
                {
                    TempData["error"] = "Doctor Email is invalid.";
                    return RedirectToAction("Login");

                }
                if (user.IsActive == false)
                {
                    TempData["error"] = "Account is inactive";

                    return RedirectToAction("Login");
                }
                string roleName = "Doctor";
                if (user.DepartmentName == "Xét nghiệm" || user.DepartmentName == "Chẩn đoán hình ảnh")
                {
                    roleName = "TestDoctor";
                }
                // Tạo Claim và Identity cho Doctor
                var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Email, user.Email),
                        new Claim(ClaimTypes.Role, roleName),
                        new Claim("DoctorID", user.DoctorId.ToString()),
                        new Claim("IsDepartmentHead", user.IsDepartmentHead.ToString()),
                    };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                TempData["success"] = "Doctor Login successful!";
                return RedirectToAction("Index", "Home");
            }
            else // Staff => nhu doctor
            {
                var user = _context.Staff.SingleOrDefault(u => u.Email == email);

                if (user == null)
                {
                    TempData["error"] = "Staff Email is invalid.";
                    return RedirectToAction("Login");

                }
                if (user.IsActive == false)
                {
                    TempData["error"] = "Account is inactive";

                    return RedirectToAction("Login");
                }
                // Tạo Claim và Identity cho Staff
                var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Email, user.Email),

                        new Claim(ClaimTypes.Role, user.RoleName),

                        new Claim("StaffID", user.StaffId.ToString()),
                    };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                TempData["success"] = user.RoleName + " Login successful!";
                return RedirectToAction("Index", "Home");
            }
        }



        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ResetPasswordModel model)
        {
            if (model.Role == "Patient")
            {
                var user = await _context.Patients.FirstOrDefaultAsync(x => x.Email == model.Email);
                if (user == null)
                {
                    TempData["error"] = "Email không tồn tại.";
                    return View(model);
                }

                // Tạo token reset
                string token = Guid.NewGuid().ToString();
                var reset = new PasswordReset
                {
                    Email = model.Email,
                    Token = token,
                    ExpireAt = DateTime.Now.AddMinutes(30) // 30 phut
                };
                _context.PasswordResets.Add(reset);
                await _context.SaveChangesAsync();

                // Gửi email
                var resetLink = Url.Action("ResetPassword", "Auth", new { token = token, role = model.Role }, Request.Scheme);
                string subject = "Đặt lại mật khẩu";
                string body = $"<p>Nhấn vào liên kết để đặt lại mật khẩu:</p><a href='{resetLink}'>{resetLink}</a>";

                Console.WriteLine(await _emailService.SendEmailAsync(model.Email, subject, body));
                TempData["success"] = "Đã gửi liên kết đặt lại mật khẩu qua email.";
                return RedirectToAction("Login");
            }
            else if (model.Role == "Doctor")
            {
                var user = await _context.Doctors.FirstOrDefaultAsync(x => x.Email == model.Email);
                if (user == null)
                {
                    TempData["error"] = "Email không tồn tại.";
                    return View(model);
                }

                // Tạo token reset
                string token = Guid.NewGuid().ToString();
                var reset = new PasswordReset
                {
                    Email = model.Email,
                    Token = token,
                    ExpireAt = DateTime.Now.AddMinutes(30) // 30 phut
                };
                _context.PasswordResets.Add(reset);
                await _context.SaveChangesAsync();

                // Gửi email
                var resetLink = Url.Action("ResetPassword", "Auth", new { token = token, role = model.Role }, Request.Scheme);
                string subject = "Đặt lại mật khẩu";
                string body = $"<p>Nhấn vào liên kết để đặt lại mật khẩu:</p><a href='{resetLink}'>{resetLink}</a>";

                Console.WriteLine(await _emailService.SendEmailAsync(model.Email, subject, body));
                TempData["success"] = "Đã gửi liên kết đặt lại mật khẩu qua email.";
                return RedirectToAction("Login");
            }
            else if (model.Role == "Staff")// Staff
            {
                var user = await _context.Staff.FirstOrDefaultAsync(x => x.Email == model.Email);
                if (user == null)
                {
                    TempData["error"] = "Email không tồn tại.";
                    return View(model);
                }

                // Tạo token reset
                string token = Guid.NewGuid().ToString();
                var reset = new PasswordReset
                {
                    Email = model.Email,
                    Token = token,
                    ExpireAt = DateTime.Now.AddMinutes(30) // 30 phut
                };
                _context.PasswordResets.Add(reset);
                await _context.SaveChangesAsync();

                // Gửi email
                var resetLink = Url.Action("ResetPassword", "Auth", new { token = token, role = model.Role }, Request.Scheme);
                string subject = "Đặt lại mật khẩu";
                string body = $"<p>Nhấn vào liên kết để đặt lại mật khẩu:</p><a href='{resetLink}'>{resetLink}</a>";

                Console.WriteLine(await _emailService.SendEmailAsync(model.Email, subject, body));
                TempData["success"] = "Đã gửi liên kết đặt lại mật khẩu qua email.";
                return RedirectToAction("Login");
            }
            // send email


            TempData["error"] = "Unknow Role Access";
            return RedirectToAction("Login");
        }


        [HttpGet]
        public async Task<IActionResult> ResetPassword(string token, string role)
        {
            var reset = await _context.PasswordResets.FirstOrDefaultAsync(x => x.Token == token && x.ExpireAt > DateTime.Now);
            if (reset == null)
            {
                TempData["error"] = "Token is invalid or expired.";
                return RedirectToAction("ForgotPassword");
            }

            HttpContext.Session.SetString("Token", token); // truyen token vao session de DoResetPassword get
            return View(new ResetPasswordModel() { Role = role }); // to HTML form
        }



        [HttpPost]
        public async Task<IActionResult> DoResetPassword(ResetPasswordModel model)
        {
            string token = HttpContext.Session.GetString("Token");

            var reset = await _context.PasswordResets.FirstOrDefaultAsync(x => x.Token == token && x.ExpireAt > DateTime.Now);
            if (reset == null)
            {
                TempData["error"] = "Token is invalid or expired.";

                return RedirectToAction("ForgotPassword");
            }

            if (model.NewPassword == null || model.NewPassword != model.ConfirmPassword)
            {
                TempData["error"] = "Two password is not match";

                return RedirectToAction("ForgotPassword");
            }

            if (model.Role == "Patient")
            {
                var user = await _context.Patients.FirstOrDefaultAsync(x => x.Email == reset.Email);
                if (user == null)
                {
                    TempData["error"] = "Không tìm thấy tài khoản.";
                    return RedirectToAction("ForgotPassword");
                }

                user.PasswordHash = _patientHasher.HashPassword(user, model.NewPassword);
                _context.PasswordResets.Remove(reset);
                await _context.SaveChangesAsync();

                var userJson = JsonConvert.SerializeObject(user);
                HttpContext.Session.SetString("PatientSession", userJson);

                TempData["success"] = "Đổi mật khẩu thành công.";
                return RedirectToAction("Index", "Home");
            }
            else if (model.Role == "Doctor")
            {
                var user = await _context.Doctors.FirstOrDefaultAsync(x => x.Email == reset.Email);
                if (user == null)
                {
                    TempData["error"] = "Không tìm thấy tài khoản.";
                    return RedirectToAction("ForgotPassword");
                }

                user.PasswordHash = _doctorHasher.HashPassword(user, model.NewPassword);
                _context.PasswordResets.Remove(reset);
                await _context.SaveChangesAsync();

                var userJson = JsonConvert.SerializeObject(user);
                HttpContext.Session.SetString("PatientSession", userJson);

                TempData["success"] = "Đổi mật khẩu thành công.";
                return RedirectToAction("Index", "Home");
            }
            else if (model.Role == "Staff")
            {
                var user = await _context.Staff.FirstOrDefaultAsync(x => x.Email == reset.Email);
                if (user == null)
                {
                    TempData["error"] = "Không tìm thấy tài khoản.";
                    return RedirectToAction("ForgotPassword");
                }

                user.PasswordHash = _staffHasher.HashPassword(user, model.NewPassword);
                _context.PasswordResets.Remove(reset);
                await _context.SaveChangesAsync();

                var userJson = JsonConvert.SerializeObject(user);
                HttpContext.Session.SetString("PatientSession", userJson);

                TempData["success"] = "Đổi mật khẩu thành công.";
                return RedirectToAction("Index", "Home");
            }

            TempData["error"] = "Unknow Role Access" + model.Role + "!";
            return RedirectToAction("Index", "Home");
        }


    }
}