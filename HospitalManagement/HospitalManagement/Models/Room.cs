using System;
using System.Collections.Generic;

namespace HospitalManagement.Models;

public partial class Room
{
    public int RoomId { get; set; }

    public string RoomName { get; set; } = null!;

    public string? Status { get; set; }

    public virtual ICollection<RoomTest> RoomTests { get; set; } = new List<RoomTest>();

    public virtual ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();

    public virtual ICollection<Tracking> Trackings { get; set; } = new List<Tracking>();
}
