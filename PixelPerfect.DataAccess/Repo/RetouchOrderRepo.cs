using Microsoft.EntityFrameworkCore;
using PixelPerfect.Core.Entities;

namespace PixelPerfect.DataAccess.Repos;

public class RetouchOrderRepo
{
    private PhotoBookingDbContext _context;

    public RetouchOrderRepo(PhotoBookingDbContext context)
    {
        _context = context;
    }

    public async Task<Retouchorder?> GetByIdAsync(int orderId)
    {
        return await _context.Retouchorders
            .Include(o => o.User)
            .Include(o => o.Retoucher)
                .ThenInclude(r => r.User)
            .Include(o => o.Photo)
            .FirstOrDefaultAsync(o => o.OrderId == orderId);
    }

    public async Task<List<Retouchorder>> GetByUserIdAsync(int userId)
    {
        return await _context.Retouchorders
            .Include(o => o.User)
            .Include(o => o.Retoucher)
                .ThenInclude(r => r.User)
            .Include(o => o.Photo)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Retouchorder>> GetByRetoucherIdAsync(int retoucherId)
    {
        return await _context.Retouchorders
            .Include(o => o.User)
            .Include(o => o.Retoucher)
                .ThenInclude(r => r.User)
            .Include(o => o.Photo)
            .Where(o => o.RetoucherId == retoucherId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Retouchorder>> GetByPhotoIdAsync(int photoId)
    {
        return await _context.Retouchorders
            .Include(o => o.User)
            .Include(o => o.Retoucher)
                .ThenInclude(r => r.User)
            .Include(o => o.Photo)
            .Where(o => o.PhotoId == photoId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Retouchorder>> SearchAsync(int? userId = null, int? retoucherId = null,
                                                    int? photoId = null, string status = null,
                                                    DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.Retouchorders
            .Include(o => o.User)
            .Include(o => o.Retoucher)
                .ThenInclude(r => r.User)
            .Include(o => o.Photo)
            .AsQueryable();

        if (userId.HasValue)
            query = query.Where(o => o.UserId == userId.Value);

        if (retoucherId.HasValue)
            query = query.Where(o => o.RetoucherId == retoucherId.Value);

        if (photoId.HasValue)
            query = query.Where(o => o.PhotoId == photoId.Value);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(o => o.Status == status);

        if (startDate.HasValue)
            query = query.Where(o => o.CreatedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(o => o.CreatedAt <= endDate.Value);

        return await query.OrderByDescending(o => o.CreatedAt).ToListAsync();
    }

    public async Task<Retouchorder> CreateAsync(Retouchorder order)
    {
        _context.Retouchorders.Add(order);
        await _context.SaveChangesAsync();
        return order;
    }

    public async Task<bool> UpdateAsync(Retouchorder order)
    {
        _context.Retouchorders.Update(order);
        var affected = await _context.SaveChangesAsync();
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(int orderId)
    {
        var order = await GetByIdAsync(orderId);
        if (order == null) return false;

        _context.Retouchorders.Remove(order);
        var affected = await _context.SaveChangesAsync();
        return affected > 0;
    }
}