// Controllers/UserController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using PixelPerfect.Entities;
using PixelPerfect.Services;
using System.Security.Claims;
using PixelPerfect.Models;
using RegisterRequest = PixelPerfect.Models.RegisterRequest;
using LoginRequest = PixelPerfect.Models.LoginRequest; // 添加模型引用

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
                    UserType = "Regular",
                    IsActive = true
                };

                var createdUser = await _userService.CreateUserAsync(user, request.Password);

                return Ok(new
                {
                    UserId = createdUser.UserId,
                    Username = createdUser.Username,
                    Email = createdUser.Email,
                    UserType = createdUser.UserType
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

                Console.WriteLine($"登录成功: UserId = {user.UserId}, Username = {user.Username}");
                return Ok(new
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    Email = user.Email,
                    UserType = user.UserType,
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

                var response = new
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    PhoneNumber = user.PhoneNumber,
                    UserType = user.UserType,
                    CreatedAt = user.CreatedAt,
                    LastLogin = user.LastLogin
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

                // 只有管理员可以修改用户类型和活跃状态
                if (User.IsInRole("Admin"))
                {
                    existingUser.UserType = request.UserType ?? existingUser.UserType;
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

        // 以下是管理员功能

        // 获取所有用户
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetAllUsers([FromQuery] string? userType)
        {
            try
            {
                var users = await _userService.GetAllUsersAsync(userType);

                var response = users.Select(u => new
                {
                    UserId = u.UserId,
                    Username = u.Username,
                    Email = u.Email,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    UserType = u.UserType,
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt,
                    LastLogin = u.LastLogin
                }).ToList();

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving users." });
            }
        }

        // 获取指定用户信息
        [Authorize(Roles = "Admin")]
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetUserById(int userId)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(userId);

                var response = new
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    PhoneNumber = user.PhoneNumber,
                    UserType = user.UserType,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    LastLogin = user.LastLogin
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

                var response = users.Select(u => new
                {
                    UserId = u.UserId,
                    Username = u.Username,
                    Email = u.Email,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    UserType = u.UserType,
                    IsActive = u.IsActive
                }).ToList();

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
    }
}