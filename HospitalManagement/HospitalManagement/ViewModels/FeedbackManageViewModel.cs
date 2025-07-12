namespace HospitalManagement.ViewModels
{
    public class FeedbackManageViewModel
    {
        public int FeedbackId { get; set; }
        public string PatientName { get; set; }
        public string? ItemName { get; set; } 
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsSpecial { get; set; }
    }
}
