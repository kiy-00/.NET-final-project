using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PixelPerfect.Core.Models;
using PixelPerfect.Services;
using System.Security.Claims;

namespace PixelPerfect.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RetouchOrderController : ControllerBase
    {
        private readonly IRetouchOrderService _retouchOrderService;
        private readonly IRetoucherService _retoucherService;
        private readonly IPhotoService _photoService;

        public RetouchOrderController(
            IRetouchOrderService retouchOrderService,
            IRetoucherService retoucherService,
            IPhotoService photoService)
        {
            _retouchOrderService = retouchOrderService;
            _retoucherService = retoucherService;
            _photoService = photoService;
        }

        // 获取修图订单详情
        [HttpGet("{orderId}")]
        [Authorize]
        public async Task<IActionResult> GetOrderById(int orderId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var order = await _retouchOrderService.GetOrderByIdAsync(orderId);
                if (order == null)
                    return NotFound(new { message = $"Order with ID {orderId} not found." });

                // 检查权限 - 只有订单相关方和管理员可以查看
                bool isOrderUser = order.UserId == userId;
                bool isOrderRetoucher = false;

                // 如果用户是修图师，检查是否是该订单的修图师
                var retoucher = await _retoucherService.GetRetoucherByUserIdAsync(userId);
                if (retoucher != null)
                {
                    isOrderRetoucher = order.RetoucherId == retoucher.RetoucherId;
                }

                if (!isOrderUser && !isOrderRetoucher && !User.IsInRole("Admin"))
                    return Forbid();

                return Ok(order);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving order information." });
            }
        }

        // 用户获取自己的订单列表
        [HttpGet("user")]
        [Authorize]
        public async Task<IActionResult> GetUserOrders()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var orders = await _retouchOrderService.GetOrdersByUserIdAsync(userId);
                return Ok(orders);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving user orders." });
            }
        }

        // 修图师获取自己的订单列表
        [HttpGet("retoucher")]
        [Authorize]
        public async Task<IActionResult> GetRetoucherOrders()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // 获取当前用户的修图师信息
                var retoucher = await _retoucherService.GetRetoucherByUserIdAsync(userId);
                if (retoucher == null)
                    return BadRequest(new { message = "Current user is not a retoucher." });

                var orders = await _retouchOrderService.GetOrdersByRetoucherIdAsync(retoucher.RetoucherId);
                return Ok(orders);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving retoucher orders." });
            }
        }

        // 获取照片的修图订单
        [HttpGet("photo/{photoId}")]
        [Authorize]
        public async Task<IActionResult> GetOrdersByPhoto(int photoId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // 检查是否有权限访问照片
                if (!await _photoService.CanAccessPhotoAsync(photoId, userId) && !User.IsInRole("Admin"))
                    return Forbid();

                var orders = await _retouchOrderService.GetOrdersByPhotoIdAsync(photoId);
                return Ok(orders);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving photo orders." });
            }
        }

        // 创建修图订单
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateOrder([FromBody] RetouchOrderCreateRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var order = await _retouchOrderService.CreateOrderAsync(userId, request);
                return CreatedAtAction(nameof(GetOrderById), new { orderId = order.OrderId }, order);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while creating the order." });
            }
        }

        // 更新订单状态（用户可以取消，修图师可以接受/拒绝/完成）
        [HttpPut("{orderId}/status")]
        [Authorize]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, [FromBody] RetouchOrderStatusUpdateRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // 检查权限
                bool isUserOrder = await _retouchOrderService.IsUserOrderAsync(orderId, userId);

                // 验证是否是修图师的订单
                var retoucher = await _retoucherService.GetRetoucherByUserIdAsync(userId);
                bool isRetoucherOrder = retoucher != null &&
                                     await _retouchOrderService.IsRetoucherOrderAsync(orderId, retoucher.RetoucherId);

                // 用户和修图师对状态的控制不同
                bool isAdmin = User.IsInRole("Admin");

                // 检查特定状态的权限
                // - 用户可以取消(Cancel)订单
                // - 修图师可以接受(Accept)、拒绝(Reject)、设置进行中(InProgress)和完成(Completed)
                if (!isAdmin)
                {
                    if (isUserOrder && request.Status != "Cancelled")
                        return BadRequest(new { message = "User can only cancel an order." });

                    if (isRetoucherOrder &&
                        !new[] { "Accepted", "Rejected", "InProgress", "Completed" }.Contains(request.Status))
                        return BadRequest(new { message = "Retoucher can only accept, reject, set in-progress or complete an order." });

                    if (!isUserOrder && !isRetoucherOrder)
                        return Forbid();
                }

                var success = await _retouchOrderService.UpdateOrderStatusAsync(orderId, request.Status);
                if (success)
                    return Ok(new { message = $"Order status updated to {request.Status}." });
                else
                    return BadRequest(new { message = "Failed to update order status." });
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
                return StatusCode(500, new { message = "An error occurred while updating order status." });
            }
        }

        // 完成修图订单（仅修图师）
        [HttpPut("{orderId}/complete")]
        [Authorize]
        public async Task<IActionResult> CompleteOrder(int orderId, [FromForm] RetouchOrderCompleteRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // 验证是否是修图师的订单
                var retoucher = await _retoucherService.GetRetoucherByUserIdAsync(userId);
                if (retoucher == null && !User.IsInRole("Admin"))
                    return Forbid();

                bool isRetoucherOrder = retoucher != null &&
                                     await _retouchOrderService.IsRetoucherOrderAsync(orderId, retoucher.RetoucherId);

                if (!isRetoucherOrder && !User.IsInRole("Admin"))
                    return Forbid();

                // 验证上传的文件
                if (request.RetouchedPhoto == null || request.RetouchedPhoto.Length == 0)
                    return BadRequest(new { message = "No file uploaded or empty file." });

                // 检查文件类型
                var validImageTypes = new[] { "image/jpeg", "image/png" };
                if (!validImageTypes.Contains(request.RetouchedPhoto.ContentType.ToLower()))
                    return BadRequest(new { message = "Invalid file type. Only JPEG and PNG images are allowed." });

                // 上传文件并获取URL
                var uploadResult = await _photoService.UploadGeneralPhotoAsync(
                    userId,
                    request.RetouchedPhoto,
                    $"Retouched Photo - Order #{orderId}",
                    request.Comment ?? $"Retouched photo uploaded on {DateTime.UtcNow:yyyy-MM-dd}"
                );

                // 获取照片URL
                string retouchedPhotoUrl = uploadResult.Url;

                // 完成订单并保存URL
                var result = await _retouchOrderService.CompleteOrderWithPhotoAsync(orderId, request.RetouchedPhoto, request);

                if (result != null)
                    return Ok(new { message = "Order completed successfully.", order = result });
                else
                    return BadRequest(new { message = "Failed to complete order." });
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
                return StatusCode(500, new { message = $"An error occurred while completing the order: {ex.Message}" });
            }
        }

        // 搜索订单（管理员功能）
        [HttpGet("search")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SearchOrders([FromQuery] RetouchOrderSearchParams searchParams)
        {
            try
            {
                var orders = await _retouchOrderService.SearchOrdersAsync(searchParams);
                return Ok(orders);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while searching orders." });
            }
        }
    }
}