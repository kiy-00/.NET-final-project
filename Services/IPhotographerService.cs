using PixelPerfect.Entities;
using PixelPerfect.Models;

namespace PixelPerfect.Services
{
    public interface IPhotographerService
    {
        // 获取摄影师信息
        Task<PhotographerDto> GetPhotographerByIdAsync(int photographerId);
        Task<List<PhotographerDto>> GetAllPhotographersAsync(bool verifiedOnly = false);
        Task<PhotographerDto> GetPhotographerByUserIdAsync(int userId);

        // 创建和更新摄影师档案
        Task<PhotographerDto> CreatePhotographerProfileAsync(int userId, PhotographerCreateRequest request);
        Task<bool> UpdatePhotographerProfileAsync(int photographerId, PhotographerUpdateRequest request);

        // 权限检查
        Task<bool> IsOwnerAsync(int photographerId, int userId);

        // 搜索摄影师
        Task<List<PhotographerDto>> SearchPhotographersAsync(PhotographerSearchParams searchParams);
    }
}