using System.ComponentModel.DataAnnotations;
namespace PixelPerfect.Models
{
    public class RegisterRequest
    {
        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string Username { get; set; }
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        // 个人简介字段
        public string Biography { get; set; }
        // 可选的初始角色
        public List<string> Roles { get; set; } = new List<string> { "Regular" };
    }
    public class LoginRequest
    {
        [Required]
        public string UsernameOrEmail { get; set; }
        [Required]
        public string Password { get; set; }
    }
    public class UpdateUserRequest
    {
        [Required]
        public int UserId { get; set; }
        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string Username { get; set; }
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        // 个人简介字段
        public string Biography { get; set; }
        // 以下属性仅管理员可用
        public bool? IsActive { get; set; }
    }
    public class ChangePasswordRequest
    {
        [Required]
        public string CurrentPassword { get; set; }
        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string NewPassword { get; set; }
    }
    public class SetUserStatusRequest
    {
        [Required]
        public bool IsActive { get; set; }
    }
    // 新增用户角色相关的模型
    public class UserRoleModel
    {
        public int UserRoleId { get; set; }
        public int UserId { get; set; }
        public string RoleType { get; set; }
        public DateTime CreatedAt { get; set; }
    }
    public class UserRoleRequest
    {
        [Required]
        public int UserId { get; set; }
        [Required]
        public string RoleType { get; set; }
    }
    public class UserWithRolesResponse
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public string Biography { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
    }

    // 新增关注功能相关模型
    public class FollowRequest
    {
        [Required]
        public int FollowedId { get; set; }
    }

    public class UnfollowRequest
    {
        [Required]
        public int FollowedId { get; set; }
    }

    public class FollowStatusResponse
    {
        public bool IsFollowing { get; set; }
    }

    public class UserBriefInfo
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Biography { get; set; }
    }

    public class FollowListResponse
    {
        public List<UserBriefInfo> Users { get; set; }
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
    }

    public class FollowStatsResponse
    {
        public int FollowersCount { get; set; }
        public int FollowingCount { get; set; }
    }

    // 扩展用户响应模型，包含关注统计信息
    public class UserProfileResponse
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public string Biography { get; set; }
        public List<string> Roles { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
        // 关注统计
        public int FollowersCount { get; set; }
        public int FollowingCount { get; set; }
        // 当前用户是否关注了此用户
        public bool IsFollowedByCurrentUser { get; set; }
    }
}