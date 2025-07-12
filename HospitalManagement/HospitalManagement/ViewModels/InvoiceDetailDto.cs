namespace HospitalManagement.ViewModels
{
    public class InvoiceDetailDto
    {
        public string PatientName { get; set; }
        public string ItemType { get; set; }
        public string ItemName { get; set; }
        public decimal UnitPrice { get; set; }
        public string PaymentStatus { get; set; }
        public DateTime? PaymentTime { get; set; }
    }
}
