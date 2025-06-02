using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HospitalManagement.ViewModels
{
    public class BookingApointment
    {

        public string? Name { get; set; }


        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }


        public DateOnly AppointmentDate { get; set; }


        public int SelectedDoctorId { get; set; }

        [BindNever]
        public List<SelectListItem> DoctorOptions { get; set; } = new List<SelectListItem>();

        public int SelectedSlotId { get; set; }

        [BindNever]
        public List<SelectListItem> SlotOptions { get; set; } = new List<SelectListItem>();
        public int SelectedServiceId { get; set; }

        public List<SelectListItem> ServiceOptions { get; set; } = new List<SelectListItem>();

        [Required(ErrorMessage = "Please enter a note for the appointment.")]
        public string? Note { get; set; }

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
