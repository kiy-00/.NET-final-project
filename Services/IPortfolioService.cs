// Services/IPortfolioService.cs
using Microsoft.AspNetCore.Http;
using PixelPerfect.Entities;
using PixelPerfect.Models;

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
    }
}