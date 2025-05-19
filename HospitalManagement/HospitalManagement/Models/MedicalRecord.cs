using System;
using System.Collections.Generic;

namespace HospitalManagement.Models;

public partial class MedicalRecord
{
    public int RecordId { get; set; }

    public int? AppointmentId { get; set; }

    public string? Symptoms { get; set; }

    public string? Diagnosis { get; set; }

    public string? Note { get; set; }

    public decimal? TotalPrice { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Appointment? Appointment { get; set; }

    public virtual ICollection<AppointmentTest> AppointmentTests { get; set; } = new List<AppointmentTest>();

    public virtual ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
}
