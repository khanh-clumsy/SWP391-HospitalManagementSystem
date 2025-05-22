using System;
using System.Collections.Generic;

namespace HospitalManagement.Models;

public partial class Service
{
    public int ServiceType { get; set; }

    public decimal ServicePrice { get; set; }

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    public virtual ICollection<Consultant> Consultants { get; set; } = new List<Consultant>();
}
