
using System.ComponentModel.DataAnnotations;

public class RequestConsultantViewModel
{
    public string Name { get; set; }

    public string Email { get; set; }

    public string PhoneNumber { get; set; }
    
    [Required]
    public string ConsultantType { get; set; }
    
    [Required]
    public string Service { get; set; }
    public string Note { get; set; }

}

