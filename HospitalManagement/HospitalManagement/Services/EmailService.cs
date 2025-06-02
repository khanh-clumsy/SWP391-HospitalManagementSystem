using MailKit.Net.Smtp;
using MimeKit;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace HospitalManagement.Services
{
    public class EmailService
    {
        private readonly string _emailFrom = "kdodjeksdkkd@gmail.com";
        private readonly string _emailPassword = "oeew lwvb hesu cfis";
        public async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                var email = new MimeMessage();
                email.From.Add(MailboxAddress.Parse(_emailFrom));
                email.To.Add(MailboxAddress.Parse(toEmail));
                email.Subject = subject;

                var builder = new BodyBuilder
                {
                    HtmlBody = body
                };
                email.Body = builder.ToMessageBody();

                using var smtp = new SmtpClient();
                // Thêm đoạn này ngay sau khi tạo smtp:
                smtp.ServerCertificateValidationCallback = (sender, certificate, chain, errors) =>
                {
                    if (errors == SslPolicyErrors.RemoteCertificateChainErrors)
                    {
                        if (chain != null && chain.ChainStatus != null)
                        {
                            foreach (var status in chain.ChainStatus)
                            {
                                if (status.Status == X509ChainStatusFlags.RevocationStatusUnknown)
                                {
                                    return true; // Bỏ qua lỗi kiểm tra revocation
                                }
                            }
                        }
                    }
                    return errors == SslPolicyErrors.None;
                };
                await smtp.ConnectAsync("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(_emailFrom, _emailPassword);
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);

                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
        }
    }
}
