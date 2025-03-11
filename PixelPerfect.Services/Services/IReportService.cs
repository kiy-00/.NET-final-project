using PixelPerfect.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PixelPerfect.Services
{
    public interface IReportService
    {
        // 获取举报
        Task<ReportDto> GetReportByIdAsync(int reportId);
        Task<List<ReportDto>> GetReportsByUserIdAsync(int userId);
        Task<List<ReportDto>> GetReportsByPostIdAsync(int postId);
        Task<PagedResult<ReportDto>> SearchReportsAsync(ReportSearchParams searchParams);

        // 创建举报
        Task<ReportDto> CreateReportAsync(int userId, ReportCreateRequest request);

        // 处理举报
        Task<ReportDto> HandleReportAsync(int reportId, int adminUserId, string status);

        // 获取未处理的举报
        Task<PagedResult<ReportDto>> GetPendingReportsAsync(int page = 1, int pageSize = 10);

        // 统计信息
        Task<int> GetPendingReportsCountAsync();
        Task<Dictionary<string, int>> GetReportStatusCountsAsync();
    }
}