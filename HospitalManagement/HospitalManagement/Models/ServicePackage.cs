using System;
using System.Collections.Generic;

namespace HospitalManagement.Models;

public partial class ServicePackage
{
    public int PackageId { get; set; }

    public string? Name { get; set; }

    public string? Type { get; set; }

    public decimal? Price { get; set; }

    public string? Description { get; set; }

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
