using System;
using System.Collections.Generic;

namespace HospitalManagement.Models;

public partial class PasswordReset
{
    public int Id { get; set; }

    public string? Email { get; set; }

    public string? Token { get; set; }

    public DateTime? ExpireAt { get; set; }
}
