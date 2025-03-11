using Microsoft.EntityFrameworkCore;
using PixelPerfect.Core.Entities;
using PixelPerfect.Core.Models;
using PixelPerfect.DataAccess.Repos;

namespace PixelPerfect.Services.Impl
{
    public class BookingService : IBookingService
    {
        private PhotoBookingDbContext _context;
        private readonly BookingRepo _bookingRepo;
        private readonly PhotographerRepo _photographerRepo;

        public BookingService(PhotoBookingDbContext context, BookingRepo bookingRepo, PhotographerRepo photographerRepo)
        {
            _context = context;
            _bookingRepo = bookingRepo;
            _photographerRepo = photographerRepo;
        }

        public async Task<BookingDto> GetBookingByIdAsync(int bookingId)
        {
            var booking = await _bookingRepo.GetByIdAsync(bookingId);
            if (booking == null)
                return null;

            return MapToDto(booking);
        }

        public async Task<List<BookingDto>> GetBookingsByUserIdAsync(int userId)
        {
            var bookings = await _bookingRepo.GetByUserIdAsync(userId);
            return bookings.Select(MapToDto).ToList();
        }

        public async Task<List<BookingDto>> GetBookingsByPhotographerIdAsync(int photographerId)
        {
            var bookings = await _bookingRepo.GetByPhotographerIdAsync(photographerId);
            return bookings.Select(MapToDto).ToList();
        }

        public async Task<List<BookingDto>> SearchBookingsAsync(BookingSearchParams searchParams)
        {
            var bookings = await _bookingRepo.SearchAsync(
                searchParams.Status,
                searchParams.StartDate,
                searchParams.EndDate,
                searchParams.PhotographerId,
                searchParams.UserId
            );

            return bookings.Select(MapToDto).ToList();
        }

        public async Task<BookingDto> CreateBookingAsync(int userId, BookingCreateRequest request)
        {
            // 检查摄影师是否存在
            var photographer = await _photographerRepo.GetByIdAsync(request.PhotographerId);
            if (photographer == null)
                throw new KeyNotFoundException($"Photographer with ID {request.PhotographerId} not found.");

            // 检查用户是否存在
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new KeyNotFoundException($"User with ID {userId} not found.");

            // 计算初始金额 (基于所选服务)
            decimal initialAmount = request.Services.Sum(s => s.Price);

            // 创建预约
            var newBooking = new Booking
            {
                UserId = userId,
                PhotographerId = request.PhotographerId,
                BookingDate = request.BookingDate,
                Location = request.Location,
                Status = "Pending", // 初始状态为待确认
                InitialAmount = initialAmount,
                Requirements = request.Requirements,
                PhotoCount = request.PhotoCount,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsPublic = false // 默认不公开
            };

            // 使用事务确保预约和服务一起创建
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var createdBooking = await _bookingRepo.CreateAsync(newBooking);

                    // 添加预约服务
                    foreach (var serviceRequest in request.Services)
                    {
                        var bookingService = new Bookingservice
                        {
                            BookingId = createdBooking.BookingId,
                            ServiceName = serviceRequest.ServiceName,
                            Description = serviceRequest.Description,
                            Price = serviceRequest.Price
                        };

                        _context.Bookingservices.Add(bookingService);
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // 重新获取完整的预约信息（包括关联的服务）
                    return await GetBookingByIdAsync(createdBooking.BookingId);
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }

        public async Task<bool> UpdateBookingStatusAsync(int bookingId, string status)
        {
            var booking = await _bookingRepo.GetByIdAsync(bookingId);
            if (booking == null)
                throw new KeyNotFoundException($"Booking with ID {bookingId} not found.");

            // 验证状态变更是否有效
            var validStatuses = new[] { "Pending", "Confirmed", "InProgress", "Completed", "Cancelled", "Rejected" };
            if (!validStatuses.Contains(status))
                throw new ArgumentException($"Invalid booking status: {status}");

            booking.Status = status;
            return await _bookingRepo.UpdateAsync(booking);
        }

        public async Task<bool> UpdateBookingFinalAmountAsync(int bookingId, decimal finalAmount)
        {
            var booking = await _bookingRepo.GetByIdAsync(bookingId);
            if (booking == null)
                throw new KeyNotFoundException($"Booking with ID {bookingId} not found.");

            // 验证金额是否合理
            if (finalAmount <= 0)
                throw new ArgumentException("Final amount must be greater than zero.");

            booking.FinalAmount = finalAmount;
            return await _bookingRepo.UpdateAsync(booking);
        }

        public async Task<bool> UpdateBookingPublicStatusAsync(int bookingId, bool isPublic)
        {
            var booking = await _bookingRepo.GetByIdAsync(bookingId);
            if (booking == null)
                throw new KeyNotFoundException($"Booking with ID {bookingId} not found.");

            booking.IsPublic = isPublic;
            return await _bookingRepo.UpdateAsync(booking);
        }

        public async Task<bool> IsUserBookingAsync(int bookingId, int userId)
        {
            var booking = await _bookingRepo.GetByIdAsync(bookingId);
            if (booking == null)
                return false;

            return booking.UserId == userId;
        }

        public async Task<bool> IsPhotographerBookingAsync(int bookingId, int photographerId)
        {
            var booking = await _bookingRepo.GetByIdAsync(bookingId);
            if (booking == null)
                return false;

            return booking.PhotographerId == photographerId;
        }

        // 辅助方法 - 实体映射到DTO
        private BookingDto MapToDto(Booking booking)
        {
            return new BookingDto
            {
                BookingId = booking.BookingId,
                UserId = booking.UserId,
                Username = booking.User?.Username,
                PhotographerId = booking.PhotographerId,
                PhotographerName = booking.Photographer?.User?.Username,
                BookingDate = booking.BookingDate,
                Location = booking.Location,
                Status = booking.Status,
                InitialAmount = booking.InitialAmount,
                FinalAmount = booking.FinalAmount,
                Requirements = booking.Requirements,
                PhotoCount = booking.PhotoCount,
                CreatedAt = booking.CreatedAt,
                UpdatedAt = booking.UpdatedAt,
                IsPublic = booking.IsPublic,
                Services = booking.Bookingservices.Select(s => new BookingServiceDto
                {
                    ServiceId = s.ServiceId,
                    ServiceName = s.ServiceName,
                    Description = s.Description,
                    Price = s.Price
                }).ToList()
            };
        }
    }
}