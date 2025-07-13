using HospitalManagement.ViewModels.VnPay;
using Microsoft.AspNetCore.Http;
namespace HospitalManagement.Services.VnPay
{
    public interface IVnPayService
    {
        string CreatePaymentUrl(VnPayViewModel model, HttpContext context);
        VnPayResponseViewModel PaymentExecute(IQueryCollection collections);
    }
}
