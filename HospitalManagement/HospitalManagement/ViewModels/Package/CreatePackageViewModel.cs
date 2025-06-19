using System.ComponentModel.DataAnnotations;
using HospitalManagement.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HospitalManagement.ViewModels.Package
{
    public class CreatePackageViewModel
    {
        [Required]
        public string? PackageName { get; set; }

        [Required]
        public string? Description { get; set; }

        [Required]
        public string? TargetGender { get; set; }

        public int AgeFrom { get; set; }

        public int AgeTo { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập phần trăm giảm giá.")]
        [Range(0, 100, ErrorMessage = "Giảm giá phải nằm trong khoảng từ 0 đến 100.")]
        public decimal DiscountPercent { get; set; }

        public string? Thumbnail { get; set; }
        public IFormFile? ThumbnailFile { get; set; }

        [Required]
        public int SelectedCategoryId { get; set; }

        public List<SelectListItem> Categories = new List<SelectListItem>();

        [Required]
        public string? AgeRange { get; set; }

        public List<SelectListItem> AgeOptions = new List<SelectListItem>();

        public List<Test> AvailableTests { get; set; } = new List<Test>();
        public List<int> SelectedTestIds { get; set; } = new List<int>();
    }
}
