using System;
using System.Collections.Generic;

namespace HospitalManagement.Models;

public partial class Prescription
{
    public int PrescriptionId { get; set; }

    public int? RecordId { get; set; }

    public int? MedicineId { get; set; }

    public int? Quantity { get; set; }

    public decimal? TotalPrice { get; set; }

    public virtual Medicine? Medicine { get; set; }

    public virtual MedicalRecord? Record { get; set; }
}
