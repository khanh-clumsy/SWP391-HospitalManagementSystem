using System;
using System.Collections.Generic;

namespace HospitalManagement.Models;

public partial class Appointment
{
    public int AppointmentId { get; set; }

    public int? DoctorId { get; set; }

    public int PatientId { get; set; }

    public int? ServiceId { get; set; }

    public int? PackageId { get; set; }

    public int? StaffId { get; set; }

    public int? SlotId { get; set; }

    public DateOnly Date { get; set; }

    public string Status { get; set; } = null!;

    public string? PrescriptionNote { get; set; }

    public string? PaymentMethod { get; set; }

    public string? PaymentStatus { get; set; }

    public string? TransactionCode { get; set; }

    public decimal? TotalPrice { get; set; }

    public string? Symptoms { get; set; }

    public string? Diagnosis { get; set; }

    public string? Note { get; set; }

    public DateTime? RecordCreatedAt { get; set; }

    public virtual Doctor? Doctor { get; set; }

    public virtual ICollection<InvoiceDetail> InvoiceDetails { get; set; } = new List<InvoiceDetail>();

    public virtual Package? Package { get; set; }

    public virtual Patient Patient { get; set; } = null!;

    public virtual Service? Service { get; set; }

    public virtual Slot? Slot { get; set; }

    public virtual Staff? Staff { get; set; }

    public virtual ICollection<TestList> TestLists { get; set; } = new List<TestList>();

    public virtual ICollection<Tracking> Trackings { get; set; } = new List<Tracking>();
}
