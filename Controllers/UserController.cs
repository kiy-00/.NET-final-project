using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using PixelPerfect.Core.Entities;
using PixelPerfect.Services;
using System.Security.Claims;
using PixelPerfect.Core.Models;
using RegisterRequest = PixelPerfect.Core.Models.RegisterRequest;
using LoginRequest = PixelPerfect.Core.Models.LoginRequest;

namespace PixelPerfect.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        // 用户注册
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var user = new User
                {
                    Username = request.Username,
                    Email = request.Email,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    PhoneNumber = request.PhoneNumber,
                    Biography = request.Biography,
                    IsActive = true
                };

                // 默认注册为普通用户
                var roles = request.Roles?.Count > 0 ? request.Roles : new List<string> { "Regular" };
                var createdUser = await _userService.CreateUserAsync(user, request.Password, roles);

                // 获取用户角色
                var userRoles = await _userService.GetUserRolesAsync(createdUser.UserId);

                return Ok(new
                {
                    UserId = createdUser.UserId,
                    Username = createdUser.Username,
                    Email = createdUser.Email,
                    Biography = createdUser.Biography,
                    Roles = userRoles
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while registering the user." });
            }
        }

        // 用户登录
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            // 添加请求信息日志
            Console.WriteLine($"登录请求: 用户名/邮箱: {request.UsernameOrEmail}");

            try
            {
                // 记录认证开始
                Console.WriteLine("开始用户认证...");

                // 记录输入参数
                if (string.IsNullOrEmpty(request.UsernameOrEmail))
                {
                    Console.WriteLine("错误: UsernameOrEmail为空");
                    return BadRequest(new { message = "用户名/邮箱不能为空" });
                }

                if (string.IsNullOrEmpty(request.Password))
                {
                    Console.WriteLine("错误: Password为空");
                    return BadRequest(new { message = "密码不能为空" });
                }

                // 认证前记录
                Console.WriteLine($"正在调用_userService.AuthenticateAsync方法, 参数: {request.UsernameOrEmail}");

                var (user, token) = await _userService.AuthenticateAsync(request.UsernameOrEmail, request.Password);

                // 认证后记录
                Console.WriteLine($"认证结果: user是否为null: {user == null}");

                if (user == null)
                {
                    Console.WriteLine("认证失败: 用户名/邮箱或密码无效");
                    return Unauthorized(new { message = "Invalid username/email or password." });
                }

                Console.WriteLine($"用户状态: IsActive = {user.IsActive}");
                if ((bool)!user.IsActive)
                {
                    Console.WriteLine("用户账户未激活");
                    return StatusCode(403, new { message = "Account is inactive. Please contact support." });
                }

                // 获取用户角色
                var userRoles = await _userService.GetUserRolesAsync(user.UserId);

                Console.WriteLine($"登录成功: UserId = {user.UserId}, Username = {user.Username}");
                return Ok(new
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    Email = user.Email,
                    Biography = user.Biography,
                    Roles = userRoles,
                    Token = token
                });
            }
            catch (Exception ex)
            {
                // 详细记录异常信息
                Console.WriteLine($"登录异常: {ex.GetType().Name}");
                Console.WriteLine($"异常消息: {ex.Message}");
                Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");

                // 如果有内部异常，也记录
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"内部异常: {ex.InnerException.GetType().Name}");
                    Console.WriteLine($"内部异常消息: {ex.InnerException.Message}");
                    Console.WriteLine($"内部异常堆栈: {ex.InnerException.StackTrace}");
                }

                return StatusCode(500, new
                {
                    message = "登录过程中发生错误。",
                    error = ex.Message,
                    errorType = ex.GetType().Name,
                    // 在开发环境中可以返回更多信息
                    stackTrace = ex.StackTrace
                });
            }
        }

        // 获取当前用户信息
        [Authorize]
        [HttpGet("current")]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var user = await _userService.GetUserByIdAsync(userId);
                var roles = await _userService.GetUserRolesAsync(userId);

                // 获取关注统计信息
                var followStats = await _userService.GetFollowStatsAsync(userId);

                var response = new
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    PhoneNumber = user.PhoneNumber,
                    Biography = user.Biography,
                    Roles = roles,
                    CreatedAt = user.CreatedAt,
                    LastLogin = user.LastLogin,
                    // 关注统计
                    FollowersCount = followStats.followersCount,
                    FollowingCount = followStats.followingCount
                };

                return Ok(response);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving user information." });
            }
        }

        // 更新用户信息
        [Authorize]
        [HttpPut]
        public async Task<IActionResult> UpdateUser([FromBody] UpdateUserRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

                // 只能更新自己的信息
                if (userId != request.UserId && !User.IsInRole("Admin"))
                    return StatusCode(403, new { message = "You are not authorized to update this user." });

                var existingUser = await _userService.GetUserByIdAsync(request.UserId);

                existingUser.Username = request.Username;
                existingUser.Email = request.Email;
                existingUser.FirstName = request.FirstName;
                existingUser.LastName = request.LastName;
                existingUser.PhoneNumber = request.PhoneNumber;
                existingUser.Biography = request.Biography;

                // 只有管理员可以修改用户活跃状态
                if (User.IsInRole("Admin"))
                {
                    existingUser.IsActive = request.IsActive ?? existingUser.IsActive;
                }

                var success = await _userService.UpdateUserAsync(existingUser);

                if (success)
                    return Ok(new { message = "User updated successfully." });
                else
                    return BadRequest(new { message = "Failed to update user." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating user information." });
            }
        }

        // 修改密码
        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var success = await _userService.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword);

                if (success)
                    return Ok(new { message = "Password changed successfully." });
                else
                    return BadRequest(new { message = "Current password is incorrect." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while changing password." });
            }
        }

        // 查询是否可用的用户名/邮箱
        [HttpGet("check-availability")]
        public async Task<IActionResult> CheckAvailability([FromQuery] string? username, [FromQuery] string? email)
        {
            try
            {
                var result = new Dictionary<string, bool>();

                if (!string.IsNullOrEmpty(username))
                {
                    result["username"] = await _userService.IsUsernameUniqueAsync(username);
                }

                if (!string.IsNullOrEmpty(email))
                {
                    result["email"] = await _userService.IsEmailUniqueAsync(email);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while checking availability." });
            }
        }

        // 用户角色管理

        // 添加用户角色
        [Authorize(Roles = "Admin")]
        [HttpPost("role")]
        public async Task<IActionResult> AddUserRole([FromBody] UserRoleRequest request)
        {
            try
            {
                var result = await _userService.AddUserRoleAsync(request.UserId, request.RoleType);
                if (result)
                    return Ok(new { message = $"Role '{request.RoleType}' added to user successfully." });
                else
                    return BadRequest(new { message = "Failed to add role to user." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while adding user role." });
            }
        }

        // 移除用户角色
        [Authorize(Roles = "Admin")]
        [HttpDelete("role")]
        public async Task<IActionResult> RemoveUserRole([FromBody] UserRoleRequest request)
        {
            try
            {
                var result = await _userService.RemoveUserRoleAsync(request.UserId, request.RoleType);
                if (result)
                    return Ok(new { message = $"Role '{request.RoleType}' removed from user successfully." });
                else
                    return BadRequest(new { message = "Failed to remove role from user." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while removing user role." });
            }
        }

        // 获取用户角色
        [Authorize]
        [HttpGet("{userId}/roles")]
        public async Task<IActionResult> GetUserRoles(int userId)
        {
            try
            {
                // 非管理员只能查看自己的角色
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                if (currentUserId != userId && !User.IsInRole("Admin"))
                    return StatusCode(403, new { message = "You are not authorized to view this user's roles." });

                var roles = await _userService.GetUserRolesAsync(userId);
                return Ok(roles);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving user roles." });
            }
        }

        // 查询用户是否有特定角色
        [Authorize]
        [HttpGet("{userId}/has-role")]
        public async Task<IActionResult> CheckUserRole(int userId, [FromQuery] string roleType)
        {
            try
            {
                // 非管理员只能查看自己的角色
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                if (currentUserId != userId && !User.IsInRole("Admin"))
                    return StatusCode(403, new { message = "You are not authorized to view this user's roles." });

                var hasRole = await _userService.HasRoleAsync(userId, roleType);
                return Ok(hasRole);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while checking user role." });
            }
        }

        // 以下是管理员功能

        // 获取所有用户
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetAllUsers([FromQuery] string? roleType)
        {
            try
            {
                var users = await _userService.GetAllUsersAsync(roleType);
                var response = new List<object>();

                foreach (var user in users)
                {
                    var roles = await _userService.GetUserRolesAsync(user.UserId);
                    var followStats = await _userService.GetFollowStatsAsync(user.UserId);

                    response.Add(new
                    {
                        UserId = user.UserId,
                        Username = user.Username,
                        Email = user.Email,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Biography = user.Biography,
                        Roles = roles,
                        IsActive = user.IsActive,
                        CreatedAt = user.CreatedAt,
                        LastLogin = user.LastLogin,
                        FollowersCount = followStats.followersCount,
                        FollowingCount = followStats.followingCount
                    });
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving users." });
            }
        }

        // 获取指定用户信息
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetUserById(int userId)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(userId);
                var roles = await _userService.GetUserRolesAsync(userId);

                // 获取关注统计
                var followStats = await _userService.GetFollowStatsAsync(userId);

                // 获取当前登录用户是否关注了该用户
                bool isFollowedByCurrentUser = false;
                if (User.Identity.IsAuthenticated)
                {
                    var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                    isFollowedByCurrentUser = await _userService.IsFollowingAsync(currentUserId, userId);
                }

                var response = new UserProfileResponse
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    PhoneNumber = user.PhoneNumber,
                    Biography = user.Biography,
                    Roles = roles,
                    IsActive = user.IsActive ?? true,
                    CreatedAt = user.CreatedAt,
                    LastLogin = user.LastLogin,
                    // 关注统计
                    FollowersCount = followStats.followersCount,
                    FollowingCount = followStats.followingCount,
                    // 当前用户是否关注了此用户
                    IsFollowedByCurrentUser = isFollowedByCurrentUser
                };

                return Ok(response);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving user information." });
            }
        }

        // 设置用户活跃状态
        [Authorize(Roles = "Admin")]
        [HttpPatch("{userId}/status")]
        public async Task<IActionResult> SetUserStatus(int userId, [FromBody] SetUserStatusRequest request)
        {
            try
            {
                var success = await _userService.SetUserActiveStatusAsync(userId, request.IsActive);

                if (success)
                    return Ok(new { message = $"User status set to {(request.IsActive ? "active" : "inactive")} successfully." });
                else
                    return BadRequest(new { message = "Failed to update user status." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating user status." });
            }
        }

        // 搜索用户
        [Authorize(Roles = "Admin")]
        [HttpGet("search")]
        public async Task<IActionResult> SearchUsers([FromQuery] string searchTerm)
        {
            try
            {
                var users = await _userService.SearchUsersAsync(searchTerm);
                var response = new List<object>();

                foreach (var user in users)
                {
                    var roles = await _userService.GetUserRolesAsync(user.UserId);
                    var followStats = await _userService.GetFollowStatsAsync(user.UserId);

                    response.Add(new
                    {
                        UserId = user.UserId,
                        Username = user.Username,
                        Email = user.Email,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Biography = user.Biography,
                        Roles = roles,
                        IsActive = user.IsActive,
                        FollowersCount = followStats.followersCount,
                        FollowingCount = followStats.followingCount
                    });
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while searching users." });
            }
        }

        // 验证摄影师
        [Authorize(Roles = "Admin")]
        [HttpPost("photographers/{photographerId}/verify")]
        public async Task<IActionResult> VerifyPhotographer(int photographerId)
        {
            try
            {
                var success = await _userService.VerifyPhotographerAsync(photographerId);

                if (success)
                    return Ok(new { message = "Photographer verified successfully." });
                else
                    return BadRequest(new { message = "Failed to verify photographer." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while verifying photographer." });
            }
        }

        // 验证修图师
        [Authorize(Roles = "Admin")]
        [HttpPost("retouchers/{retoucherId}/verify")]
        public async Task<IActionResult> VerifyRetoucher(int retoucherId)
        {
            try
            {
                var success = await _userService.VerifyRetoucherAsync(retoucherId);

                if (success)
                    return Ok(new { message = "Retoucher verified successfully." });
                else
                    return BadRequest(new { message = "Failed to verify retoucher." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while verifying retoucher." });
            }
        }


        // 获取用户对应的修图师ID
        [HttpGet("{userId}/retoucher-id")]
        public async Task<IActionResult> GetRetoucherIdByUserId(int userId)
        {
            try
            {
                // 只允许当前用户或管理员查询
                if (User.Identity.IsAuthenticated)
                {
                    var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                    if (currentUserId != userId && !User.IsInRole("Admin"))
                        return StatusCode(403, new { message = "You are not authorized to access this information." });
                }

                var retoucherId = await _userService.GetRetoucherIdByUserIdAsync(userId);

                if (retoucherId.HasValue)
                    return Ok(new { retoucherId = retoucherId.Value });
                else
                    return NotFound(new { message = "User is not a retoucher." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving retoucher information." });
            }
        }

        // 获取用户对应的摄影师ID
        [HttpGet("{userId}/photographer-id")]
        public async Task<IActionResult> GetPhotographerIdByUserId(int userId)
        {
            try
            {
                // 只允许当前用户或管理员查询
                if (User.Identity.IsAuthenticated)
                {
                    var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                    if (currentUserId != userId && !User.IsInRole("Admin"))
                        return StatusCode(403, new { message = "You are not authorized to access this information." });
                }

                var photographerId = await _userService.GetPhotographerIdByUserIdAsync(userId);

                if (photographerId.HasValue)
                    return Ok(new { photographerId = photographerId.Value });
                else
                    return NotFound(new { message = "User is not a photographer." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving photographer information." });
            }
        }
    }

}