using System;
using System.Collections.Generic;

namespace HospitalManagement.Models;

public partial class Test
{
    public int TestId { get; set; }

    public string? Name { get; set; }

    public string? Description { get; set; }

    public virtual ICollection<AppointmentTest> AppointmentTests { get; set; } = new List<AppointmentTest>();
}
