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
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
    }
}