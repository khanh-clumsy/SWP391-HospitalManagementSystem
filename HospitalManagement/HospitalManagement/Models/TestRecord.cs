using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace HospitalManagement.Models;

public partial class TestRecord
{
    public int TestRecordId { get; set; }

    public int TestId { get; set; }

    public int AppointmentId { get; set; }

    public int? DoctorId { get; set; }

    public string? Result { get; set; }

    public string? TestNote { get; set; }

    public DateTime? CompletedAt { get; set; }

    public string TestStatus { get; set; } = null!;

    public virtual Appointment Appointment { get; set; } = null!;

    public virtual Doctor? Doctor { get; set; }

    public virtual Test Test { get; set; } = null!;

    public virtual ICollection<Tracking> Trackings { get; set; } = new List<Tracking>();
    
    [NotMapped]
    public string RoomName { get; set; } = "";

}
