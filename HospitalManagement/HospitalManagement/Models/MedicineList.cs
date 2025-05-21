using System;
using System.Collections.Generic;

namespace HospitalManagement.Models;

public partial class MedicineList
{
    public int MedicalRecordId { get; set; }

    public int MedicineId { get; set; }

    public int? Quantity { get; set; }

    public virtual MedicalRecord MedicalRecord { get; set; } = null!;

    public virtual Medicine Medicine { get; set; } = null!;
}
