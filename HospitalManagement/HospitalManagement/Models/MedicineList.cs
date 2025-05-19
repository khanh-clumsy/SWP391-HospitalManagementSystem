using System;
using System.Collections.Generic;

namespace HospitalManagement.Models;

public partial class MedicineList
{
    public int MedicineId { get; set; }

    public string? Name { get; set; }

    public string? Unit { get; set; }

    public string? Description { get; set; }

    public virtual ICollection<MedicinePrice> MedicinePrices { get; set; } = new List<MedicinePrice>();

    public virtual ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
}
