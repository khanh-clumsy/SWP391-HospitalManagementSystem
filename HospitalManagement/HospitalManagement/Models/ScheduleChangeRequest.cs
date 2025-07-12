using System;
using System.Collections.Generic;

namespace HospitalManagement.Models;

public partial class ScheduleChangeRequest
{
    public int RequestId { get; set; }

    public int DoctorId { get; set; }

    public int FromScheduleId { get; set; }

    public int ToSlotId { get; set; }

    public DateOnly ToDay { get; set; }

    public int? ToRoomId { get; set; }

    public string? Reason { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual Doctor Doctor { get; set; } = null!;

    public virtual Schedule FromSchedule { get; set; } = null!;

    public virtual Room? ToRoom { get; set; }

    public virtual Slot ToSlot { get; set; } = null!;
}
