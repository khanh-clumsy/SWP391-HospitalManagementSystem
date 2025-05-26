using System.ComponentModel.DataAnnotations;
namespace HospitalManagement.ViewModels;

public class ChangePass
{
    [Required]
    public string OldPassword { get; set; }

    [Required]
    [MinLength(6)]
    public string NewPassword { get; set; }

    [Required]
    [Compare("NewPassword")]
    public string ConfirmPassword { get; set; }
}
