using System;
using System.Collections.Generic;

namespace HospitalManagement.Models;

public partial class Invoice
{
    public int InvoiceId { get; set; }

    public int? AppointmentId { get; set; }

    public int? ConsultantId { get; set; }

    public decimal? MedicinePrice { get; set; }

    public decimal? Price { get; set; }

    public string? Method { get; set; }

    public string? Status { get; set; }

    public string? TransactionCode { get; set; }

    public virtual Appointment? Appointment { get; set; }

    public virtual Consultant? Consultant { get; set; }
}
