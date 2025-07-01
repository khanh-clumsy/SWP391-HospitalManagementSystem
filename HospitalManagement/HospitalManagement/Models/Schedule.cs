using System;
using System.Collections.Generic;

namespace HospitalManagement.Models;

public partial class Schedule
{
    public int ScheduleId { get; set; }

    public int DoctorId { get; set; }

    public int SlotId { get; set; }

    public int RoomId { get; set; }

    public DateOnly Day { get; set; }

    public virtual Doctor Doctor { get; set; } = null!;

    public virtual Room Room { get; set; } = null!;

    public virtual ICollection<ScheduleChangeRequest> ScheduleChangeRequests { get; set; } = new List<ScheduleChangeRequest>();

    public virtual Slot Slot { get; set; } = null!;
}
