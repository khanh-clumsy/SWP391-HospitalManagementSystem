using Microsoft.AspNetCore.Mvc;
using HospitalManagement.Data;
using HospitalManagement.Models;
using HospitalManagement.ViewModels; 
using HospitalManagement.ViewModels.VnPay; 
using Microsoft.EntityFrameworkCore;
using HospitalManagement.Services;
using HospitalManagement.Services.VnPay;
using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;
using System.Net;
using System.Text.RegularExpressions;

public class PaymentController : Controller
{
    private readonly HospitalManagementContext _context;
    private readonly IVnPayService _vnPayService;

    private readonly IConfiguration _configuration; 

    public PaymentController(HospitalManagementContext context, IVnPayService vnPayService,IConfiguration configuration)
    {
        _context = context;
        _vnPayService = vnPayService;
        _configuration = configuration; // gán vào field

    }

    [HttpGet]
    public async Task<IActionResult> PayStartAppointment(int invoiceId)
    {
        var invoicedetail = await _context.InvoiceDetails
            .Include(x => x.Appointment)
            .FirstOrDefaultAsync(a => a.AppointmentId == invoiceId);


        if (invoicedetail == null)
            return NotFound();

        var paymentModel = new VnPayViewModel
        {
            Name = "Name",
            Amount = invoicedetail.UnitPrice,
            OrderDescription = $"{invoicedetail.ItemType} - {invoicedetail.ItemName}",
            OrderType = "other",
            InvoiceId = invoiceId
        };

        string paymentUrl = _vnPayService.CreatePaymentUrl(paymentModel, HttpContext);
        ViewBag.PaymentUrl = paymentUrl;

        return View(invoicedetail);
    }

    public IActionResult GenerateQr(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return NotFound();

        var decodedUrl = WebUtility.UrlDecode(url);

        using (var qrGenerator = new QRCodeGenerator())
        {
            var qrData = qrGenerator.CreateQrCode(decodedUrl, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new SvgQRCode(qrData);
            var svgImage = qrCode.GetGraphic(5);

            // Trả về SVG (vector, tốt hơn PNG)
            return Content(svgImage, "image/svg+xml");
        }
    }

    [HttpGet]
    public IActionResult PaymentCallbackVnpay()
    {
        var vnpayData = Request.Query;
        var responseData = new SortedList<string, string>();

        foreach (var key in vnpayData.Keys)
        {
            if (key.StartsWith("vnp_"))
            {
                responseData.Add(key, vnpayData[key]);
            }
        }

        // Lấy mã giao dịch và mã đơn hàng
        string responseCode = responseData["vnp_ResponseCode"];
        string txnRef = responseData["vnp_TxnRef"];

        switch (responseCode)
        {
            case "00":
                // Lấy OrderInfo và trích InvoiceId
                string orderInfo = responseData["vnp_OrderInfo"];
                int invoiceId = 0;

                if (orderInfo.StartsWith("Invoice:"))
                {
                    // Cắt chuỗi trước dấu " -"
                    var match = Regex.Match(orderInfo, @"Invoice:(\d+)");
                    if (match.Success)
                    {
                        invoiceId = int.Parse(match.Groups[1].Value);
                    }
                }

                if (invoiceId > 0)
                {
                    var invoiceDetail = _context.InvoiceDetails
                        .Include(i => i.Appointment)
                        .FirstOrDefault(i => i.InvoiceDetailId == invoiceId);

                    if (invoiceDetail != null)
                    {
                        // Cập nhật trạng thái thanh toán cho InvoiceDetail
                        invoiceDetail.PaymentMethod = "Banking";
                        invoiceDetail.PaymentStatus = "Paid";
                        
                        // Console.WriteLine("======== APP ID: " + invoiceDetail.Appointment.AppointmentId);

                        // var app = _context.Appointments.FirstOrDefault(app => app.AppointmentId == invoiceDetail.Appointment.AppointmentId);

                        // if (app != null)
                        // {
                        //     app.IsServiceOrPackagePaid = true;
                        // }
                        // Console.WriteLine("======== APP check: " + invoiceDetail.Appointment.IsServiceOrPackagePaid);

                        _context.SaveChanges();
                    }

                    ViewBag.Message = "Thanh toán thành công.";
                }
                else
                {
                    ViewBag.Message = "Không xác định được hóa đơn cần cập nhật.";
                }

                break;
            case "07":
                ViewBag.Message = "Giao dịch bị nghi ngờ gian lận.";
                break;
            case "09":
                ViewBag.Message = "Tài khoản chưa đăng ký Internet Banking.";
                break;
            case "10":
                ViewBag.Message = "Xác thực giao dịch thất bại (sai OTP).";
                break;
            case "11":
                ViewBag.Message = "Giao dịch hết hạn.";
                break;
            case "12":
                ViewBag.Message = "Dữ liệu gửi đi không hợp lệ.";
                break;
            case "13":
                ViewBag.Message = "Tài khoản bị khóa hoặc không đủ số dư.";
                break;
            case "24":
                ViewBag.Message = "Giao dịch đã bị hủy bởi người dùng.";
                break;
            case "51":
                ViewBag.Message = "Tài khoản không đủ số dư.";
                break;
            case "65":
                ViewBag.Message = "Tài khoản vượt quá hạn mức giao dịch.";
                break;
            case "75":
                ViewBag.Message = "Ngân hàng đang bảo trì.";
                break;
            case "91":
                ViewBag.Message = "Không nhận được phản hồi từ ngân hàng.";
                break;
            case "93":
                ViewBag.Message = "Lỗi hệ thống ngân hàng hoặc VNPAY.";
                break;
            case "94":
                ViewBag.Message = "Giao dịch bị trùng lặp.";
                break;
            case "95":
                ViewBag.Message = "Không tìm thấy giao dịch.";
                break;
            case "97":
                ViewBag.Message = "Lỗi CSRF hoặc tham số không hợp lệ.";
                break;
            case "99":
                ViewBag.Message = "Lỗi không xác định.";
                break;
            default:
                ViewBag.Message = $"Giao dịch thất bại. Mã lỗi: {responseCode}";
                break;
        }

        return View("PaymentResult");
    }




}
