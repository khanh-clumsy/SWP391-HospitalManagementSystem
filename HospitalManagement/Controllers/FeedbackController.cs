using HospitalFETemplate.Controllers;
using HospitalManagement.Data;
using HospitalManagement.Models;
using HospitalManagement.Repositories;
using HospitalManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using X.PagedList;

namespace HospitalManagement.Controllers
{
    public class FeedbackController : Controller
    {
        private readonly IFeedbackRepository _feedbackRepo;
        private readonly HospitalManagementContext _context;
        public FeedbackController(IFeedbackRepository feedbackRepo, HospitalManagementContext context)
        {
            _feedbackRepo = feedbackRepo;
            _context = context;
        }

        [HttpGet]
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> Create(int? packageId, int? serviceId)
        {
            // Lấy PatientId từ Claims
            var patientIdClaim = User.FindFirst("PatientID");
            if (patientIdClaim == null || !int.TryParse(patientIdClaim.Value, out int patientId))
            {
                return Unauthorized();
            }

            // Kiểm tra tồn tại Package/Service và quyền đánh giá
            if (packageId != null)
            {
                if (!await _feedbackRepo.PackageExistsAsync(packageId.Value))
                    return NotFound();

                if (!await _feedbackRepo.HasCompletedAppointmentWithPackageAsync(patientId, packageId.Value))
                    return RedirectToAction("AccessDenied", "Home");
            }

            if (serviceId != null)
            {
                if (!await _feedbackRepo.ServiceExistsAsync(serviceId.Value))
                    return NotFound();

                if (!await _feedbackRepo.HasCompletedAppointmentWithServiceAsync(patientId, serviceId.Value))
                    return RedirectToAction("AccessDenied", "Home");
            }

            var model = new FeedbackCreateViewModel
            {
                PackageId = packageId,
                ServiceId = serviceId
            };

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> Create(FeedbackCreateViewModel model)
        {
            // Lấy PatientId từ Claims
            var patientIdClaim = User.FindFirst("PatientID");
            if (patientIdClaim == null || !int.TryParse(patientIdClaim.Value, out int patientId))
            {
                return Unauthorized();
            }

            // Kiểm tra dữ liệu đầu vào
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Kiểm tra tồn tại và quyền đánh giá
            if (model.PackageId != null)
            {
                if (!await _feedbackRepo.PackageExistsAsync(model.PackageId.Value))
                {
                    return NotFound();
                }

                if (!await _feedbackRepo.HasCompletedAppointmentWithPackageAsync(patientId, model.PackageId.Value))
                {
                    return Unauthorized();
                }
            }

            if (model.ServiceId != null)
            {
                if (!await _feedbackRepo.ServiceExistsAsync(model.ServiceId.Value))
                {
                    return NotFound();
                }

                if (!await _feedbackRepo.HasCompletedAppointmentWithServiceAsync(patientId, model.ServiceId.Value))
                {
                    return Unauthorized();
                }
            }

            // Tạo feedback mới
            var feedback = new Feedback
            {
                PatientId = patientId,
                PackageId = model.PackageId,
                ServiceId = model.ServiceId,
                Rating = model.Rating,
                Comment = model.Comment,
                CreatedAt = DateTime.Now,
                IsSpecial = false
            };

            _context.Feedbacks.Add(feedback);
            await _context.SaveChangesAsync();

            TempData["success"] = "Cảm ơn bạn đã đánh giá!";
            return RedirectToAction("MyAppointments", "Appointment", new { type = "Completed" });
        }

        [HttpGet]
        [Authorize(Roles = "Admin, Sales")]
        public async Task<IActionResult> ManageFeedback(string? name, int? rating, DateOnly? date, int? page)
        {
            name = HomeController.NormalizeName(name);
            int pageSize = 10;
            int pageNumber = page ?? 1;

            var totalFeedbacks = await _feedbackRepo.CountFeedbackAsync(name, rating, date);
            var feedbacks = await _feedbackRepo.SearchFeedbackAsync(name, rating, date, pageNumber, pageSize);

            var pagedList = new StaticPagedList<FeedbackManageViewModel>(feedbacks, pageNumber, pageSize, totalFeedbacks);

            // Gửi lại filter cho View
            ViewBag.Name = name;
            ViewBag.Rating = rating;
            ViewBag.Date = date?.ToString("yyyy-MM-dd");

            return View(pagedList);
        }

        [HttpPost]
        [Authorize(Roles = "Admin, Sales")]
        public async Task<IActionResult> DeleteFeedback(int id)
        {
            var feedback = await _context.Feedbacks.FindAsync(id);
            if (feedback != null)
            {
                _context.Feedbacks.Remove(feedback);
                await _context.SaveChangesAsync();
                TempData["success"] = "Xóa đánh giá thành công!";
            }
            else
            {
                TempData["error"] = "Không tìm thấy đánh giá cần xóa.";
            }

            return RedirectToAction(nameof(ManageFeedback));
        }

        [HttpPost]
        [Authorize(Roles = "Admin, Sales")]
        public async Task<IActionResult> UpdateFeedbackSpecial(List<FeedbackManageViewModel> feedbacks)
        {
            foreach (var item in feedbacks)
            {
                var feedback = await _context.Feedbacks.FindAsync(item.FeedbackId);
                if (feedback != null)
                {
                    feedback.IsSpecial = item.IsSpecial;
                }
            }

            await _context.SaveChangesAsync();
            TempData["success"] = "Cập nhật thành công!";
            return RedirectToAction(nameof(ManageFeedback));
        }

    }
}
