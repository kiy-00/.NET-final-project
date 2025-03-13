using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PixelPerfect.Core.Entities;
using PixelPerfect.Core.Models;
using PixelPerfect.DataAccess.Repos;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace PixelPerfect.Services.Impl
{
    public class PortfolioService : IPortfolioService
    {
        private readonly PhotoBookingDbContext _context;
        private readonly PortfolioRepo _portfolioRepo;
        private readonly IFileStorageService _fileStorage;
        private readonly IConfiguration _config;

        public PortfolioService(
            PhotoBookingDbContext context,
            PortfolioRepo portfolioRepo,
            IFileStorageService fileStorage,
            IConfiguration config)
        {
            _context = context;
            _portfolioRepo = portfolioRepo;
            _fileStorage = fileStorage;
            _config = config;
        }

        #region 摄影师作品集方法

        public async Task<List<PhotographerPortfolioDto>> GetAllPhotographerPortfoliosAsync(PortfolioSearchParams searchParams)
        {
            var query = _context.Photographerportfolios
                .Include(p => p.Photographer)
                    .ThenInclude(p => p.User)
                .Include(p => p.Portfolioitems.Where(i => i.PortfolioType == "Photographer"))
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
            var dtos = portfolios.Select(MapToPhotographerPortfolioDto).ToList();

            // 查找并设置每个作品集的封面
            foreach (var dto in dtos)
            {
                var cover = await GetPortfolioCoverAsync(dto.PortfolioId, false);
                if (cover != null)
                {
                    dto.CoverImageUrl = cover.ImageUrl;
                    dto.CoverThumbnailUrl = cover.ThumbnailUrl;
                }
            }

            return dtos;
        }

        public async Task<List<PhotographerPortfolioDto>> GetPhotographerPortfoliosByPhotographerIdAsync(int photographerId, PortfolioSearchParams searchParams)
        {
            var query = _context.Photographerportfolios
                .Include(p => p.Photographer)
                    .ThenInclude(p => p.User)
                .Include(p => p.Portfolioitems.Where(i => i.PortfolioType == "Photographer"))
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
            var dtos = portfolios.Select(MapToPhotographerPortfolioDto).ToList();

            // 查找并设置每个作品集的封面
            foreach (var dto in dtos)
            {
                var cover = await GetPortfolioCoverAsync(dto.PortfolioId, false);
                if (cover != null)
                {
                    dto.CoverImageUrl = cover.ImageUrl;
                    dto.CoverThumbnailUrl = cover.ThumbnailUrl;
                }
            }

            return dtos;
        }

        public async Task<PhotographerPortfolioDto> GetPhotographerPortfolioByIdAsync(int portfolioId)
        {
            var portfolio = await _portfolioRepo.GetPhotographerPortfolioByIdAsync(portfolioId);
            if (portfolio == null)
                return null;

            var dto = MapToPhotographerPortfolioDto(portfolio);

            // 查找并设置作品集封面
            var cover = await GetPortfolioCoverAsync(portfolioId, false);
            if (cover != null)
            {
                dto.CoverImageUrl = cover.ImageUrl;
                dto.CoverThumbnailUrl = cover.ThumbnailUrl;
            }

            return dto;
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

        // 摄影师作品集删除方法 - 保持一致性
        public async Task<bool> DeletePhotographerPortfolioAsync(int portfolioId)
        {
            // 1. 获取作品集信息
            var portfolio = await _portfolioRepo.GetPhotographerPortfolioByIdAsync(portfolioId);
            if (portfolio == null)
                return false;

            // 2. 删除封面图片
            var cover = await GetPortfolioCoverAsync(portfolioId, false);
            if (cover != null)
            {
                await DeletePortfolioItemAsync(cover.ItemId);
            }

            // 3. 获取该作品集的所有项目并删除
            var items = await _portfolioRepo.GetPortfolioitemsByPortfolioIdAsync(portfolioId, "Photographer");
            foreach (var item in items)
            {
                await DeletePortfolioItemAsync(item.ItemId);
            }

            // 4. 删除作品集记录
            bool result = await _portfolioRepo.DeletePhotographerPortfolioAsync(portfolioId);

            // 5. 清理空目录
            if (result)
            {
                // 清理各种可能的文件夹路径
                string portfolioDir = $"portfolio/photographer/{portfolioId}";
                await _fileStorage.CleanEmptyDirectoriesAsync(portfolioDir);

                string coverDir = $"portfolio/photographer/{portfolioId}/cover";
                await _fileStorage.CleanEmptyDirectoriesAsync(coverDir);

                string batchDir = $"portfolio/photographer/{portfolioId}/batch";
                await _fileStorage.CleanEmptyDirectoriesAsync(batchDir);

                // 如果上一级目录也是空的，也一并清理
                await _fileStorage.CleanEmptyDirectoriesAsync("portfolio/photographer");
            }

            return result;
        }

        #endregion

        #region 修图师作品集方法

        public async Task<List<RetoucherPortfolioDto>> GetAllRetoucherPortfoliosAsync(PortfolioSearchParams searchParams)
        {
            var query = _context.Retoucherportfolios
                .Include(p => p.Retoucher)
                    .ThenInclude(p => p.User)
                .Include(p => p.Portfolioitems.Where(i => i.PortfolioType == "Retoucher"))
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
            var dtos = portfolios.Select(MapToRetoucherPortfolioDto).ToList();

            // 查找并设置每个作品集的封面
            foreach (var dto in dtos)
            {
                var cover = await GetPortfolioCoverAsync(dto.PortfolioId, true);
                if (cover != null)
                {
                    dto.CoverImageUrl = cover.ImageUrl;
                    dto.CoverThumbnailUrl = cover.ThumbnailUrl;
                }
            }

            return dtos;
        }

        public async Task<List<RetoucherPortfolioDto>> GetRetoucherPortfoliosByRetoucherIdAsync(int retoucherId, PortfolioSearchParams searchParams)
        {
            var query = _context.Retoucherportfolios
                .Include(p => p.Retoucher)
                    .ThenInclude(p => p.User)
                .Include(p => p.Portfolioitems.Where(i => i.PortfolioType == "Retoucher"))
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
            var dtos = portfolios.Select(MapToRetoucherPortfolioDto).ToList();

            // 查找并设置每个作品集的封面
            foreach (var dto in dtos)
            {
                var cover = await GetPortfolioCoverAsync(dto.PortfolioId, true);
                if (cover != null)
                {
                    dto.CoverImageUrl = cover.ImageUrl;
                    dto.CoverThumbnailUrl = cover.ThumbnailUrl;
                }
            }

            return dtos;
        }

        public async Task<RetoucherPortfolioDto> GetRetoucherPortfolioByIdAsync(int portfolioId)
        {
            var portfolio = await _portfolioRepo.GetRetoucherPortfolioByIdAsync(portfolioId);
            if (portfolio == null)
                return null;

            var dto = MapToRetoucherPortfolioDto(portfolio);

            // 查找并设置作品集封面
            var cover = await GetPortfolioCoverAsync(portfolioId, true);
            if (cover != null)
            {
                dto.CoverImageUrl = cover.ImageUrl;
                dto.CoverThumbnailUrl = cover.ThumbnailUrl;
            }

            return dto;
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

        // 修图师作品集删除方法
        public async Task<bool> DeleteRetoucherPortfolioAsync(int portfolioId)
        {
            // 1. 获取作品集信息
            var portfolio = await _portfolioRepo.GetRetoucherPortfolioByIdAsync(portfolioId);
            if (portfolio == null)
                return false;

            // 2. 删除封面图片
            var cover = await GetPortfolioCoverAsync(portfolioId, true);
            if (cover != null)
            {
                await DeletePortfolioItemAsync(cover.ItemId);
            }

            // 3. 获取该作品集的所有项目并删除
            var items = await _portfolioRepo.GetPortfolioitemsByPortfolioIdAsync(portfolioId, "Retoucher");
            foreach (var item in items)
            {
                await DeletePortfolioItemAsync(item.ItemId);
            }

            // 4. 删除作品集记录
            bool result = await _portfolioRepo.DeleteRetoucherPortfolioAsync(portfolioId);

            // 5. 清理空目录
            if (result)
            {
                // 清理各种可能的文件夹路径
                string portfolioDir = $"portfolio/retoucher/{portfolioId}";
                await _fileStorage.CleanEmptyDirectoriesAsync(portfolioDir);

                string coverDir = $"portfolio/retoucher/{portfolioId}/cover";
                await _fileStorage.CleanEmptyDirectoriesAsync(coverDir);

                string batchDir = $"portfolio/retoucher/{portfolioId}/batch";
                await _fileStorage.CleanEmptyDirectoriesAsync(batchDir);

                string beforeAfterDir = $"portfolio/retoucher/{portfolioId}/before-after";
                await _fileStorage.CleanEmptyDirectoriesAsync(beforeAfterDir);

                // 如果上一级目录也是空的，也一并清理
                await _fileStorage.CleanEmptyDirectoriesAsync("portfolio/retoucher");
            }

            return result;
        }

        #endregion

        #region 作品项方法

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

            // 使用文件存储服务保存文件
            string directory = $"portfolio/photographer/{portfolioId}";
            string filePath = await _fileStorage.SaveFileAsync(file, directory);

            // 生成缩略图
            string thumbnailPath = await _fileStorage.GenerateThumbnailAsync(
                filePath,
                _config.GetValue<int>("FileStorage:ThumbnailWidth", 300),
                _config.GetValue<int>("FileStorage:ThumbnailHeight", 300)
            );

            // 创建元数据
            var metadata = new
            {
                OriginalFileName = file.FileName,
                ContentType = file.ContentType,
                Size = file.Length,
                ThumbnailPath = thumbnailPath,
                UploadedAt = DateTime.UtcNow,
                IsPortfolioCover = request.IsPortfolioCover
            };

            // 创建作品项
            var item = new Portfolioitem
            {
                PortfolioId = portfolioId,
                PortfolioType = "Photographer", // 设置作品集类型
                ImagePath = filePath,
                Title = request.Title,
                Description = request.Description,
                Metadata = JsonSerializer.Serialize(metadata),
                CreatedAt = DateTime.UtcNow,
                IsBeforeImage = request.IsBeforeImage,
                AfterImageId = request.AfterImageId
            };

            var createdItem = await _portfolioRepo.CreatePortfolioItemAsync(item, false);

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

            // 使用文件存储服务保存文件
            string directory = $"portfolio/retoucher/{portfolioId}";
            string filePath = await _fileStorage.SaveFileAsync(file, directory);

            // 生成缩略图
            string thumbnailPath = await _fileStorage.GenerateThumbnailAsync(
                filePath,
                _config.GetValue<int>("FileStorage:ThumbnailWidth", 300),
                _config.GetValue<int>("FileStorage:ThumbnailHeight", 300)
            );

            // 创建元数据
            var metadata = new
            {
                OriginalFileName = file.FileName,
                ContentType = file.ContentType,
                Size = file.Length,
                ThumbnailPath = thumbnailPath,
                UploadedAt = DateTime.UtcNow,
                IsPortfolioCover = request.IsPortfolioCover
            };

            // 创建作品项
            var item = new Portfolioitem
            {
                PortfolioId = portfolioId,
                PortfolioType = "Retoucher", // 设置作品集类型
                ImagePath = filePath,
                Title = request.Title,
                Description = request.Description,
                Metadata = JsonSerializer.Serialize(metadata),
                CreatedAt = DateTime.UtcNow,
                IsBeforeImage = request.IsBeforeImage,
                AfterImageId = request.AfterImageId
            };

            var createdItem = await _portfolioRepo.CreatePortfolioItemAsync(item, true);

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

        // 改进的作品项删除方法
        public async Task<bool> DeletePortfolioItemAsync(int itemId)
        {
            var item = await _portfolioRepo.GetPortfolioItemByIdAsync(itemId);
            if (item == null)
                return false;

            // 删除主图片
            if (!string.IsNullOrEmpty(item.ImagePath))
            {
                await _fileStorage.DeleteFileAsync(item.ImagePath);
            }

            // 处理元数据中的其他文件路径
            if (!string.IsNullOrEmpty(item.Metadata))
            {
                try
                {
                    var metadataObj = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(item.Metadata);
                    if (metadataObj != null)
                    {
                        // 删除缩略图
                        if (metadataObj.ContainsKey("ThumbnailPath"))
                        {
                            string thumbnailPath = metadataObj["ThumbnailPath"].GetString();
                            if (!string.IsNullOrEmpty(thumbnailPath))
                            {
                                await _fileStorage.DeleteFileAsync(thumbnailPath);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // 记录异常但继续执行
                    Console.WriteLine($"Error parsing metadata for item {itemId}: {ex.Message}");
                }
            }

            // 删除数据库记录
            return await _portfolioRepo.DeletePortfolioItemAsync(itemId);
        }
        #endregion

        #region 新增方法 - 封面相关

        public async Task<PortfolioItemDto> SetPhotographerPortfolioCoverAsync(int portfolioId, IFormFile file)
        {
            // 验证作品集是否存在
            var portfolio = await _portfolioRepo.GetPhotographerPortfolioByIdAsync(portfolioId);
            if (portfolio == null)
                throw new KeyNotFoundException($"Portfolio with ID {portfolioId} not found.");

            // 检查是否已有封面，如果有则删除
            var existingCover = await GetPortfolioCoverAsync(portfolioId, false);
            if (existingCover != null)
            {
                await DeletePortfolioItemAsync(existingCover.ItemId);
            }

            // 使用文件存储服务保存文件
            string directory = $"portfolio/photographer/{portfolioId}/cover";
            string filePath = await _fileStorage.SaveFileAsync(file, directory);

            // 生成缩略图
            string thumbnailPath = await _fileStorage.GenerateThumbnailAsync(
                filePath,
                _config.GetValue<int>("FileStorage:ThumbnailWidth", 300),
                _config.GetValue<int>("FileStorage:ThumbnailHeight", 300)
            );

            // 创建元数据
            var metadata = new
            {
                OriginalFileName = file.FileName,
                ContentType = file.ContentType,
                Size = file.Length,
                ThumbnailPath = thumbnailPath,
                UploadedAt = DateTime.UtcNow,
                IsPortfolioCover = true
            };

            // 创建作品项作为封面
            var item = new Portfolioitem
            {
                PortfolioId = portfolioId,
                PortfolioType = "Photographer", // 设置作品集类型
                ImagePath = filePath,
                Title = "Cover Image",
                Description = "Portfolio Cover Image",
                Metadata = JsonSerializer.Serialize(metadata),
                CreatedAt = DateTime.UtcNow,
                IsBeforeImage = false,
                AfterImageId = null
            };

            var createdItem = await _portfolioRepo.CreatePortfolioItemAsync(item, false);

            // 更新作品集的最后修改时间
            portfolio.UpdatedAt = DateTime.UtcNow;
            await _portfolioRepo.UpdatePhotographerPortfolioAsync(portfolio);

            return MapToPortfolioItemDto(createdItem);
        }

        public async Task<PortfolioItemDto> SetRetoucherPortfolioCoverAsync(int portfolioId, IFormFile file)
        {
            // 验证作品集是否存在
            var portfolio = await _portfolioRepo.GetRetoucherPortfolioByIdAsync(portfolioId);
            if (portfolio == null)
                throw new KeyNotFoundException($"Portfolio with ID {portfolioId} not found.");

            // 检查是否已有封面，如果有则删除
            var existingCover = await GetPortfolioCoverAsync(portfolioId, true);
            if (existingCover != null)
            {
                await DeletePortfolioItemAsync(existingCover.ItemId);
            }

            // 使用文件存储服务保存文件
            string directory = $"portfolio/retoucher/{portfolioId}/cover";
            string filePath = await _fileStorage.SaveFileAsync(file, directory);

            // 生成缩略图
            string thumbnailPath = await _fileStorage.GenerateThumbnailAsync(
                filePath,
                _config.GetValue<int>("FileStorage:ThumbnailWidth", 300),
                _config.GetValue<int>("FileStorage:ThumbnailHeight", 300)
            );

            // 创建元数据
            var metadata = new
            {
                OriginalFileName = file.FileName,
                ContentType = file.ContentType,
                Size = file.Length,
                ThumbnailPath = thumbnailPath,
                UploadedAt = DateTime.UtcNow,
                IsPortfolioCover = true
            };

            // 创建作品项作为封面
            var item = new Portfolioitem
            {
                PortfolioId = portfolioId,
                PortfolioType = "Retoucher", // 设置作品集类型
                ImagePath = filePath,
                Title = "Cover Image",
                Description = "Portfolio Cover Image",
                Metadata = JsonSerializer.Serialize(metadata),
                CreatedAt = DateTime.UtcNow,
                IsBeforeImage = false,
                AfterImageId = null
            };

            var createdItem = await _portfolioRepo.CreatePortfolioItemAsync(item, true);

            // 更新作品集的最后修改时间
            portfolio.UpdatedAt = DateTime.UtcNow;
            await _portfolioRepo.UpdateRetoucherPortfolioAsync(portfolio);

            return MapToPortfolioItemDto(createdItem);
        }

        public async Task<PortfolioItemDto> GetPortfolioCoverAsync(int portfolioId, bool isRetoucherPortfolio)
        {
            // 查询具有封面标记的作品项
            string portfolioType = isRetoucherPortfolio ? "Retoucher" : "Photographer";
            var items = await _portfolioRepo.GetPortfolioitemsByPortfolioIdAsync(portfolioId, portfolioType);
            if (items == null || !items.Any())
                return null;

            foreach (var item in items)
            {
                try
                {
                    if (!string.IsNullOrEmpty(item.Metadata))
                    {
                        var metadataObj = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(item.Metadata);
                        if (metadataObj != null &&
                            metadataObj.ContainsKey("IsPortfolioCover") &&
                            metadataObj["IsPortfolioCover"].GetBoolean())
                        {
                            return MapToPortfolioItemDto(item);
                        }
                    }
                }
                catch
                {
                    // 忽略元数据解析错误
                    continue;
                }
            }

            // 如果没有找到封面，则返回第一个作品项作为默认封面
            return items.Any() ? MapToPortfolioItemDto(items.First()) : null;
        }

        #endregion

        #region 新增方法 - 修图前后对比相关

        public async Task<PortfolioItemDto> AddRetoucherPortfolioItemWithBeforeAfterAsync(
            int portfolioId,
            IFormFile afterImage,
            IFormFile beforeImage,
            UploadRetoucherPortfolioItemRequest request)
        {
            // 验证作品集是否存在
            var portfolio = await _portfolioRepo.GetRetoucherPortfolioByIdAsync(portfolioId);
            if (portfolio == null)
                throw new KeyNotFoundException($"Portfolio with ID {portfolioId} not found.");

            // 确保两个图片都有提供
            if (afterImage == null)
                throw new ArgumentNullException(nameof(afterImage), "After image is required");

            // 设置目录
            string directory = $"portfolio/retoucher/{portfolioId}/before-after";

            Portfolioitem beforeItem = null;

            // 如果有修图前图片，先处理它
            if (beforeImage != null)
            {
                // 保存修图前图片
                string beforeFilePath = await _fileStorage.SaveFileAsync(beforeImage, directory, $"before_{Guid.NewGuid()}");
                string beforeThumbnailPath = await _fileStorage.GenerateThumbnailAsync(beforeFilePath);

                // 创建修图前的元数据
                var beforeMetadata = new
                {
                    OriginalFileName = beforeImage.FileName,
                    ContentType = beforeImage.ContentType,
                    Size = beforeImage.Length,
                    ThumbnailPath = beforeThumbnailPath,
                    UploadedAt = DateTime.UtcNow,
                    IsBeforeAfterPair = true,
                    IsBefore = true
                };

                // 创建修图前作品项
                beforeItem = new Portfolioitem
                {
                    PortfolioId = portfolioId,
                    PortfolioType = "Retoucher", // 设置作品集类型
                    ImagePath = beforeFilePath,
                    Title = $"{request.Title} (Before)",
                    Description = request.Description,
                    Metadata = JsonSerializer.Serialize(beforeMetadata),
                    CreatedAt = DateTime.UtcNow,
                    IsBeforeImage = true
                };

                beforeItem = await _portfolioRepo.CreatePortfolioItemAsync(beforeItem, true);
            }

            // 保存修图后图片
            string afterFilePath = await _fileStorage.SaveFileAsync(afterImage, directory, $"after_{Guid.NewGuid()}");
            string afterThumbnailPath = await _fileStorage.GenerateThumbnailAsync(afterFilePath);

            // 创建修图后的元数据
            var afterMetadata = new
            {
                OriginalFileName = afterImage.FileName,
                ContentType = afterImage.ContentType,
                Size = afterImage.Length,
                ThumbnailPath = afterThumbnailPath,
                UploadedAt = DateTime.UtcNow,
                IsBeforeAfterPair = beforeImage != null,
                IsAfter = true,
                BeforeItemId = beforeItem?.ItemId
            };

            // 创建修图后作品项
            var afterItem = new Portfolioitem
            {
                PortfolioId = portfolioId,
                PortfolioType = "Retoucher", // 设置作品集类型
                ImagePath = afterFilePath,
                Title = request.Title ?? "Retouched Image",
                Description = request.Description,
                Metadata = JsonSerializer.Serialize(afterMetadata),
                CreatedAt = DateTime.UtcNow,
                IsBeforeImage = false
            };

            var createdAfterItem = await _portfolioRepo.CreatePortfolioItemAsync(afterItem, true);

            // 如果有修图前图片，更新关联关系
            if (beforeItem != null)
            {
                beforeItem.AfterImageId = createdAfterItem.ItemId;
                await _portfolioRepo.UpdatePortfolioItemAsync(beforeItem);
            }

            // 更新作品集的最后修改时间
            portfolio.UpdatedAt = DateTime.UtcNow;
            await _portfolioRepo.UpdateRetoucherPortfolioAsync(portfolio);

            // 返回的是修图后的图片
            var dto = MapToPortfolioItemDto(createdAfterItem);

            // 如果有修图前图片，设置相关URL
            if (beforeItem != null)
            {
                dto.BeforeImageUrl = _fileStorage.GetFileUrl(beforeItem.ImagePath);

                try
                {
                    var metadataObj = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(beforeItem.Metadata);
                    if (metadataObj != null && metadataObj.ContainsKey("ThumbnailPath"))
                    {
                        string thumbnailPath = metadataObj["ThumbnailPath"].GetString();
                        if (!string.IsNullOrEmpty(thumbnailPath))
                        {
                            dto.BeforeThumbnailUrl = _fileStorage.GetFileUrl(thumbnailPath);
                        }
                    }
                }
                catch
                {
                    // 忽略元数据解析错误
                }
            }

            return dto;
        }

        public async Task<(PortfolioItemDto Before, PortfolioItemDto After)> GetBeforeAfterImagesAsync(int itemId)
        {
            // 获取当前项
            var item = await _portfolioRepo.GetPortfolioItemByIdAsync(itemId);
            if (item == null)
                throw new KeyNotFoundException($"Portfolio item with ID {itemId} not found.");

            PortfolioItemDto beforeDto = null;
            PortfolioItemDto afterDto = null;

            // 如果当前项是修图前图片
            if (item.IsBeforeImage == true && item.AfterImageId.HasValue)
            {
                beforeDto = MapToPortfolioItemDto(item);

                // 获取关联的修图后图片
                var afterItem = await _portfolioRepo.GetPortfolioItemByIdAsync(item.AfterImageId.Value);
                if (afterItem != null)
                {
                    afterDto = MapToPortfolioItemDto(afterItem);
                }
            }
            // 如果当前项是修图后图片
            else
            {
                afterDto = MapToPortfolioItemDto(item);

                // 查找关联的修图前图片
                var beforeItems = await _context.Portfolioitems
                    .Where(i => i.AfterImageId == itemId && i.IsBeforeImage == true && i.PortfolioType == item.PortfolioType)
                    .ToListAsync();

                if (beforeItems.Any())
                {
                    beforeDto = MapToPortfolioItemDto(beforeItems.First());
                }
            }

            return (beforeDto, afterDto);
        }

        #endregion

        #region 新增方法 - 批量上传

        public async Task<List<PortfolioItemDto>> BatchAddPhotographerPortfolioItemsAsync(
            int portfolioId,
            IEnumerable<IFormFile> files,
            BatchPortfolioItemUploadRequest request)
        {
            // 验证作品集是否存在
            var portfolio = await _portfolioRepo.GetPhotographerPortfolioByIdAsync(portfolioId);
            if (portfolio == null)
                throw new KeyNotFoundException($"Portfolio with ID {portfolioId} not found.");

            if (files == null || !files.Any())
                throw new ArgumentException("No files provided", nameof(files));

            var uploadedItems = new List<PortfolioItemDto>();
            string directory = $"portfolio/photographer/{portfolioId}/batch";

            foreach (var file in files)
            {
                try
                {
                    // 保存文件
                    string filePath = await _fileStorage.SaveFileAsync(file, directory);

                    // 生成缩略图
                    string thumbnailPath = await _fileStorage.GenerateThumbnailAsync(filePath);

                    // 生成文件标题（使用文件名，但移除扩展名）
                    string title = Path.GetFileNameWithoutExtension(file.FileName);

                    // 创建元数据
                    var metadata = new
                    {
                        OriginalFileName = file.FileName,
                        ContentType = file.ContentType,
                        Size = file.Length,
                        ThumbnailPath = thumbnailPath,
                        UploadedAt = DateTime.UtcNow,
                        BatchUpload = true
                    };

                    // 创建作品项
                    var item = new Portfolioitem
                    {
                        PortfolioId = portfolioId,
                        PortfolioType = "Photographer", // 设置作品集类型
                        ImagePath = filePath,
                        Title = title,
                        Description = request.Description,
                        Metadata = JsonSerializer.Serialize(metadata),
                        CreatedAt = DateTime.UtcNow,
                        IsBeforeImage = false,
                        AfterImageId = null
                    };

                    var createdItem = await _portfolioRepo.CreatePortfolioItemAsync(item, false);
                    uploadedItems.Add(MapToPortfolioItemDto(createdItem));
                }
                catch (Exception ex)
                {
                    // 记录错误但继续处理下一个文件
                    Console.WriteLine($"Error uploading file {file.FileName}: {ex.Message}");
                }
            }

            // 更新作品集的最后修改时间
            portfolio.UpdatedAt = DateTime.UtcNow;
            await _portfolioRepo.UpdatePhotographerPortfolioAsync(portfolio);

            return uploadedItems;
        }

        public async Task<List<PortfolioItemDto>> BatchAddRetoucherPortfolioItemsAsync(
            int portfolioId,
            IEnumerable<IFormFile> files,
            BatchPortfolioItemUploadRequest request)
        {
            // 验证作品集是否存在
            var portfolio = await _portfolioRepo.GetRetoucherPortfolioByIdAsync(portfolioId);
            if (portfolio == null)
                throw new KeyNotFoundException($"Portfolio with ID {portfolioId} not found.");

            if (files == null || !files.Any())
                throw new ArgumentException("No files provided", nameof(files));

            var uploadedItems = new List<PortfolioItemDto>();
            string directory = $"portfolio/retoucher/{portfolioId}/batch";

            foreach (var file in files)
            {
                try
                {
                    // 保存文件
                    string filePath = await _fileStorage.SaveFileAsync(file, directory);

                    // 生成缩略图
                    string thumbnailPath = await _fileStorage.GenerateThumbnailAsync(filePath);

                    // 生成文件标题（使用文件名，但移除扩展名）
                    string title = Path.GetFileNameWithoutExtension(file.FileName);

                    // 创建元数据
                    var metadata = new
                    {
                        OriginalFileName = file.FileName,
                        ContentType = file.ContentType,
                        Size = file.Length,
                        ThumbnailPath = thumbnailPath,
                        UploadedAt = DateTime.UtcNow,
                        BatchUpload = true
                    };

                    // 创建作品项
                    var item = new Portfolioitem
                    {
                        PortfolioId = portfolioId,
                        PortfolioType = "Retoucher", // 设置作品集类型
                        ImagePath = filePath,
                        Title = title,
                        Description = request.Description,
                        Metadata = JsonSerializer.Serialize(metadata),
                        CreatedAt = DateTime.UtcNow,
                        IsBeforeImage = false,
                        AfterImageId = null
                    };

                    var createdItem = await _portfolioRepo.CreatePortfolioItemAsync(item, true);
                    uploadedItems.Add(MapToPortfolioItemDto(createdItem));
                }
                catch (Exception ex)
                {
                    // 记录错误但继续处理下一个文件
                    Console.WriteLine($"Error uploading file {file.FileName}: {ex.Message}");
                }
            }

            // 更新作品集的最后修改时间
            portfolio.UpdatedAt = DateTime.UtcNow;
            await _portfolioRepo.UpdateRetoucherPortfolioAsync(portfolio);

            return uploadedItems;
        }

        #endregion

        #region 辅助方法 - 实体映射到DTO

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
            var dto = new PortfolioItemDto
            {
                ItemId = item.ItemId,
                PortfolioId = item.PortfolioId,
                PortfolioType = item.PortfolioType, // 添加类型信息到DTO
                ImagePath = item.ImagePath,
                ImageUrl = _fileStorage.GetFileUrl(item.ImagePath),
                Title = item.Title,
                Description = item.Description,
                Metadata = item.Metadata,
                CreatedAt = item.CreatedAt,
                IsBeforeImage = item.IsBeforeImage ?? false,  // 处理可空布尔值
                AfterImageId = item.AfterImageId,
                AfterImage = item.AfterImage != null ? MapToPortfolioItemDto(item.AfterImage) : null
            };

            // 检查是否为作品集封面
            try
            {
                if (!string.IsNullOrEmpty(item.Metadata))
                {
                    var metadataObj = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(item.Metadata);
                    if (metadataObj != null)
                    {
                        // 设置缩略图URL
                        if (metadataObj.ContainsKey("ThumbnailPath"))
                        {
                            string thumbnailPath = metadataObj["ThumbnailPath"].GetString();
                            if (!string.IsNullOrEmpty(thumbnailPath))
                            {
                                dto.ThumbnailUrl = _fileStorage.GetFileUrl(thumbnailPath);
                            }
                        }

                        // 检查是否为封面
                        if (metadataObj.ContainsKey("IsPortfolioCover") && metadataObj["IsPortfolioCover"].GetBoolean())
                        {
                            dto.IsPortfolioCover = true;
                        }

                        // 检查是否为前后对比图片对的一部分
                        if (metadataObj.ContainsKey("IsBeforeAfterPair") && metadataObj["IsBeforeAfterPair"].GetBoolean())
                        {
                            // 如果是修图前图片，尝试获取关联的修图后图片信息
                            if (item.IsBeforeImage == true && item.AfterImageId.HasValue)
                            {
                                var afterItem = _portfolioRepo.GetPortfolioItemByIdAsync(item.AfterImageId.Value).Result;
                                if (afterItem != null)
                                {
                                    dto.AfterImageUrl = _fileStorage.GetFileUrl(afterItem.ImagePath);

                                    // 尝试获取后图的缩略图
                                    try
                                    {
                                        var afterMetadata = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(afterItem.Metadata);
                                        if (afterMetadata != null && afterMetadata.ContainsKey("ThumbnailPath"))
                                        {
                                            string afterThumbnailPath = afterMetadata["ThumbnailPath"].GetString();
                                            if (!string.IsNullOrEmpty(afterThumbnailPath))
                                            {
                                                dto.AfterThumbnailUrl = _fileStorage.GetFileUrl(afterThumbnailPath);
                                            }
                                        }
                                    }
                                    catch
                                    {
                                        // 忽略元数据解析错误
                                    }
                                }
                            }
                            // 如果是修图后图片，尝试查找关联的修图前图片
                            else if (metadataObj.ContainsKey("BeforeItemId") && item.IsBeforeImage == false)
                            {
                                int beforeItemId = metadataObj["BeforeItemId"].GetInt32();
                                var beforeItem = _portfolioRepo.GetPortfolioItemByIdAsync(beforeItemId).Result;
                                if (beforeItem != null)
                                {
                                    dto.BeforeImageUrl = _fileStorage.GetFileUrl(beforeItem.ImagePath);

                                    // 尝试获取前图的缩略图
                                    try
                                    {
                                        var beforeMetadata = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(beforeItem.Metadata);
                                        if (beforeMetadata != null && beforeMetadata.ContainsKey("ThumbnailPath"))
                                        {
                                            string beforeThumbnailPath = beforeMetadata["ThumbnailPath"].GetString();
                                            if (!string.IsNullOrEmpty(beforeThumbnailPath))
                                            {
                                                dto.BeforeThumbnailUrl = _fileStorage.GetFileUrl(beforeThumbnailPath);
                                            }
                                        }
                                    }
                                    catch
                                    {
                                        // 忽略元数据解析错误
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                // 忽略元数据解析错误
            }

            return dto;
        }
        #endregion
    }
}