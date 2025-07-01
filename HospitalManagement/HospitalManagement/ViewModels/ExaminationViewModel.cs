using HospitalManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HospitalManagement.ViewModels
{
    public class ExaminationViewModel
    {
        public int AppointmentId { get; set; }

        // Thông tin bệnh nhân
        public string PatientName { get; set; }

        public string TestStatus { get; set; }

        public DateTime? DateOfBirth { get; set; }
        public int? PackageId { get; set; }
        public int? ServiceId { get; set; }

        public string? PackageName { get; set; }
        public string? ServiceName { get; set; }

        public string Gender { get; set; }

        // Các trường bác sĩ nhập
        public string? Symptoms { get; set; }

        public string? Diagnosis { get; set; }

        public string? PrescriptionNote { get; set; }

        // ID các phòng được bác sĩ chọn
        public List<int> SelectedRoomIds { get; set; } = new List<int>();

        // Các phòng đã chỉ định trước đó
        public List<Tracking> AssignedRooms { get; set; } = new List<Tracking>();

        public List<Test> AvailableTests { get; set; } = new List<Test>();
        // Có kết quả test hay chưa
        public bool AllTestsCompleted { get; set; }
    }
}
