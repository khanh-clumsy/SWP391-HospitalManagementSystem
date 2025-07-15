using Microsoft.AspNetCore.Mvc;

namespace HospitalManagement.Controllers
{
    public class BaseController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public void SetReturnUrl()
        {
            var referer = Request.Headers["Referer"].ToString();

            if (!string.IsNullOrEmpty(referer) && Url.IsLocalUrl(referer))
            {
                TempData["PreviousUrl"] = referer;
            }
        }

        public string GetSafeBackUrl()
        {
            var backUrl = TempData["PreviousUrl"] as string;
            return backUrl ?? Url.Action("Index", "Home");
        }

    }
}
