using HospitalManagement.Models;
using System.Text;

namespace HospitalManagement.Services
{
    public class EmailBuilder
    {
        public static string BuildAccountInfoEmail(string fullName, string email, string password)
        {
            return $@"
            <h3>🔐 Thông tin tài khoản truy cập hệ thống</h3>
            <p>Kính gửi <strong>{fullName}</strong>,</p>
            <p>Bạn đã được tạo tài khoản thành công trên hệ thống của chúng tôi với thông tin đăng nhập như sau:</p>
            <ul>
                <li><strong>Email:</strong> {email}</li>
                <li><strong>Mật khẩu:</strong> {password}</li>
            </ul>
            <p>Vui lòng đăng nhập và đổi mật khẩu ngay sau lần đăng nhập đầu tiên để đảm bảo bảo mật.</p>
            <p>Trân trọng,<br/>Đội ngũ hỗ trợ</p>";
        }

        public static string BuildPendingAppointmentEmail(Appointment appointment)
        {
            var builder = new StringBuilder();
            builder.AppendLine("<h3>📋 Đặt lịch hẹn thành công! Vui lòng chờ duyệt!</h3>");
            AppendCommonAppointmentInfo(builder, appointment);
            return builder.ToString();
        }

        public static string BuildConfirmedAppointmentEmail(Appointment appointment)
        {
            var builder = new StringBuilder();
            builder.AppendLine("<h3>✅ Lịch hẹn của bạn đã được xác nhận!</h3>");
            AppendCommonAppointmentInfo(builder, appointment);
            return builder.ToString();
        }

        private static void AppendCommonAppointmentInfo(StringBuilder builder, Appointment appointment)
        {
            builder.AppendLine($"<p><strong>Bệnh nhân:</strong> {appointment.Patient?.FullName}</p>");
            builder.AppendLine($"<p><strong>Ngày hẹn:</strong> {appointment.Date:dd/MM/yyyy}</p>");

            if (appointment.Doctor != null)
            {
                builder.AppendLine($"<p><strong>Bác sĩ:</strong> {appointment.Doctor.FullName}</p>");
                builder.AppendLine($"<p><strong>Khoa:</strong> {appointment.Doctor.DepartmentName}</p>");
            }

            if (appointment.Service != null)
            {
                builder.AppendLine($"<p><strong>Dịch vụ:</strong> {appointment.Service.ServiceType}</p>");
            }

            if (appointment.Package != null)
            {
                builder.AppendLine($"<p><strong>Gói khám:</strong> {appointment.Package.PackageName}</p>");
            }

            if (appointment.Slot != null)
            {
                builder.AppendLine($"<p><strong>Giờ:</strong> {appointment.Slot.StartTime} - {appointment.Slot.EndTime}</p>");
            }
            
            if (!string.IsNullOrWhiteSpace(appointment.Note))
            {
                builder.AppendLine($"<p><strong>Ghi chú:</strong> {appointment.Note}</p>");
            }

            if (appointment.Staff != null)
            {
                builder.AppendLine($"<p><strong>Nhân viên tạo lịch:</strong> {appointment.Staff.FullName}</p>");
            }
        }
    }
}
