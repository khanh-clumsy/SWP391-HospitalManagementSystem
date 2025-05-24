using System;
using System.Collections.Generic;

namespace HospitalManagement.Models;

public partial class Patient
{
    public int PatientId { get; set; }

    public int AccountId { get; set; }

    public DateOnly? Dob { get; set; }

    public string? Address { get; set; }

    public string? HealthInsurance { get; set; }

    public string? BloodGroup { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    public virtual ICollection<Consultant> Consultants { get; set; } = new List<Consultant>();

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
}
