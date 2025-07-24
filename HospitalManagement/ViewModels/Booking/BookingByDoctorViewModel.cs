using HospitalManagement.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace HospitalManagement.ViewModels.Booking
{
    public class BookingByDoctorViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
        public string? Name { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập email.")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày khám.")]
        [DataType(DataType.Date)]
        public DateOnly AppointmentDate { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn khung giờ.")]
        public int? SelectedSlotId { get; set; }

        public List<Slot> Slots { get; set; } = new List<Slot>();

        public string? SelectedDepartmentId { get; set; }

        public List<SelectListItem> DepartmentOptions { get; set; } = new List<SelectListItem>();

        public int? SelectedDoctorId { get; set; }

        public List<SelectListItem> DoctorOptions { get; set; } = new List<SelectListItem>();

        public int? SelectedServiceId { get; set; }

        public List<SelectListItem> ServiceOptions { get; set; } = new List<SelectListItem>();

        public int? SelectedPackageId { get; set; }

        public List<SelectListItem> PackageOptions { get; set; } = new List<SelectListItem>();

        [StringLength(500, ErrorMessage = "Ghi chú không được vượt quá 500 ký tự.")]
        public string? Note { get; set; }

        public List<DoctorScheduleViewModel.ScheduleItem> WeeklySchedule { get; set; } = new List<DoctorScheduleViewModel.ScheduleItem>();

        // Custom validation method to ensure appointment date is not before tomorrow
        public static ValidationResult ValidateAppointmentDate(DateTime date, ValidationContext context)
        {
            if (date.Date < DateTime.Now.Date.AddDays(1))
            {
                return new ValidationResult("Ngày khám phải bắt đầu từ ngày mai.");
            }
            return ValidationResult.Success;
        }
    }
}
