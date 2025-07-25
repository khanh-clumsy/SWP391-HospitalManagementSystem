﻿using System;
using System.Collections.Generic;

namespace HospitalManagement.Models;

public partial class Staff
{
    public int StaffId { get; set; }

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string? PhoneNumber { get; set; }

    public bool IsActive { get; set; }

    public string RoleName { get; set; } = null!;

    public string? Gender { get; set; }

    public string? ProfileImage { get; set; }

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    public virtual ICollection<News> News { get; set; } = new List<News>();
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
