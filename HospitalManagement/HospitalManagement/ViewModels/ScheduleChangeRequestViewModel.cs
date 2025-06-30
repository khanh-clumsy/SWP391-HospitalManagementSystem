namespace HospitalManagement.ViewModels
{
    public class ScheduleChangeRequestViewModel
    {
        public int FromScheduleId { get; set; }
        public int SelectedSlot { get; set; }
        public DateOnly SelectedDay { get; set; }
        public string? Reason { get; set; }
    }
}
