
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

public class RequestConsultantViewModel
{
    public RequestConsultantViewModel()
    {
    }

    public RequestConsultantViewModel(string name, string email, string phoneNumber, string consultantType, List<SelectListItem> consultantTypes, int selectedServiceId, string note)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Email = email ?? throw new ArgumentNullException(nameof(email));
        PhoneNumber = phoneNumber ?? throw new ArgumentNullException(nameof(phoneNumber));
        ConsultantType = consultantType ?? throw new ArgumentNullException(nameof(consultantType));
        ConsultantTypes = consultantTypes ?? throw new ArgumentNullException(nameof(consultantTypes));
        SelectedServiceId = selectedServiceId;
        Note = note ?? throw new ArgumentNullException(nameof(note));
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

    public List<SelectListItem> ConsultantTypes { get; set; }

    [Required(ErrorMessage = "Please select a service")]
    public int SelectedServiceId { get; set; }

    public string Note { get; set; }


}

