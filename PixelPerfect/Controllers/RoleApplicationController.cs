using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PixelPerfect.Core.Models;
using PixelPerfect.Services;
using System.Security.Claims;

namespace PixelPerfect.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RoleApplicationController : ControllerBase
    {
        private readonly IRoleApplicationService _roleApplicationService;

        public RoleApplicationController(IRoleApplicationService roleApplicationService)
        {
            _roleApplicationService = roleApplicationService;
        }

        // 获取当前用户的所有申请
        [Authorize]
        [HttpGet("my-applications")]
        public async Task<IActionResult> GetMyApplications()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var applications = await _roleApplicationService.GetUserApplicationsAsync(userId);
                return Ok(applications);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving applications.", error = ex.Message });
            }
        }

        // 创建角色申请
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateApplication([FromBody] CreateRoleApplicationRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // 验证请求
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var application = await _roleApplicationService.CreateApplicationAsync(userId, request);
                return Ok(application);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while creating application.", error = ex.Message });
            }
        }

        // 检查用户是否有待处理的申请
        [Authorize]
        [HttpGet("check-pending")]
        public async Task<IActionResult> CheckPendingApplication([FromQuery] string roleType)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var hasPending = await _roleApplicationService.HasPendingApplicationAsync(userId, roleType);
                return Ok(new { hasPendingApplication = hasPending });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while checking pending application.", error = ex.Message });
            }
        }

        // 以下是管理员功能

        // 获取所有申请（分页）
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetAllApplications([FromQuery] RoleApplicationQueryParams queryParams)
        {
            try
            {
                var applications = await _roleApplicationService.GetAllApplicationsAsync(
                    queryParams.Status,
                    queryParams.RoleType,
                    queryParams.PageNumber,
                    queryParams.PageSize);

                return Ok(applications);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving applications.", error = ex.Message });
            }
        }

        // 获取特定状态的申请（分页）
        [Authorize(Roles = "Admin")]
        [HttpGet("status/{status}")]
        public async Task<IActionResult> GetApplicationsByStatus(
            string status,
            [FromQuery] string? roleType,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var applications = await _roleApplicationService.GetApplicationsByStatusAsync(
                    status,
                    roleType,
                    pageNumber,
                    pageSize);

                return Ok(applications);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving applications.", error = ex.Message });
            }
        }

        // 获取申请详情
        [Authorize]
        [HttpGet("{applicationId}")]
        public async Task<IActionResult> GetApplicationById(int applicationId)
        {
            try
            {
                var application = await _roleApplicationService.GetApplicationByIdAsync(applicationId);
                if (application == null)
                    return NotFound(new { message = $"Application with ID {applicationId} not found." });

                // 非管理员只能查看自己的申请
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                if (application.UserId != userId && !User.IsInRole("Admin"))
                    return StatusCode(403, new { message = "You are not authorized to view this application." });

                var response = await _roleApplicationService.GetApplicationResponseAsync(application);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving application.", error = ex.Message });
            }
        }

        // 处理申请（管理员）
        [Authorize(Roles = "Admin")]
        [HttpPut("{applicationId}/process")]
        public async Task<IActionResult> ProcessApplication(
            int applicationId,
            [FromBody] ProcessRoleApplicationRequest request)
        {
            try
            {
                // 验证请求
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var application = await _roleApplicationService.ProcessApplicationAsync(applicationId, adminId, request);
                return Ok(application);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while processing application.", error = ex.Message });
            }
        }
    }
}