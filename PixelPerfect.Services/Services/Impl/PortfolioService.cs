using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PixelPerfect.Core.Entities;
using PixelPerfect.Core.Models;
using PixelPerfect.DataAccess.Repos;
using System.Text.Json;

namespace PixelPerfect.Services.Impl
{
    public class PortfolioService : IPortfolioService
    {
        private PhotoBookingDbContext _context;
        private readonly PortfolioRepo _portfolioRepo;
        private readonly IWebHostEnvironment _environment;
        private readonly string _uploadDirectory;

        public PortfolioService(PhotoBookingDbContext context, PortfolioRepo portfolioRepo, IWebHostEnvironment environment)
        {
            _context = context;
            _portfolioRepo = portfolioRepo;
            _environment = environment;
            _uploadDirectory = Path.Combine(_environment.WebRootPath, "uploads", "portfolio");

            // 确保上传目录存在
            if (!Directory.Exists(_uploadDirectory))
                Directory.CreateDirectory(_uploadDirectory);
        }

        // 摄影师作品集方法
        public async Task<List<PhotographerPortfolioDto>> GetAllPhotographerPortfoliosAsync(PortfolioSearchParams searchParams)
        {
            var query = _context.Photographerportfolios
                .Include(p => p.Photographer)
                    .ThenInclude(p => p.User)
                .Include(p => p.Portfolioitems)
                .AsQueryable();

            if (searchParams.IsPublic.HasValue)
                query = query.Where(p => p.IsPublic == searchParams.IsPublic.Value);
            else
                query = query.Where(p => p.IsPublic == true); // 默认只返回公开的

            if (!string.IsNullOrEmpty(searchParams.Category))
                query = query.Where(p => p.Category == searchParams.Category);

            if (!string.IsNullOrEmpty(searchParams.Keyword))
                query = query.Where(p =>
                    p.Title.Contains(searchParams.Keyword) ||
                    (p.Description != null && p.Description.Contains(searchParams.Keyword)));

            var portfolios = await query.ToListAsync();
            return portfolios.Select(MapToPhotographerPortfolioDto).ToList();
        }

        public async Task<List<PhotographerPortfolioDto>> GetPhotographerPortfoliosByPhotographerIdAsync(int photographerId, PortfolioSearchParams searchParams)
        {
            var query = _context.Photographerportfolios
                .Include(p => p.Photographer)
                    .ThenInclude(p => p.User)
                .Include(p => p.Portfolioitems)
                .Where(p => p.PhotographerId == photographerId);

            if (searchParams.IsPublic.HasValue)
                query = query.Where(p => p.IsPublic == searchParams.IsPublic.Value);
            else
                query = query.Where(p => p.IsPublic == true); // 默认只返回公开的

            if (!string.IsNullOrEmpty(searchParams.Category))
                query = query.Where(p => p.Category == searchParams.Category);

            if (!string.IsNullOrEmpty(searchParams.Keyword))
                query = query.Where(p =>
                    p.Title.Contains(searchParams.Keyword) ||
                    (p.Description != null && p.Description.Contains(searchParams.Keyword)));

            var portfolios = await query.ToListAsync();
            return portfolios.Select(MapToPhotographerPortfolioDto).ToList();
        }

        public async Task<PhotographerPortfolioDto> GetPhotographerPortfolioByIdAsync(int portfolioId)
        {
            var portfolio = await _portfolioRepo.GetPhotographerPortfolioByIdAsync(portfolioId);
            if (portfolio == null)
                return null;

            return MapToPhotographerPortfolioDto(portfolio);
        }

