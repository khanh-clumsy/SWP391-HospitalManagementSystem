using System.Security.Claims;
using HospitalManagement.Models;
using HospitalManagement.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalManagement.Controllers
{
    public class NewsController : Controller
    {
        private readonly INewsRepository _newsRepository;

        public NewsController(INewsRepository newsRepository)
        {
            _newsRepository = newsRepository;
        }

        [Authorize(Roles = "Doctor, Admin")]
        public async Task<IActionResult> Index()
        {
            var newsList = await _newsRepository.GetAllAsync();
            if (User.IsInRole("Doctor"))
            {
                var doctorIdClaim = User.FindFirst("DoctorID")?.Value;
                if (doctorIdClaim == null)
                    return RedirectToAction("Login", "Auth");

                int doctorId = int.Parse(doctorIdClaim);
                newsList = await _newsRepository.GetByDoctorIdAsync(doctorId);
            }
            else if (User.IsInRole("Admin"))
            {
                newsList = await _newsRepository.GetAllAsync();
            }
            return View(newsList);
        }

        public async Task<IActionResult> News()
        {
            var newsList = await _newsRepository.GetAllAsync();
            return View(newsList);
        }

        public async Task<IActionResult> Detail(int id)
        {
            var news = await _newsRepository.GetByIdAsync(id);
            if (news == null) return NotFound();
            return View(news);
        }

        [HttpGet]
        [Authorize(Roles = "Doctor, Admin")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Doctor, Admin")]
        public async Task<IActionResult> Create(News model, IFormFile photo)
        {
            if (!ModelState.IsValid)
                return View(model);

            model.CreatedAt = DateTime.Now;

            if (photo != null && photo.Length > 0)
            {
                var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/jpg" };
                var maxSize = 1024 * 1024;

                if (!allowedTypes.Contains(photo.ContentType.ToLower()))
                {
                    TempData["Error"] = "Unsupported file type.";
                    return View(model);
                }

                if (photo.Length > maxSize)
                {
                    TempData["Error"] = "File too large.";
                    return View(model);
                }

                using var ms = new MemoryStream();
                await photo.CopyToAsync(ms);
                model.Thumbnail = Convert.ToBase64String(ms.ToArray());
            }

            var (roleKey, userId) = GetUserRoleAndId(User);
            if (roleKey == "DoctorID") model.DoctorId = userId;
            else if (roleKey == "StaffID") model.StaffId = userId;

            await _newsRepository.CreateAsync(model);
            TempData["SuccessMessage"] = "Thêm bài viết thành công!";
            return RedirectToAction("Index");
        }

        [HttpGet]
        [Authorize(Roles = "Doctor, Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var news = await _newsRepository.GetByIdAsync(id);
            if (news == null) return NotFound();
            return View("Update", news);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Doctor, Admin")]
        public async Task<IActionResult> Update(int id, News updatedNews, IFormFile photo)
        {
            if (id != updatedNews.NewsId)
                return NotFound();

            var oldNews = await _newsRepository.GetByIdAsync(id);
            if (oldNews == null)
                return NotFound();

            oldNews.Title = updatedNews.Title;
            oldNews.Description = updatedNews.Description;
            oldNews.Content = updatedNews.Content;

            if (photo != null && photo.Length > 0)
            {
                var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/jpg" };
                var maxSize = 1024 * 1024;

                if (!allowedTypes.Contains(photo.ContentType.ToLower()))
                {
                    TempData["Error"] = "Unsupported file type.";
                    return View("Update", updatedNews);
                }

                if (photo.Length > maxSize)
                {
                    TempData["Error"] = "image must be <= 2mb.";
                    return View("Update", updatedNews);
                }

                using var ms = new MemoryStream();
                await photo.CopyToAsync(ms);
                oldNews.Thumbnail = Convert.ToBase64String(ms.ToArray());
            }

            await _newsRepository.UpdateAsync(oldNews);
            TempData["SuccessMessage"] = "Cập nhật thành công!";
            return RedirectToAction("Index");
        }


        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Doctor, Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            await _newsRepository.DeleteAsync(id);
            return RedirectToAction("Index");
        }

        private (string RoleKey, int? UserId) GetUserRoleAndId(ClaimsPrincipal user)
        {
            if (user.IsInRole("Admin"))
                return ("StaffID", GetUserIdFromClaim(user, "StaffID"));
            if (user.IsInRole("Doctor"))
                return ("DoctorID", GetUserIdFromClaim(user, "DoctorID"));
            return default;
        }

        private int? GetUserIdFromClaim(ClaimsPrincipal user, string claimType)
        {
            var claim = user.FindFirst(claimType);
            return claim != null && int.TryParse(claim.Value, out var id) ? id : null;
        }
    }
}
