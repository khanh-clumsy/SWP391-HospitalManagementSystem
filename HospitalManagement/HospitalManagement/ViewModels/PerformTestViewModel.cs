using Microsoft.AspNetCore.Mvc;

namespace HospitalManagement.ViewModels
{
    public class PerformTestViewModel
    {
        public int TestListId { get; set; }

        public string TestName { get; set; } = string.Empty;

        public string? ResultDescription { get; set; }

        public IFormFile? ResultFile { get; set; }

        // Thông tin bệnh nhân
        public string PatientName { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public string Gender { get; set; } = string.Empty;
    }
}