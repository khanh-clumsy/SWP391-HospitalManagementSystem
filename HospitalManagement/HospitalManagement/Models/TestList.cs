using System;
using System.Collections.Generic;

namespace HospitalManagement.Models;

public partial class TestList
{
    public int TestListId { get; set; }

    public int TestId { get; set; }

    public int AppointmentId { get; set; }

    public string? Result { get; set; }

    public string? Note { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Appointment Appointment { get; set; } = null!;

    public virtual Test Test { get; set; } = null!;
}
