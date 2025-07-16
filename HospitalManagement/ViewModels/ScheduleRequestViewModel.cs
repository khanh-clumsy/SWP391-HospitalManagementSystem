using HospitalManagement.Models;
namespace HospitalManagement.ViewModels
{
    public class ScheduleRequestViewModel
    {
        public int RequestId { get; set; }
        public int FromScheduleId { get; set; }
        public Doctor DoctorName { get; set; }
        public Room CurrentRoom { get; set; }
        public int FromSlotTime { get; set; }
        public DateOnly FromDay { get; set; }

        public int ToSlotTime { get; set; }
        public DateOnly ToDay { get; set; }
        public int ToRoom { get; set; }
        public string? Status { get; set; } 
        public string Reason { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
