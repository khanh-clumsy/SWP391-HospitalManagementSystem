using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Concurrent;

namespace HospitalManagement.Filters
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class PreventSpamAttribute : ActionFilterAttribute
    {
        private static readonly ConcurrentDictionary<string, DateTime> _lastPostTimes = new();
        public int Seconds { get; set; } = 2; // khoảng cách tối thiểu giữa 2 lần submit mặc định 2s

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var request = context.HttpContext.Request;

            // Chỉ áp dụng cho POST
            if (request.Method != "POST")
                return;

            var ip = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var now = DateTime.UtcNow;

            if (_lastPostTimes.TryGetValue(ip, out var lastTime))
            {
                if ((now - lastTime).TotalSeconds < Seconds)
                {
                    if (context.Controller is Controller controller)
                    {
                        controller.TempData["error"] = "Bạn đang gửi quá nhanh. Vui lòng thử lại sau vài giây.";
                    }

                    context.Result = new RedirectResult("/Home/TooMuchAttempt");
                    return;
                }
            }

            _lastPostTimes[ip] = now;
        }
    }
}
