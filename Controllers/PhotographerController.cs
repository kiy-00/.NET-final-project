using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PixelPerfect.Models;
using PixelPerfect.Services;
using System.Security.Claims;

namespace PixelPerfect.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PhotographerController : ControllerBase
    {
        private readonly IPhotographerService _photographerService;

        public PhotographerController(IPhotographerService photographerService)
        {
            _photographerService = photographerService;
        }

        // 获取所有摄影师
        [HttpGet]
        public async Task<IActionResult> GetAllPhotographers([FromQuery] bool verifiedOnly = false)
        {
            try
            {
                var photographers = await _photographerService.GetAllPhotographersAsync(verifiedOnly);
                return Ok(photographers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving photographers." });
            }
        }

        // 获取指定摄影师信息
        [HttpGet("{photographerId}")]
        public async Task<IActionResult> GetPhotographerById(int photographerId)
        {
            try
            {
                var photographer = await _photographerService.GetPhotographerByIdAsync(photographerId);
                if (photographer == null)
                    return NotFound(new { message = $"Photographer with ID {photographerId} not found." });

                return Ok(photographer);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving photographer information." });
            }
        }

        // 创建摄影师档案
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreatePhotographerProfile([FromBody] PhotographerCreateRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var photographer = await _photographerService.CreatePhotographerProfileAsync(userId, request);
                return CreatedAtAction(nameof(GetPhotographerById), new { photographerId = photographer.PhotographerId }, photographer);
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
                return StatusCode(500, new { message = "An error occurred while creating photographer profile." });
            }
        }

        // 更新摄影师档案
        [Authorize]
        [HttpPut("{photographerId}")]
        public async Task<IActionResult> UpdatePhotographerProfile(int photographerId, [FromBody] PhotographerUpdateRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // 检查是否是该摄影师本人或管理员
                if (!await _photographerService.IsOwnerAsync(photographerId, userId) && !User.IsInRole("Admin"))
                    return Forbid();

                var success = await _photographerService.UpdatePhotographerProfileAsync(photographerId, request);
                if (success)
                    return Ok(new { message = "Photographer profile updated successfully." });
                else
                    return BadRequest(new { message = "Failed to update photographer profile." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating photographer profile." });
            }
        }

        // 搜索摄影师
        [HttpGet("search")]
        public async Task<IActionResult> SearchPhotographers([FromQuery] PhotographerSearchParams searchParams)
        {
            try
            {
                var photographers = await _photographerService.SearchPhotographersAsync(searchParams);
                return Ok(photographers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while searching photographers." });
            }
        }
    }
}