        public async Task<PhotographerPortfolioDto> CreatePhotographerPortfolioAsync(int photographerId, CreatePhotographerPortfolioRequest request)
        {
            // 验证摄影师是否存在
            var photographer = await _context.Photographers.FindAsync(photographerId);
            if (photographer == null)
                throw new KeyNotFoundException($"Photographer with ID {photographerId} not found.");

            // 创建作品集
            var portfolio = new Photographerportfolio
            {
                PhotographerId = photographerId,
                Title = request.Title,
                Description = request.Description,
                Category = request.Category,
                IsPublic = request.IsPublic,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createdPortfolio = await _portfolioRepo.CreatePhotographerPortfolioAsync(portfolio);
            return MapToPhotographerPortfolioDto(createdPortfolio);
        }

        public async Task<bool> UpdatePhotographerPortfolioAsync(int portfolioId, UpdatePortfolioRequest request)
        {
            var portfolio = await _portfolioRepo.GetPhotographerPortfolioByIdAsync(portfolioId);
            if (portfolio == null)
                throw new KeyNotFoundException($"Portfolio with ID {portfolioId} not found.");

            // 更新字段
            if (!string.IsNullOrEmpty(request.Title))
                portfolio.Title = request.Title;

            if (request.Description != null)
                portfolio.Description = request.Description;

            if (!string.IsNullOrEmpty(request.Category))
                portfolio.Category = request.Category;

            if (request.IsPublic.HasValue)
                portfolio.IsPublic = request.IsPublic.Value;

            portfolio.UpdatedAt = DateTime.UtcNow;

            return await _portfolioRepo.UpdatePhotographerPortfolioAsync(portfolio);
        }

        public async Task<bool> DeletePhotographerPortfolioAsync(int portfolioId)
        {
            // 先删除作品集中的所有作品项
            var items = await _portfolioRepo.GetPortfolioitemsByPortfolioIdAsync(portfolioId);
            foreach (var item in items)
            {
                // 删除文件
                if (!string.IsNullOrEmpty(item.ImagePath))
                {
                    var filePath = Path.Combine(_environment.WebRootPath, item.ImagePath.TrimStart('/'));
                    if (File.Exists(filePath))
                        File.Delete(filePath);
                }

                await _portfolioRepo.DeletePortfolioItemAsync(item.ItemId);
            }

            // 删除作品集
            return await _portfolioRepo.DeletePhotographerPortfolioAsync(portfolioId);
        }

        // 修图师作品集方法
        public async Task<List<RetoucherPortfolioDto>> GetAllRetoucherPortfoliosAsync(PortfolioSearchParams searchParams)
        {
            var query = _context.Retoucherportfolios
                .Include(p => p.Retoucher)
                    .ThenInclude(p => p.User)
                .Include(p => p.Portfolioitems)
                .AsQueryable();

            if (searchParams.IsPublic.HasValue)
                query = query.Where(p => p.IsPublic == searchParams.IsPublic.Value);
            else
                query = query.Where(p => p.IsPublic == true); // 默认只返回公开的

            if (!string.IsNullOrEmpty(searchParams.Category))
                query = query.Where(p => p.Category == searchParams.Category);

            if (!string.IsNullOrEmpty(searchParams.Keyword))
                query = query.Where(p =>
                    p.Title.Contains(searchParams.Keyword) ||
                    (p.Description != null && p.Description.Contains(searchParams.Keyword)));

            var portfolios = await query.ToListAsync();
            return portfolios.Select(MapToRetoucherPortfolioDto).ToList();
        }

        public async Task<List<RetoucherPortfolioDto>> GetRetoucherPortfoliosByRetoucherIdAsync(int retoucherId, PortfolioSearchParams searchParams)
        {
            var query = _context.Retoucherportfolios
                .Include(p => p.Retoucher)
                    .ThenInclude(p => p.User)
                .Include(p => p.Portfolioitems)
                .Where(p => p.RetoucherId == retoucherId);

            if (searchParams.IsPublic.HasValue)
                query = query.Where(p => p.IsPublic == searchParams.IsPublic.Value);
            else
                query = query.Where(p => p.IsPublic == true); // 默认只返回公开的

            if (!string.IsNullOrEmpty(searchParams.Category))
                query = query.Where(p => p.Category == searchParams.Category);

            if (!string.IsNullOrEmpty(searchParams.Keyword))
                query = query.Where(p =>
                    p.Title.Contains(searchParams.Keyword) ||
                    (p.Description != null && p.Description.Contains(searchParams.Keyword)));

            var portfolios = await query.ToListAsync();
            return portfolios.Select(MapToRetoucherPortfolioDto).ToList();
        }

        public async Task<RetoucherPortfolioDto> GetRetoucherPortfolioByIdAsync(int portfolioId)
        {
            var portfolio = await _portfolioRepo.GetRetoucherPortfolioByIdAsync(portfolioId);
            if (portfolio == null)
                return null;

            return MapToRetoucherPortfolioDto(portfolio);
        }

        public async Task<RetoucherPortfolioDto> CreateRetoucherPortfolioAsync(int retoucherId, CreateRetoucherPortfolioRequest request)
        {
            // 验证修图师是否存在
            var retoucher = await _context.Retouchers.FindAsync(retoucherId);
            if (retoucher == null)
                throw new KeyNotFoundException($"Retoucher with ID {retoucherId} not found.");

            // 创建作品集
            var portfolio = new Retoucherportfolio
            {
                RetoucherId = retoucherId,
                Title = request.Title,
                Description = request.Description,
                Category = request.Category,
                IsPublic = request.IsPublic,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createdPortfolio = await _portfolioRepo.CreateRetoucherPortfolioAsync(portfolio);
            return MapToRetoucherPortfolioDto(createdPortfolio);
        }

        public async Task<bool> UpdateRetoucherPortfolioAsync(int portfolioId, UpdatePortfolioRequest request)
        {
            var portfolio = await _portfolioRepo.GetRetoucherPortfolioByIdAsync(portfolioId);
            if (portfolio == null)
                throw new KeyNotFoundException($"Portfolio with ID {portfolioId} not found.");

            // 更新字段
            if (!string.IsNullOrEmpty(request.Title))
                portfolio.Title = request.Title;

            if (request.Description != null)
                portfolio.Description = request.Description;

            if (!string.IsNullOrEmpty(request.Category))
                portfolio.Category = request.Category;

            if (request.IsPublic.HasValue)
                portfolio.IsPublic = request.IsPublic.Value;

            portfolio.UpdatedAt = DateTime.UtcNow;

            return await _portfolioRepo.UpdateRetoucherPortfolioAsync(portfolio);
        }

        public async Task<bool> DeleteRetoucherPortfolioAsync(int portfolioId)
        {
            // 先删除作品集中的所有作品项
            var items = await _portfolioRepo.GetPortfolioitemsByPortfolioIdAsync(portfolioId);
            foreach (var item in items)
            {
                // 删除文件
                if (!string.IsNullOrEmpty(item.ImagePath))
                {
                    var filePath = Path.Combine(_environment.WebRootPath, item.ImagePath.TrimStart('/'));
                    if (File.Exists(filePath))
                        File.Delete(filePath);
                }

                await _portfolioRepo.DeletePortfolioItemAsync(item.ItemId);
            }

            // 删除作品集
            return await _portfolioRepo.DeleteRetoucherPortfolioAsync(portfolioId);
        }

        // 作品项方法
        public async Task<PortfolioItemDto> GetPortfolioItemByIdAsync(int itemId)
        {
            var item = await _portfolioRepo.GetPortfolioItemByIdAsync(itemId);
            if (item == null)
                return null;

            return MapToPortfolioItemDto(item);
        }

        public async Task<PortfolioItemDto> AddItemToPhotographerPortfolioAsync(int portfolioId, IFormFile file, UploadPortfolioItemRequest request)
        {
            // 验证作品集是否存在
            var portfolio = await _portfolioRepo.GetPhotographerPortfolioByIdAsync(portfolioId);
            if (portfolio == null)
                throw new KeyNotFoundException($"Portfolio with ID {portfolioId} not found.");

            // 保存文件
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(_uploadDirectory, fileName);
            var relativePath = $"/uploads/portfolio/{fileName}";

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // 创建元数据
            var metadata = new
            {
                OriginalFileName = file.FileName,
                ContentType = file.ContentType,
                Size = file.Length,
                UploadedAt = DateTime.UtcNow
            };

            // 创建作品项
            var item = new Portfolioitem
            {
                PortfolioId = portfolioId,
                ImagePath = relativePath,
                Title = request.Title,
                Description = request.Description,
                Metadata = JsonSerializer.Serialize(metadata),
                CreatedAt = DateTime.UtcNow,
                IsBeforeImage = request.IsBeforeImage,
                AfterImageId = request.AfterImageId
            };

            var createdItem = await _portfolioRepo.CreatePortfolioItemAsync(item);

            // 更新作品集的最后修改时间
            portfolio.UpdatedAt = DateTime.UtcNow;
            await _portfolioRepo.UpdatePhotographerPortfolioAsync(portfolio);

            return MapToPortfolioItemDto(createdItem);
        }

        public async Task<PortfolioItemDto> AddItemToRetoucherPortfolioAsync(int portfolioId, IFormFile file, UploadPortfolioItemRequest request)
        {
            // 验证作品集是否存在
            var portfolio = await _portfolioRepo.GetRetoucherPortfolioByIdAsync(portfolioId);
            if (portfolio == null)
                throw new KeyNotFoundException($"Portfolio with ID {portfolioId} not found.");

            // 保存文件
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(_uploadDirectory, fileName);
            var relativePath = $"/uploads/portfolio/{fileName}";

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // 创建元数据
            var metadata = new
            {
                OriginalFileName = file.FileName,
                ContentType = file.ContentType,
                Size = file.Length,
                UploadedAt = DateTime.UtcNow
            };

            // 创建作品项
            var item = new Portfolioitem
            {
                PortfolioId = portfolioId,
                ImagePath = relativePath,
                Title = request.Title,
                Description = request.Description,
                Metadata = JsonSerializer.Serialize(metadata),
                CreatedAt = DateTime.UtcNow,
                IsBeforeImage = request.IsBeforeImage,
                AfterImageId = request.AfterImageId
            };

            var createdItem = await _portfolioRepo.CreatePortfolioItemAsync(item);

            // 更新作品集的最后修改时间
            portfolio.UpdatedAt = DateTime.UtcNow;
            await _portfolioRepo.UpdateRetoucherPortfolioAsync(portfolio);

            return MapToPortfolioItemDto(createdItem);
        }

        public async Task<bool> UpdatePortfolioItemAsync(int itemId, UpdatePortfolioItemRequest request)
        {
            var item = await _portfolioRepo.GetPortfolioItemByIdAsync(itemId);
            if (item == null)
                throw new KeyNotFoundException($"Portfolio item with ID {itemId} not found.");

            // 更新字段
            if (!string.IsNullOrEmpty(request.Title))
                item.Title = request.Title;

            if (request.Description != null)
                item.Description = request.Description;

            if (request.IsBeforeImage.HasValue)
                item.IsBeforeImage = request.IsBeforeImage.Value;

            if (request.AfterImageId.HasValue)
                item.AfterImageId = request.AfterImageId;

            return await _portfolioRepo.UpdatePortfolioItemAsync(item);
        }

        public async Task<bool> DeletePortfolioItemAsync(int itemId)
        {
            var item = await _portfolioRepo.GetPortfolioItemByIdAsync(itemId);
            if (item == null)
                return false;

            // 删除文件
            if (!string.IsNullOrEmpty(item.ImagePath))
            {
                var filePath = Path.Combine(_environment.WebRootPath, item.ImagePath.TrimStart('/'));
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }

            return await _portfolioRepo.DeletePortfolioItemAsync(itemId);
        }

        // 辅助方法 - 实体映射到DTO
        private PhotographerPortfolioDto MapToPhotographerPortfolioDto(Photographerportfolio portfolio)
        {
            return new PhotographerPortfolioDto
            {
                PortfolioId = portfolio.PortfolioId,
                PhotographerId = portfolio.PhotographerId,
                PhotographerName = portfolio.Photographer?.User?.Username,
                Title = portfolio.Title,
                Description = portfolio.Description,
                Category = portfolio.Category,
                IsPublic = portfolio.IsPublic ?? false,  // 处理可空布尔值
                CreatedAt = portfolio.CreatedAt,
                UpdatedAt = portfolio.UpdatedAt,
                Items = portfolio.Portfolioitems?.Select(MapToPortfolioItemDto).ToList() ?? new List<PortfolioItemDto>()
            };
        }

        private RetoucherPortfolioDto MapToRetoucherPortfolioDto(Retoucherportfolio portfolio)
        {
            return new RetoucherPortfolioDto
            {
                PortfolioId = portfolio.PortfolioId,
                RetoucherId = portfolio.RetoucherId,
                RetoucherName = portfolio.Retoucher?.User?.Username,
                Title = portfolio.Title,
                Description = portfolio.Description,
                Category = portfolio.Category,
                IsPublic = portfolio.IsPublic ?? false,  // 处理可空布尔值
                CreatedAt = portfolio.CreatedAt,
                UpdatedAt = portfolio.UpdatedAt,
                Items = portfolio.Portfolioitems?.Select(MapToPortfolioItemDto).ToList() ?? new List<PortfolioItemDto>()
            };
        }

        private PortfolioItemDto MapToPortfolioItemDto(Portfolioitem item)
        {
            return new PortfolioItemDto
            {
                ItemId = item.ItemId,
                PortfolioId = item.PortfolioId,
                ImagePath = item.ImagePath,
                Title = item.Title,
                Description = item.Description,
                Metadata = item.Metadata,
                CreatedAt = item.CreatedAt,
                IsBeforeImage = item.IsBeforeImage ?? false,  // 添加可空布尔值处理
                AfterImageId = item.AfterImageId,
                AfterImage = item.AfterImage != null ? MapToPortfolioItemDto(item.AfterImage) : null
            };
        }
    }
}