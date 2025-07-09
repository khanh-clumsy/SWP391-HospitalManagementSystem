using System.ComponentModel.DataAnnotations;

namespace HospitalManagement.ViewModels
{
    public class ReplacementInfoViewModel
    {
        [Required]
        public int RequestId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phòng.")]
        public int RoomId { get; set; }

        public bool HasAppointment { get; set; } // để biết có hiển thị bác sĩ không

        public int? ReplacementDoctorId { get; set; } // chỉ required nếu HasAppointment == true
    }
}
