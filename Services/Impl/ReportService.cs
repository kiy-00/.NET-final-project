using PixelPerfect.Entities;
using PixelPerfect.Models;
using PixelPerfect.Repos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PixelPerfect.Services.Impl
{
    public class ReportService : IReportService
    {
        private readonly PhotoBookingDbContext _context;
        private readonly ReportRepo _reportRepo;
        private readonly PostRepo _postRepo;

        public ReportService(
            PhotoBookingDbContext context,
            ReportRepo reportRepo,
            PostRepo postRepo)
        {
            _context = context;
            _reportRepo = reportRepo;
            _postRepo = postRepo;
        }

        public async Task<ReportDto> GetReportByIdAsync(int reportId)
        {
            var report = await _reportRepo.GetByIdAsync(reportId);
            if (report == null)
                return null;

            return MapToDto(report);
        }

        public async Task<List<ReportDto>> GetReportsByUserIdAsync(int userId)
        {
            var reports = await _reportRepo.GetByUserIdAsync(userId);
            return reports.Select(MapToDto).ToList();
        }

        public async Task<List<ReportDto>> GetReportsByPostIdAsync(int postId)
        {
            var reports = await _reportRepo.GetByPostIdAsync(postId);
            return reports.Select(MapToDto).ToList();
        }

        public async Task<PagedResult<ReportDto>> SearchReportsAsync(ReportSearchParams searchParams)
        {
            var (reports, totalCount) = await _reportRepo.SearchAsync(
                searchParams.UserId,
                searchParams.PostId,
                searchParams.Status,
                searchParams.StartDate,
                searchParams.EndDate,
                searchParams.Page,
                searchParams.PageSize
            );

            var reportDtos = reports.Select(MapToDto).ToList();

            return new PagedResult<ReportDto>
            {
                Items = reportDtos,
                TotalCount = totalCount,
                Page = searchParams.Page,
                PageSize = searchParams.PageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)searchParams.PageSize)
            };
        }

        public async Task<ReportDto> CreateReportAsync(int userId, ReportCreateRequest request)
        {
            // 检查帖子是否存在
            var post = await _postRepo.GetByIdAsync(request.PostId);
            if (post == null)
                throw new KeyNotFoundException($"Post with ID {request.PostId} not found.");

            // 检查用户是否已举报过该帖子
            var existingReports = await _reportRepo.GetByPostIdAsync(request.PostId);
            if (existingReports.Any(r => r.UserId == userId && r.Status == "Pending"))
                throw new InvalidOperationException("You have already reported this post and it's still pending review.");

            // 创建举报
            var report = new Report
            {
                UserId = userId,
                PostId = request.PostId,
                Reason = request.Reason,
                Status = "Pending", // 默认待处理
                CreatedAt = DateTime.UtcNow,
                HandledAt = null,
                HandledByUserId = null
            };

            var createdReport = await _reportRepo.CreateAsync(report);
            return MapToDto(createdReport);
        }

        public async Task<ReportDto> HandleReportAsync(int reportId, int adminUserId, string status)
        {
            var report = await _reportRepo.GetByIdAsync(reportId);
            if (report == null)
                throw new KeyNotFoundException($"Report with ID {reportId} not found.");

            // 验证状态是否有效
            var validStatuses = new[] { "Approved", "Rejected", "Pending" };
            if (!validStatuses.Contains(status))
                throw new ArgumentException($"Invalid report status: {status}");

            // 更新举报状态
            report.Status = status;
            report.HandledAt = DateTime.UtcNow;
            report.HandledByUserId = adminUserId;

            // 如果举报被批准，更新帖子状态
            if (status == "Approved")
            {
                // 移除帖子的批准状态
                var post = report.Post;
                post.IsApproved = false;
                _context.Posts.Update(post);
            }

            await _reportRepo.UpdateAsync(report);
            return MapToDto(report);
        }

        public async Task<PagedResult<ReportDto>> GetPendingReportsAsync(int page = 1, int pageSize = 10)
        {
            var skip = (page - 1) * pageSize;
            var reports = await _reportRepo.GetPendingReportsAsync(skip, pageSize);
            var totalCount = await _reportRepo.GetPendingReportsCountAsync();

            var reportDtos = reports.Select(MapToDto).ToList();

            return new PagedResult<ReportDto>
            {
                Items = reportDtos,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }

        public async Task<int> GetPendingReportsCountAsync()
        {
            return await _reportRepo.GetPendingReportsCountAsync();
        }

        public async Task<Dictionary<string, int>> GetReportStatusCountsAsync()
        {
            return await _reportRepo.GetReportStatusCountsAsync();
        }

        // 辅助方法 - 实体映射到DTO
        private ReportDto MapToDto(Report report)
        {
            return new ReportDto
            {
                ReportId = report.ReportId,
                UserId = report.UserId,
                Username = report.User?.Username,
                PostId = report.PostId,
                PostTitle = report.Post?.Title,
                Reason = report.Reason,
                Status = report.Status,
                CreatedAt = report.CreatedAt,
                HandledAt = report.HandledAt,
                HandledByUserId = report.HandledByUserId,
                HandledByUsername = report.HandledByUser?.Username
            };
        }
    }
}