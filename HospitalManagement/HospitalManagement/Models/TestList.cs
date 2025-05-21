using System;
using System.Collections.Generic;

namespace HospitalManagement.Models;

public partial class TestList
{
    public int MedicalRecordId { get; set; }

    public int TestId { get; set; }

    public string? Result { get; set; }

    public virtual MedicalRecord MedicalRecord { get; set; } = null!;

    public virtual Test Test { get; set; } = null!;
}
