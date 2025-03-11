using Microsoft.EntityFrameworkCore;
using PixelPerfect.Core.Entities;

namespace PixelPerfect.DataAccess.Repos
{
    public class BookingRepo
    {
        private PhotoBookingDbContext _context;

        public BookingRepo(PhotoBookingDbContext context)
        {
            _context = context;
        }

        public async Task<Booking?> GetByIdAsync(int bookingId)
        {
            return await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Photographer)
                    .ThenInclude(p => p.User)
                .Include(b => b.Bookingservices)
                .Include(b => b.Photos)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);
        }

        public async Task<List<Booking>> GetByUserIdAsync(int userId)
        {
            return await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Photographer)
                    .ThenInclude(p => p.User)
                .Include(b => b.Bookingservices)
                .Where(b => b.UserId == userId)
                .ToListAsync();
        }

        public async Task<List<Booking>> GetByPhotographerIdAsync(int photographerId)
        {
            return await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Photographer)
                    .ThenInclude(p => p.User)
                .Include(b => b.Bookingservices)
                .Where(b => b.PhotographerId == photographerId)
                .ToListAsync();
        }

        public async Task<List<Booking>> SearchAsync(string status = null, DateTime? startDate = null,
            DateTime? endDate = null, int? photographerId = null, int? userId = null)
        {
            var query = _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Photographer)
                    .ThenInclude(p => p.User)
                .Include(b => b.Bookingservices)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(b => b.Status == status);

            if (startDate.HasValue)
                query = query.Where(b => b.BookingDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(b => b.BookingDate <= endDate.Value);

            if (photographerId.HasValue)
                query = query.Where(b => b.PhotographerId == photographerId.Value);

            if (userId.HasValue)
                query = query.Where(b => b.UserId == userId.Value);

            return await query.ToListAsync();
        }

        public async Task<Booking> CreateAsync(Booking booking)
        {
            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();
            return booking;
        }

        public async Task<bool> UpdateAsync(Booking booking)
        {
            booking.UpdatedAt = DateTime.UtcNow;
            _context.Bookings.Update(booking);
            var affected = await _context.SaveChangesAsync();
            return affected > 0;
        }

        public async Task<bool> DeleteAsync(int bookingId)
        {
            var booking = await GetByIdAsync(bookingId);
            if (booking == null) return false;

            _context.Bookings.Remove(booking);
            var affected = await _context.SaveChangesAsync();
            return affected > 0;
        }
    }
}