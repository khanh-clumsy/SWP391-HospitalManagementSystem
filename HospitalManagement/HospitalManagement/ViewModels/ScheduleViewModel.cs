namespace HospitalManagement.ViewModels
{
    public class ScheduleViewModel
    {
        public int ScheduleId { get; set; }
        public DateOnly Day { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public int SlotIndex { get; set; }
        public string RoomName { get; set; }
    }
}
