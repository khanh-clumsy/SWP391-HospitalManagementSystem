namespace HospitalManagement.ViewModels.VnPay
{
    public class VnPayViewModel
    {
        public string OrderType { get; set; }
        public decimal Amount { get; set; }
        public string OrderDescription { get; set; }
        public string Name { get; set; }
        public int InvoiceId { get; set; }

    }
}