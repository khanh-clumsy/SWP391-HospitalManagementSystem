﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

public class CreateAppointmentViewModel
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

    [Required(ErrorMessage = "Please select a doctor.")]
    public int SelectedDoctorId { get; set; }

    public List<SelectListItem> DoctorOptions { get; set; } = new List<SelectListItem>();

    [Required(ErrorMessage = "Please select a time slot.")]
    public int SelectedSlotId { get; set; }

    public List<SelectListItem> SlotOptions { get; set; } = new List<SelectListItem>();

    [Required(ErrorMessage = "Please select a service.")]
    public int SelectedServiceId { get; set; }

    public List<SelectListItem> ServiceOptions { get; set; } = new List<SelectListItem>();

    [StringLength(500, ErrorMessage = "Note must be under 500 characters.")]
    public string? Note { get; set; }
}
