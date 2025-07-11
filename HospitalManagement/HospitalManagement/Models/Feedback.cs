﻿using System;
using System.Collections.Generic;

namespace HospitalManagement.Models;

public partial class Feedback
{
    public int FeedbackId { get; set; }

    public int PatientId { get; set; }

    public int? ServiceId { get; set; }

    public int? PackageId { get; set; }

    public int Rating { get; set; }

    public string Comment { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual Package? Package { get; set; }

    public virtual Patient Patient { get; set; } = null!;

    public virtual Service? Service { get; set; }
}
