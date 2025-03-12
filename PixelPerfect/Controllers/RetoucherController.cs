using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PixelPerfect.Core.Models;
using PixelPerfect.Services;
using System.Security.Claims;

namespace PixelPerfect.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RetoucherController : ControllerBase
    {
        private readonly IRetoucherService _retoucherService;

        public RetoucherController(IRetoucherService retoucherService)
        {
            _retoucherService = retoucherService;
        }

        // 获取所有修图师
        [HttpGet]
        public async Task<IActionResult> GetAllRetouchers([FromQuery] bool verifiedOnly = false)
        {
            try
            {
                var retouchers = await _retoucherService.GetAllRetouchersAsync(verifiedOnly);
                return Ok(retouchers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving retouchers." });
            }
        }

        // 获取指定修图师信息
        [HttpGet("{retoucherId}")]
        public async Task<IActionResult> GetRetoucherById(int retoucherId)
        {
            try
            {
                var retoucher = await _retoucherService.GetRetoucherByIdAsync(retoucherId);
                if (retoucher == null)
                    return NotFound(new { message = $"Retoucher with ID {retoucherId} not found." });

                return Ok(retoucher);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving retoucher information." });
            }
        }

        // 创建修图师档案
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateRetoucherProfile([FromBody] RetoucherCreateRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var retoucher = await _retoucherService.CreateRetoucherProfileAsync(userId, request);
                return CreatedAtAction(nameof(GetRetoucherById), new { retoucherId = retoucher.RetoucherId }, retoucher);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while creating retoucher profile." });
            }
        }

        // 更新修图师档案
        [Authorize]
        [HttpPut("{retoucherId}")]
        public async Task<IActionResult> UpdateRetoucherProfile(int retoucherId, [FromBody] RetoucherUpdateRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // 检查是否是该修图师本人或管理员
                if (!await _retoucherService.IsOwnerAsync(retoucherId, userId) && !User.IsInRole("Admin"))
                    return Forbid();

                var success = await _retoucherService.UpdateRetoucherProfileAsync(retoucherId, request);
                if (success)
                    return Ok(new { message = "Retoucher profile updated successfully." });
                else
                    return BadRequest(new { message = "Failed to update retoucher profile." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating retoucher profile." });
            }
        }

        // 搜索修图师
        [HttpGet("search")]
        public async Task<IActionResult> SearchRetouchers([FromQuery] RetoucherSearchParams searchParams)
        {
            try
            {
                var retouchers = await _retoucherService.SearchRetouchersAsync(searchParams);
                return Ok(retouchers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while searching retouchers." });
            }
        }
    }
}