using System.ComponentModel.DataAnnotations;

namespace HospitalManagement.ViewModels
{
    public class ResetPasswordModel
    {
        [Required]
        public string Email { get; set; }

        [Required]
        public string NewPassword { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; }
    }

}
