using System;
using System.Collections.Generic;

namespace HospitalManagement.Models;

public partial class Medicine
{
    public int MedicineId { get; set; }

    public string MedicineType { get; set; } = null!;

    public string Name { get; set; } = null!;

    public decimal Price { get; set; }

    public string Description { get; set; } = null!;

    public string Unit { get; set; } = null!;

    public string? Image { get; set; }

    public virtual ICollection<MedicineList> MedicineLists { get; set; } = new List<MedicineList>();
}