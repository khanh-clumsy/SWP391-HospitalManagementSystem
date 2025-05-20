using System;
using System.Collections.Generic;

namespace HospitalManagement.Models;

public partial class DoctorSpecialization
{
    public int SpecializationId { get; set; }

    public string? Name { get; set; }

    public virtual ICollection<Doctor> Doctors { get; set; } = new List<Doctor>();
}
