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
            builder.AppendLine("<p>Trân trọng,<br/>Đội ngũ hỗ trợ</p>");
            return builder.ToString();
        }

        public static string BuildConfirmedAppointmentEmail(Appointment appointment)
        {
            var builder = new StringBuilder();
            builder.AppendLine("<h3>✅ Lịch hẹn của bạn đã được xác nhận!</h3>");
            AppendCommonAppointmentInfo(builder, appointment);
            builder.AppendLine("<p>Trân trọng,<br/>Đội ngũ hỗ trợ</p>");
            return builder.ToString();
        }

        public static string BuildRequestAppointmentFailed(Appointment appointment)
        {
            var builder = new StringBuilder();
            builder.AppendLine("<h3>❌ Lịch hẹn của bạn đã bị từ chối!</h3>");
            builder.AppendLine("<p>Chúng tôi rất tiếc phải thông báo rằng lịch hẹn của bạn đã bị từ chối. Vui lòng kiểm tra lại thông tin hoặc đặt lịch vào thời gian khác.</p>");
            AppendCommonAppointmentInfo(builder, appointment);
            builder.AppendLine("<p>Trân trọng,<br/>Đội ngũ hỗ trợ</p>");
            return builder.ToString();
        }

        public static string BuildExpiredAppointmentEmail(Appointment appointment)
        {
            var builder = new StringBuilder();
            builder.AppendLine("<h3>⚠️ Cuộc hẹn của bạn đã quá hạn!</h3>");
            builder.AppendLine("<p>Chúng tôi xin thông báo rằng cuộc hẹn sau đã quá thời gian quy định và được đánh dấu là <b>đã hết hạn</b>:</p>");
            AppendCommonAppointmentInfo(builder, appointment);
            builder.AppendLine("<p>Nếu bạn vẫn muốn sử dụng dịch vụ, vui lòng đặt lại lịch mới trên hệ thống.</p>");
            builder.AppendLine("<p>Trân trọng,<br/>Đội ngũ hỗ trợ</p>");
            return builder.ToString();
        }

        public static string BuildFailedAppointmentEmail(Appointment appointment, string reason)
        {
            var builder = new StringBuilder();
            builder.AppendLine("<h3>❌ Đặt lịch không thành công</h3>");
            builder.AppendLine($"<p>{reason}</p>");
            AppendCommonAppointmentInfo(builder, appointment);
            builder.AppendLine("<p>Vui lòng chọn khung giờ khác hoặc liên hệ hỗ trợ nếu cần.</p>");
            builder.AppendLine("<p>Trân trọng,<br/>Đội ngũ hỗ trợ</p>");
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

            if (appointment.CreatedByStaff != null)
            {
                builder.AppendLine($"<p><strong>Nhân viên tạo lịch:</strong> {appointment.CreatedByStaff.FullName}</p>");
            }
        }
    }
}
