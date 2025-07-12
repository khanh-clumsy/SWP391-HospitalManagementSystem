using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HospitalManagement.Models;

public partial class Test
{
    public int TestId { get; set; }

    [Required(ErrorMessage = "Tên xét nghiệm là bắt buộc")]
    [Display(Name = "Tên xét nghiệm")]
    public string Name { get; set; } = null!;

    [Required(ErrorMessage = "Giá tiền là bắt buộc")]
    [Range(0, double.MaxValue, ErrorMessage = "Giá tiền phải lớn hơn hoặc bằng 0")]
    [Display(Name = "Giá tiền")]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "Loại phòng là bắt buộc")]
    [Display(Name = "Loại phòng")]
    public string? RoomType { get; set; }

    [Display(Name = "Mô tả")]
    public string? Description { get; set; }

    [Display(Name = "Đã xóa")]
    public bool IsDeleted { get; set; } = false;

    public virtual ICollection<PackageTest> PackageTests { get; set; } = new List<PackageTest>();

    public virtual ICollection<TestRecord> TestRecords { get; set; } = new List<TestRecord>();
}
