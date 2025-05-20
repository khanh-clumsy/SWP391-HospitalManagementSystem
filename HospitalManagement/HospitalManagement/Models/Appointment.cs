using System;
using System.Collections.Generic;

namespace HospitalManagement.Models;

public partial class Appointment
{
    public int AppointmentId { get; set; }

    public int? PatientId { get; set; }

    public int? DoctorId { get; set; }

    public int? RoomId { get; set; }

    public int? ServicePackageId { get; set; }

    public DateOnly? Date { get; set; }

    public TimeOnly? Time { get; set; }

    public string? Status { get; set; }

    public virtual ICollection<AppointmentTest> AppointmentTests { get; set; } = new List<AppointmentTest>();

    public virtual ICollection<AppointmentTracking> AppointmentTrackings { get; set; } = new List<AppointmentTracking>();

    public virtual Doctor? Doctor { get; set; }

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    public virtual ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();

    public virtual Patient? Patient { get; set; }

    public virtual Room? Room { get; set; }

    public virtual ServicePackage? ServicePackage { get; set; }
}
