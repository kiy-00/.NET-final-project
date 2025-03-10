// Controllers/PhotographerPortfolioController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PixelPerfect.Models;
using PixelPerfect.Services;
using System.Security.Claims;

namespace PixelPerfect.Controllers
{
    [ApiController]
    [Route("api/photographer-portfolios")]
    public class PhotographerPortfolioController : ControllerBase
    {
        private readonly IPortfolioService _portfolioService;
        private readonly IPhotographerService _photographerService;

        public PhotographerPortfolioController(IPortfolioService portfolioService, IPhotographerService photographerService)
        {
            _portfolioService = portfolioService;
            _photographerService = photographerService;
        }

        // 获取所有公开作品集
        [HttpGet]
        public async Task<IActionResult> GetAllPublicPortfolios([FromQuery] PortfolioSearchParams searchParams)
        {
            try
            {
                var portfolios = await _portfolioService.GetAllPhotographerPortfoliosAsync(searchParams);
                return Ok(portfolios);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving portfolios." });
            }
        }

        // 获取指定摄影师的所有公开作品集
        [HttpGet("photographer/{photographerId}")]
        public async Task<IActionResult> GetPhotographerPortfolios(int photographerId, [FromQuery] PortfolioSearchParams searchParams)
        {
            try
            {
                var portfolios = await _portfolioService.GetPhotographerPortfoliosByPhotographerIdAsync(photographerId, searchParams);
                return Ok(portfolios);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving photographer portfolios." });
            }
        }

        // 获取指定作品集
        [HttpGet("{portfolioId}")]
        public async Task<IActionResult> GetPortfolio(int portfolioId)
        {
            try
            {
                var portfolio = await _portfolioService.GetPhotographerPortfolioByIdAsync(portfolioId);
                if (portfolio == null)
                    return NotFound(new { message = $"Portfolio with ID {portfolioId} not found." });

                // 检查作品集是否公开，如果不是则检查用户权限
                if (!portfolio.IsPublic)
                {
                    if (!User.Identity.IsAuthenticated)
                        return Forbid();

                    var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                    var photographer = await _photographerService.GetPhotographerByIdAsync(portfolio.PhotographerId);

                    if (photographer == null || (photographer.UserId != userId && !User.IsInRole("Admin")))
                        return Forbid();
                }

                return Ok(portfolio);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving portfolio." });
            }
        }

        // 创建作品集
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreatePortfolio([FromBody] CreatePhotographerPortfolioRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // 获取当前用户的摄影师信息
                var photographer = await _photographerService.GetPhotographerByUserIdAsync(userId);
                if (photographer == null)
                    return BadRequest(new { message = "Only photographers can create portfolios." });

                var portfolio = await _portfolioService.CreatePhotographerPortfolioAsync(photographer.PhotographerId, request);
                return CreatedAtAction(nameof(GetPortfolio), new { portfolioId = portfolio.PortfolioId }, portfolio);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while creating portfolio." });
            }
        }

