using System;
using System.Collections.Generic;

namespace HospitalManagement.Models;

public partial class AppointmentTest
{
    public int AppTestId { get; set; }

    public int? RecordId { get; set; }

    public int? TestId { get; set; }

    public string? Result { get; set; }

    public decimal? Price { get; set; }

    public virtual MedicalRecord? Record { get; set; }

    public virtual Test? Test { get; set; }
}
