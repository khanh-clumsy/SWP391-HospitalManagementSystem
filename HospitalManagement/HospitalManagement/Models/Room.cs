using System;
using System.Collections.Generic;

namespace HospitalManagement.Models;

public partial class Room
{
    public int RoomId { get; set; }

    public string RoomName { get; set; } = null!;

    public string? Status { get; set; }

    public string? RoomType { get; set; }

    public virtual ICollection<ScheduleChangeRequest> ScheduleChangeRequests { get; set; } = new List<ScheduleChangeRequest>();

    public virtual ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();

    public virtual ICollection<Tracking> Trackings { get; set; } = new List<Tracking>();
}
