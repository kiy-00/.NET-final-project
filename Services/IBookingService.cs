using PixelPerfect.Models;

namespace PixelPerfect.Services
{
    public interface IBookingService
    {
        // 获取预约
        Task<BookingDto> GetBookingByIdAsync(int bookingId);
        Task<List<BookingDto>> GetBookingsByUserIdAsync(int userId);
        Task<List<BookingDto>> GetBookingsByPhotographerIdAsync(int photographerId);
        Task<List<BookingDto>> SearchBookingsAsync(BookingSearchParams searchParams);

        // 创建预约
        Task<BookingDto> CreateBookingAsync(int userId, BookingCreateRequest request);

        // 更新预约
        Task<bool> UpdateBookingStatusAsync(int bookingId, string status);
        Task<bool> UpdateBookingFinalAmountAsync(int bookingId, decimal finalAmount);
        Task<bool> UpdateBookingPublicStatusAsync(int bookingId, bool isPublic);

        // 权限检查
        Task<bool> IsUserBookingAsync(int bookingId, int userId);
        Task<bool> IsPhotographerBookingAsync(int bookingId, int photographerId);
    }
}