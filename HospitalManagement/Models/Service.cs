using System;
using System.Collections.Generic;
using HospitalManagement.Services;

namespace HospitalManagement.Models;

public partial class Service : ISoftDelete
{
    public int ServiceId { get; set; }

    public string ServiceType { get; set; } = null!;

    public decimal ServicePrice { get; set; }

    public bool IsDeleted { get; set; }

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
}
