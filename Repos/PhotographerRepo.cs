using Microsoft.EntityFrameworkCore;
using PixelPerfect.Entities;

namespace PixelPerfect.Repos
{
    public class PhotographerRepo
    {
        private readonly PhotobookingdbContext _context;

        public PhotographerRepo(PhotobookingdbContext context)
        {
            _context = context;
        }

        public async Task<Photographer?> GetByIdAsync(int photographerId)
        {
            return await _context.Photographers
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.PhotographerId == photographerId);
        }

        public async Task<Photographer?> GetByUserIdAsync(int userId)
        {
            return await _context.Photographers
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.UserId == userId);
        }

        public async Task<List<Photographer>> GetAllAsync(bool verifiedOnly = false)
        {
            var query = _context.Photographers
                .Include(p => p.User)
                .AsQueryable();

            if (verifiedOnly)
                query = query.Where(p => p.IsVerified);

            return await query.ToListAsync();
        }

        public async Task<Photographer> CreateAsync(Photographer photographer)
        {
            _context.Photographers.Add(photographer);
            await _context.SaveChangesAsync();
            return photographer;
        }

        public async Task<bool> UpdateAsync(Photographer photographer)
        {
            _context.Photographers.Update(photographer);
            var affected = await _context.SaveChangesAsync();
            return affected > 0;
        }

        public async Task<bool> DeleteAsync(int photographerId)
        {
            var photographer = await GetByIdAsync(photographerId);
            if (photographer == null) return false;

            _context.Photographers.Remove(photographer);
            var affected = await _context.SaveChangesAsync();
            return affected > 0;
        }
    }
}