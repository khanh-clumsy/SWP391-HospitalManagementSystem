using System;
using System.Collections.Generic;

namespace HospitalManagement.Models;

public partial class PasswordReset
{
    public int Id { get; set; }

    public string Email { get; set; } = null!;

    public string Token { get; set; } = null!;

    public DateTime ExpireAt { get; set; }
}
