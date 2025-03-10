// Controllers/RetoucherPortfolioController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PixelPerfect.Models;
using PixelPerfect.Services;
using System.Security.Claims;

namespace PixelPerfect.Controllers
{
    [ApiController]
    [Route("api/retoucher-portfolios")]
    public class RetoucherPortfolioController : ControllerBase
    {
        private readonly IPortfolioService _portfolioService;
        private readonly IRetoucherService _retoucherService;

        public RetoucherPortfolioController(IPortfolioService portfolioService, IRetoucherService retoucherService)
        {
            _portfolioService = portfolioService;
            _retoucherService = retoucherService;
        }

        // 获取所有公开作品集
        [HttpGet]
        public async Task<IActionResult> GetAllPublicPortfolios([FromQuery] PortfolioSearchParams searchParams)
        {
            try
            {
                var portfolios = await _portfolioService.GetAllRetoucherPortfoliosAsync(searchParams);
                return Ok(portfolios);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving portfolios." });
            }
        }

        // 获取指定修图师的所有公开作品集
        [HttpGet("retoucher/{retoucherId}")]
        public async Task<IActionResult> GetRetoucherPortfolios(int retoucherId, [FromQuery] PortfolioSearchParams searchParams)
        {
            try
            {
                var portfolios = await _portfolioService.GetRetoucherPortfoliosByRetoucherIdAsync(retoucherId, searchParams);
                return Ok(portfolios);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving retoucher portfolios." });
            }
        }

        // 获取指定作品集
        [HttpGet("{portfolioId}")]
        public async Task<IActionResult> GetPortfolio(int portfolioId)
        {
            try
            {
                var portfolio = await _portfolioService.GetRetoucherPortfolioByIdAsync(portfolioId);
                if (portfolio == null)
                    return NotFound(new { message = $"Portfolio with ID {portfolioId} not found." });

                // 检查作品集是否公开，如果不是则检查用户权限
                if (!portfolio.IsPublic)
                {
                    if (!User.Identity.IsAuthenticated)
                        return Forbid();

                    var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                    var retoucher = await _retoucherService.GetRetoucherByIdAsync(portfolio.RetoucherId);

                    if (retoucher == null || (retoucher.UserId != userId && !User.IsInRole("Admin")))
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
        public async Task<IActionResult> CreatePortfolio([FromBody] CreateRetoucherPortfolioRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // 获取当前用户的修图师信息
                var retoucher = await _retoucherService.GetRetoucherByUserIdAsync(userId);
                if (retoucher == null)
                    return BadRequest(new { message = "Only retouchers can create portfolios." });

                var portfolio = await _portfolioService.CreateRetoucherPortfolioAsync(retoucher.RetoucherId, request);
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
                var portfolio = await _portfolioService.GetRetoucherPortfolioByIdAsync(portfolioId);
                if (portfolio == null)
                    return NotFound(new { message = $"Portfolio with ID {portfolioId} not found." });

                var retoucher = await _retoucherService.GetRetoucherByIdAsync(portfolio.RetoucherId);
                if (retoucher == null || (retoucher.UserId != userId && !User.IsInRole("Admin")))
                    return Forbid();

                var success = await _portfolioService.UpdateRetoucherPortfolioAsync(portfolioId, request);
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
                var portfolio = await _portfolioService.GetRetoucherPortfolioByIdAsync(portfolioId);
                if (portfolio == null)
                    return NotFound(new { message = $"Portfolio with ID {portfolioId} not found." });

                var retoucher = await _retoucherService.GetRetoucherByIdAsync(portfolio.RetoucherId);
                if (retoucher == null || (retoucher.UserId != userId && !User.IsInRole("Admin")))
                    return Forbid();

                var success = await _portfolioService.DeleteRetoucherPortfolioAsync(portfolioId);
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
                var portfolio = await _portfolioService.GetRetoucherPortfolioByIdAsync(portfolioId);
                if (portfolio == null)
                    return NotFound(new { message = $"Portfolio with ID {portfolioId} not found." });

                var retoucher = await _retoucherService.GetRetoucherByIdAsync(portfolio.RetoucherId);
                if (retoucher == null || retoucher.UserId != userId)
                    return Forbid();

                // 检查是否有文件上传
                if (Request.Form.Files.Count == 0)
                    return BadRequest(new { message = "No files uploaded." });

                var file = Request.Form.Files[0];
                var item = await _portfolioService.AddItemToRetoucherPortfolioAsync(portfolioId, file, request);

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

                var portfolio = await _portfolioService.GetRetoucherPortfolioByIdAsync(item.PortfolioId);
                if (portfolio == null)
                    return NotFound(new { message = $"Portfolio not found." });

                var retoucher = await _retoucherService.GetRetoucherByIdAsync(portfolio.RetoucherId);
                if (retoucher == null || retoucher.UserId != userId)
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

                var portfolio = await _portfolioService.GetRetoucherPortfolioByIdAsync(item.PortfolioId);
                if (portfolio == null)
                    return NotFound(new { message = $"Portfolio not found." });

                var retoucher = await _retoucherService.GetRetoucherByIdAsync(portfolio.RetoucherId);
                if (retoucher == null || retoucher.UserId != userId)
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