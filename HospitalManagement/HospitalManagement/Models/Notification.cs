using System;
using System.Collections.Generic;

namespace HospitalManagement.Models;

public partial class Notification
{
    public int NotificationId { get; set; }

    public string? Role { get; set; }


    public string Role { get; set; } = null!;

    public int RefId { get; set; }

    public string? Title { get; set; }

    public bool? IsRead { get; set; }

    public string? Link { get; set; }

    public string? NotificationType { get; set; }

    public bool? IsRead { get; set; }

    public DateTime? CreatedAt { get; set; }
}
