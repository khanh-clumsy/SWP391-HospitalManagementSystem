using System;
using System.Collections.Generic;

namespace HospitalManagement.Models;

public partial class RequestConsultant
{
    public int RequestId { get; set; }

    public int PatientId { get; set; }

    public string? Phone { get; set; }

    public string? ServiceType { get; set; }

    public decimal? ServicePrice { get; set; }

    public int? DoctorId { get; set; }

    public string? Description { get; set; }

    public DateTime? RequestedDate { get; set; }

    public string? Status { get; set; }

    public virtual Doctor? Doctor { get; set; }

    public virtual Patient Patient { get; set; } = null!;
}
