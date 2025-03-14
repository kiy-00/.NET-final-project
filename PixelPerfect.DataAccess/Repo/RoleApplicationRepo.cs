using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using PixelPerfect.Core.Entities;

namespace PixelPerfect.DataAccess.Repos
{
    public class RoleApplicationRepo
    {
        private readonly PhotoBookingDbContext _context;

        public RoleApplicationRepo(PhotoBookingDbContext context)
        {
            _context = context;
        }

        // 获取特定ID的角色申请
        public async Task<RoleApplication?> GetByIdAsync(int applicationId)
        {
            return await _context.RoleApplications
                .Include(ra => ra.User)
                .Include(ra => ra.ProcessedByUser)
                .FirstOrDefaultAsync(ra => ra.ApplicationId == applicationId);
        }

        // 获取用户的所有角色申请
        public async Task<List<RoleApplication>> GetByUserIdAsync(int userId)
        {
            return await _context.RoleApplications
                .Where(ra => ra.UserId == userId)
                .Include(ra => ra.User)
                .Include(ra => ra.ProcessedByUser)
                .OrderByDescending(ra => ra.SubmittedAt)
                .ToListAsync();
        }

        // 获取特定状态的申请
        public async Task<List<RoleApplication>> GetByStatusAsync(
            string status,
            string? roleType = null,
            int pageNumber = 1,
            int pageSize = 20)
        {
            var query = _context.RoleApplications
                .Where(ra => ra.Status == status);

            // 如果指定了角色类型，添加筛选条件
            if (!string.IsNullOrEmpty(roleType))
            {
                query = query.Where(ra => ra.RoleType == roleType);
            }

            return await query
                .Include(ra => ra.User)
                .Include(ra => ra.ProcessedByUser)
                .OrderByDescending(ra => ra.SubmittedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        // 获取角色申请总数
        public async Task<int> GetCountAsync(string? status = null, string? roleType = null)
        {
            var query = _context.RoleApplications.AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(ra => ra.Status == status);
            }

            if (!string.IsNullOrEmpty(roleType))
            {
                query = query.Where(ra => ra.RoleType == roleType);
            }

            return await query.CountAsync();
        }

        // 创建角色申请
        public async Task<RoleApplication> CreateAsync(RoleApplication application)
        {
            _context.RoleApplications.Add(application);
            await _context.SaveChangesAsync();
            return application;
        }

        // 更新角色申请
        public async Task<bool> UpdateAsync(RoleApplication application)
        {
            _context.RoleApplications.Update(application);
            var affected = await _context.SaveChangesAsync();
            return affected > 0;
        }

        // 获取用户特定角色类型的最新申请
        public async Task<RoleApplication?> GetLatestByUserAndRoleTypeAsync(int userId, string roleType)
        {
            return await _context.RoleApplications
                .Where(ra => ra.UserId == userId && ra.RoleType == roleType)
                .OrderByDescending(ra => ra.SubmittedAt)
                .FirstOrDefaultAsync();
        }

        // 获取所有申请（分页）
        public async Task<List<RoleApplication>> GetAllAsync(
            string? status = null,
            string? roleType = null,
            int pageNumber = 1,
            int pageSize = 20)
        {
            var query = _context.RoleApplications.AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(ra => ra.Status == status);
            }

            if (!string.IsNullOrEmpty(roleType))
            {
                query = query.Where(ra => ra.RoleType == roleType);
            }

            return await query
                .Include(ra => ra.User)
                .Include(ra => ra.ProcessedByUser)
                .OrderByDescending(ra => ra.SubmittedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
    }
}