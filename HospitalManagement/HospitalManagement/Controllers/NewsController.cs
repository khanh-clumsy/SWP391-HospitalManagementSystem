﻿using System.Security.Claims;
using HospitalManagement.Models;
using HospitalManagement.Repositories;
using HospitalManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using X.PagedList.Extensions;

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
            List<NewsViewModel> newsList;
            if (User.IsInRole("Doctor"))
            {
                var doctorIdClaim = User.FindFirst("DoctorID")?.Value;
                if (doctorIdClaim == null)
                    return RedirectToAction("Login", "Auth");

                int doctorId = int.Parse(doctorIdClaim);
                newsList = await _newsRepository.GetByDoctorIdAsync(doctorId);
            }
            else 
            {
                newsList = await _newsRepository.GetAllAsync();
            }
            return View(newsList);
        }

        public async Task<IActionResult> News(int? page)
        {
            int pageSize = 5;

            int pageNumber = page ?? 1;

            var newsList = await _newsRepository.GetAllAsync(); 
            var pagedNews = newsList.ToPagedList(pageNumber, pageSize);

            return View(pagedNews);
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
            ViewBag.Title = model.Title;
            ViewBag.Description = model.Description;
            ViewBag.Content = model.Content;

            model.CreatedAt = DateTime.Now;

            if (model.Description.Length > 4000)
            {
                TempData["Error"] = "Mô tả ít hơn 4000 ký tự";
                return View(model);
            }

            if (photo == null)
            {
                TempData["Error"] = "Thiếu ảnh thumbnail";
                return View(model);
            }

            if (photo.Length > 0)
            {
                var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/jpg" };
                var maxSize = 5 * 1024 * 1024;

                if (!allowedTypes.Contains(photo.ContentType.ToLower()))
                {
                    TempData["Error"] = "Loại file không được hỗ trợ.";
                    return View(model);
                }

                if (photo.Length > maxSize)
                {
                    TempData["Error"] = "File quá lớn, phải nhỏ hơn 2MB.";
                    return View(model);
                }

                // Tạo tên file duy nhất để tránh trùng
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(photo.FileName);
                var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", fileName);

                // Lưu file vào wwwroot/uploads
                using (var stream = new FileStream(uploadPath, FileMode.Create))
                {
                    await photo.CopyToAsync(stream);
                }
                model.Thumbnail = fileName;
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

            // Cập nhật các trường văn bản
            oldNews.Title = updatedNews.Title;
            oldNews.Description = updatedNews.Description;
            oldNews.Content = updatedNews.Content;

            if (photo != null && photo.Length > 0)
            {
                // Validate file
                var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/jpg" };
                var maxSize = 2 * 1024 * 1024;
                if (!allowedTypes.Contains(photo.ContentType.ToLower()))
                {
                    TempData["Error"] = "Loại file không được hỗ trợ.";
                    return View("Update", updatedNews);
                }
                if (photo.Length > maxSize)
                {
                    TempData["Error"] = "File quá lớn, phải nhỏ hơn 2MB.";
                    return View("Update", updatedNews);
                }

                // Tạo tên file mới
                var fileName = Guid.NewGuid() + Path.GetExtension(photo.FileName);
                var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img");
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                var uploadPath = Path.Combine(folderPath, fileName);
                using (var stream = new FileStream(uploadPath, FileMode.Create))
                {
                    await photo.CopyToAsync(stream);
                }
                // Xóa file cũ nếu có
                if (!string.IsNullOrEmpty(oldNews.Thumbnail))
                {
                    var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", oldNews.Thumbnail.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(oldPath))
                        System.IO.File.Delete(oldPath);
                }

                // Lưu đường dẫn mới (dùng Url.Content sẽ trả về "/img/xxxx.jpg")
                oldNews.Thumbnail = fileName;
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
