using HospitalManagement.Data;
using HospitalManagement.Models;
using Microsoft.EntityFrameworkCore;
namespace HospitalManagement.Repositories
{
    public class SlotRepository : ISlotRepository
    {
        private readonly HospitalManagementContext _context;
        public SlotRepository(HospitalManagementContext context)
        {
            _context = context;
        }
        public async Task<List<Slot>> GetAllSlotsAsync()
        {
            return await _context.Slots.OrderBy(s => s.StartTime).ToListAsync();
        }
    }
}
