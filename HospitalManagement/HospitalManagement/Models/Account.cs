using System;
using System.Collections.Generic;

namespace HospitalManagement.Models;

public partial class Account
{
    public int AccountId { get; set; }

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string? PhoneNumber { get; set; }

    public string? Gender { get; set; }

    public bool IsActive { get; set; }

    public string RoleName { get; set; } = null!;

    public string? ProfileImagePath { get; set; }

   
    public virtual ICollection<Doctor> Doctors { get; set; } = new List<Doctor>();

    public virtual ICollection<Patient> Patients { get; set; } = new List<Patient>();
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
