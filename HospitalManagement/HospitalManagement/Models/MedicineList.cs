using System;
using System.Collections.Generic;

namespace HospitalManagement.Models;

public partial class MedicineList
{
    public int MedicineListId { get; set; }

    public int AppointmentId { get; set; }

    public int MedicineId { get; set; }

    public int Quantity { get; set; }

    public virtual Appointment Appointment { get; set; } = null!;

    public virtual Medicine Medicine { get; set; } = null!;
}
