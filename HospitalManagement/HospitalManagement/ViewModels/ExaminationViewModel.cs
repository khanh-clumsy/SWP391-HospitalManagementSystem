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

        public string Gender { get; set; }

        // Các trường bác sĩ nhập
        public string Symptoms { get; set; } = string.Empty;
        public string Diagnosis { get; set; } = string.Empty;

        public string PrescriptionNote { get; set; } = string.Empty;

        

        // ID các phòng được bác sĩ chọn
        public List<int> SelectedRoomIds { get; set; } = new();

        // Các phòng đã chỉ định trước đó
        public List<Tracking> AssignedRooms { get; set; } = new();

        public List<Test> AvailableTests { get; set; } = new List<Test>();
        // Có kết quả test hay chưa
        public bool AllTestsCompleted { get; set; }
    }
}
