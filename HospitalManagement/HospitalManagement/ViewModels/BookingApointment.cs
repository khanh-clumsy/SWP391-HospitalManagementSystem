using System;
using System.ComponentModel.DataAnnotations;

namespace HospitalManagement.ViewModels
{
    public class BookingApointment
    {
        public int SelectedDoctorId { get; set; }
        public BookingApointment()
        {
        }

        public BookingApointment(string name, string email, string phoneNumber, string consultantType, int selectedServiceId, string note, DateTime appointmentDate, string timeSlot)
        {
            Name = name;
            Email = email;
            PhoneNumber = phoneNumber;
            ConsultantType = consultantType;
            SelectedServiceId = selectedServiceId;
            Note = note;
            AppointmentDate = appointmentDate;
            TimeSlot = timeSlot;
        }

        [Required(ErrorMessage = "Name is required")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Email format is invalid")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Consultant Type is required")]
        public string ConsultantType { get; set; }

        [Required(ErrorMessage = "Please select a service")]
        public int SelectedServiceId { get; set; }

        [Required(ErrorMessage = "Appointment date is required")]
        [DataType(DataType.Date)]
        [CustomValidation(typeof(BookingApointment), nameof(ValidateAppointmentDate))]
        public DateTime AppointmentDate { get; set; }

        [Required(ErrorMessage = "Please select a time slot")]
        public string TimeSlot { get; set; }

        public string Note { get; set; }

        // Custom validation method to ensure appointment date is not before tomorrow
        public static ValidationResult ValidateAppointmentDate(DateTime date, ValidationContext context)
        {
            if (date.Date < DateTime.Now.Date.AddDays(1))
            {
                return new ValidationResult("Appointment date must be from tomorrow onward.");
            }
            return ValidationResult.Success;
        }
    }
}
