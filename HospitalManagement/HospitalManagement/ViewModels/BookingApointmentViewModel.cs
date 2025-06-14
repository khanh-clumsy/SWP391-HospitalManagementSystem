using System;
using System.ComponentModel.DataAnnotations;
using HospitalManagement.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HospitalManagement.ViewModels
{
    public class BookingApointmentViewModel
    {
        [Required(ErrorMessage = "Name is required.")]
        public string? Name { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Phone number is required.")]
        [Phone(ErrorMessage = "Invalid phone number.")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Appointment date is required.")]
        [DataType(DataType.Date)]
        public DateOnly AppointmentDate { get; set; }

        public int? SelectedDoctorId { get; set; }

        public List<Doctor> Doctors { get; set; } = new List<Doctor>();

        public List<Slot> Slots { get; set; } = new List<Slot>();


        public int? SelectedSlotId { get; set; }

        public int SelectedServiceId { get; set; }

        public List<SelectListItem> ServiceOptions { get; set; } = new List<SelectListItem>();

        [StringLength(500, ErrorMessage = "Note must be under 500 characters.")]
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