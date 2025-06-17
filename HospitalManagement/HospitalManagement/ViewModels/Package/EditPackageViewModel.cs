using HospitalManagement.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace HospitalManagement.ViewModels.Package
{
    public class EditPackageViewModel
    {
        [Required]
        public int PackageId { get; set; }

        [Required]
        public string? PackageName { get; set; }

        [Required]
        public string? Description { get; set; }

        [Required]
        public int SelectedCategoryId { get; set; }

        public List<SelectListItem> Categories { get; set; } = new();

        [Required]
        public string? TargetGender { get; set; }

        [Required]
        public string? AgeRange { get; set; }

        public List<SelectListItem> AgeOptions { get; set; } = new();

        [Range(0, 100, ErrorMessage = "Giảm giá phải nằm trong khoảng từ 0 đến 100.")]
        public decimal DiscountPercent { get; set; }
        public decimal? OriginalPrice { get; set; } // Tổng giá xét nghiệm
        public decimal? FinalPrice { get; set; } //Giá sau khi áp dụng Discount
        public string? CurrentThumbnail { get; set; } // For displaying existing image
        public IFormFile? ThumbnailFile { get; set; } // For uploading new image

        public List<Test> AvailableTests { get; set; } = new();
        public List<int> SelectedTestIds { get; set; } = new();
    }
}
