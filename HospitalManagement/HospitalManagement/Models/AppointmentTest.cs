using System;
using System.Collections.Generic;

namespace HospitalManagement.Models;

public partial class AppointmentTest
{
    public int AppTestId { get; set; }

    public int? AppointmentId { get; set; }

    public int? TestId { get; set; }

    public string? Result { get; set; }

    public virtual Appointment? Appointment { get; set; }

    public virtual Test? Test { get; set; }
}
