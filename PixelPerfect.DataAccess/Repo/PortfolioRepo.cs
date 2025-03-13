// Repos/PortfolioRepo.cs
using Microsoft.EntityFrameworkCore;
using PixelPerfect.Core.Entities;

namespace PixelPerfect.DataAccess.Repos
{
    public class PortfolioRepo
    {
        private PhotoBookingDbContext _context;

        public PortfolioRepo(PhotoBookingDbContext context)
        {
            _context = context;
        }

        // 摄影师作品集方法
        public async Task<Photographerportfolio> GetPhotographerPortfolioByIdAsync(int portfolioId)
        {
            return await _context.Photographerportfolios
                .Include(p => p.Photographer)
                    .ThenInclude(p => p.User)
                .Include(p => p.Portfolioitems.Where(i => i.PortfolioType == "Photographer"))
                .FirstOrDefaultAsync(p => p.PortfolioId == portfolioId);
        }

        public async Task<List<Photographerportfolio>> GetPhotographerPortfoliosByPhotographerIdAsync(int photographerId, bool publicOnly = true)
        {
            var query = _context.Photographerportfolios
                .Include(p => p.Photographer)
                    .ThenInclude(p => p.User)
                .Include(p => p.Portfolioitems.Where(i => i.PortfolioType == "Photographer"))
                .Where(p => p.PhotographerId == photographerId);

            if (publicOnly)
                query = query.Where(p => p.IsPublic == true);

            return await query.ToListAsync();
        }

        public async Task<List<Photographerportfolio>> GetAllPhotographerPortfoliosAsync(bool publicOnly = true)
        {
            var query = _context.Photographerportfolios
                .Include(p => p.Photographer)
                  .ThenInclude(p => p.User)
               .Include(p => p.Portfolioitems.Where(i => i.PortfolioType == "Photographer"))
               .AsQueryable();

            if (publicOnly)
                query = query.Where(p => p.IsPublic == true);

            return await query.ToListAsync();
        }

        public async Task<Photographerportfolio> CreatePhotographerPortfolioAsync(Photographerportfolio portfolio)
        {
            _context.Photographerportfolios.Add(portfolio);
            await _context.SaveChangesAsync();
            return portfolio;
        }

        public async Task<bool> UpdatePhotographerPortfolioAsync(Photographerportfolio portfolio)
        {
            _context.Photographerportfolios.Update(portfolio);
            var affected = await _context.SaveChangesAsync();
            return affected > 0;
        }

        public async Task<bool> DeletePhotographerPortfolioAsync(int portfolioId)
        {
            var portfolio = await _context.Photographerportfolios.FindAsync(portfolioId);
            if (portfolio == null) return false;

            _context.Photographerportfolios.Remove(portfolio);
            var affected = await _context.SaveChangesAsync();
            return affected > 0;
        }

        // 修图师作品集方法
        public async Task<Retoucherportfolio> GetRetoucherPortfolioByIdAsync(int portfolioId)
        {
            return await _context.Retoucherportfolios
                .Include(p => p.Retoucher)
                    .ThenInclude(p => p.User)
                .Include(p => p.Portfolioitems.Where(i => i.PortfolioType == "Retoucher"))
                .FirstOrDefaultAsync(p => p.PortfolioId == portfolioId);
        }

        public async Task<List<Retoucherportfolio>> GetRetoucherPortfoliosByRetoucherIdAsync(int retoucherId, bool publicOnly = true)
        {
            var query = _context.Retoucherportfolios
                .Include(p => p.Retoucher)
                    .ThenInclude(p => p.User)
                .Include(p => p.Portfolioitems.Where(i => i.PortfolioType == "Retoucher"))
                .Where(p => p.RetoucherId == retoucherId);

            if (publicOnly)
                query = query.Where(p => p.IsPublic == true);

            return await query.ToListAsync();
        }

