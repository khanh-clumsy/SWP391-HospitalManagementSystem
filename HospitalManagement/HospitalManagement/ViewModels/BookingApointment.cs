using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HospitalManagement.ViewModels
{
    public class BookingApointment
    {

        public string Name { get; set; }


        public string Email { get; set; }

        public string PhoneNumber { get; set; }


        public DateTime AppointmentDate { get; set; }


        public int SelectedDoctorId { get; set; }

        [BindNever]
        public List<SelectListItem> DoctorOptions { get; set; }

        public int SelectedSlotId { get; set; }

        [BindNever]
        public List<SelectListItem> SlotOptions { get; set; }
        public int SelectedServiceId { get; set; }

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
