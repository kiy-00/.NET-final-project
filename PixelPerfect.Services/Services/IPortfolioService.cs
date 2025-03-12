// Services/IPortfolioService.cs
using Microsoft.AspNetCore.Http;
using PixelPerfect.Core.Entities;
using PixelPerfect.Core.Models;

namespace PixelPerfect.Services
{
    public interface IPortfolioService
    {
        // 摄影师作品集方法
        Task<List<PhotographerPortfolioDto>> GetAllPhotographerPortfoliosAsync(PortfolioSearchParams searchParams);
        Task<List<PhotographerPortfolioDto>> GetPhotographerPortfoliosByPhotographerIdAsync(int photographerId, PortfolioSearchParams searchParams);
        Task<PhotographerPortfolioDto> GetPhotographerPortfolioByIdAsync(int portfolioId);
        Task<PhotographerPortfolioDto> CreatePhotographerPortfolioAsync(int photographerId, CreatePhotographerPortfolioRequest request);
        Task<bool> UpdatePhotographerPortfolioAsync(int portfolioId, UpdatePortfolioRequest request);
        Task<bool> DeletePhotographerPortfolioAsync(int portfolioId);

        // 修图师作品集方法
        Task<List<RetoucherPortfolioDto>> GetAllRetoucherPortfoliosAsync(PortfolioSearchParams searchParams);
        Task<List<RetoucherPortfolioDto>> GetRetoucherPortfoliosByRetoucherIdAsync(int retoucherId, PortfolioSearchParams searchParams);
        Task<RetoucherPortfolioDto> GetRetoucherPortfolioByIdAsync(int portfolioId);
        Task<RetoucherPortfolioDto> CreateRetoucherPortfolioAsync(int retoucherId, CreateRetoucherPortfolioRequest request);
        Task<bool> UpdateRetoucherPortfolioAsync(int portfolioId, UpdatePortfolioRequest request);
        Task<bool> DeleteRetoucherPortfolioAsync(int portfolioId);

        // 作品项方法
        Task<PortfolioItemDto> GetPortfolioItemByIdAsync(int itemId);
        Task<PortfolioItemDto> AddItemToPhotographerPortfolioAsync(int portfolioId, IFormFile file, UploadPortfolioItemRequest request);
        Task<PortfolioItemDto> AddItemToRetoucherPortfolioAsync(int portfolioId, IFormFile file, UploadPortfolioItemRequest request);
        Task<bool> UpdatePortfolioItemAsync(int itemId, UpdatePortfolioItemRequest request);
        Task<bool> DeletePortfolioItemAsync(int itemId);

        // 新增 - 摄影师作品集封面方法
        Task<PortfolioItemDto> SetPhotographerPortfolioCoverAsync(int portfolioId, IFormFile file);

        // 新增 - 修图师作品集封面方法
        Task<PortfolioItemDto> SetRetoucherPortfolioCoverAsync(int portfolioId, IFormFile file);

        // 新增 - 修图师作品项前后对比上传方法
        Task<PortfolioItemDto> AddRetoucherPortfolioItemWithBeforeAfterAsync(
            int portfolioId,
            IFormFile afterImage,
            IFormFile beforeImage,
            UploadRetoucherPortfolioItemRequest request);

        // 新增 - 批量上传摄影师作品项方法
        Task<List<PortfolioItemDto>> BatchAddPhotographerPortfolioItemsAsync(
            int portfolioId,
            IEnumerable<IFormFile> files,
            BatchPortfolioItemUploadRequest request);

        // 新增 - 批量上传修图师作品项方法
        Task<List<PortfolioItemDto>> BatchAddRetoucherPortfolioItemsAsync(
            int portfolioId,
            IEnumerable<IFormFile> files,
            BatchPortfolioItemUploadRequest request);

        // 新增 - 获取作品集封面方法
        Task<PortfolioItemDto> GetPortfolioCoverAsync(int portfolioId, bool isRetoucherPortfolio);

        // 新增 - 获取修图前后对比图片
        Task<(PortfolioItemDto Before, PortfolioItemDto After)> GetBeforeAfterImagesAsync(int itemId);
    }
}