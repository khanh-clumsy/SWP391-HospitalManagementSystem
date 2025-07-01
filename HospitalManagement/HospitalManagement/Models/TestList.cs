using System;
using System.Collections.Generic;

namespace HospitalManagement.Models;

public partial class TestList
{
    public int TestListId { get; set; }

    public int TestId { get; set; }

    public int AppointmentId { get; set; }

    public string? Result { get; set; }

    public string? TestNote { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string TestStatus { get; set; } = null!;

    public virtual Appointment Appointment { get; set; } = null!;

    public virtual Test Test { get; set; } = null!;

    public virtual ICollection<Tracking> Trackings { get; set; } = new List<Tracking>();
}
