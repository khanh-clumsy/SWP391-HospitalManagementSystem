using System;
using HospitalManagement.Data;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Helpers
{
    public class BatchHelper
    {
        public static async Task<int?> GetOpenBatchAsync(HospitalManagementContext context, int appointmentId)
        {
            return await context.Trackings
                .Where(t => t.AppointmentId == appointmentId && t.TestRecordId != null)
                .GroupBy(t => t.TrackingBatch)
                .Where(g => g.All(x => x.TestRecord != null && x.TestRecord.TestStatus != AppConstants.TestStatus.Completed))
                .OrderByDescending(g => g.Key)
                .Select(g => (int?)g.Key)
                .FirstOrDefaultAsync();
        }

        public static async Task<int> GetOpenOrNewBatchAsync(HospitalManagementContext context, int appointmentId)
        {
            // 1. Tìm batch chưa có test nào Completed
            int? openBatch = await GetOpenBatchAsync(context, appointmentId);

            // 2. Nếu có thì dùng, không thì tạo batch mới
            if (openBatch.HasValue)
                return openBatch.Value;

            int newBatch = ((await context.Trackings
                .Where(t => t.AppointmentId == appointmentId)
                .MaxAsync(t => (int?)t.TrackingBatch)) ?? 0) + 1;

            return newBatch;
        }
    }
}
