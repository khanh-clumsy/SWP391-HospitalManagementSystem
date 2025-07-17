using System.ComponentModel.DataAnnotations;
namespace HospitalManagement.ViewModels;

public class ChangePass
{
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại.")]
    public string OldPassword { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
    [MinLength(6, ErrorMessage = "Mật khẩu mới phải có ít nhất 6 ký tự.")]
    public string NewPassword { get; set; }

    [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu mới.")]
    [Compare("NewPassword", ErrorMessage = "Mật khẩu mới và xác nhận mật khẩu không khớp.")]
    public string ConfirmPassword { get; set; }
}
