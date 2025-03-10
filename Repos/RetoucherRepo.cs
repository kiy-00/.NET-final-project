using Microsoft.EntityFrameworkCore;
using PixelPerfect.Entities;

namespace PixelPerfect.Repos
{
    public class RetoucherRepo
    {
        private readonly PhotobookingdbContext _context;

        public RetoucherRepo(PhotobookingdbContext context)
        {
            _context = context;
        }

        public async Task<Retoucher?> GetByIdAsync(int retoucherId)
        {
            return await _context.Retouchers
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.RetoucherId == retoucherId);
        }

        public async Task<Retoucher?> GetByUserIdAsync(int userId)
        {
            return await _context.Retouchers
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.UserId == userId);
        }

        public async Task<List<Retoucher>> GetAllAsync(bool verifiedOnly = false)
        {
            var query = _context.Retouchers
                .Include(r => r.User)
                .AsQueryable();

            if (verifiedOnly)
                query = query.Where(r => r.IsVerified);

            return await query.ToListAsync();
        }

        public async Task<Retoucher> CreateAsync(Retoucher retoucher)
        {
            _context.Retouchers.Add(retoucher);
            await _context.SaveChangesAsync();
            return retoucher;
        }

        public async Task<bool> UpdateAsync(Retoucher retoucher)
        {
            _context.Retouchers.Update(retoucher);
            var affected = await _context.SaveChangesAsync();
            return affected > 0;
        }

        public async Task<bool> DeleteAsync(int retoucherId)
        {
            var retoucher = await GetByIdAsync(retoucherId);
            if (retoucher == null) return false;

            _context.Retouchers.Remove(retoucher);
            var affected = await _context.SaveChangesAsync();
            return affected > 0;
        }
    }
}