        // 更新作品集
        [Authorize]
        [HttpPut("{portfolioId}")]
        public async Task<IActionResult> UpdatePortfolio(int portfolioId, [FromBody] UpdatePortfolioRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // 检查是否是作品集所有者或管理员
                var portfolio = await _portfolioService.GetPhotographerPortfolioByIdAsync(portfolioId);
                if (portfolio == null)
                    return NotFound(new { message = $"Portfolio with ID {portfolioId} not found." });

                var photographer = await _photographerService.GetPhotographerByIdAsync(portfolio.PhotographerId);
                if (photographer == null || (photographer.UserId != userId && !User.IsInRole("Admin")))
                    return Forbid();

                var success = await _portfolioService.UpdatePhotographerPortfolioAsync(portfolioId, request);
                if (success)
                    return Ok(new { message = "Portfolio updated successfully." });
                else
                    return BadRequest(new { message = "Failed to update portfolio." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating portfolio." });
            }
        }

        // 删除作品集
        [Authorize]
        [HttpDelete("{portfolioId}")]
        public async Task<IActionResult> DeletePortfolio(int portfolioId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // 检查是否是作品集所有者或管理员
                var portfolio = await _portfolioService.GetPhotographerPortfolioByIdAsync(portfolioId);
                if (portfolio == null)
                    return NotFound(new { message = $"Portfolio with ID {portfolioId} not found." });

                var photographer = await _photographerService.GetPhotographerByIdAsync(portfolio.PhotographerId);
                if (photographer == null || (photographer.UserId != userId && !User.IsInRole("Admin")))
                    return Forbid();

                var success = await _portfolioService.DeletePhotographerPortfolioAsync(portfolioId);
                if (success)
                    return Ok(new { message = "Portfolio deleted successfully." });
                else
                    return BadRequest(new { message = "Failed to delete portfolio." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting portfolio." });
            }
        }

        // 上传作品项（照片）
        [Authorize]
        [HttpPost("{portfolioId}/items")]
        public async Task<IActionResult> UploadPortfolioItem(int portfolioId, [FromForm] UploadPortfolioItemRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // 检查是否是作品集所有者
                var portfolio = await _portfolioService.GetPhotographerPortfolioByIdAsync(portfolioId);
                if (portfolio == null)
                    return NotFound(new { message = $"Portfolio with ID {portfolioId} not found." });

                var photographer = await _photographerService.GetPhotographerByIdAsync(portfolio.PhotographerId);
                if (photographer == null || photographer.UserId != userId)
                    return Forbid();

                // 检查是否有文件上传
                if (Request.Form.Files.Count == 0)
                    return BadRequest(new { message = "No files uploaded." });

                var file = Request.Form.Files[0];
                var item = await _portfolioService.AddItemToPhotographerPortfolioAsync(portfolioId, file, request);

                return Ok(item);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while uploading portfolio item." });
            }
        }

        // 更新作品项
        [Authorize]
        [HttpPut("items/{itemId}")]
        public async Task<IActionResult> UpdatePortfolioItem(int itemId, [FromBody] UpdatePortfolioItemRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // 检查是否是作品项所有者
                var item = await _portfolioService.GetPortfolioItemByIdAsync(itemId);
                if (item == null)
                    return NotFound(new { message = $"Portfolio item with ID {itemId} not found." });

                var portfolio = await _portfolioService.GetPhotographerPortfolioByIdAsync(item.PortfolioId);
                if (portfolio == null)
                    return NotFound(new { message = $"Portfolio not found." });

                var photographer = await _photographerService.GetPhotographerByIdAsync(portfolio.PhotographerId);
                if (photographer == null || photographer.UserId != userId)
                    return Forbid();

                var success = await _portfolioService.UpdatePortfolioItemAsync(itemId, request);
                if (success)
                    return Ok(new { message = "Portfolio item updated successfully." });
                else
                    return BadRequest(new { message = "Failed to update portfolio item." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating portfolio item." });
            }
        }

        // 删除作品项
        [Authorize]
        [HttpDelete("items/{itemId}")]
        public async Task<IActionResult> DeletePortfolioItem(int itemId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // 检查是否是作品项所有者
                var item = await _portfolioService.GetPortfolioItemByIdAsync(itemId);
                if (item == null)
                    return NotFound(new { message = $"Portfolio item with ID {itemId} not found." });

                var portfolio = await _portfolioService.GetPhotographerPortfolioByIdAsync(item.PortfolioId);
                if (portfolio == null)
                    return NotFound(new { message = $"Portfolio not found." });

                var photographer = await _photographerService.GetPhotographerByIdAsync(portfolio.PhotographerId);
                if (photographer == null || photographer.UserId != userId)
                    return Forbid();

                var success = await _portfolioService.DeletePortfolioItemAsync(itemId);
                if (success)
                    return Ok(new { message = "Portfolio item deleted successfully." });
                else
                    return BadRequest(new { message = "Failed to delete portfolio item." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting portfolio item." });
            }
        }
    }
}