using System.ComponentModel.DataAnnotations;
namespace HospitalManagement.ViewModels
{
    public class Register
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = null!;
        [Required]
        public string FullName { get; set; }

        [Phone]
        public string PhoneNumber { get; set; }

        [Required]
        public string Gender { get; set; } = null!;

        public string? ProfileImage { get; set; }


        public string RoleName { get; set; } = "Patient"; // mặc định


        // for doctor 
        [Required]
        public string? DepartmentName { get; set; }

        public bool? IsDepartmentHead { get; set; }

        [Required]
        [Range(0, 99)]
        public int? ExperienceYear { get; set; }

        public string? Degree { get; set; }

        public bool? IsSpecial { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;


    }

}