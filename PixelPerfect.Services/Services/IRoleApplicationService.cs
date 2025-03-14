using PixelPerfect.Core.Entities;
using PixelPerfect.Core.Models;

namespace PixelPerfect.Services
{
    public interface IRoleApplicationService
    {
        // 获取申请信息
        Task<RoleApplication?> GetApplicationByIdAsync(int applicationId);
        Task<List<RoleApplicationResponse>> GetUserApplicationsAsync(int userId);
        Task<PaginatedRoleApplicationResponse> GetApplicationsByStatusAsync(
            string status,
            string? roleType = null,
            int pageNumber = 1,
            int pageSize = 20);
        Task<PaginatedRoleApplicationResponse> GetAllApplicationsAsync(
            string? status = null,
            string? roleType = null,
            int pageNumber = 1,
            int pageSize = 20);

        // 创建申请
        Task<RoleApplicationResponse> CreateApplicationAsync(int userId, CreateRoleApplicationRequest request);

        // 处理申请
        Task<RoleApplicationResponse> ProcessApplicationAsync(
            int applicationId,
            int adminId,
            ProcessRoleApplicationRequest request);

        // 检查用户是否已有待审核的申请
        Task<bool> HasPendingApplicationAsync(int userId, string roleType);

        // 获取申请详情响应
        Task<RoleApplicationResponse> GetApplicationResponseAsync(RoleApplication application);
    }
}