        public async Task<List<Retoucherportfolio>> GetAllRetoucherPortfoliosAsync(bool publicOnly = true)
        {
            var query = _context.Retoucherportfolios
                .Include(p => p.Retoucher)
                    .ThenInclude(p => p.User)
                .Include(p => p.Portfolioitems.Where(i => i.PortfolioType == "Retoucher"))
                .AsQueryable();

            if (publicOnly)
                query = query.Where(p => p.IsPublic == true);

            return await query.ToListAsync();
        }

        public async Task<Retoucherportfolio> CreateRetoucherPortfolioAsync(Retoucherportfolio portfolio)
        {
            _context.Retoucherportfolios.Add(portfolio);
            await _context.SaveChangesAsync();
            return portfolio;
        }

        public async Task<bool> UpdateRetoucherPortfolioAsync(Retoucherportfolio portfolio)
        {
            _context.Retoucherportfolios.Update(portfolio);
            var affected = await _context.SaveChangesAsync();
            return affected > 0;
        }

        public async Task<bool> DeleteRetoucherPortfolioAsync(int portfolioId)
        {
            var portfolio = await _context.Retoucherportfolios.FindAsync(portfolioId);
            if (portfolio == null) return false;

            _context.Retoucherportfolios.Remove(portfolio);
            var affected = await _context.SaveChangesAsync();
            return affected > 0;
        }

        // 作品项方法
        public async Task<Portfolioitem> GetPortfolioItemByIdAsync(int itemId)
        {
            return await _context.Portfolioitems
                .Include(i => i.AfterImage)
                .FirstOrDefaultAsync(i => i.ItemId == itemId);
        }

        public async Task<List<Portfolioitem>> GetPortfolioitemsByPortfolioIdAsync(int portfolioId, string portfolioType)
        {
            return await _context.Portfolioitems
                .Include(i => i.AfterImage)
                .Where(i => i.PortfolioId == portfolioId && i.PortfolioType == portfolioType)
                .ToListAsync();
        }

        // 向后兼容方法
        public async Task<List<Portfolioitem>> GetPortfolioitemsByPortfolioIdAsync(int portfolioId)
        {
            return await _context.Portfolioitems
                .Include(i => i.AfterImage)
                .Where(i => i.PortfolioId == portfolioId)
                .ToListAsync();
        }

        public async Task<Portfolioitem> CreatePortfolioItemAsync(Portfolioitem item, bool isRetoucher = false)
        {
            // 设置作品集类型
            item.PortfolioType = isRetoucher ? "Retoucher" : "Photographer";

            _context.Portfolioitems.Add(item);
            await _context.SaveChangesAsync();
            return item;
        }

        // 保留原始方法以向后兼容，但内部实现已更新
        public async Task<Portfolioitem> CreatePortfolioItemAsync(Portfolioitem item)
        {
            // 检查是否为修图师作品集
            bool isRetoucher = await _context.Retoucherportfolios.AnyAsync(p => p.PortfolioId == item.PortfolioId);
            return await CreatePortfolioItemAsync(item, isRetoucher);
        }

        public async Task<bool> UpdatePortfolioItemAsync(Portfolioitem item)
        {
            // 如果没设置PortfolioType，自动检测并设置
            if (string.IsNullOrEmpty(item.PortfolioType))
            {
                bool isRetoucher = await _context.Retoucherportfolios.AnyAsync(p => p.PortfolioId == item.PortfolioId);
                item.PortfolioType = isRetoucher ? "Retoucher" : "Photographer";
            }

            _context.Portfolioitems.Update(item);
            var affected = await _context.SaveChangesAsync();
            return affected > 0;
        }

        public async Task<bool> DeletePortfolioItemAsync(int itemId)
        {
            var item = await _context.Portfolioitems.FindAsync(itemId);
            if (item == null) return false;

            _context.Portfolioitems.Remove(item);
            var affected = await _context.SaveChangesAsync();
            return affected > 0;
        }
    }
}