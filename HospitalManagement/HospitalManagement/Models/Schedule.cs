using System;
using System.Collections.Generic;

namespace HospitalManagement.Models;

public partial class Schedule
{
    public int ScheduleId { get; set; }

    public int? AccountId { get; set; }

    public int? RoomId { get; set; }

    public int? SlotId { get; set; }

    public DateOnly Day { get; set; }

    public virtual Account? Account { get; set; }

    public virtual Room? Room { get; set; }

    public virtual Slot? Slot { get; set; }
}
