using Microsoft.EntityFrameworkCore;
using PixelPerfect.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PixelPerfect.Repos
{
    public class ReportRepo
    {
        private readonly PhotoBookingDbContext _context;

        public ReportRepo(PhotoBookingDbContext context)
        {
            _context = context;
        }

        public async Task<Report> GetByIdAsync(int reportId)
        {
            return await _context.Reports
                .Include(r => r.User)
                .Include(r => r.Post)
                    .ThenInclude(p => p.User)
                .Include(r => r.HandledByUser)
                .FirstOrDefaultAsync(r => r.ReportId == reportId);
        }

        public async Task<List<Report>> GetByUserIdAsync(int userId)
        {
            return await _context.Reports
                .Include(r => r.User)
                .Include(r => r.Post)
                    .ThenInclude(p => p.User)
                .Include(r => r.HandledByUser)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Report>> GetByPostIdAsync(int postId)
        {
            return await _context.Reports
                .Include(r => r.User)
                .Include(r => r.Post)
                    .ThenInclude(p => p.User)
                .Include(r => r.HandledByUser)
                .Where(r => r.PostId == postId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<(List<Report> Reports, int TotalCount)> SearchAsync(
            int? userId = null,
            int? postId = null,
            string status = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int page = 1,
            int pageSize = 10)
        {
            var query = _context.Reports
                .Include(r => r.User)
                .Include(r => r.Post)
                    .ThenInclude(p => p.User)
                .Include(r => r.HandledByUser)
                .AsQueryable();

            // 应用筛选条件
            if (userId.HasValue)
                query = query.Where(r => r.UserId == userId.Value);

            if (postId.HasValue)
                query = query.Where(r => r.PostId == postId.Value);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(r => r.Status == status);

            if (startDate.HasValue)
                query = query.Where(r => r.CreatedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(r => r.CreatedAt <= endDate.Value);

            // 计算总数
            var totalCount = await query.CountAsync();

            // 应用分页
            var reports = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (reports, totalCount);
        }

        public async Task<List<Report>> GetPendingReportsAsync(int skip = 0, int take = 10)
        {
            return await _context.Reports
                .Include(r => r.User)
                .Include(r => r.Post)
                    .ThenInclude(p => p.User)
                .Include(r => r.HandledByUser)
                .Where(r => r.Status == "Pending")
                .OrderByDescending(r => r.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<int> GetPendingReportsCountAsync()
        {
            return await _context.Reports
                .Where(r => r.Status == "Pending")
                .CountAsync();
        }

        public async Task<Dictionary<string, int>> GetReportStatusCountsAsync()
        {
            var counts = await _context.Reports
                .GroupBy(r => r.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            return counts.ToDictionary(x => x.Status, x => x.Count);
        }

        public async Task<Report> CreateAsync(Report report)
        {
            _context.Reports.Add(report);
            await _context.SaveChangesAsync();
            return report;
        }

        public async Task<bool> UpdateAsync(Report report)
        {
            _context.Reports.Update(report);
            var affected = await _context.SaveChangesAsync();
            return affected > 0;
        }
    }
}