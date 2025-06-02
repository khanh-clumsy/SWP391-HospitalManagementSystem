using System;
using System.Collections.Generic;

namespace HospitalManagement.Models;

public partial class Patient
{
    public int PatientId { get; set; }

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string? PhoneNumber { get; set; }

    public DateTime? Dob { get; set; }

    public string? Address { get; set; }

    public string? HealthInsurance { get; set; }

    public string? BloodGroup { get; set; }

    public string FullName { get; set; } = null!;

    public bool IsActive { get; set; }

    public string? Gender { get; set; }

    public string? ProfileImage { get; set; }

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
    public string GetFullGender()
    {
        if (this.Gender == "M")
        {
            return "Male";
        }
        else if (this.Gender == "F")
        {
            return "Female";
        }
        else
        {
            return "Other";
        }
    }
}
