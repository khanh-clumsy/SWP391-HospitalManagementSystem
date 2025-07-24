using System.ComponentModel.DataAnnotations;

namespace HospitalManagement.ViewModels
{
    public class ScheduleChangeRequestViewModel
    {
        public int FromScheduleId { get; set; }
        public int SelectedSlot { get; set; }
        public DateOnly SelectedDay { get; set; }

        [StringLength(500, ErrorMessage = "Lý do không được vượt quá 500 ký tự.")]
        public string? Reason { get; set; }
    }
}
