using System;
using System.Collections.Generic;

namespace HospitalManagement.Models;

public partial class Room
{
    public int RoomId { get; set; }

    public string? Name { get; set; }

    public virtual ICollection<AppointmentTracking> AppointmentTrackings { get; set; } = new List<AppointmentTracking>();

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    public virtual ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
}
