using System.ComponentModel.DataAnnotations;
namespace HospitalManagement.Models
{
    public class Register
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = null!;

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = null!;

        [Required]
        public string FullName { get; set; } = null!;

        public string RoleName { get; set; } = "Patient"; // mặc định

        [Required]
        public string PhoneNumber { get; set; } = null!;

        [Required]
        public string Gender { get; set; } = null!;
    }

}