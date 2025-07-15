using HospitalManagement.Models;
public class TotalInvoiceViewModel
{
    public Appointment Appointment { get; set; }
    public List<InvoiceDetailDisplayItem> Items { get; set; }
    public decimal TotalAmount => Items.Sum(i => i.Amount);
}