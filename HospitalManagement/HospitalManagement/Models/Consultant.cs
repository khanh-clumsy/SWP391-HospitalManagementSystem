using System;
using System.Collections.Generic;

namespace HospitalManagement.Models;

public partial class Consultant
{
    public int ConsultantId { get; set; }

    public int? AccountId { get; set; }

    public int? ServiceType { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Description { get; set; }

    public DateOnly? RequestedDate { get; set; }

    public string? Status { get; set; }

    public int? DoctorAccountId { get; set; }

    public virtual Account? Account { get; set; }

    public virtual Account? DoctorAccount { get; set; }

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    public virtual Service? ServiceTypeNavigation { get; set; }
}
