using System;
using System.Collections.Generic;

namespace HospitalManagement.Models;

public partial class AppointmentTracking
{
    public int AppointmentTrId { get; set; }

    public int? AppointmentId { get; set; }

    public int? RoomNextId { get; set; }

    public DateTime? Time { get; set; }

    public virtual Appointment? Appointment { get; set; }

    public virtual Room? RoomNext { get; set; }
}
