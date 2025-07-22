using System.ComponentModel.DataAnnotations;
namespace HospitalManagement.ViewModels
{
    public class Register
    {
        private const int V = 100;

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = null!;


        [Required(ErrorMessage = "Tên không được để trống")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [Phone]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Giới tính không được để trống")]
        public string Gender { get; set; } = null!;

        public string? ProfileImage { get; set; }

        [Required(ErrorMessage = "Vị trí không được để trống")]
        public string RoleName { get; set; } = "Patient"; // mặc định


        // for doctor 
        [Required(ErrorMessage = "Tên khoa không được để trống")]
        public string? DepartmentName { get; set; }

        public bool? IsDepartmentHead { get; set; }

        [Required(ErrorMessage = "Năm kinh nghiệm không được để trống")]
        [Range(0, 60, ErrorMessage = "Vui lòng nhập số từ 0 đến 60.")]
        public int? ExperienceYear { get; set; }

        [Required(ErrorMessage = "Bằng cấp không được để trống")]
        public string? Degree { get; set; }

        public bool? IsSpecial { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;


    }

}