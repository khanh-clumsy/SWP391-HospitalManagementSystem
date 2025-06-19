public class DoctorScheduleViewModel
{
    public class ScheduleItem
    {
        public DateTime Day { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public int SlotIndex { get; set; }
        public string RoomName { get; set; }
    }
}
