using System;
using System.Collections.Generic;

namespace HospitalManagement.Models;

public partial class InvoiceDetail
{
    public int InvoiceDetailId { get; set; }

    public int AppointmentId { get; set; }

    public string ItemType { get; set; } = null!;

    public int ItemId { get; set; }

    public string ItemName { get; set; } = null!;

    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    public decimal? TotalPrice { get; set; }

    public virtual Appointment Appointment { get; set; } = null!;
}
