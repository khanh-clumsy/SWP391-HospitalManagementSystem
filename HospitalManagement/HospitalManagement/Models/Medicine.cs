using System;
using System.Collections.Generic;

namespace HospitalManagement.Models;

public partial class Medicine
{
    public int MedicineId { get; set; }

    public string MedicineType { get; set; } = null!;

    public string Name { get; set; } = null!;

    public decimal Price { get; set; }

    public string? Description { get; set; }

    public string? Unit { get; set; }

    public virtual ICollection<MedicineList> MedicineLists { get; set; } = new List<MedicineList>();
}
