using System.ComponentModel;
using HospitalManagement.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HospitalManagement.ViewModels.Package
{
    public class PackageViewModel
    {
        public int PackageId { get; set; }

        public string PackageName { get; set; } = null!;

        public string? TargetGender { get; set; }

        public int? AgeFrom { get; set; }

        public int? AgeTo { get; set; }

        public string? Thumbnail { get; set; }

        public int PackageCategoryId { get; set; }

        public PackageCategory PackageCategory { get; set; } = new PackageCategory();

        public string? Description { get; set; }

        public decimal? DiscountPercent { get; set; }

        public decimal FinalPrice { get; set; }

        public decimal OriginalPrice { get; set; }

        public decimal CurrentPrice => Math.Round(OriginalPrice * (1 - (DiscountPercent ?? 0) / 100), 0);

        public bool IsDeleted { get; set; } = false;
    }
}
