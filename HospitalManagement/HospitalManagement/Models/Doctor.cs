using System;
using System.Collections.Generic;

namespace HospitalManagement.Models;

public partial class Doctor
{
    public int DoctorId { get; set; }

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string? PhoneNumber { get; set; }

    public string DepartmentName { get; set; } = null!;

    public bool IsDepartmentHead { get; set; }

    public int ExperienceYear { get; set; }

    public string? Degree { get; set; }

    public bool IsActive { get; set; }

    public bool IsSpecial { get; set; }

    public string? Gender { get; set; }

    public string? ProfileImage { get; set; }

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    public virtual ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();

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
