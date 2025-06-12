using System;
using System.Collections.Generic;

namespace HospitalManagement.Models;

public partial class News
{
    public int NewsId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string? Content { get; set; }

    public string? Thumbnail { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? StaffId { get; set; }

    public int? DoctorId { get; set; }

    public virtual Doctor? Doctor { get; set; }

    public virtual Staff? Staff { get; set; }
}
