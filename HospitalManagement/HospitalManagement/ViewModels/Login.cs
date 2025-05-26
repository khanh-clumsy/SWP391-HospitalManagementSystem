using System.ComponentModel.DataAnnotations;

namespace HospitalManagement.ViewModels
{
    public class Login
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = null!;
    }
}
