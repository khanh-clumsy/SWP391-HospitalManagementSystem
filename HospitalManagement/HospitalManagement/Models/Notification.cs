using System;
using System.Collections.Generic;

namespace HospitalManagement.Models;

public partial class Notification
{
    public int NotificationId { get; set; }

    public string? Role { get; set; }

    public int? RefId { get; set; }

    public string? Message { get; set; }

    public bool? IsRead { get; set; }

    public DateTime? CreatedAt { get; set; }
}
