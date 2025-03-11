using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PixelPerfect.Core.Models;
using PixelPerfect.Services;
using System.Security.Claims;

namespace PixelPerfect.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BookingController : ControllerBase
    {
        private readonly IBookingService _bookingService;
        private readonly IPhotographerService _photographerService;

        public BookingController(IBookingService bookingService, IPhotographerService photographerService)
        {
            _bookingService = bookingService;
            _photographerService = photographerService;
        }

        // 获取预约信息
        [HttpGet("{bookingId}")]
        [Authorize]
        public async Task<IActionResult> GetBookingById(int bookingId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var booking = await _bookingService.GetBookingByIdAsync(bookingId);

                if (booking == null)
                    return NotFound(new { message = $"Booking with ID {bookingId} not found." });

                // 检查权限 - 只有预约相关方和管理员可以查看
                bool isOwner = booking.UserId == userId;
                bool isPhotographer = false;

                // 如果用户是摄影师，检查是否是该预约的摄影师
                var photographer = await _photographerService.GetPhotographerByUserIdAsync(userId);
                if (photographer != null)
                {
                    isPhotographer = booking.PhotographerId == photographer.PhotographerId;
                }

                if (!isOwner && !isPhotographer && !User.IsInRole("Admin"))
                    return Forbid();

                return Ok(booking);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving booking information." });
            }
        }

        // 用户获取自己的预约列表
        [HttpGet("user")]
        [Authorize]
        public async Task<IActionResult> GetUserBookings()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var bookings = await _bookingService.GetBookingsByUserIdAsync(userId);
                return Ok(bookings);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving user bookings." });
            }
        }

        // 摄影师获取自己的预约列表
        [HttpGet("photographer")]
        [Authorize]
        public async Task<IActionResult> GetPhotographerBookings()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // 获取当前用户的摄影师信息
                var photographer = await _photographerService.GetPhotographerByUserIdAsync(userId);
                if (photographer == null)
                    return BadRequest(new { message = "Current user is not a photographer." });

                var bookings = await _bookingService.GetBookingsByPhotographerIdAsync(photographer.PhotographerId);
                return Ok(bookings);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving photographer bookings." });
            }
        }

        // 创建新预约
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateBooking([FromBody] BookingCreateRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // 验证预约日期是否在未来
                if (request.BookingDate < DateTime.Now.Date)
                    return BadRequest(new { message = "Booking date must be in the future." });

                var booking = await _bookingService.CreateBookingAsync(userId, request);
                return CreatedAtAction(nameof(GetBookingById), new { bookingId = booking.BookingId }, booking);
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
                return StatusCode(500, new { message = "An error occurred while creating the booking." });
            }
        }

        // 更新预约状态（可由用户或摄影师操作）
        [HttpPut("{bookingId}/status")]
        [Authorize]
        public async Task<IActionResult> UpdateBookingStatus(int bookingId, [FromBody] BookingStatusUpdateRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // 检查权限
                bool isUserBooking = await _bookingService.IsUserBookingAsync(bookingId, userId);

                // 验证是否是摄影师的预约
                var photographer = await _photographerService.GetPhotographerByUserIdAsync(userId);
                bool isPhotographerBooking = photographer != null &&
                                          await _bookingService.IsPhotographerBookingAsync(bookingId, photographer.PhotographerId);

                // 用户和摄影师对状态的控制不同
                bool isAdmin = User.IsInRole("Admin");

                // 检查特定状态的权限
                // - 用户可以取消(Cancel)预约
                // - 摄影师可以确认(Confirm)、拒绝(Reject)、设置进行中(InProgress)和完成(Completed)
                // - 管理员可以设置任何状态
                if (!isAdmin)
                {
                    if (isUserBooking && request.Status != "Cancelled")
                        return BadRequest(new { message = "User can only cancel a booking." });

                    if (isPhotographerBooking &&
                        !new[] { "Confirmed", "Rejected", "InProgress", "Completed" }.Contains(request.Status))
                        return BadRequest(new { message = "Photographer can only confirm, reject, set in-progress or complete a booking." });

                    if (!isUserBooking && !isPhotographerBooking)
                        return Forbid();
                }

                var success = await _bookingService.UpdateBookingStatusAsync(bookingId, request.Status);
                if (success)
                    return Ok(new { message = $"Booking status updated to {request.Status}." });
                else
                    return BadRequest(new { message = "Failed to update booking status." });
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
                return StatusCode(500, new { message = "An error occurred while updating booking status." });
            }
        }

        // 更新预约最终金额（仅摄影师）
        [HttpPut("{bookingId}/amount")]
        [Authorize]
        public async Task<IActionResult> UpdateBookingAmount(int bookingId, [FromBody] BookingAmountUpdateRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // 验证是否是摄影师的预约
                var photographer = await _photographerService.GetPhotographerByUserIdAsync(userId);
                if (photographer == null && !User.IsInRole("Admin"))
                    return Forbid();

                bool isPhotographerBooking = photographer != null &&
                                           await _bookingService.IsPhotographerBookingAsync(bookingId, photographer.PhotographerId);

                if (!isPhotographerBooking && !User.IsInRole("Admin"))
                    return Forbid();

                var success = await _bookingService.UpdateBookingFinalAmountAsync(bookingId, request.FinalAmount);
                if (success)
                    return Ok(new { message = "Booking final amount updated." });
                else
                    return BadRequest(new { message = "Failed to update booking amount." });
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
                return StatusCode(500, new { message = "An error occurred while updating booking amount." });
            }
        }

        // 设置预约成片公开状态（用户确认）
        [HttpPut("{bookingId}/public")]
        [Authorize]
        public async Task<IActionResult> UpdateBookingPublicStatus(int bookingId, [FromBody] BookingDeliverRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // 只有预约的用户或管理员可以更新公开状态
                bool isUserBooking = await _bookingService.IsUserBookingAsync(bookingId, userId);

                if (!isUserBooking && !User.IsInRole("Admin"))
                    return Forbid();

                var success = await _bookingService.UpdateBookingPublicStatusAsync(bookingId, request.IsPublic);
                if (success)
                    return Ok(new { message = $"Booking public status updated to {(request.IsPublic ? "public" : "private")}." });
                else
                    return BadRequest(new { message = "Failed to update booking public status." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating booking public status." });
            }
        }

        // 搜索预约（管理员功能）
        [HttpGet("search")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SearchBookings([FromQuery] BookingSearchParams searchParams)
        {
            try
            {
                var bookings = await _bookingService.SearchBookingsAsync(searchParams);
                return Ok(bookings);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while searching bookings." });
            }
        }
    }
}