using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HospitalManagement.Models;

public partial class Account
{
    public int AccountId { get; set; }

    public string? Username { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string? FullName { get; set; }

    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    public DateOnly? Dob { get; set; }

    public bool? IsActive { get; set; }

    public int? DepartmentId { get; set; }

    public virtual ICollection<Appointment> AppointmentAccounts { get; set; } = new List<Appointment>();

    public virtual ICollection<Appointment> AppointmentDoctorAccounts { get; set; } = new List<Appointment>();

    public virtual ICollection<Consultant> ConsultantAccounts { get; set; } = new List<Consultant>();

    public virtual ICollection<Consultant> ConsultantDoctorAccounts { get; set; } = new List<Consultant>();

    public virtual Department? Department { get; set; }

    public virtual ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();

    public virtual ICollection<Role> Roles { get; set; } = new List<Role>();
}
