using System;
using System.Collections.Generic;

namespace HospitalManagement.Models;

public partial class Doctor
{
    public int DoctorId { get; set; }

    public int AccountId { get; set; }

    public string? DepartmentName { get; set; }

    public bool IsDepartmentHead { get; set; }

    public int? ExperienceYear { get; set; }

    public string? Degree { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    public virtual ICollection<Consultant> Consultants { get; set; } = new List<Consultant>();

    public virtual ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
}
