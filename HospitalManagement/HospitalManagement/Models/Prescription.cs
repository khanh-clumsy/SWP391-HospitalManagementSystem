using System;
using System.Collections.Generic;

namespace HospitalManagement.Models;

public partial class Prescription
{
    public int PrescriptionId { get; set; }

    public int? RecordId { get; set; }

    public int? MedicineId { get; set; }

    public int? Quantity { get; set; }

    public string? UsageInstruction { get; set; }

    public virtual MedicineList? Medicine { get; set; }

    public virtual MedicalRecord? Record { get; set; }
}
