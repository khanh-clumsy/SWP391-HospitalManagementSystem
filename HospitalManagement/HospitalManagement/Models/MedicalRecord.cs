using System;
using System.Collections.Generic;

namespace HospitalManagement.Models;

public partial class MedicalRecord
{
    public int MedicalRecordId { get; set; }

    public int? AppointmentId { get; set; }

    public string? Symptoms { get; set; }

    public string? Diagnosis { get; set; }

    public string? Note { get; set; }

    public decimal? TotalPrice { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Appointment? Appointment { get; set; }

    public virtual ICollection<MedicineList> MedicineLists { get; set; } = new List<MedicineList>();

    public virtual ICollection<TestList> TestLists { get; set; } = new List<TestList>();
}
