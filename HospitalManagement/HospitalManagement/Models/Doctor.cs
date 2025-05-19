using System;
using System.Collections.Generic;

namespace HospitalManagement.Models;

public partial class Doctor
{
    public int DoctorId { get; set; }

    public string? FullName { get; set; }

    public string? Gender { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public int? SpecializationId { get; set; }

    public virtual ICollection<Account> Accounts { get; set; } = new List<Account>();

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    public virtual ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();

    public virtual DoctorSpecialization? Specialization { get; set; }
}
