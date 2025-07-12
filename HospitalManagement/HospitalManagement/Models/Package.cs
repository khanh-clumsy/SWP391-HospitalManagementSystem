using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HospitalManagement.Models;

public partial class Package
{
    public int PackageId { get; set; }

    [Required(ErrorMessage = "Tên gói khám là bắt buộc")]
    [Display(Name = "Tên gói khám")]
    public string PackageName { get; set; } = null!;

    [Required(ErrorMessage = "Danh mục gói khám là bắt buộc")]
    [Display(Name = "Danh mục")]
    public int PackageCategoryId { get; set; }

    [Display(Name = "Giới tính mục tiêu")]
    public string? TargetGender { get; set; }

    [Display(Name = "Độ tuổi từ")]
    public int? AgeFrom { get; set; }

    [Display(Name = "Độ tuổi đến")]
    public int? AgeTo { get; set; }

    [Display(Name = "Hình ảnh")]
    public string? Thumbnail { get; set; }

    [Display(Name = "Mô tả")]
    public string? Description { get; set; }

    [Display(Name = "Phần trăm giảm giá")]
    public decimal? DiscountPercent { get; set; }

    [Required(ErrorMessage = "Giá gốc là bắt buộc")]
    [Range(0, double.MaxValue, ErrorMessage = "Giá gốc phải lớn hơn hoặc bằng 0")]
    [Display(Name = "Giá gốc")]
    public decimal OriginalPrice { get; set; }

    [Required(ErrorMessage = "Giá cuối là bắt buộc")]
    [Range(0, double.MaxValue, ErrorMessage = "Giá cuối phải lớn hơn hoặc bằng 0")]
    [Display(Name = "Giá cuối")]
    public decimal FinalPrice { get; set; }

    [Display(Name = "Ngày tạo")]
    public DateTime? CreatedAt { get; set; }

    [Display(Name = "Đã xóa")]
    public bool IsDeleted { get; set; } = false;

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    public virtual PackageCategory PackageCategory { get; set; } = null!;

    public virtual ICollection<PackageTest> PackageTests { get; set; } = new List<PackageTest>();
}
