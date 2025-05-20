using System;
using System.Collections.Generic;

namespace HospitalManagement.Models;

public partial class Invoice
{
    public int InvoiceId { get; set; }

    public int? AppointmentId { get; set; }

    public decimal? TotalAmount { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? PaymentStatus { get; set; }

    public virtual Appointment? Appointment { get; set; }
}
