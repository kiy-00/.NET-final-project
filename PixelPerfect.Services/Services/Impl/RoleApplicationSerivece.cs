using System.Text.Json;
using PixelPerfect.Core.Entities;
using PixelPerfect.Core.Models;
using PixelPerfect.DataAccess.Repos;

namespace PixelPerfect.Services.Impl
{
    public class RoleApplicationService : IRoleApplicationService
    {
        private readonly RoleApplicationRepo _roleApplicationRepo;
        private readonly UserRepo _userRepo;
        private readonly IUserService _userService;

        public RoleApplicationService(
            RoleApplicationRepo roleApplicationRepo,
            UserRepo userRepo,
            IUserService userService)
        {
            _roleApplicationRepo = roleApplicationRepo;
            _userRepo = userRepo;
            _userService = userService;
        }

        // 获取申请详情
        public async Task<RoleApplication?> GetApplicationByIdAsync(int applicationId)
        {
            return await _roleApplicationRepo.GetByIdAsync(applicationId);
        }

        // 获取用户的所有申请
        public async Task<List<RoleApplicationResponse>> GetUserApplicationsAsync(int userId)
        {
            var applications = await _roleApplicationRepo.GetByUserIdAsync(userId);
            var responses = new List<RoleApplicationResponse>();

            foreach (var application in applications)
            {
                responses.Add(await GetApplicationResponseAsync(application));
            }

            return responses;
        }

        // 获取特定状态的申请（分页）
        public async Task<PaginatedRoleApplicationResponse> GetApplicationsByStatusAsync(
            string status,
            string? roleType = null,
            int pageNumber = 1,
            int pageSize = 20)
        {
            var applications = await _roleApplicationRepo.GetByStatusAsync(status, roleType, pageNumber, pageSize);
            var totalCount = await _roleApplicationRepo.GetCountAsync(status, roleType);

            var responses = new List<RoleApplicationResponse>();
            foreach (var application in applications)
            {
                responses.Add(await GetApplicationResponseAsync(application));
            }

            return new PaginatedRoleApplicationResponse
            {
                Applications = responses,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        // 获取所有申请（分页）
        public async Task<PaginatedRoleApplicationResponse> GetAllApplicationsAsync(
            string? status = null,
            string? roleType = null,
            int pageNumber = 1,
            int pageSize = 20)
        {
            var applications = await _roleApplicationRepo.GetAllAsync(status, roleType, pageNumber, pageSize);
            var totalCount = await _roleApplicationRepo.GetCountAsync(status, roleType);

            var responses = new List<RoleApplicationResponse>();
            foreach (var application in applications)
            {
                responses.Add(await GetApplicationResponseAsync(application));
            }

            return new PaginatedRoleApplicationResponse
            {
                Applications = responses,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        // 创建申请
        public async Task<RoleApplicationResponse> CreateApplicationAsync(int userId, CreateRoleApplicationRequest request)
        {
            // 验证用户是否存在
            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null)
                throw new KeyNotFoundException($"User with ID {userId} not found.");

            // 验证角色类型
            if (request.RoleType != "Photographer" && request.RoleType != "Retoucher")
                throw new ArgumentException("Role type must be either 'Photographer' or 'Retoucher'.");

            // 检查用户是否已经有相同角色
            var hasRole = await _userService.HasRoleAsync(userId, request.RoleType);
            if (hasRole)
                throw new InvalidOperationException($"User already has the '{request.RoleType}' role.");

            // 检查用户是否已有待处理的申请
            var hasPending = await HasPendingApplicationAsync(userId, request.RoleType);
            if (hasPending)
                throw new InvalidOperationException($"User already has a pending application for '{request.RoleType}' role.");

            // 创建申请
            var application = new RoleApplication
            {
                UserId = userId,
                RoleType = request.RoleType,
                Status = "Pending",
                ApplicationData = JsonSerializer.Serialize(request.ApplicationData),
                SubmittedAt = DateTime.UtcNow
            };

            var createdApplication = await _roleApplicationRepo.CreateAsync(application);
            return await GetApplicationResponseAsync(createdApplication);
        }

        // 处理申请
        public async Task<RoleApplicationResponse> ProcessApplicationAsync(
            int applicationId,
            int adminId,
            ProcessRoleApplicationRequest request)
        {
            // 验证申请是否存在
            var application = await _roleApplicationRepo.GetByIdAsync(applicationId);
            if (application == null)
                throw new KeyNotFoundException($"Application with ID {applicationId} not found.");

            // 验证管理员是否存在
            var admin = await _userRepo.GetByIdAsync(adminId);
            if (admin == null)
                throw new KeyNotFoundException($"Admin with ID {adminId} not found.");

            // 验证申请状态
            if (application.Status != "Pending")
                throw new InvalidOperationException("Only pending applications can be processed.");

            // 验证处理状态
            if (request.Status != "Approved" && request.Status != "Rejected")
                throw new ArgumentException("Status must be either 'Approved' or 'Rejected'.");

            // 更新申请
            application.Status = request.Status;
            application.ProcessedAt = DateTime.UtcNow;
            application.ProcessedByUserId = adminId;
            application.Feedback = request.Feedback;

            // 如果批准，触发器会自动添加相应角色并创建相关记录

            await _roleApplicationRepo.UpdateAsync(application);

            // 返回更新后的申请
            var updatedApplication = await _roleApplicationRepo.GetByIdAsync(applicationId);
            return await GetApplicationResponseAsync(updatedApplication!);
        }

        // 检查用户是否已有待审核的申请
        public async Task<bool> HasPendingApplicationAsync(int userId, string roleType)
        {
            var latestApplication = await _roleApplicationRepo.GetLatestByUserAndRoleTypeAsync(userId, roleType);
            return latestApplication != null && latestApplication.Status == "Pending";
        }

        // 将实体转换为响应模型
        public async Task<RoleApplicationResponse> GetApplicationResponseAsync(RoleApplication application)
        {
            var applicationData = JsonSerializer.Deserialize<Dictionary<string, object>>(application.ApplicationData);

            return new RoleApplicationResponse
            {
                ApplicationID = application.ApplicationId,
                UserID = application.UserId,
                Username = application.User?.Username ?? "Unknown",
                RoleType = application.RoleType,
                Status = application.Status,
                ApplicationData = applicationData!,
                SubmittedAt = application.SubmittedAt,
                ProcessedAt = application.ProcessedAt,
                ProcessedByUserID = application.ProcessedByUserId,
                ProcessedByUsername = application.ProcessedByUser?.Username,
                Feedback = application.Feedback
            };
        }
    }
}