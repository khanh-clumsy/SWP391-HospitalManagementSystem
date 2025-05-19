using System;
using System.Collections.Generic;

namespace HospitalManagement.Models;

public partial class MedicinePrice
{
    public int MedicinePriceId { get; set; }

    public int? MedicineId { get; set; }

    public decimal? Price { get; set; }

    public DateOnly? EffectiveDate { get; set; }

    public virtual MedicineList? Medicine { get; set; }
}
