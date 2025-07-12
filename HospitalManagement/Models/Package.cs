using System;
using System.Collections.Generic;

namespace HospitalManagement.Models;

public partial class Package
{
    public int PackageId { get; set; }

    public string PackageName { get; set; } = null!;

    public int PackageCategoryId { get; set; }

    public string? TargetGender { get; set; }

    public int? AgeFrom { get; set; }

    public int? AgeTo { get; set; }

    public string? Thumbnail { get; set; }

    public string? Description { get; set; }

    public decimal? DiscountPercent { get; set; }

    public decimal OriginalPrice { get; set; }

    public decimal FinalPrice { get; set; }

    public DateTime? CreatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    public virtual PackageCategory PackageCategory { get; set; } = null!;

    public virtual ICollection<PackageTest> PackageTests { get; set; } = new List<PackageTest>();
}
