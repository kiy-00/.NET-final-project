using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PixelPerfect.Core.Models;
using PixelPerfect.Services;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PixelPerfect.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportController : ControllerBase
    {
        private readonly IReportService _reportService;
        private readonly IPostService _postService;

        public ReportController(
            IReportService reportService,
            IPostService postService)
        {
            _reportService = reportService;
            _postService = postService;
        }

        // 获取举报详情（仅限管理员和举报者本人）
        [HttpGet("{reportId}")]
        [Authorize]
        public async Task<IActionResult> GetReportById(int reportId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var report = await _reportService.GetReportByIdAsync(reportId);
                if (report == null)
                    return NotFound(new { message = $"Report with ID {reportId} not found." });

                // 检查权限 - 只有举报者本人和管理员可以查看
                if (report.UserId != userId && !User.IsInRole("Admin"))
                    return Forbid();

                return Ok(report);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving report information." });
            }
        }

        // 获取用户提交的举报（需登录）
        [HttpGet("user")]
        [Authorize]
        public async Task<IActionResult> GetUserReports()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var reports = await _reportService.GetReportsByUserIdAsync(userId);
                return Ok(reports);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving user reports." });
            }
        }

        // 获取帖子的举报（仅限管理员和帖子作者）
        [HttpGet("post/{postId}")]
        [Authorize]
        public async Task<IActionResult> GetReportsByPostId(int postId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // 检查权限 - 只有帖子作者和管理员可以查看
                if (!await _postService.IsPostOwnerAsync(postId, userId) && !User.IsInRole("Admin"))
                    return Forbid();

                var reports = await _reportService.GetReportsByPostIdAsync(postId);
                return Ok(reports);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving post reports." });
            }
        }

        // 搜索举报（仅限管理员）
        [HttpGet("search")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SearchReports([FromQuery] ReportSearchParams searchParams)
        {
            try
            {
                var reports = await _reportService.SearchReportsAsync(searchParams);
                return Ok(reports);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while searching reports." });
            }
        }

        // 创建举报（需登录）
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateReport([FromBody] ReportCreateRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var report = await _reportService.CreateReportAsync(userId, request);
                return CreatedAtAction(nameof(GetReportById), new { reportId = report.ReportId }, report);
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
                return StatusCode(500, new { message = "An error occurred while creating the report." });
            }
        }

        // 处理举报（仅限管理员）
        [HttpPut("{reportId}/handle")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> HandleReport(int reportId, [FromBody] ReportHandleRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var adminUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var report = await _reportService.HandleReportAsync(reportId, adminUserId, request.Status);
                return Ok(report);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while handling the report." });
            }
        }

        // 获取待处理的举报（仅限管理员）
        [HttpGet("pending")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPendingReports([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var reports = await _reportService.GetPendingReportsAsync(page, pageSize);
                return Ok(reports);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving pending reports." });
            }
        }

        // 获取待处理的举报数量（仅限管理员）
        [HttpGet("pending/count")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPendingReportsCount()
        {
            try
            {
                var count = await _reportService.GetPendingReportsCountAsync();
                return Ok(new { count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving pending reports count." });
            }
        }

        // 获取举报状态计数（仅限管理员）
        [HttpGet("status/counts")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetReportStatusCounts()
        {
            try
            {
                var counts = await _reportService.GetReportStatusCountsAsync();
                return Ok(counts);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving report status counts." });
            }
        }
    }
}