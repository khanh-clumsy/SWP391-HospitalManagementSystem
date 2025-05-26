using System;
using System.Collections.Generic;

namespace HospitalManagement.Models;

public partial class Consultant
{
    public int ConsultantId { get; set; }

    public int PatientId { get; set; }

    public int DoctorId { get; set; }

    public int ServiceId { get; set; }

    public string? RequestedPersonType { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Description { get; set; }

    public DateOnly? RequestedDate { get; set; }

    public string? Status { get; set; }

    public int? Method { get; set; }

    public int? PaymentStatus { get; set; }

    public int? TransactionCode { get; set; }

    public virtual Doctor Doctor { get; set; } = null!;

    public virtual Patient Patient { get; set; } = null!;

    public virtual Service Service { get; set; } = null!;
}
