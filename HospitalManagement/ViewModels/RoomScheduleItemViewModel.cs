namespace HospitalManagement.ViewModels
{
    public class RoomScheduleItemViewModel
    {
        public int SlotId { get; set; }
        public DateOnly Day { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public int DoctorId { get; set; }
        public string? DoctorName { get; set; }
        public int ScheduleId { get; set; }
    }

}
