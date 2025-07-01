using HospitalManagement.ViewModels.Booking;
using System.Threading.Channels;

namespace HospitalManagement.Services
{
    public class BookingQueueService
    {
        private readonly Channel<BookingRequest> _channel = Channel.CreateUnbounded<BookingRequest>();
        public async Task EnqueueAsync(BookingRequest request) => await _channel.Writer.WriteAsync(request);
        public ChannelReader<BookingRequest> Reader => _channel.Reader;
    }

}