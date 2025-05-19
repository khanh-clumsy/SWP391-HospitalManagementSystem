using System;
using System.Collections.Generic;

namespace HospitalManagement.Models;

public partial class Patient
{
    public int PatientId { get; set; }

    public string? FullName { get; set; }

    public DateOnly? Dob { get; set; }

    public string? Gender { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Address { get; set; }

    public virtual ICollection<Account> Accounts { get; set; } = new List<Account>();

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    public virtual ICollection<RequestConsultant> RequestConsultants { get; set; } = new List<RequestConsultant>();
}
