using System;
using System.Collections.Generic;

namespace HospitalManagement.Models;

public partial class Medicine
{
    public int MedicineId { get; set; }

    public string? MedicineType { get; set; }

    public string? Name { get; set; }

    public decimal? Price { get; set; }

    public int? Unit { get; set; }

    public string? Description { get; set; }

    public virtual ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
}
