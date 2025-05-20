using System;
using System.Collections.Generic;

namespace HospitalManagement.Models;

public partial class Admin
{
    public int AdminId { get; set; }

    public string? FullName { get; set; }

    public string? Email { get; set; }

    public virtual ICollection<Account> Accounts { get; set; } = new List<Account>();
}
