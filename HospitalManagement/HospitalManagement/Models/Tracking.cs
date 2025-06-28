using System;
using System.Collections.Generic;

namespace HospitalManagement.Models;

public partial class Tracking
{
    public int TrackingId { get; set; }

    public int AppointmentId { get; set; }

    public int? TestListId { get; set; }

    public int RoomId { get; set; }

    public DateTime Time { get; set; }

    public virtual Appointment Appointment { get; set; } = null!;

    public virtual Room Room { get; set; } = null!;

    public virtual TestList? TestList { get; set; }
}
