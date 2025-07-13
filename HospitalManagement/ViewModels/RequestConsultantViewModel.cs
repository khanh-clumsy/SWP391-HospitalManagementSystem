
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

public class RequestConsultantViewModel
{
    public RequestConsultantViewModel()
    {
    }

    public RequestConsultantViewModel(string name, string email, string phoneNumber, string consultantType, int selectedServiceId, string note)
    {
        Name = name;
        Email = email;
        PhoneNumber = phoneNumber;
        ConsultantType = consultantType;
        SelectedServiceId = selectedServiceId;
        Note = note;
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

    public string Note { get; set; }


}

