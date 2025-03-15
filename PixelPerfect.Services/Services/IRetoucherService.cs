using PixelPerfect.Core.Entities;
using PixelPerfect.Core.Models;

namespace PixelPerfect.Services
{
    public interface IRetoucherService
    {
        // 获取修图师信息
        Task<RetoucherDto> GetRetoucherByIdAsync(int retoucherId);
        Task<List<RetoucherDto>> GetAllRetouchersAsync(bool verifiedOnly = false);
        Task<RetoucherDto> GetRetoucherByUserIdAsync(int userId);

        // 创建和更新修图师档案
        Task<RetoucherDto> CreateRetoucherProfileAsync(int userId, RetoucherCreateRequest request);
        Task<bool> UpdateRetoucherProfileAsync(int retoucherId, RetoucherUpdateRequest request);

        // 权限检查
        Task<bool> IsOwnerAsync(int retoucherId, int userId);

        // 搜索修图师
        Task<List<RetoucherDto>> SearchRetouchersAsync(RetoucherSearchParams searchParams);

        // 新增方法
        Task<List<RetoucherDto>> SearchRetouchersV2Async(RetoucherSearchParamsV2 searchParams);
    }
}