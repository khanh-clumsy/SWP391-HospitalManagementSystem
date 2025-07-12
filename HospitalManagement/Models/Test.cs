using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HospitalManagement.Models;

public partial class Test
{
    public int TestId { get; set; }

    [Required(ErrorMessage = "Tên xét nghiệm không được để trống")]
    [StringLength(100, ErrorMessage = "Tên xét nghiệm không được vượt quá 100 ký tự")]
    public string Name { get; set; } = null!;

    [Required(ErrorMessage = "Giá tiền không được để trống")]
    [Range(0, double.MaxValue, ErrorMessage = "Giá tiền phải lớn hơn hoặc bằng 0")]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "Loại phòng không được để trống")]
    public string? RoomType { get; set; }

    [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
    public string? Description { get; set; }

    public bool IsDeleted { get; set; }

    public virtual ICollection<PackageTest> PackageTests { get; set; } = new List<PackageTest>();

    public virtual ICollection<TestRecord> TestRecords { get; set; } = new List<TestRecord>();
}
