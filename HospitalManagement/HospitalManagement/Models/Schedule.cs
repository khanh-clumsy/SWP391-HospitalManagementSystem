using System;
using System.Collections.Generic;

namespace HospitalManagement.Models;

public partial class Schedule
{
    public int ScheduleId { get; set; }

    public int? DoctorId { get; set; }

    public int? RoomId { get; set; }

    public TimeOnly? StartTime { get; set; }

    public TimeOnly? EndTime { get; set; }

    public string? Day { get; set; }

    public virtual Doctor? Doctor { get; set; }

    public virtual Room? Room { get; set; }
}
