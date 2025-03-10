using Microsoft.EntityFrameworkCore;
using PixelPerfect.Entities;

namespace PixelPerfect.Repos
{
    public class PhotoRepo
    {
        private readonly PhotobookingdbContext _context;

        public PhotoRepo(PhotobookingdbContext context)
        {
            _context = context;
        }

        public async Task<Photo?> GetByIdAsync(int photoId)
        {
            return await _context.Photos
                .Include(p => p.Booking)
                    .ThenInclude(b => b.User)
                .Include(p => p.Booking)
                    .ThenInclude(b => b.Photographer)
                        .ThenInclude(ph => ph.User)
                .FirstOrDefaultAsync(p => p.PhotoId == photoId);
        }

        public async Task<List<Photo>> GetByBookingIdAsync(int bookingId)
        {
            return await _context.Photos
                .Where(p => p.BookingId == bookingId)
                .OrderByDescending(p => p.UploadedAt)
                .ToListAsync();
        }

        public async Task<List<Photo>> GetByUserBookingsAsync(int userId)
        {
            return await _context.Photos
                .Include(p => p.Booking)
                .Where(p => p.Booking.UserId == userId)
                .OrderByDescending(p => p.UploadedAt)
                .ToListAsync();
        }

        public async Task<List<Photo>> GetByPhotographerBookingsAsync(int photographerId)
        {
            return await _context.Photos
                .Include(p => p.Booking)
                .Where(p => p.Booking.PhotographerId == photographerId)
                .OrderByDescending(p => p.UploadedAt)
                .ToListAsync();
        }

        public async Task<List<Photo>> SearchAsync(int? bookingId = null, int? userId = null,
            int? photographerId = null, bool? isPublic = null, bool? clientApproved = null,
            DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Photos
                .Include(p => p.Booking)
                    .ThenInclude(b => b.User)
                .Include(p => p.Booking)
                    .ThenInclude(b => b.Photographer)
                        .ThenInclude(ph => ph.User)
                .AsQueryable();

            if (bookingId.HasValue)
                query = query.Where(p => p.BookingId == bookingId.Value);

            if (userId.HasValue)
                query = query.Where(p => p.Booking.UserId == userId.Value);

            if (photographerId.HasValue)
                query = query.Where(p => p.Booking.PhotographerId == photographerId.Value);

            if (isPublic.HasValue)
                query = query.Where(p => p.IsPublic == isPublic.Value);

            if (clientApproved.HasValue)
                query = query.Where(p => p.ClientApproved == clientApproved.Value);

            if (startDate.HasValue)
                query = query.Where(p => p.UploadedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(p => p.UploadedAt <= endDate.Value);

            return await query.OrderByDescending(p => p.UploadedAt).ToListAsync();
        }

        public async Task<Photo> CreateAsync(Photo photo)
        {
            _context.Photos.Add(photo);
            await _context.SaveChangesAsync();
            return photo;
        }

        public async Task<bool> UpdateAsync(Photo photo)
        {
            _context.Photos.Update(photo);
            var affected = await _context.SaveChangesAsync();
            return affected > 0;
        }

        public async Task<bool> DeleteAsync(int photoId)
        {
            var photo = await GetByIdAsync(photoId);
            if (photo == null) return false;

            _context.Photos.Remove(photo);
            var affected = await _context.SaveChangesAsync();
            return affected > 0;
        }

        public async Task<List<Booking>> GetBookingsWithPhotosAsync(int userId)
        {
            return await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Photographer)
                    .ThenInclude(p => p.User)
                .Include(b => b.Photos)
                .Where(b => b.UserId == userId && b.Photos.Any())
                .ToListAsync();
        }

        public async Task<List<Booking>> GetPhotographerBookingsWithPhotosAsync(int photographerId)
        {
            return await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Photographer)
                    .ThenInclude(p => p.User)
                .Include(b => b.Photos)
                .Where(b => b.PhotographerId == photographerId && b.Photos.Any())
                .ToListAsync();
        }
    }
}