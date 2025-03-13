// Controllers/RetoucherPortfolioController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PixelPerfect.Core.Models;
using PixelPerfect.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace PixelPerfect.Controllers
{
    [ApiController]
    [Route("api/retoucher-portfolios")]
    public class RetoucherPortfolioController : ControllerBase
    {
        private readonly IPortfolioService _portfolioService;
        private readonly IRetoucherService _retoucherService;
        private readonly IFileStorageService _fileStorage;

        public RetoucherPortfolioController(
            IPortfolioService portfolioService,
            IRetoucherService retoucherService,
            IFileStorageService fileStorage)
        {
            _portfolioService = portfolioService;
            _retoucherService = retoucherService;
            _fileStorage = fileStorage;
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

        // 获取所有公开作品集（无需关键词和分类）
        [HttpGet("public")]
        public async Task<IActionResult> GetAllPublicPortfoliosSimple()
        {
            try
            {
                // 创建默认搜索参数，仅设置IsPublic为true
                var searchParams = new PortfolioSearchParams
                {
                    IsPublic = true
                };

                var portfolios = await _portfolioService.GetAllRetoucherPortfoliosAsync(searchParams);
                return Ok(portfolios);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving public portfolios." });
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

        // 获取指定修图师的所有公开作品集（无需关键词和分类）
        [HttpGet("retoucher/{retoucherId}/public")]
        public async Task<IActionResult> GetRetoucherPublicPortfoliosSimple(int retoucherId)
        {
            try
            {
                // 创建默认搜索参数，仅设置IsPublic为true
                var searchParams = new PortfolioSearchParams
                {
                    IsPublic = true
                };

                var portfolios = await _portfolioService.GetRetoucherPortfoliosByRetoucherIdAsync(retoucherId, searchParams);
                return Ok(portfolios);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving retoucher public portfolios." });
            }
        }

        // 获取指定修图师的所有未公开作品集（仅限作品集所有者访问）
        [Authorize]
        [HttpGet("retoucher/{retoucherId}/private")]
        public async Task<IActionResult> GetRetoucherPrivatePortfolios(int retoucherId, [FromQuery] PortfolioSearchParams searchParams)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // 验证用户是否是作品集所有者
                var retoucher = await _retoucherService.GetRetoucherByIdAsync(retoucherId);
                if (retoucher == null)
                    return NotFound(new { message = $"Retoucher with ID {retoucherId} not found." });

                // 只有作品集所有者可以查看，管理员也无法查看
                if (retoucher.UserId != userId)
                    return Forbid();

                // 创建一个专门查询非公开作品集的搜索参数
                var privateSearchParams = new PortfolioSearchParams
                {
                    Keyword = searchParams.Keyword,
                    Category = searchParams.Category,
                    IsPublic = false // 确保只返回未公开的作品集
                };

                var portfolios = await _portfolioService.GetRetoucherPortfoliosByRetoucherIdAsync(retoucherId, privateSearchParams);
                return Ok(portfolios);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving private retoucher portfolios." });
            }
        }

        // 获取指定修图师的所有未公开作品集（无需关键词和分类，仅限作品集所有者访问）
        [Authorize]
        [HttpGet("retoucher/{retoucherId}/private-simple")]
        public async Task<IActionResult> GetRetoucherPrivatePortfoliosSimple(int retoucherId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // 验证用户是否是作品集所有者
                var retoucher = await _retoucherService.GetRetoucherByIdAsync(retoucherId);
                if (retoucher == null)
                    return NotFound(new { message = $"Retoucher with ID {retoucherId} not found." });

                // 只有作品集所有者可以查看，管理员也无法查看
                if (retoucher.UserId != userId)
                    return Forbid();

                // 创建一个专门查询非公开作品集的搜索参数
                var privateSearchParams = new PortfolioSearchParams
                {
                    IsPublic = false // 确保只返回未公开的作品集
                };

                var portfolios = await _portfolioService.GetRetoucherPortfoliosByRetoucherIdAsync(retoucherId, privateSearchParams);
                return Ok(portfolios);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving private retoucher portfolios." });
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

        // 上传作品集封面
        [Authorize]
        [HttpPost("{portfolioId}/cover")]
        public async Task<IActionResult> UploadPortfolioCover(int portfolioId, IFormFile file)
        {
            if (file == null)
                return BadRequest(new { message = "No file uploaded." });

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

                // 上传封面图片
                var coverItem = await _portfolioService.SetRetoucherPortfolioCoverAsync(portfolioId, file);

                return Ok(new
                {
                    message = "Portfolio cover uploaded successfully.",
                    coverUrl = coverItem.ImageUrl,
                    thumbnailUrl = coverItem.ThumbnailUrl,
                    itemId = coverItem.ItemId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while uploading portfolio cover." });
            }
        }

        // 上传作品项（修图前后对比）
        [Authorize]
        [HttpPost("{portfolioId}/before-after")]
        public async Task<IActionResult> UploadBeforeAfterItem(int portfolioId, [FromForm] UploadRetoucherPortfolioItemRequest request)
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

                // 检查必要的文件
                var afterImage = Request.Form.Files["afterImage"];
                if (afterImage == null)
                    return BadRequest(new { message = "After image is required." });

                // 可选的修图前图片
                var beforeImage = Request.Form.Files.Count > 1 ? Request.Form.Files["beforeImage"] : null;

                // 上传前后对比图片
                var item = await _portfolioService.AddRetoucherPortfolioItemWithBeforeAfterAsync(
                    portfolioId,
                    afterImage,
                    beforeImage,
                    request
                );

                return Ok(new
                {
                    message = "Portfolio before-after item uploaded successfully.",
                    portfolioItemId = item.ItemId,
                    afterImageUrl = item.ImageUrl,
                    afterThumbnailUrl = item.ThumbnailUrl,
                    beforeImageUrl = item.BeforeImageUrl,
                    beforeThumbnailUrl = item.BeforeThumbnailUrl,
                    title = item.Title,
                    description = item.Description
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while uploading before-after portfolio item." });
            }
        }

        // 批量上传作品项（照片）
        [Authorize]
        [HttpPost("{portfolioId}/items/batch")]
        public async Task<IActionResult> BatchUploadPortfolioItems(int portfolioId, [FromForm] BatchPortfolioItemUploadRequest request)
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

                var uploadedItems = await _portfolioService.BatchAddRetoucherPortfolioItemsAsync(
                    portfolioId,
                    Request.Form.Files,
                    request
                );

                return Ok(new
                {
                    message = $"Successfully uploaded {uploadedItems.Count} items.",
                    items = uploadedItems.Select(item => new
                    {
                        itemId = item.ItemId,
                        imageUrl = item.ImageUrl,
                        thumbnailUrl = item.ThumbnailUrl,
                        title = item.Title
                    })
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while batch uploading portfolio items." });
            }
        }

        // 获取作品项前后对比信息
        [HttpGet("items/{itemId}/before-after")]
        public async Task<IActionResult> GetBeforeAfterItem(int itemId)
        {
            try
            {
                // 先获取作品项确认它是修图师类型
                var item = await _portfolioService.GetPortfolioItemByIdAsync(itemId);
                if (item == null)
                    return NotFound(new { message = $"Portfolio item with ID {itemId} not found." });

                if (item.PortfolioType != "Retoucher")
                    return BadRequest(new { message = "This item does not belong to a retoucher portfolio." });

                var result = await _portfolioService.GetBeforeAfterImagesAsync(itemId);

                return Ok(new
                {
                    beforeImage = result.Before != null ? new
                    {
                        itemId = result.Before.ItemId,
                        imageUrl = result.Before.ImageUrl,
                        thumbnailUrl = result.Before.ThumbnailUrl,
                        title = result.Before.Title,
                        description = result.Before.Description
                    } : null,
                    afterImage = result.After != null ? new
                    {
                        itemId = result.After.ItemId,
                        imageUrl = result.After.ImageUrl,
                        thumbnailUrl = result.After.ThumbnailUrl,
                        title = result.After.Title,
                        description = result.After.Description
                    } : null
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving before-after images." });
            }
        }

        // 上传作品项（照片）- 保留原有方法，但标记为已过时
        [Obsolete("Use /before-after endpoint for uploading before-after images or /items/batch for batch uploads")]
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

                // 检查作品项类型是否正确
                if (item.PortfolioType != "Retoucher")
                    return BadRequest(new { message = "This item does not belong to a retoucher portfolio." });

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

                // 检查作品项类型是否正确
                if (item.PortfolioType != "Retoucher")
                    return BadRequest(new { message = "This item does not belong to a retoucher portfolio." });

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