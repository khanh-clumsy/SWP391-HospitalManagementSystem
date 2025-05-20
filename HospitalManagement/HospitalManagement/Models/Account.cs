using System;
using System.Collections.Generic;

namespace HospitalManagement.Models;

public partial class Account
{
    public int AccountId { get; set; }

    public string Username { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public int RoleId { get; set; }

    public int? PatientId { get; set; }

    public int? DoctorId { get; set; }

    public int? AdminId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Admin? Admin { get; set; }

    public virtual Doctor? Doctor { get; set; }

    public virtual Patient? Patient { get; set; }

    public virtual Role Role { get; set; } = null!;
}
