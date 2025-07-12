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

    public string? PaymentMethod { get; set; }

    public string? PaymentStatus { get; set; }

    public DateTime? PaymentTime { get; set; }

    public string? TransactionCode { get; set; }

    public string? ResponseCode { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Appointment Appointment { get; set; } = null!;
}
